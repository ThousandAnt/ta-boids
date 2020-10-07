using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace ThousandAnt.Boids {

    public interface IRunner {
        ref BoidWeights GetWeights();
        float3 Center();
    }

    [System.Serializable]
    public struct BoidWeights {
        public float TendencyWeight;
        public float AlignmentWeight;
        public float NoiseWeight;

        public static BoidWeights Default() {
            return new BoidWeights {
                TendencyWeight   = 1,
                AlignmentWeight  = 1,
                NoiseWeight      = 1
            };
        }
    }

    public static class TransformExtensions {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion Rotation(this in float4x4 m) {
            return new quaternion(m);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Position(this in float4x4 m) {
            return m.c3.xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 Forward(this in float4x4 m) {
            return m.c2.xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion QuaternionBetween(this in float3 from, in float3 to) {
            var cross = math.cross(from, to);

            var w = math.sqrt(math.lengthsq(from) * math.lengthsq(to)) + math.dot(from, to);
            return new quaternion(new float4(cross, w));
        }
    }

    [BurstCompile]
    public struct CopyTransformJob : IJobParallelForTransform {

        [ReadOnly]
        public NativeArray<float4x4> Src;

        public void Execute(int index, TransformAccess transform) {
            var m = Src[index];

            transform.localPosition = m.Position();
            transform.rotation      = new quaternion(m);
        }
    }

    [BurstCompile]
    public unsafe struct AverageCenterJob : IJob {

        [ReadOnly]
        public NativeArray<float4x4> Matrices;

        [NativeDisableUnsafePtrRestriction]
        public float3* Center;

        public int Size;

        public void Execute() {
            var center = float3.zero;
            for (int i = 0; i < Size; i++) {
                float4x4 m = Matrices[i];
                center += m.Position();
            }

            *Center = center /= Size;
        }
    }

    [BurstCompile]
    public unsafe struct CopyMatrixJob : IJobParallelFor {

        [WriteOnly]
        public NativeArray<float4x4> Dst;

        [NativeDisableUnsafePtrRestriction]
        public Matrix4x4* Src;

        public void Execute(int index) {
            Dst[index] = Src[index];
        }
    }

    [BurstCompile]
    public unsafe struct BatchedJob : IJobParallelFor {

        public BoidWeights   Weights;
        public float         Time;
        public float         DeltaTime;
        public float         MaxDist;
        public float         Speed;
        public float         RotationSpeed;
        public int           Size;
        public float3        Goal;

        [ReadOnly]
        public NativeArray<float> NoiseOffsets;

        [ReadOnly]
        public NativeArray<float4x4> Src;

        [NativeDisableUnsafePtrRestriction]
        public float4x4* Dst;

        public void Execute(int index) {
            var current       = Src[index];
            var currentPos    = current.Position();
            var perceivedSize = Size - 1;

            // TODO: Add weights
            var separation = float3.zero;
            var alignment  = float3.zero;
            var cohesion   = float3.zero;
            var tendency   = math.normalizesafe(Goal - currentPos) * Weights.TendencyWeight;

            for (int i = 0; i < Size; i++) {
                if (i == index) {
                    continue;
                }

                var b = Dst[i];
                var other = b.Position();

                // Perform separation
                separation += SeparationVector(currentPos, other);

                // Perform alignment
                alignment  += b.Forward();

                // Perform cohesion
                cohesion   += other;
            }

            var avg = 1f / perceivedSize;

            alignment     *= avg;
            cohesion      *= avg;
            cohesion       = math.normalizesafe(cohesion - currentPos);
            var direction  = separation +
                             Weights.AlignmentWeight * alignment +
                             cohesion +
                             Weights.TendencyWeight * tendency;

            var targetRotation = current.Forward().QuaternionBetween(math.normalizesafe(direction));
            var finalRotation  = current.Rotation();

            if (!targetRotation.Equals(current.Rotation())) {
                finalRotation = math.lerp(finalRotation.value, targetRotation.value, RotationSpeed * DeltaTime);
            }

            var pNoise = math.abs(noise.cnoise(new float2(Time, NoiseOffsets[index])) * 2f - 1f);
            var speedNoise = Speed * (1f + pNoise * Weights.NoiseWeight * 0.9f);
            var finalPosition = currentPos + current.Forward() * speedNoise * DeltaTime;

            Dst[index] = float4x4.TRS(finalPosition, finalRotation, new float3(1));
        }

        float3 SeparationVector(in float3 current, in float3 other) {
            var diff   = current - other;
            var mag    = math.length(diff);
            var scalar = math.clamp(1 - mag / MaxDist, 0, 1);

            return diff * (scalar / mag);
        }
    }
}
