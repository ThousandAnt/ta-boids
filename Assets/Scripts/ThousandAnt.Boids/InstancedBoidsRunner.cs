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

        internal Matrix4x4[] Src;   // Our source buffer for reading
        internal Matrix4x4[] Dst;   // Our double buffer for writing

        internal float4x4* SrcPtr;
        internal float4x4* DstPtr;

        internal int Size { get; private set; }

        internal PinnedMatrixArray(int size) {
            Src = new Matrix4x4[size];
            fixed (Matrix4x4* ptr = Src) {
                SrcPtr = (float4x4*)ptr;
            }

            Dst = new Matrix4x4[size];
            fixed (Matrix4x4* ptr = Dst) {
                DstPtr = (float4x4*)ptr;
            }

            Size = size;
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
        private PinnedMatrixArray matrices;
        private NativeArray<float> noiseOffsets;
        private float3* centerFlock;
        private JobHandle boidsHandle;
        private Vector4[] colors;

#if UNITY_EDITOR
        private AtomicSafetyHandle safetySrc;
        private AtomicSafetyHandle safetyDst;
#endif

        private void Start() {
            tempBlock    = new MaterialPropertyBlock();
            matrices     = new PinnedMatrixArray(Size);
            noiseOffsets = new NativeArray<float>(Size, Allocator.Persistent);
            colors       = new Vector4[Size];

            for (int i = 0; i < Size; i++) {
                var pos         = transform.position + URandom.insideUnitSphere * Radius;
                var rotation    = Quaternion.Slerp(transform.rotation, URandom.rotation, 0.3f);
                noiseOffsets[i] = URandom.value * 10f;
                matrices.Src[i] = Matrix4x4.TRS(pos, rotation, Vector3.one);

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
                matrices.Src,
                matrices.Src.Length,
                tempBlock,
                Mode,
                ReceiveShadows,
                0,
                null);

            var avgCenterJob = new BoidsPointerOnly.AverageCenterJob {
                Matrices = (float4x4*)matrices.SrcPtr,
                Center   = centerFlock,
                Size     = matrices.Size
            }.Schedule();

            var boidJob      = new BoidsPointerOnly.BatchedBoidJob {
                Weights       = Weights,
                Goal          = Destination.position,
                NoiseOffsets  = noiseOffsets,
                Time          = Time.time,
                DeltaTime     = Time.deltaTime,
                MaxDist       = SeparationDistance,
                Speed         = MaxSpeed,
                RotationSpeed = RotationSpeed,
                Size          = matrices.Size,
                Src           = (float4x4*)matrices.SrcPtr,
                Dst           = (float4x4*)matrices.DstPtr,
            }.Schedule(matrices.Size, 32);

            var combinedJob = JobHandle.CombineDependencies(boidJob, avgCenterJob);

            boidsHandle = new BoidsPointerOnly.CopyMatrixJob {
                Dst = (float4x4*)matrices.SrcPtr,
                Src = (float4x4*)matrices.DstPtr
            }.Schedule(matrices.Size, 32, combinedJob);
        }
    }
}
