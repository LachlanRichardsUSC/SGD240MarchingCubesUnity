// Meshing Algorithm ported from Unreal Engine
// This code still runs on the CPU and is
// not multithreaded, so performance leaves
// a lot to be desired.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes.Scripts
{
    public class PlanetGenerator : MonoBehaviour
    {
        public int gridSize = 32; // Number of voxels along each axis
        public float voxelSize = 16.0f; // Size of each voxel
        public float planetRadius = 400f; // Explicit planet radius
        public Material planetMaterial; // Material to apply to the planet mesh
        public float noiseScale = 0.02f;  // Controls the frequency of the noise
        public float noiseAmplitude = 150.0f;  // Controls the height of the terrain (amplitude)
        public bool showDebug = true;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        // Define Voxel struct to store corner positions and density values
        private struct Voxel
        {
            public Vector3[] CornerPositions; // 8 corner positions of a voxel
            public float[] CornerValues; // 8 density values at each corner
        }

        private List<Voxel> _voxels;

        // Start is called before the first frame update
        void Start()
        {
            // Initialize MeshFilter and MeshRenderer components
            _meshFilter = gameObject.GetComponent<MeshFilter>();
            if (_meshFilter == null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            _meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            _meshRenderer.material = planetMaterial;

            UpdateGridSize();    // Ensure grid size is correct before generating the planet
            GeneratePlanet();    // Now generate the planet
            AdjustPlanetScale();
        }

        private void OnValidate()
        {
            UpdateGridSize();
        }

        void UpdateGridSize()
        {
            gridSize = Mathf.CeilToInt((2 * planetRadius) / voxelSize) + 2; // Adding a buffer to make sure the planet fits within the bounds
        }

        // Method to generate the entire planet
        void GeneratePlanet()
        {
            // Generate the voxel grid
            _voxels = GenerateVoxelGrid();

            // Run marching cubes algorithm to generate mesh data
            List<Vector3> vertices;
            List<int> triangles;
            MarchingCubes(_voxels, out vertices, out triangles);

            // Create mesh and assign it to the MeshFilter
            CreateMesh(vertices, triangles);
        }
        
        // New method to adjust the scale of the planet based on planetRadius
        private void AdjustPlanetScale()
        {
            float gridRadius = gridSize * voxelSize / 2.0f; // Original radius based on grid and voxel size
            float scaleFactor = planetRadius / gridRadius;

            // Apply scaling to the transform of the planet
            transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        }

        // Method to generate the voxel grid
        private List<Voxel> GenerateVoxelGrid()
        {
            List<Voxel> voxelList = new List<Voxel>();
    
            // Calculate the grid center offset to center the planet at (0, 0, 0)
            Vector3 gridCenterOffset = new Vector3(gridSize * voxelSize / 2.0f, gridSize * voxelSize / 2.0f, gridSize * voxelSize / 2.0f) - new Vector3(voxelSize / 2.0f, voxelSize / 2.0f, voxelSize / 2.0f);

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        Voxel voxel = new Voxel();
                        voxel.CornerPositions = new Vector3[8];
                        voxel.CornerValues = new float[8];

                        // Base position of each voxel, centered on the planet
                        Vector3 basePosition = new Vector3(x, y, z) * voxelSize - gridCenterOffset;

                        // Define the voxel corners
                        voxel.CornerPositions[0] = basePosition;
                        voxel.CornerPositions[1] = basePosition + new Vector3(voxelSize, 0, 0);
                        voxel.CornerPositions[2] = basePosition + new Vector3(voxelSize, voxelSize, 0);
                        voxel.CornerPositions[3] = basePosition + new Vector3(0, voxelSize, 0);
                        voxel.CornerPositions[4] = basePosition + new Vector3(0, 0, voxelSize);
                        voxel.CornerPositions[5] = basePosition + new Vector3(voxelSize, 0, voxelSize);
                        voxel.CornerPositions[6] = basePosition + new Vector3(voxelSize, voxelSize, voxelSize);
                        voxel.CornerPositions[7] = basePosition + new Vector3(0, voxelSize, voxelSize);

                        // Assign density values to the voxel corners
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
        
        public static float PerlinNoise3D(float x, float y, float z)
        {
            float offset = 400.0f; // Arbitrary large value to offset axes
            float xy = Mathf.PerlinNoise(x, y);
            float yz = Mathf.PerlinNoise(y + offset, z);
            float xz = Mathf.PerlinNoise(x + offset * 2, z);
            float yx = Mathf.PerlinNoise(y + offset * 3, x);
            float zx = Mathf.PerlinNoise(z + offset * 4, x);
            float zy = Mathf.PerlinNoise(z + offset * 5, y);
    
            return (xy + yz + xz + yx + zx + zy) / 6.0f;
        }

        // Method to calculate the density value for a given position
        private float CalculateDensity(Vector3 position)
        {
            Vector3 planetCenter = Vector3.zero;
            float distanceFromCenter = Vector3.Distance(position, planetCenter);

            // Calculate a noise value for the current position
            float noiseValue = PerlinNoise3D(position.x * noiseScale, position.y * noiseScale, position.z * noiseScale);

            // Ensure the noise effect doesn't push the density too far
            float clampedNoise = Mathf.Clamp(noiseValue * noiseAmplitude, -planetRadius * 0.1f, planetRadius * 0.1f);

            // Combine the base density with the noise, ensuring the density remains within reasonable bounds
            float density = (planetRadius - distanceFromCenter) + clampedNoise;

            return density;
        }

        // Marching cubes algorithm to generate vertices and triangles based on the voxel grid
        private void MarchingCubes(List<Voxel> voxels, out List<Vector3> vertices, out List<int> triangles)
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

        // Interpolation function to find the vertex position along the edge
        private Vector3 InterpolateEdge(Vector3 cornerA, Vector3 cornerB, float valueA, float valueB)
        {
            if (Mathf.Approximately(valueA, valueB))
                return cornerA;  // Avoid division by zero or weird results if both values are the same

            float t = (0 - valueA) / (valueB - valueA);  // Note the zero-crossing at the isosurface
            return cornerA + t * (cornerB - cornerA);
        }

        // Method to create a mesh from the generated vertices and triangles
        private void CreateMesh(List<Vector3> vertices, List<int> triangles)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();

            _meshFilter.mesh = mesh;
        }

        void OnDrawGizmos()
        {
            if (showDebug)
            {
                Gizmos.color = Color.green;
                Vector3 gridCenter = transform.position;  // Properly center on the object's position
                Gizmos.DrawWireCube(gridCenter, new Vector3(gridSize * voxelSize, gridSize * voxelSize, gridSize * voxelSize));

                // Also draw a sphere representing the intended planet radius
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, planetRadius);
            }
        }
    }
}
