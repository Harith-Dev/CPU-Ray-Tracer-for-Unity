CPU Ray Tracer For Unity using only C# with (Unity Burst & Job System)

-Sepport

1- Ray Tracing Shadow

2- Ray Tracing Global ilomimtion

3- Ray Tracing Caustics

4- Ray Tracing Reflaction & Reflaction (Shadow only)

-Bugs & Probloms

1- Very slow in Rendering

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

3/ Only Sepport "SkinnedMeshRenderer" and "MeshCollider"

3- drop "RayTracingCamera" Script in your Camera

4- drop "FreeLook" script in you camera to move and Update Rendering(Optional)

5- Create a raw image and make it scale to fit the entire screen

6- just set "UpdateFrame" to true to Update Rendering

----------information------------

its not very optimazed for Real Time so to Run it in Real time you shoud set "ScreenSize" to about (200,100) and its use very simple math Functions and mothods and its not sepport multiple bounces



