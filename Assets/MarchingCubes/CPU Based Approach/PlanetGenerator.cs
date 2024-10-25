using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes.Scripts
{
    public class PlanetGenerator : MonoBehaviour
    {
        [Header("Planet Parameters")]
        public float planetRadius = 400f;
        
        [Header("Grid Settings")]
        [Tooltip("Number of voxels along each axis. Higher values create more detailed terrain but increase generation time.")]
        public int gridSize = 192;
        
        [Header("Advanced Settings")]
        public bool useAutoVoxelSize = true;
        [SerializeField] private float manualVoxelSize = 16.0f;

        [Header("Noise Settings")]
        [Tooltip("Seed value for the noise.")]
        [Range(0.0001f, 0.01f)]
        public int noiseSeed;
        
        [Tooltip("Controls the frequency of terrain features. Larger values create smaller, more numerous features.")]
        [Range(0.0001f, 0.01f)]
        public float noiseScale = 0.001f;
    
        [Tooltip("Controls the height of terrain features.")]
        [Range(10f, 500f)]
        public float noiseAmplitude = 100f;
    
        [Tooltip("Controls how many layers of noise are combined.")]
        [Range(1, 8)]
        public int octaves = 4;
    
        [Tooltip("Controls how much each octave contributes.")]
        [Range(0f, 1f)]
        public float persistence = 0.5f;
    
        [Tooltip("Controls the sharpness of terrain features.")]
        [Range(1f, 2.5f)]
        public float terrainSharpness = 2f;
        
        public Material planetMaterial;

        // Calculated internally
        private float VoxelSize 
        {
            get 
            {
                if (useAutoVoxelSize)
                {
                    return (planetRadius * 2.5f) / gridSize;
                }
                return manualVoxelSize;
            }
        }
        private Vector3 gridOrigin;
        private float gridWorldSize;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private struct Voxel
        {
            public Vector3[] CornerPositions;
            public float[] CornerValues;
        }

        private List<Voxel> _voxels;

        void Start()
        {
            if (EnsureComponents())
            {
                CalculateGridParameters();
                GeneratePlanet();
            }
            else
            {
                Debug.LogError("Failed to initialize required components on " + gameObject.name);
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
            {
                RegenerateNow();
            }
        }

        public void RegenerateNow()
        {
            if (EnsureComponents())
            {
                Debug.Log($"Regenerating Planet with NoiseScale: {noiseScale}, NoiseAmplitude: {noiseAmplitude}");
                CalculateGridParameters();
                GeneratePlanet();
            }
        }

        private void CalculateGridParameters()
        {
            // Calculate grid world size to ensure it can contain the planet with padding
            gridWorldSize = planetRadius * 2.5f; // Add 25% padding on each side
            
            // Calculate grid origin to ensure planet is centered
            gridOrigin = Vector3.one * (-gridWorldSize / 2f);
        }

        void GeneratePlanet()
        {
            _voxels = GenerateVoxelGrid();
            
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            GenerateMarchingCubesMesh(_voxels, out vertices, out triangles);
            
            CreateMesh(vertices, triangles);
        }

        private List<Voxel> GenerateVoxelGrid()
        {
            List<Voxel> voxelList = new List<Voxel>();
            
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        Vector3 worldPos = gridOrigin + new Vector3(x * VoxelSize, y * VoxelSize, z * VoxelSize);
                        
                        // Skip voxels definitely outside the planet's influence
                        float distanceToCenter = Vector3.Distance(worldPos + (Vector3.one * VoxelSize / 2f), Vector3.zero);
                        if (distanceToCenter > planetRadius * 1.5f)
                            continue;

                        Voxel voxel = new Voxel
                        {
                            CornerPositions = new Vector3[8],
                            CornerValues = new float[8]
                        };

                        // Define corners in world space
                        voxel.CornerPositions[0] = worldPos;
                        voxel.CornerPositions[1] = worldPos + new Vector3(VoxelSize, 0, 0);
                        voxel.CornerPositions[2] = worldPos + new Vector3(VoxelSize, VoxelSize, 0);
                        voxel.CornerPositions[3] = worldPos + new Vector3(0, VoxelSize, 0);
                        voxel.CornerPositions[4] = worldPos + new Vector3(0, 0, VoxelSize);
                        voxel.CornerPositions[5] = worldPos + new Vector3(VoxelSize, 0, VoxelSize);
                        voxel.CornerPositions[6] = worldPos + new Vector3(VoxelSize, VoxelSize, VoxelSize);
                        voxel.CornerPositions[7] = worldPos + new Vector3(0, VoxelSize, VoxelSize);

                        // Calculate density values
                        for (int i = 0; i < 8; i++)
                        {
                            voxel.CornerValues[i] = CalculateDensity(voxel.CornerPositions[i]);
                        }

                        voxelList.Add(voxel);
                    }
                }
            }

            return voxelList;
        }

        private float CalculateDensity(Vector3 position)
        {
            float distanceFromCenter = Vector3.Distance(position, Vector3.zero);
        
            // Normalize position relative to planet radius for consistent noise scale
            Vector3 normalizedPos = position / planetRadius;
        
            // Calculate base noise with multiple octaves
            float noiseValue = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxValue = 0f;
        
            for (int i = 0; i < octaves; i++)
            {
                Vector3 noisePos = normalizedPos * noiseScale * frequency * 1000f; // Scale up for more noticeable effect
            
                float octaveValue = PerlinNoise3D(
                    noisePos.x,
                    noisePos.y,
                    noisePos.z
                );
            
                // Create more pronounced features
                octaveValue = Mathf.Pow(octaveValue, terrainSharpness);
            
                noiseValue += octaveValue * amplitude;
                maxValue += amplitude;
            
                amplitude *= persistence;
                frequency *= 2f;
            }
        
            // Normalize and enhance the noise
            noiseValue = noiseValue / maxValue;
            noiseValue = (noiseValue * 2f - 1f); // Convert to -1 to 1 range
        
            // Calculate surface with noise influence
            float surfaceDistance = planetRadius - distanceFromCenter;
        
            // Create more dramatic terrain features
            float terrainInfluence = noiseValue * noiseAmplitude;
        
            // Apply distance-based falloff for smoother blending
            float distanceFromSurface = Mathf.Abs(surfaceDistance);
            float falloff = Mathf.Clamp01(1f - (distanceFromSurface / (noiseAmplitude * 2f)));
        
            // Combine base surface with terrain features
            return surfaceDistance + (terrainInfluence * falloff);
        }

        private float PerlinNoise3D(float x, float y, float z)
        {
            // Combine noise samples from different angles for more varied terrain
            float xy = Mathf.PerlinNoise(x, y);
            float yz = Mathf.PerlinNoise(y, z);
            float xz = Mathf.PerlinNoise(x, z);
            float yx = Mathf.PerlinNoise(y, x);
            float zy = Mathf.PerlinNoise(z, y);
            float zx = Mathf.PerlinNoise(z, x);
        
            // Average all samples for true 3D noise effect
            return (xy + yz + xz + yx + zy + zx) / 6f;
        }

        private void GenerateMarchingCubesMesh(List<Voxel> voxels, out List<Vector3> vertices, out List<int> triangles)
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();

            foreach (var voxel in voxels)
            {
                int voxelConfig = 0;
                for (int i = 0; i < 8; i++)
                {
                    if (voxel.CornerValues[i] > 0)
                    {
                        voxelConfig |= (1 << i);
                    }
                }

                if (MarchingCubesTable.EdgeTable[voxelConfig] == 0) continue;

                Vector3[] edgeVertices = new Vector3[12];
                for (int i = 0; i < 12; i++)
                {
                    if ((MarchingCubesTable.EdgeTable[voxelConfig] & (1 << i)) != 0)
                    {
                        Vector3 cornerA = voxel.CornerPositions[MarchingCubesTable.EdgeVertices[i][0]];
                        Vector3 cornerB = voxel.CornerPositions[MarchingCubesTable.EdgeVertices[i][1]];
                        float valueA = voxel.CornerValues[MarchingCubesTable.EdgeVertices[i][0]];
                        float valueB = voxel.CornerValues[MarchingCubesTable.EdgeVertices[i][1]];

                        edgeVertices[i] = InterpolateEdge(cornerA, cornerB, valueA, valueB);
                    }
                }

                for (int i = 0; MarchingCubesTable.TriTable[voxelConfig][i] != -1; i += 3)
                {
                    int vertexIndex = vertices.Count;
                    vertices.Add(edgeVertices[MarchingCubesTable.TriTable[voxelConfig][i]]);
                    vertices.Add(edgeVertices[MarchingCubesTable.TriTable[voxelConfig][i + 2]]);
                    vertices.Add(edgeVertices[MarchingCubesTable.TriTable[voxelConfig][i + 1]]);

                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                }
            }
        }

        private Vector3 InterpolateEdge(Vector3 cornerA, Vector3 cornerB, float valueA, float valueB)
        {
            float t = valueA / (valueA - valueB);
            return cornerA + t * (cornerB - cornerA);
        }

        private void CreateMesh(List<Vector3> vertices, List<int> triangles)
        {
            if (_meshFilter == null)
            {
                Debug.LogError("MeshFilter is null! Cannot create mesh.");
                return;
            }

            Mesh mesh = new Mesh();
            
            // Always use 32-bit index buffer to support any number of vertices
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            
            try 
            {
                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();
                mesh.RecalculateNormals();

                _meshFilter.mesh = mesh;
                
                Debug.Log($"Mesh created successfully with {vertices.Count} vertices and {triangles.Count / 3} triangles");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create mesh: {e.Message}\nVertex count: {vertices.Count}\nTriangle count: {triangles.Count / 3}");
            }
            
            if (vertices.Count > 1024000) // 1+ million vertices
            {
                Debug.LogWarning($"High vertex count detected: {vertices.Count:N0} vertices. This may impact performance.");
            }
            
        }

        private bool EnsureComponents()
        {
            try
            {
                // First ensure MeshFilter exists
                if (_meshFilter == null)
                {
                    _meshFilter = gameObject.GetComponent<MeshFilter>();
                    if (_meshFilter == null)
                    {
                        _meshFilter = gameObject.AddComponent<MeshFilter>();
                    }
                }
            
                // Then ensure MeshRenderer exists
                if (_meshRenderer == null)
                {
                    _meshRenderer = gameObject.GetComponent<MeshRenderer>();
                    if (_meshRenderer == null)
                    {
                        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    }
                }
            
                // Only set material after ensuring renderer exists
                if (_meshRenderer != null && planetMaterial != null)
                {
                    _meshRenderer.sharedMaterial = planetMaterial;
                }
                else if (planetMaterial == null)
                {
                    Debug.LogWarning("Planet material is not assigned!");
                }
            
                return _meshFilter != null && _meshRenderer != null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize components: {e.Message}");
                return false;
            }
        }

        void OnValidate()
        {
            // Only regenerate if we're in play mode and components are initialized
            if (Application.isPlaying && _meshFilter != null && _meshRenderer != null)
            {
                CalculateGridParameters();
                GeneratePlanet();
            }
        }

        void OnDrawGizmos()
        {
            // Draw the grid bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * gridWorldSize);
            
            // Draw the planet radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Vector3.zero, planetRadius);
        }
    }
}