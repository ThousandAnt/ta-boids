# ta-boids

A repository demoing how to use the Job System + Burst Compiler with traditional
GameObjects.

## Scenes

All scenes have a `BoidsRunner` GameObject, which will have the appropriate `*BoidsRunner`.

### GameObject
For the GameObject version, open up the `GameObjectsJobs.unity` scene. This scene
will spawn N GameObjects, where the transforms are written in parallel using the
an IJobParallelForTransform job.

### DrawMeshInstanced
Open up the `InstancedJobs.unity` scene. Instead of GameObjects, we open up a buffer of N
matrix elements we want to store and push the data to the `Graphics.DrawMeshInstanced`
command. This example is a little more advanced than the previous one since we want to
take advantage of pointers and implicit casting for the Mathematics library, which is
Burst friendly.

## Jobs Content
All jobs are found in the `Boids.cs` file. For the jobs used in the `InstancedBoidsRunner.cs`,
take a look at the `BoidsOnlyPointer.cs` file. Comments have been added where appropriate to
explain the content and how this is set up.

## Notes
Feel free to use this content if you would like to use it in your projects.