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

    public unsafe class InstancedBoidsRunner : Runner {

        public Mesh              Mesh;
        public Material          Material;
        public ShadowCastingMode Mode;
        public bool              ReceiveShadows;
        public Color             Initial;
        public Color             Final;

        private MaterialPropertyBlock tempBlock;
        private PinnedMatrixArray srcMatrices;
        private NativeArray<float> noiseOffsets;
        private NativeArray<float4x4> dblBuffer;
        private float3* centerFlock;
        private JobHandle boidsHandle;
        private Vector4[] colors;

        private void Start() {
            tempBlock    = new MaterialPropertyBlock();
            srcMatrices  = new PinnedMatrixArray(Size);
            noiseOffsets = new NativeArray<float>(Size, Allocator.Persistent);
            dblBuffer    = new NativeArray<float4x4>(Size, Allocator.Persistent);
            colors       = new Vector4[Size];

            for (int i = 0; i < Size; i++) {
                var pos         = transform.position + URandom.insideUnitSphere * Radius;
                var rotation    = Quaternion.Slerp(transform.rotation, URandom.rotation, 0.3f);
                dblBuffer[i]    = float4x4.TRS(pos, rotation, Vector3.one);
                noiseOffsets[i] = URandom.value * 10f;

                colors[i] = new Color(
                    URandom.Range(Initial.r, Final.r),
                    URandom.Range(Initial.b, Final.b),
                    URandom.Range(Initial.g, Final.g),
                    URandom.Range(Initial.a, Final.a));
            }

            tempBlock.SetVectorArray("_Color", colors);

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

            if (dblBuffer.IsCreated) {
                dblBuffer.Dispose();
            }

            // Free this memory
            if (centerFlock != null) {
                UnsafeUtility.Free(centerFlock, Allocator.Persistent);
                centerFlock = null;
            }
        }

        private unsafe void Update() {
            Debug.LogError("InstancedBoidsRunner is not implemented!");
            /*
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

            var batchedJob    = new BatchedJob {
                Weights       = Weights,
                Goal          = Destination.position,
                NoiseOffsets  = noiseOffsets,
                Time          = Time.time,
                DeltaTime     = Time.deltaTime,
                MaxDist       = SeparationDistance,
                Speed         = MaxSpeed,
                RotationSpeed = RotationSpeed,
                Size          = srcMatrices.Values.Length,
                // Dst           = (float4x4*)(srcMatrices.Ptr),
                Src           = dblBuffer
            }.Schedule(srcMatrices.Values.Length, 32);

            var avgCenterJob = new AverageCenterJob {
                Center    = centerFlock,
                Matrices  = dblBuffer,
                Size      = srcMatrices.Values.Length
            }.Schedule();

            boidsHandle = JobHandle.CombineDependencies(batchedJob, avgCenterJob);

            boidsHandle = new CopyMatrixJob {
                Dst = dblBuffer,
                Src = srcMatrices.Ptr
            }.Schedule(srcMatrices.Values.Length, 32, boidsHandle);
            */
        }
    }
}
