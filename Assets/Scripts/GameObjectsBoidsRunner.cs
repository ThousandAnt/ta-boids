using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

using URandom = UnityEngine.Random;

namespace ThousandAnt.Boids {

    public class GameObjectsBoidsRunner : MonoBehaviour {

        public Transform FlockMember;

        public float SeparationDistance  = 10f;
        public float Radius              = 20;
        public int   Size                = 512;
        public float MaxSpeed            = 2f;
        public float RotationCoefficient = 4f;

        [Header("Goal Setting")]
        public bool AllowDestination;
        public Transform Destination;

        [Header("Tendency")]
        public float3 Wind;

        private NativeArray<float4x4> srcMatrices;
        private Transform[] transforms;
        private TransformAccessArray transformAccessArray;

        private JobHandle boidsHandle;

        private float noiseOffset;

        private void Start() {
            transforms   = new Transform[Size];
            srcMatrices     = new NativeArray<float4x4>(transforms.Length, Allocator.Persistent);
            // dstMatrices = new NativeArray<float4x4>(transforms.Length, Allocator.Persistent);

            for (int i = 0; i < Size; i++) {
                var pos        = transform.position + URandom.insideUnitSphere * Radius;
                var rotation   = Quaternion.Slerp(transform.rotation, URandom.rotation, 0.3f);
                transforms[i]  = GameObject.Instantiate(FlockMember, pos, rotation) as Transform;
                srcMatrices[i] = transforms[i].localToWorldMatrix;
            }

            transformAccessArray = new TransformAccessArray(transforms);
            noiseOffset = URandom.value * 10f;
        }

        private void OnDisable() {
            boidsHandle.Complete();

            if (srcMatrices.IsCreated) {
                srcMatrices.Dispose();
            }
        }

        private unsafe void Update() {
            boidsHandle.Complete();
            boidsHandle             = new BatchedJob {
                NoiseOffset         = noiseOffset,
                Time                = Time.time,
                DeltaTime           = Time.deltaTime,
                MaxDist             = SeparationDistance,
                Speed               = MaxSpeed,
                RotationCoefficient = RotationCoefficient,
                Size                = srcMatrices.Length,
                Src                 = (float4x4*)(srcMatrices.GetUnsafePtr())
            }.Schedule(transforms.Length, 32, boidsHandle);

            boidsHandle = new CopyTransformJob {
                Src = srcMatrices
            }.Schedule(transformAccessArray, boidsHandle);
        }
    }
}
