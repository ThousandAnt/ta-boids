using Unity.Collections;
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

    public class InstancedBoidsRunner : MonoBehaviour {

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

        private void Start() {
            tempBlock = new MaterialPropertyBlock();
            srcMatrices = new PinnedMatrixArray(Size);

            for (int i = 0; i < Size; i++) {
                var pos               = transform.position + URandom.insideUnitSphere * Radius;
                var rotation          = Quaternion.Slerp(transform.rotation, URandom.rotation, 0.3f);
                srcMatrices.Values[i] = float4x4.TRS(pos, rotation, Vector3.one);
            }
        }

        private void OnDisable() {
            boidsHandle.Complete();
        }

        private unsafe void Update() {
            boidsHandle.Complete();

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

            boidsHandle             = new BatchedJob {
                Time                = Time.time,
                DeltaTime           = Time.deltaTime,
                MaxDist             = SeparationDistance,
                Speed               = MaxSpeed,
                RotationCoefficient = RotationCoefficient,
                Size                = srcMatrices.Values.Length,
                Src                 = (float4x4*)(srcMatrices.Ptr),
            }.Schedule(srcMatrices.Values.Length, 32, boidsHandle);
        }
    }
}