using UnityEngine;

namespace LabGame.AI
{
    /// <summary>Всё, что участник толпы знает о мире на текущий кадр.</summary>
    public struct CrowdContext
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Home;
        public Vector3 WanderTarget;

        public CrowdMode Mode;

        public Vector3 ThreatPosition;
        public bool HasThreat;

        public Vector3 InterestPosition;
        public bool HasInterest;
    }

    /// <summary>
    /// Чистая математика стайного поведения (boids):
    /// разделение, выравнивание, сплочённость плюс цели.
    /// Не зависит от сцены — покрывается юнит-тестами.
    /// </summary>
    public static class CrowdSteering
    {
        /// <summary>Единичное направление к цели.</summary>
        public static Vector3 Seek(Vector3 position, Vector3 target)
        {
            Vector3 direction = target - position;
            direction.y = 0f;

            return direction.sqrMagnitude < 0.0001f ? Vector3.zero : direction.normalized;
        }

        /// <summary>Единичное направление от угрозы.</summary>
        public static Vector3 Flee(Vector3 position, Vector3 threat)
        {
            return -Seek(position, threat);
        }

        /// <summary>
        /// Расталкивание: чем ближе сосед, тем сильнее отталкивание.
        /// Суммируется по всем соседям в радиусе <paramref name="radius"/>.
        /// </summary>
        public static Vector3 Separation(Vector3 position, Vector3[] neighbors, int count, int self, float radius)
        {
            Vector3 force = Vector3.zero;

            if (neighbors == null || radius <= 0f) return force;

            for (int i = 0; i < count && i < neighbors.Length; i++)
            {
                if (i == self) continue;

                Vector3 offset = position - neighbors[i];
                offset.y = 0f;

                float distance = offset.magnitude;
                if (distance < 0.001f || distance >= radius) continue;

                force += offset.normalized * (1f - distance / radius);
            }

            return force;
        }

        /// <summary>Выравнивание: средняя скорость соседей.</summary>
        public static Vector3 Alignment(Vector3[] velocities, int count, int self, float radius,
            Vector3[] neighbors, Vector3 position)
        {
            Vector3 sum = Vector3.zero;
            int found = 0;

            if (velocities == null || neighbors == null) return sum;

            for (int i = 0; i < count && i < velocities.Length; i++)
            {
                if (i == self) continue;
                if ((neighbors[i] - position).sqrMagnitude > radius * radius) continue;

                sum += velocities[i];
                found++;
            }

            if (found == 0) return Vector3.zero;

            sum /= found;
            return sum.sqrMagnitude < 0.0001f ? Vector3.zero : sum.normalized;
        }

        /// <summary>Сплочённость: направление к центру группы.</summary>
        public static Vector3 Cohesion(Vector3 position, Vector3[] neighbors, int count, int self, float radius)
        {
            Vector3 sum = Vector3.zero;
            int found = 0;

            if (neighbors == null) return Vector3.zero;

            for (int i = 0; i < count && i < neighbors.Length; i++)
            {
                if (i == self) continue;
                if ((neighbors[i] - position).sqrMagnitude > radius * radius) continue;

                sum += neighbors[i];
                found++;
            }

            if (found == 0) return Vector3.zero;

            sum /= found;
            return Seek(position, sum);
        }

        /// <summary>Итоговое ускорение участника толпы.</summary>
        public static Vector3 Compute(CrowdContext context, Vector3[] positions, Vector3[] velocities,
            int count, int self, CrowdSettings settings)
        {
            if (settings == null) return Vector3.zero;

            Vector3 separation = Separation(context.Position, positions, count, self, settings.separationRadius);
            Vector3 alignment = Alignment(velocities, count, self, settings.neighborRadius, positions, context.Position);
            Vector3 cohesion = Cohesion(context.Position, positions, count, self, settings.neighborRadius);

            Vector3 force = separation * settings.separationWeight +
                            alignment * settings.alignmentWeight +
                            cohesion * settings.cohesionWeight;

            switch (context.Mode)
            {
                case CrowdMode.Panic when context.HasThreat:
                    force += Flee(context.Position, context.ThreatPosition) * settings.panicWeight;
                    break;

                case CrowdMode.Curious when context.HasInterest:
                {
                    Vector3 toInterest = context.InterestPosition - context.Position;
                    toInterest.y = 0f;

                    // Подходит, только пока не вошёл в личную зону.
                    if (toInterest.magnitude > settings.personalSpace)
                        force += toInterest.normalized * settings.interestWeight;

                    break;
                }

                default:
                    force += Seek(context.Position, context.WanderTarget) * settings.seekWeight;
                    break;
            }

            // Слабое притяжение к дому, чтобы толпа не расползалась по карте.
            force += Seek(context.Position, context.Home) * settings.homeWeight;

            force.y = 0f;
            return Vector3.ClampMagnitude(force, settings.maxForce);
        }
    }
}
