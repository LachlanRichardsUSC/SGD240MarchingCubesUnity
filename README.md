# SGD240-Planets
For this assessment I aim to create marching cubes in a game engine. On this particular wiki, I have decided to use Unity
to implement the meshing algorithm.

# Sprint 1 - Week 7

In its current form, planets are able to be generated procedurally at runtime. The approach is very 'primitive', all the work is currently being done
on the CPU. Future iterations of this project will try to offload some of the work to the GPU so mesh can be generated in parallel. Mesh chunking, hyperthreading
and a dynamic LODs are features I am also considering. Currently the procedural planet (which is not planet sized) takes approx. 10-30 seconds to generate at runtime
on 32GB RAM, R7 7800X3D, RTX 4090, so it is incredibly likely that more average systems may run out of memory at runtime.

Hurdles overcome so far:
- Voxel Grid (and debug visualization*)
- Marching cubes algorithm with lookup table (stored in a seperate class)
- Ability to assign materials
- Ability to adjust Radius (Voxel Density and Bounds are still hardcoded)
- Perlin3D noise to create more dynamic mesh
- Normals are recalculated after mesh generation and deformation

Hurdles to overcome:
- Set up classes for compute shader in unreal (incl. readback to CPU)
- Make meshing algorithm in compute shader
  
# Sprint 2 - Week 9

I have successfully ported my project into Unity. Fundamentally the project is the same, same meshing algorithm.
At this point in time, the project still runs on the CPU, but I have begun setting the framework for creating
compute shaders down the line. Working in Unity, both on the engine side and in the IDE, has proven to be easier
and more comfortable, as I am used to C# programming. I also have plans to decouple some functionality from my
Planet Generator script to make the code more scalable.

![Unity Progress](https://github.com/LachlanRichardsUSC/SGD240MarchingCubesUnity/blob/main/march%20cubes%20unity.png)

# Engine of Choice:
Unity 2022.3.45f1

# IDE
Jetbrains Rider

# 3rd Party Plugins/API?
https://github.com/keijiro/NoiseShader - Noise Library (to yet be implemented)

# References/Resources

-  Polygonising a Scalar Field, Paul Bourke
   https://paulbourke.net/geometry/polygonise/

-  Sebastian Lague, Coding Adventures - Terraforming
   https://www.youtube.com/watch?v=vTMEdHcKgM4&t=169s&pp=ygUbc2ViYXN0YW4gbGFndWUgdGVycmFmb3JtaW5n
