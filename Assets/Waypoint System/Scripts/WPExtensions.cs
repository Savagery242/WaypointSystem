using UnityEngine;
using System.Collections;

namespace Waypoints
{
    public static class WPExtensions
    {
        /// <summary>
        /// Raise input to exponent
        /// </summary>
        /// <param name="input"></param>
        /// <param name="exponent"></param>
        /// <returns></returns>
        public static float Pow(this float input, int exponent)
        {
            float r = input;
            for (int k = 1; k < exponent; k++)
            {
                r *= input;
            }
            return r;
        }
        /// <summary>
        /// No negative square roots
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static float SafeSqrt(this float i)
        {
            return (i > 0) ? Mathf.Sqrt(i) : -Mathf.Sqrt(-i);
        }
        /// <summary>
        /// Squares but keeps sign (squaring negative returns negative number)
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static float SqKeepSign(this float i)
        {
            return (i > 0) ? i * i : -(i * i);
        }
    }
}
