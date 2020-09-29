using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using URandom = UnityEngine.Random;

namespace ThousandAnt.Boids {

    public class BoidsRunner : MonoBehaviour {

        public Mesh Mesh;
        public Material Material;
        public float SeparationDistance = 10f;
        public float Radius = 20;
        public int Size = 512;
        public float MaxSpeed = 2f;

        [Header("Goal Setting")]
        public bool AllowDestination;
        public Transform Destination;

        [Header("Tendency")]
        public float3 Wind;

        private NativeArray<float3> positions;
        private NativeArray<float3> velocities;
        private JobHandle boidsHandle;

        private Matrix4x4[] matrices;

        private void Start() {
            positions = new NativeArray<float3>(Size, Allocator.Persistent);
            velocities = new NativeArray<float3>(Size, Allocator.Persistent);

            for (int i = 0; i < Size; i++) {
                var position = URandom.insideUnitSphere * URandom.Range(0f, 1f) * Radius;
                positions[i] = position;
            }

            matrices = new Matrix4x4[Size];
        }

        private void OnDisable() {
            boidsHandle.Complete();

            if (positions.IsCreated) {
                positions.Dispose();
            }

            if (velocities.IsCreated) {
                velocities.Dispose();
            }
        }

        private void Update() {
            boidsHandle.Complete();
            CopyMatrixData();

            var dt = Time.deltaTime;

            boidsHandle    = new VelocityApplicationJob {
                Wind       = Wind,
                DeltaTime  = dt,
                Positions  = positions,
                Velocities = velocities,
                MaxSpeed   = MaxSpeed
            }.Schedule(positions.Length, 32);

            boidsHandle = new PerceivedCenterJob {
                AccumulatedVelocity = velocities,
                Positions           = positions,
            }.Schedule(positions.Length, 32, boidsHandle);

            boidsHandle = new SeparationJob {
                AccumulatedVelocity = velocities,
                Position = positions,
                SeparationDistanceSq = SeparationDistance * SeparationDistance
            }.Schedule(positions.Length, 32, boidsHandle);

            boidsHandle = new AlignmentJob {
                AccumulatedVelocity = velocities,
            }.Schedule(positions.Length, 32, boidsHandle);

            if (AllowDestination) {
                boidsHandle = new GoalJob {
                    Positions = positions,
                    Velocities = velocities,
                    Destination = Destination.position
                }.Schedule(positions.Length, 32, boidsHandle);
            }
        }

        // TODO: Account for steering and looking at the velocity.
        private void CopyMatrixData() {
            for (int i = 0; i < matrices.Length; i++) {
                matrices[i] = Matrix4x4.TRS(positions[i], quaternion.identity, Vector3.one);
            }
        }

        private void LateUpdate() {
            Graphics.DrawMeshInstanced(Mesh, 0, Material, matrices);
        }
    }
}
