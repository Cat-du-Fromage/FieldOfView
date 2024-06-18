using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine;

using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace Kaizerwald.RTTCamera
{
    internal static class Utils
    {
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float clampAngle(float lfAngle, float lfMin, float lfMax)
        {
            lfAngle += select(0, 360f, lfAngle < -180f);
            lfAngle -= select(0, 360f, lfAngle > 180f);
            return clamp(lfAngle, lfMin, lfMax);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            lfAngle += lfAngle < -180f ? 360f : 0;
            lfAngle -= lfAngle > 180f ? 360f : 0;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Quaternion RotateFWorld(in Quaternion rotation, float x, float y, float z)
        {
            quaternion eulerRot = quaternion.EulerZXY(x, y, z);
            return mul(eulerRot, rotation);
        }
            
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Quaternion RotateFSelf(in Quaternion localRotation, float x, float y, float z)
        {
            quaternion eulerRot = quaternion.EulerZXY(x, y, z);
            return mul(localRotation, eulerRot);
        }
    }
}
