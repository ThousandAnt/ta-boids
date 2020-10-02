using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Rendering;

using URandom = UnityEngine.Random;

namespace ThousandAnt.Boids {

    internal unsafe class PinnedMatrixArray {

        internal Matrix4x4[] Values;

        internal Matrix4x4* Ptr;

        internal PinnedMatrixArray(int size) {
            Values = new Matrix4x4[size];
            fixed (Matrix4x4* ptr = Values) {
                Ptr = ptr;
            }
        }
    }

    public unsafe class InstancedBoidsRunner : MonoBehaviour {

        public Mesh              Mesh;
        public Material          Material;
        public ShadowCastingMode Mode;
        public bool              ReceiveShadows;

        public float SeparationDistance  = 10f;
        public float Radius              = 20;
        public int   Size                = 512;
        public float MaxSpeed            = 6f;
        public float RotationCoefficient = 4f;

        [Header("Goal Setting")]
        public bool AllowDestination;
        public Transform Destination;

        [Header("Tendency")]
        public float3 Wind;

        private PinnedMatrixArray srcMatrices;
        private JobHandle boidsHandle;
        private MaterialPropertyBlock tempBlock;
        private NativeArray<float> noiseOffsets;
        private float3* centerFlock;

        private void Start() {
            tempBlock = new MaterialPropertyBlock();
            srcMatrices = new PinnedMatrixArray(Size);
            noiseOffsets = new NativeArray<float>(Size, Allocator.Persistent);

            for (int i = 0; i < Size; i++) {
                var pos               = transform.position + URandom.insideUnitSphere * Radius;
                var rotation          = Quaternion.Slerp(transform.rotation, URandom.rotation, 0.3f);
                srcMatrices.Values[i] = float4x4.TRS(pos, rotation, Vector3.one);
                noiseOffsets[i]       = URandom.value * 10f;
            }

            centerFlock = (float3*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<float3>(),
                UnsafeUtility.AlignOf<float3>(),
                Allocator.Persistent);

            UnsafeUtility.MemSet(centerFlock, 0, UnsafeUtility.SizeOf<float3>());
        }

        private void OnDisable() {
            boidsHandle.Complete();

            if (noiseOffsets.IsCreated) {
                noiseOffsets.Dispose();
            }

            // Free this memory
            if (centerFlock != null) {
                UnsafeUtility.Free(centerFlock, Allocator.Persistent);
                centerFlock = null;
            }
        }

        private unsafe void Update() {
            boidsHandle.Complete();

            // Set up the transform so that we have cinemachine to look at
            transform.position = *centerFlock;

            Graphics.DrawMeshInstanced(
                Mesh,
                0,
                Material,
                srcMatrices.Values,
                srcMatrices.Values.Length,
                tempBlock,
                Mode,
                ReceiveShadows,
                0,
                null);

            var batchedJob          = new BatchedJob {
                Goal                = Destination.position,
                NoiseOffsets        = noiseOffsets,
                Time                = Time.time,
                DeltaTime           = Time.deltaTime,
                MaxDist             = SeparationDistance,
                Speed               = MaxSpeed,
                RotationCoefficient = RotationCoefficient,
                Size                = srcMatrices.Values.Length,
                Src                 = (float4x4*)(srcMatrices.Ptr),
            }.Schedule(srcMatrices.Values.Length, 32, boidsHandle);

            var centerJob = new AverageCenterJob {
                Center    = centerFlock,
                Matrices  = srcMatrices.Ptr,
                Size      = srcMatrices.Values.Length
            }.Schedule(boidsHandle);

            boidsHandle = JobHandle.CombineDependencies(centerJob, batchedJob);
        }
    }
}
