using UnityEngine;

namespace LabGame.Controls
{
    /// <summary>Математика джойстика, вынесенная отдельно для тестов.</summary>
    public static class JoystickMath
    {
        /// <summary>Ограничивает вектор кругом заданного радиуса.</summary>
        public static Vector2 ClampToCircle(Vector2 value, float radius)
        {
            if (radius <= 0f) return Vector2.zero;

            float maxSqr = radius * radius;
            if (value.sqrMagnitude <= maxSqr) return value;

            return value.normalized * radius;
        }

        /// <summary>Убирает «мёртвую зону» и растягивает оставшийся диапазон до 0…1.</summary>
        public static Vector2 ApplyDeadZone(Vector2 normalizedValue, float deadZone)
        {
            float magnitude = normalizedValue.magnitude;
            if (magnitude <= deadZone) return Vector2.zero;

            float scaled = Mathf.InverseLerp(deadZone, 1f, Mathf.Min(magnitude, 1f));
            return normalizedValue.normalized * scaled;
        }
    }
}
