using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

using URandom = UnityEngine.Random;

namespace ThousandAnt.Boids {

    public unsafe class GameObjectsBoidsRunner : Runner {

        public Transform FlockMember;

        private NativeArray<float> noiseOffsets;
        private NativeArray<float4x4> srcMatrices;
        private Transform[] transforms;
        private TransformAccessArray transformAccessArray;
        private JobHandle boidsHandle;
        private float3* center;

        private void Start() {
            transforms   = new Transform[Size];
            srcMatrices  = new NativeArray<float4x4>(transforms.Length, Allocator.Persistent);
            noiseOffsets = new NativeArray<float>(transforms.Length, Allocator.Persistent);

            for (int i = 0; i < Size; i++) {
                var pos         = transform.position + URandom.insideUnitSphere * Radius;
                var rotation    = Quaternion.Slerp(transform.rotation, URandom.rotation, 0.3f);
                transforms[i]   = GameObject.Instantiate(FlockMember, pos, rotation) as Transform;
                srcMatrices[i]  = transforms[i].localToWorldMatrix;
                noiseOffsets[i] = URandom.value * 10f;
            }

            transformAccessArray = new TransformAccessArray(transforms);

            center = (float3*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<float3>(), UnsafeUtility.AlignOf<float3>(), Allocator.Persistent);
            UnsafeUtility.MemSet(center, default, UnsafeUtility.SizeOf<float3>());
        }

        private void OnDisable() {
            boidsHandle.Complete();

            if (srcMatrices.IsCreated) {
                srcMatrices.Dispose();
            }

            if (noiseOffsets.IsCreated) {
                noiseOffsets.Dispose();
            }

            if (center != null) {
                UnsafeUtility.Free(center, Allocator.Persistent);
                center = null;
            }
        }

        private unsafe void Update() {
            boidsHandle.Complete();

            transform.position = *center;

            boidsHandle  = new AverageCenterJob {
                Matrices = (Matrix4x4*)srcMatrices.GetUnsafePtr(),
                Center   = center,
                Size     = srcMatrices.Length
            }.Schedule(boidsHandle);

            boidsHandle       = new BatchedJob {
                Weights       = Weights,
                Goal          = Destination.position,
                NoiseOffsets  = noiseOffsets,
                Time          = Time.time,
                DeltaTime     = Time.deltaTime,
                MaxDist       = SeparationDistance,
                Speed         = MaxSpeed,
                RotationSpeed = RotationSpeed,
                Size          = srcMatrices.Length,
                Src           = (float4x4*)(srcMatrices.GetUnsafePtr())
            }.Schedule(transforms.Length, 32, boidsHandle);

            boidsHandle = new CopyTransformJob {
                Src = srcMatrices
            }.Schedule(transformAccessArray, boidsHandle);
        }
    }
}
