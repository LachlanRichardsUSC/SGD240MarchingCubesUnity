# SGD240 - Planets

### Overview

**SGD240 - Planets** is a procedural planet generation project developed using Unity for the SGD240 course. The goal of this project is to create procedurally generated planets using the Marching Cubes algorithm.  Some goals I set out to achieve with this project is to:

- Ensure the code is optimized and adheres to professional coding conventions
- Add a kind of LOD/Chunk system
- Leverage Compute Shaders to cut down load times

Two implementations are provided: a CPU-based solution and a more advanced GPU-based solution using compute shaders for performance optimization.
- CPU Implementation: Designed for simplicity and ease of integration, albeit slower due to sequential processing. A planet with 750k vertices takes approximately 20 seconds to generate on an AMD Ryzen 7800X3D CPU.
- GPU Implementation: Utilizes compute shaders to parallelize the Marching Cubes algorithm, achieving faster mesh generation. On an NVIDIA RTX 4090, a planet with 2.5 million vertices is generated in under 0.5 seconds.

### How does it work?

The Marching Cubes Algorithm is a widely-used technique in computer graphics for extracting a polygonal mesh of an isosurface from a three-dimensional scalar field (such as volumetric data or noise functions). This method is essential in rendering 3D shapes like terrain, clouds, or even medical images (like MRI scans).

The goal of the Marching Cubes algorithm is to create a surface that passes through regions in a 3D volume where the values meet a specified threshold, called an iso-level. The algorithm works by iterating through a 3D grid of points, processing each "cube" formed by adjacent grid points. The volume is divided into smaller cubes, each consisting of eight vertices.

These vertices are the grid points of the 3D volume. Each vertex contains a scalar value (e.g., a density value or a noise-generated value). For each cube, the algorithm checks the scalar value at each vertex and determines whether it is above or below the specified iso-level. This classification creates a binary state (inside or outside the surface) for each vertex.

Using the binary classification from the previous step, the algorithm generates an 8-bit index (0–255) that represents the cube's configuration. Each bit in this index corresponds to a vertex of the cube (1 if the vertex is inside the surface, 0 if outside). Using the generated index, the algorithm looks up a precomputed edge table. The edge table maps each cube configuration index to a set of edges within the cube that intersect with the isosurface.

For each intersected edge, the algorithm interpolates between the two end vertices based on their scalar values. This interpolation gives the exact position of the intersection point on the edge. The algorithm then uses a triangulation table to determine which intersection points within the cube should be connected to form triangles. These triangles together represent the surface passing through the cube.The generated triangles for each cube are then stored in a buffer, and the algorithm continues to the next cube until the entire volume is processed.
 


---

## Project Installation

1. **Clone or Download the Project**  
   Click the green "Code" button on this repository's main page and select "Download ZIP" or clone the project via Git.  
   ```bash
   git clone https://github.com/YourUsername/SGD240-Planets.git
2. **Open the project using Unity Version 2023.3.45f1**

---

## File Structure

Upon opening Unity, you will see a few folders. Users should navigate to the Assets folder (if not already there) and choose a scene from the Scenes folder.

The Includes folder contains referenced classes that I do not own, but was given permission to use.

The Materials folder contains the material for the generated mesh and its chunks.

The MarchingCubes folder contains 3 subfolders - 'CPU Based Approach' for resources CPU based, 'Shader Based Approach' for resources compute shader based and 'Shared' for scripts that are mutual between the two scenes.

---

 ## Adjusting Planet Parameters
   
 **CPU Scene:**
-  Tweak parameters such as Planet Radius, Grid Size, Voxel Size (Auto by Default), Noise Seed, Scale, Height, Octaves, Octave Persistence and Sharpness.
-  The default parameters are a great starting point to generate some interesting mesh.
-  A radius of anywhere between 200 - 500 with a Grid Size of 72 - 192 is suitable. Overriding the Auto Voxel size essentially acts as a scale multiplier.
[img]

**Compute Shader Scene:**
- Adjust parameters like Number of Chunks, Points Per Axis, Bounds Size, Padding/Border, Iso Level, Flat/Smooth Shading, Noise Scale + Height, Density Texture Blurring and Blur Radius.
- The default parameters create a natural looking landmass. Having between 4-16 chunks, 8-32 Points per Axis and a bounds size of 200 - 500 are recommended.
- A noise scale between 0.2 and 0.85 are recommended, while a noise height value of 0.01 to 0.085 are recommended.
- Furthermore, the user is able to attach a TerrainDebugVisualizer script to the PlanetObject, to view extra debug information such as showing the density points and showing chunk boundaries. Density points are labelled either positive or negative and chunks are labelled with their name and position when hovering the mouse over the respective part of the mesh.
[img]
---
## Sprints

### Sprint 1 - Week 7 (Done in Unreal Engine)

In its current form, planets are able to be generated procedurally at runtime. The approach is very 'primitive', all the work is currently being done
on the CPU. Future iterations of this project will try to offload some of the work to the GPU so mesh can be generated in parallel. Mesh chunking, hyperthreading
and a dynamic LODs are features I am also considering. Currently the procedural planet (which is not planet sized) takes approx. 10-30 seconds to generate at runtime
on 32GB RAM, R7 7800X3D, RTX 4090, so it is incredibly likely that more average systems may run out of memory at runtime.

### Hurdles overcome so far:
- Voxel Grid (and debug visualization*)
- Marching cubes algorithm with lookup table (stored in a seperate class)
- Ability to assign materials
- Ability to adjust Radius (Voxel Density and Bounds are still hardcoded)
- Perlin3D noise to create more dynamic mesh
- Normals are recalculated after mesh generation and deformation

### Hurdles to overcome:
- Set up classes for compute shader in unreal (incl. readback to CPU)
- Make meshing algorithm in compute shader

---

### Sprint 2 - Week 9 (Done in Unity)

I have successfully ported my project into Unity. Fundamentally the project is the same, same meshing algorithm.
At this point in time, the project still runs on the CPU, but I have begun setting the framework for creating
compute shaders down the line. Working in Unity, both on the engine side and in the IDE, has proven to be easier
and more comfortable, as I am used to C# programming. I also have plans to decouple some functionality from my
Planet Generator script to make the code more scalable.

![Unity Progress](https://github.com/LachlanRichardsUSC/SGD240MarchingCubesUnity/blob/main/march%20cubes%20unity.png)

### Key Achievements
- Unity Port: The entire CPU-based procedural generation system was successfully translated from Unreal Engine to Unity. This port maintained the original meshing algorithm while leveraging C#'s comfort and familiarity.
- Compute Shader Framework: Began structuring the project to support compute shaders, paving the way for offloading mesh generation tasks to the GPU.

### Future Focus
- Implement compute shaders to handle mesh generation and deformation tasks on the GPU.
- Implement Chunking to optimize the generated mesh.
- Use an external noise library to use fractal noise instead of Unity Perlin Noise, which mirrors the noise pattern

---

## Post Mortem

Despite a rocky start with switching engines and struggling to wrap my head around compute shaders, I managed to succesfully make procedural planets in Unity that leverage
the power of parallel processing. The code is typically 10x faster than the CPU based approach and allows for bigger and more detailed meshes. Since Week 7, my focus had shifted to learning and attempting to implement the algorithm in a compute shader, as it was the only feasible way in my opinion to generate planets with reasonable load times and system load.

The bulk of my time on this Project from then onwards was spent optimizing the compute shader approach, as I felt I could not do much more with the CPU based approach, but ultimately I decided to keep it around. In essence, this project serves as a demonstration of the capabilities of the modern graphics card, and how some tasks are best offloaded to the GPU to leverage its fast parallel processing power. While collision were not implemented due to the player controller being a freestyle camera, CPU readback is possible and collisions are given to each mesh chunk, it'd just need to be set up with Unity's layer system.

Near the end of this project, I gave the CPU based approach some more Quality of Life features, such as automatically determining the voxel size to make adjusting the parameters more intuitive (adjust 2 parameters instead of 3). Admittedly, there are some quirks with the CPU based approach that were left unfixed, such as the Planet radius being somewhat limited to 3x the Grid Size, otherwise parts of the mesh get cut off. I believe this is an error that results the planet not being positioned and scaled with world units. This error was fixed in the compute shader version, the mesh will never cut off at any point due to proper positioning + scaling, and the implementation of a buffer of 1 voxel around the edge of the planet. Additionally, being able to blur the Density Texture with a blur compute shader helps round out some of the harsh edges that would otherwise get generated. Another strength that the compute shader approach has is the ability to weld vertices together which in turn results in smooth shading. I left a toggle to re-enable flat shading if the user prefers it.

I consider the compute shader approach to be more polished, the cpu based approach was more or less left in as an archive and proof of experimentation.

# Engine of Choice:
Unity 2022.3.45f1

# IDE
Jetbrains Rider



# References/Resources

-  Polygonising a Scalar Field, Paul Bourke
   https://paulbourke.net/geometry/polygonise/

- Sebastian Lague, Coding Adventures - Compute Shaders
  https://www.youtube.com/watch?v=9RHGLZLUuwc&pp=ygUUdW5pdCBjb21wdXRlIHNoYWRlcnM%3D

-  Sebastian Lague, Coding Adventures - Terraforming
   https://www.youtube.com/watch?v=vTMEdHcKgM4&t=169s&pp=ygUbc2ViYXN0YW4gbGFndWUgdGVycmFmb3JtaW5n

-   Noise Library by Kejiro - https://github.com/keijiro/NoiseShader
