# ta-boids

A repository demoing how to use the Job System + Burst Compiler with traditional 
GameObjects. 


## Scenes

### GameObject
For the GameObject version, open up the `GameObjectsJobs.unity` scene. This scene 
will spawn N GameObjects, where the transforms are written in parallel using the 
CopyTransforms jobs.

### DrawMeshInstanced
Open up the InstancedJobs. Instead of GameObjects, we open up a buffer of N 
matrix elements we want to store and push the data to the `Graphics.DrawMeshInstanced` 
command.


### Jobs Content
All jobs are found in the `Boids.cs` file. (The jobs do use pointers to interface well 
with managed data.)
