using UnityEngine;
using UnityEngine.Rendering;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Compute Shaders")]
    public ComputeShader marchingCubesShader;
    public ComputeShader densityMapShader;
    
    [Header("Rendering")]
    public Material terrainMaterial;
    
    [Header("Generation Parameters")]
    public int gridSize = 48;
    public float voxelSize = 0.2f;
    public float planetRadius = 0.8f;
    
    [Header("Noise Parameters")]
    public float noiseScale = 1.0f;
    public float noiseAmplitude = 0.0f;
    
    [Header("Deprecated Parameters")]
    [Tooltip("Controls the space around the sphere. Increase if seeing clipping.")]
    [Range(0.0f, 20.0f)]
    public float paddingFactor = 2.0f;
    
    // Compute buffers
    private ComputeBuffer _voxelBuffer;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _triangleBuffer;
    private ComputeBuffer _vertexCounterBuffer;
    private ComputeBuffer _triangleCounterBuffer;
    
    // Mesh components
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    
    void Start()
    {
        InitializeMeshComponents();
        InitializeBuffers();
        GenerateTerrain();
    }
    
    void OnDestroy()
    {
        ReleaseBuffers();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            Debug.Log($"Parameters changed - Padding: {paddingFactor}");
            GenerateTerrain();
        }
    }
    
    private void InitializeMeshComponents()
    {
        _meshFilter = gameObject.GetComponent<MeshFilter>();
        if (_meshFilter == null)
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            
        _meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (_meshRenderer == null)
            _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
        _meshRenderer.material = terrainMaterial;
    }
    
    private void InitializeBuffers()
    {
        int numVoxels = gridSize * gridSize * gridSize;
        int maxVertices = numVoxels * 5;
        int maxTriangles = maxVertices * 3;
        
        _voxelBuffer = new ComputeBuffer(numVoxels, sizeof(float));
        _vertexBuffer = new ComputeBuffer(maxVertices, sizeof(float) * 3);
        _triangleBuffer = new ComputeBuffer(maxTriangles, sizeof(int));
        _vertexCounterBuffer = new ComputeBuffer(1, sizeof(int));
        _triangleCounterBuffer = new ComputeBuffer(1, sizeof(int));
    }
    
    private void ReleaseBuffers()
    {
        if (_voxelBuffer != null) _voxelBuffer.Release();
        if (_vertexBuffer != null) _vertexBuffer.Release();
        if (_triangleBuffer != null) _triangleBuffer.Release();
        if (_vertexCounterBuffer != null) _vertexCounterBuffer.Release();
        if (_triangleCounterBuffer != null) _triangleCounterBuffer.Release();
    }
    
    private void GenerateTerrain()
    {
        GenerateDensity();
        DebugDensityValues();
        RunMarchingCubes();
        DebugMarchingCubes();
        CreateMesh();
    }
    
    private void GenerateDensity()
    {
        int kernelIndex = densityMapShader.FindKernel("CSMain");
        
        Debug.Log($"Setting padding factor: {paddingFactor}");
        
        densityMapShader.SetInt("gridSize", gridSize);
        densityMapShader.SetFloat("noiseScale", noiseScale);
        densityMapShader.SetFloat("noiseAmplitude", noiseAmplitude);
        densityMapShader.SetFloat("planetRadius", planetRadius);
        densityMapShader.SetFloat("paddingFactor", paddingFactor);
        
        densityMapShader.SetBuffer(kernelIndex, "densityMap", _voxelBuffer);
        
        int threadGroups = Mathf.CeilToInt(gridSize / 8.0f);
        densityMapShader.Dispatch(kernelIndex, threadGroups, threadGroups, threadGroups);
        Debug.Log($"Dispatched density shader with {threadGroups} thread groups");
    }
    
    private void DebugDensityValues()
    {
        float[] densityValues = new float[gridSize * gridSize * gridSize];
        _voxelBuffer.GetData(densityValues);
        
        float minDensity = float.MaxValue;
        float maxDensity = float.MinValue;
        
        for(int i = 0; i < densityValues.Length; i++)
        {
            minDensity = Mathf.Min(minDensity, densityValues[i]);
            maxDensity = Mathf.Max(maxDensity, densityValues[i]);
        }
        
        Debug.Log($"Density range: {minDensity} to {maxDensity}");
    }
    
    private void RunMarchingCubes()
    {
        int kernelIndex = marchingCubesShader.FindKernel("CSMain");
        
        Debug.Log($"Using padding factor in marching cubes: {paddingFactor}");
        
        _vertexCounterBuffer.SetData(new int[] { 0 });
        _triangleCounterBuffer.SetData(new int[] { 0 });
        
        marchingCubesShader.SetInt("gridSize", gridSize);
        marchingCubesShader.SetFloat("voxelSize", voxelSize);
        marchingCubesShader.SetFloat("isoLevel", 0.0f);
        marchingCubesShader.SetFloat("paddingFactor", paddingFactor);
        
        marchingCubesShader.SetBuffer(kernelIndex, "densityMap", _voxelBuffer);
        marchingCubesShader.SetBuffer(kernelIndex, "vertices", _vertexBuffer);
        marchingCubesShader.SetBuffer(kernelIndex, "triangles", _triangleBuffer);
        marchingCubesShader.SetBuffer(kernelIndex, "vertexCounter", _vertexCounterBuffer);
        marchingCubesShader.SetBuffer(kernelIndex, "triangleCounter", _triangleCounterBuffer);
        
        int threadGroups = Mathf.CeilToInt(gridSize / 8.0f);
        marchingCubesShader.Dispatch(kernelIndex, threadGroups, threadGroups, threadGroups);
    }
    
    private void DebugMarchingCubes()
    {
        int[] vertexCount = new int[1];
        int[] triangleCount = new int[1];
        _vertexCounterBuffer.GetData(vertexCount);
        _triangleCounterBuffer.GetData(triangleCount);
        
        Debug.Log($"Generated vertices: {vertexCount[0]}");
        Debug.Log($"Generated triangles: {triangleCount[0]}");
    }
    
    private void CreateMesh()
    {
        int[] vertexCount = new int[1];
        int[] triangleCount = new int[1];
        _vertexCounterBuffer.GetData(vertexCount);
        _triangleCounterBuffer.GetData(triangleCount);
        
        Debug.Log($"Creating mesh with {vertexCount[0]} vertices and {triangleCount[0]} triangles");
        
        if (vertexCount[0] == 0 || triangleCount[0] == 0)
        {
            Debug.LogWarning("No mesh data generated!");
            return;
        }
        
        Vector3[] vertices = new Vector3[vertexCount[0]];
        int[] triangles = new int[triangleCount[0]];
        
        _vertexBuffer.GetData(vertices);
        _triangleBuffer.GetData(triangles);
        
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        _meshFilter.mesh = mesh;
        
        Debug.Log($"Mesh created with bounds: {mesh.bounds}");
    }
}