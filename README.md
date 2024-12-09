CPU Ray Tracing For Unity using only C# with (Unity Burst & Job System) 

-Supports

1- Ray Tracing Shadow

2- Ray Tracing Global ilomimtion

3- Ray Tracing Caustics

4- Ray Tracing Reflaction & Reflaction (Shadow only)

5- Ray Tracing Depth of field

5- Support Clear Coat, Albedo Map, Blurry Refraction.

-Bugs & Probloms

1- Very slow in Rendering

2- not sepport multiple bounces.

-Simple Samples Images
<p style="text-align:left;">
  <img src="https://raw.githubusercontent.com/Harith-Dev/CPU-Ray-Tracer-for-Unity/main/ExamplesImage/Spheres.jpg" alt="Image 1" width="200">
  <img src="https://raw.githubusercontent.com/Harith-Dev/CPU-Ray-Tracer-for-Unity/main/ExamplesImage/FRACTALS.jpg" alt="Image 2" width="200">
  <br>
  <img src="https://raw.githubusercontent.com/Harith-Dev/CPU-Ray-Tracer-for-Unity/main/ExamplesImage/Sponza.jpg" alt="Image 3" width="200">
  <img src="https://raw.githubusercontent.com/Harith-Dev/CPU-Ray-Tracer-for-Unity/main/ExamplesImage/DOF.jpg" alt="Image 4" width="200">
</p>
------------How To Use---------------

1- in your project drag and drop "Source Code" Folder

2- every game object in you scene shoud have this

1/ Give "Object" Tag To Every Object

2/ "RayTracingMaterial" Script (to set Material Properties)

3/ Every Object in scene should have Collider (Only Sepport "SkinnedMeshRenderer" and "MeshCollider")

3- drop "RayTracingCamera" Script in your Camera

4- drop "FreeLook" script in you camera to move and Update Rendering(Optional)

5- Create a raw image and make it scale to fit the entire screen

6- just set "UpdateFrame" to true to Update Rendering

----------information------------

its not very optimazed for Real Time so to Run it in Real time you should set "ScreenSize" to about (200,100)
and its not sepport multiple bounces.
alot of shading tasks its run on main thread to keep it simple.
its use very simple math Functions and algorithms so its simple and good source to learn from it.

Planned Improvements:
- Adding multi-bounce ray tracing for better lighting effects.
- Improving performance with better algorithms or using Unity’s Burst features.

----------Dev-------------

By Harith.CSDV

From IRAQ



