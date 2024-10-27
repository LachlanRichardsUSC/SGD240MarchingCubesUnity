using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

/// <summary>
/// Represents a single chunk of terrain in the procedural generation system.
/// </summary>
/// <remarks>
/// The Chunk class manages the mesh, rendering, and collider data for a terrain chunk.
/// It supports vertex welding, flat shading, and material assignment.
/// </remarks>
public class Chunk
{
    // Public properties
    /// <summary>
    /// Gets the center position of the chunk in world space.
    /// </summary>
    public Vector3 Centre => _centre;

    /// <summary>
    /// Gets the size of the chunk.
    /// </summary>
    public float Size => _size;

    /// <summary>
    /// Gets the identifier of the chunk, representing its coordinate within the terrain grid.
    /// </summary>
    public Vector3Int Id => _id;

    // Private fields
    private Vector3 _centre;
    private float _size;
    private Vector3Int _id;
    private Mesh _mesh;
    private MeshRenderer _renderer;
    private MeshCollider _collider;
    private Dictionary<int2, int> _vertexIndexMap;
    private List<Vector3> _processedVertices;
    private List<Vector3> _processedNormals;
    private List<int> _processedTriangles;

    /// <summary>
    /// Initializes a new instance of the Chunk class with the specified coordinates, center, size, and mesh holder.
    /// </summary>
    /// <param name="coord">The coordinate of the chunk within the terrain grid.</param>
    /// <param name="centre">The center position of the chunk in world space.</param>
    /// <param name="size">The size of the chunk.</param>
    /// <param name="meshHolder">The GameObject that holds the chunk's mesh and rendering components.</param>
    public Chunk(Vector3Int coord, Vector3 centre, float size, GameObject meshHolder)
    {
        _id = coord;
        _centre = centre;
        _size = size;

        _mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

        var filter = meshHolder.AddComponent<MeshFilter>();
        _renderer = meshHolder.AddComponent<MeshRenderer>();
        filter.mesh = _mesh;
        _collider = meshHolder.AddComponent<MeshCollider>();

        _vertexIndexMap = new Dictionary<int2, int>();
        _processedVertices = new List<Vector3>();
        _processedNormals = new List<Vector3>();
        _processedTriangles = new List<int>();
    }

    /// <summary>
    /// Creates and updates the chunk's mesh using the provided vertex data.
    /// </summary>
    /// <param name="vertexData">An array of VertexData objects representing the chunk's vertices.</param>
    /// <param name="numVertices">The number of vertices to process.</param>
    /// <param name="useFlatShading">Indicates whether flat shading should be used.</param>
    public void CreateMesh(VertexData[] vertexData, int numVertices, bool useFlatShading)
    {
        // Clear previous mesh data
        _vertexIndexMap.Clear();
        _processedVertices.Clear();
        _processedNormals.Clear();
        _processedTriangles.Clear();

        int triangleIndex = 0;

        // Process each vertex
        for (int i = 0; i < numVertices; i++)
        {
            VertexData data = vertexData[i];

            // Check if we can reuse an existing vertex (unless using flat shading)
            if (!useFlatShading && _vertexIndexMap.TryGetValue(data.id, out int sharedVertexIndex))
            {
                _processedTriangles.Add(sharedVertexIndex);
            }
            else
            {
                // Add new vertex
                if (!useFlatShading)
                {
                    _vertexIndexMap.Add(data.id, triangleIndex);
                }
                _processedVertices.Add(data.position);
                _processedNormals.Add(data.normal);
                _processedTriangles.Add(triangleIndex);
                triangleIndex++;
            }
        }

        // Temporarily remove collider for mesh updates
        _collider.sharedMesh = null;

        // Update mesh
        _mesh.Clear();
        _mesh.SetVertices(_processedVertices);
        _mesh.SetTriangles(_processedTriangles, 0, true);

        if (useFlatShading)
        {
            _mesh.RecalculateNormals();
        }
        else
        {
            _mesh.SetNormals(_processedNormals);
        }

        // Reapply collider
        _collider.sharedMesh = _mesh;
    }

    /// <summary>
    /// Sets the material for the chunk's renderer.
    /// </summary>
    /// <param name="material">The material to apply to the chunk. If null, the method does nothing.</param>
    public void SetMaterial(Material material)
    {
        if (material == null) return;
        _renderer.material = material;
    }

    /// <summary>
    /// Releases the chunk's mesh to free up memory.
    /// </summary>
    public void Release()
    {
        if (_mesh != null)
        {
            Object.Destroy(_mesh);
        }
    }

    /// <summary>
    /// Draws the wireframe bounds of the chunk using Gizmos.
    /// </summary>
    /// <param name="col">The color to use for drawing the bounds.</param>
    public void DrawBoundsGizmo(Color col)
    {
        Gizmos.color = col;
        Gizmos.DrawWireCube(_centre, Vector3.one * _size);
    }
}
