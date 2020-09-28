using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ThousandAnt.Boids {

    // TODO: Check if this needs to be combined into one job, otherwise it would be better if they just ran.
    // TODO: Currently assuming that all data is persistent data we want to manipulate.
    // TODO: We pay the cost of doing a copy if we want to do rendering with draw mesh instanced. (Maybe pointer is better).
    // TODO: Goal setting per flocking group.

    /**
     * Each member in a flock tries to move towards its perceived center. So the center calculation is divided by 
     * (n - 1). This is our primary rule, otherwise known as Cohesion in boids.
     */
    [BurstCompile]
    public struct PerceivedCenterJob : IJobParallelFor {

        public float DeltaTime;
        
        [ReadOnly]
        public NativeArray<float3> Positions;

        [WriteOnly]
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> AccumulatedVelocity;

        public void Execute(int index) {
            var currentPosition = Positions[index];
            var center          = new float3();

            for (int i = 0; i < Positions.Length; i++) {
                if (i == index) {
                    continue;
                }

                center += Positions[i];
            }

            AccumulatedVelocity[index] = (currentPosition - center / (Positions.Length - 1)) * DeltaTime;
        }
    }

    /**
     * We want all boids to generally separate and be some kind of distance away from each other.
     */
    [BurstCompile]
    public struct SeparationJob : IJobParallelFor {
        
        public float DeltaTime;
        public float SeparationDistanceSq;

        [ReadOnly]
        public NativeArray<float3> Position;

        [NativeDisableParallelForRestriction]
        public NativeArray<float3> AccumulatedVelocity;

        public void Execute(int index) {
            var center = new float3();
            var currentPosition = Position[index];
            for (int i = 0; i < Position.Length; i++) {
                if (i == index) {
                    continue;
                }

                // We need to separate the current boid.
                if (math.distancesq(currentPosition, Position[i]) < SeparationDistanceSq) {
                    center = center - (currentPosition - Position[i]);
                }
            }

            AccumulatedVelocity[index] += center * DeltaTime;
        }
    }

    [BurstCompile]
    public struct AlignmentJob : IJobParallelFor {

        public float DeltaTime;

        [NativeDisableParallelForRestriction]
        public NativeArray<float3> AccumulatedVelocity;

        public void Execute(int index) {
            var velocity = new float3();

            for (int i = 0; i < AccumulatedVelocity.Length; i++) {
                if (i != index) {
                    velocity += AccumulatedVelocity[i];
                }
            }

            velocity /= (AccumulatedVelocity.Length - 1);

            AccumulatedVelocity[index] = velocity;
        }
    }

    [BurstCompile]
    public struct GoalJob : IJobParallelFor {


        public void Execute(int index) {
        }
    }

    [BurstCompile]
    public struct VelocityApplicationJob : IJobParallelFor {

        // TODO: Add velocity clamping so the boids don't infinitely become fast.

        [ReadOnly]
        public NativeArray<float3> Velocities;

        public NativeArray<float3> Positions;

        public void Execute(int index) {
            Positions[index] += Velocities[index];
        }
    }
}
