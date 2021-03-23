using UnityEngine;
/// <summary>
/// Math Helper Functions
/// </summary>

namespace EnableEducation
{
    public static class EEMath
    {
        private const float defaultTolerance = 0.001f;

        public static bool Approximately(Vector3 v1, Vector3 v2, float tolerance = defaultTolerance)
        {
            return (
                Approximately(v1.x, v2.x, tolerance) &&
                Approximately(v1.y, v2.y, tolerance) &&
                Approximately(v1.z, v2.z, tolerance)
            );
        }

        public static bool Approximately(Quaternion q1, Quaternion q2, float tolerance = defaultTolerance)
        {
            return (
                Approximately(q1.x, q2.x, tolerance) &&
                Approximately(q1.y, q2.y, tolerance) &&
                Approximately(q1.z, q2.z, tolerance) &&
                Approximately(q1.w, q2.w, tolerance)
            );
        }

        public static bool Approximately(float f1, float f2, float tolerance = defaultTolerance)
        {
            return (Mathf.Abs(f1 - f2) < tolerance);
        }

        // We compare two rotation axis values, while taking into consideration its 
        public static bool ApproximatelyRotationAxis(float f1, float f2, float tolerance = defaultTolerance)
        {
            float deltaAngle = Mathf.DeltaAngle(f1, f2);
            return (Mathf.Abs(deltaAngle) < tolerance);
        }

        // Returns the weighted center point of a number of points
        public static Vector3 WeightedCenter(Vector3[] points)
        {
            Vector3 total = Vector3.zero;
            foreach(Vector3 vec in points)
            {
                total += vec;
            }

            return total / points.Length;
        }
    }
}
