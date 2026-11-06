using UnityEngine;

namespace LabGame.Vehicles
{
    /// <summary>
    /// Чистая математика управления техникой: поворот на цель, разгон,
    /// крен в вираже, угол набора высоты. Вынесена отдельно для тестов.
    /// </summary>
    public static class VehicleMath
    {
        /// <summary>Угол между направлением вперёд и целью в плоскости XZ, −180…180.</summary>
        public static float SignedAngleToTarget(Vector3 forward, Vector3 toTarget)
        {
            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
            Vector3 flatTarget = new Vector3(toTarget.x, 0f, toTarget.z);

            if (flatForward.sqrMagnitude < 0.0001f || flatTarget.sqrMagnitude < 0.0001f) return 0f;

            return Vector3.SignedAngle(flatForward, flatTarget, Vector3.up);
        }

        /// <summary>Плавное изменение скорости: разгон и торможение разными темпами.</summary>
        public static float MoveTowardsSpeed(float current, float target, float acceleration, float braking,
            float deltaTime)
        {
            bool speedingUp = Mathf.Abs(target) > Mathf.Abs(current);
            float rate = speedingUp ? acceleration : braking;

            return Mathf.MoveTowards(current, target, Mathf.Max(0f, rate) * Mathf.Max(0f, deltaTime));
        }

        /// <summary>
        /// Шаг поворота: на малой скорости машина почти не поворачивает,
        /// на полной — выходит на полную скорость поворота.
        /// </summary>
        public static float SteerStep(float angleToTarget, float turnRate, float speed, float minTurnSpeed,
            float deltaTime)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(0.01f, minTurnSpeed));
            float maxStep = Mathf.Max(0f, turnRate) * Mathf.Max(0f, deltaTime) * speedFactor;

            return Mathf.Clamp(angleToTarget, -maxStep, maxStep);
        }

        /// <summary>Крен в вираже: чем больше ошибка курса, тем сильнее крен.</summary>
        public static float BankForTurn(float yawErrorDegrees, float maxBank, float gain = 1.2f)
        {
            return Mathf.Clamp(-yawErrorDegrees * gain, -Mathf.Abs(maxBank), Mathf.Abs(maxBank));
        }

        /// <summary>Угол набора высоты к цели, градусы (положительный — вверх).</summary>
        public static float ClimbAngle(Vector3 position, Vector3 target)
        {
            Vector3 flat = new Vector3(target.x - position.x, 0f, target.z - position.z);
            float vertical = target.y - position.y;

            return Mathf.Atan2(vertical, Mathf.Max(0.001f, flat.magnitude)) * Mathf.Rad2Deg;
        }

        /// <summary>Достигнута ли точка.</summary>
        public static bool Reached(Vector3 position, Vector3 target, float reach)
        {
            return (position - target).sqrMagnitude <= reach * reach;
        }
    }
}
