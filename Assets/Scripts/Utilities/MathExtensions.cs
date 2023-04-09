using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Scripting;

namespace OVP.Utilities
{
    public static class MathExtensions
    {
        public const float RPM_TO_RAD_CONSTANT = (2.0f * Mathf.PI) / 60.0f;
        public const float RAD_TO_RPM_CONSTANT = 60.0f / (2.0f * Mathf.PI);

        /// <summary>
        /// Converts a value from revolutions per minute (RPM) to radians per second.
        /// </summary>
        /// <param name="rpm">The value in RPM to be converted.</param>
        /// <returns>The value in radians per second.</returns>
        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RPMToRad(float rpm)
        {
            return rpm * RPM_TO_RAD_CONSTANT;
        }

        /// <summary>
        /// Converts a value from radians per second to revolutions per minute (RPM).
        /// </summary>
        /// <param name="rad">The value in radians per second to be converted.</param>
        /// <returns>The value in RPM.</returns>
        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RadToRPM(float rad)
        {
            return rad * RAD_TO_RPM_CONSTANT;
        }

        /// <summary>
        /// Maps a value from one range to another with clamping.
        /// </summary>
        /// <param name="value">The value to be mapped.</param>
        /// <param name="inRangeA">The lower bound of the input range.</param>
        /// <param name="inRangeB">The upper bound of the input range.</param>
        /// <param name="outRangeA">The lower bound of the output range.</param>
        /// <param name="outRangeB">The upper bound of the output range.</param>
        /// <returns>The mapped value clamped to the output range.</returns>
        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB)
        {
            return Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        }

        /// <summary>
        /// Safely divides two floats by checking if the divisor is zero before dividing.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <returns>The result of the division, or zero if the divisor is zero.</returns>
        [Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float SafeDivide(float dividend, float divisor)
        {
            return divisor != 0.0f ? dividend / divisor : 0.0f;
        }
    }
}