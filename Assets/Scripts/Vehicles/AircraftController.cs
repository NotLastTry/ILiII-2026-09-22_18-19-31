using UnityEngine;

namespace LabGame.Vehicles
{
    /// <summary>
    /// Контроллер авиационной техники (ПЗ №8).
    ///
    /// Самолёт летит по маршруту на заданной высоте, кренится в виражах,
    /// уходит вверх от земли и переходит к преследованию игрока,
    /// когда тот оказывается в радиусе обнаружения.
    /// </summary>
    [DisallowMultipleComponent]
    public class AircraftController : MonoBehaviour
    {
        public enum FlightMode
        {
            Patrol = 0,
            Chase = 1
        }

        [Header("Маршрут")]
        public Transform[] waypoints;
        public float waypointReach = 12f;

        [Header("Полёт")]
        public float patrolSpeed = 16f;
        public float chaseSpeed = 24f;
        public float acceleration = 6f;
        public float turnSmoothness = 2.2f;
        public float maxBank = 50f;

        [Header("Высота")]
        public float cruiseAltitude = 16f;
        public float minClearance = 6f;
        public LayerMask groundMask = ~0;

        [Header("Погоня")]
        public float detectRadius = 45f;
        public float attackAltitude = 10f;

        [Header("Модель")]
        [Tooltip("Доворот модели относительно корня, если она смотрит не вперёд. " +
                 "Полёт всегда идёт по forward корня; меняется только вид.")]
        public float modelYawOffset = 180f;

        public FlightMode Mode { get; private set; } = FlightMode.Patrol;

        public float Speed => speed;

        public float Altitude => transform.position.y;

        public float TargetSpeed { get; private set; }

        private float speed;
        private int waypointIndex;
        private Transform player;
        private readonly RaycastHit[] hits = new RaycastHit[8];

        private Transform model;
        private Quaternion modelBaseRotation;
        private float appliedYaw;

        private void Awake()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) player = playerObject.transform;

            CacheModel();
            UpdateModelOffset();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            UpdateMode();

            Vector3 target = ApplyAltitudeLimits(CurrentTarget());

            Steer(target, dt);

            TargetSpeed = Mode == FlightMode.Chase ? chaseSpeed : patrolSpeed;
            speed = VehicleMath.MoveTowardsSpeed(speed, TargetSpeed, acceleration, acceleration, dt);

            transform.position += transform.forward * speed * dt;

            UpdateModelOffset();
        }

        // ─────────────────────────────────────────────────────────────
        //  Доворот модели
        // ─────────────────────────────────────────────────────────────

        private void CacheModel()
        {
            if (transform.childCount == 0) return;

            model = transform.GetChild(0);
            modelBaseRotation = model.localRotation;
            appliedYaw = float.NaN;
        }

        /// <summary>
        /// Разворачивает только визуальную модель, не трогая направление полёта.
        /// Угол можно менять в инспекторе, в том числе на ходу.
        /// </summary>
        private void UpdateModelOffset()
        {
            if (model == null) return;

            // На ходу: [ и ] поворачивают модель шагом 90°.
            if (Input.GetKeyDown(KeyCode.LeftBracket)) modelYawOffset -= 90f;
            if (Input.GetKeyDown(KeyCode.RightBracket)) modelYawOffset += 90f;

            if (!float.IsNaN(appliedYaw) && Mathf.Approximately(appliedYaw, modelYawOffset)) return;

            appliedYaw = modelYawOffset;

            model.localRotation = Quaternion.Euler(0f, appliedYaw, 0f) * modelBaseRotation;
        }

        // ─────────────────────────────────────────────────────────────
        //  Поведение
        // ─────────────────────────────────────────────────────────────

        private void UpdateMode()
        {
            if (player != null && Vector3.Distance(transform.position, player.position) <= detectRadius)
            {
                Mode = FlightMode.Chase;
                return;
            }

            Mode = FlightMode.Patrol;

            if (waypoints == null || waypoints.Length == 0) return;

            if (VehicleMath.Reached(transform.position, CurrentWaypoint(), waypointReach))
                waypointIndex = (waypointIndex + 1) % waypoints.Length;
        }

        private Vector3 CurrentTarget()
        {
            if (Mode == FlightMode.Chase && player != null)
                return player.position + Vector3.up * attackAltitude;

            if (waypoints != null && waypoints.Length > 0)
            {
                var point = waypoints[Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1)];
                if (point != null) return point.position;
            }

            // Без маршрута — вперёд с набором крейсерской высоты.
            Vector3 forwardPoint = transform.position + transform.forward * 60f;
            return new Vector3(forwardPoint.x, cruiseAltitude, forwardPoint.z);
        }

        private Vector3 ApplyAltitudeLimits(Vector3 target)
        {
            float groundY = GroundHeight();
            float minY = groundY + minClearance;

            if (target.y < minY) target.y = minY;

            // Опасно низко — уводим цель выше, чтобы самолёт набрал высоту.
            if (transform.position.y < minY) target.y = Mathf.Max(target.y, minY + 5f);

            return target;
        }

        private void Steer(Vector3 target, float dt)
        {
            Vector3 direction = target - transform.position;
            if (direction.sqrMagnitude < 0.01f) return;

            Quaternion look = Quaternion.LookRotation(direction.normalized, Vector3.up);

            float yawError = VehicleMath.SignedAngleToTarget(transform.forward, direction);
            float bank = VehicleMath.BankForTurn(yawError, maxBank);

            // Крен добавляется в локальной системе — самолёт «закладывает вираж».
            Quaternion banked = look * Quaternion.Euler(0f, 0f, bank);

            transform.rotation = Quaternion.Slerp(transform.rotation, banked, turnSmoothness * dt);
        }

        private float GroundHeight()
        {
            int count = Physics.RaycastNonAlloc(transform.position + Vector3.up * 2f, Vector3.down, hits,
                200f, groundMask, QueryTriggerInteraction.Ignore);

            float best = 0f;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;

                if (!found || hits[i].point.y > best)
                {
                    best = hits[i].point.y;
                    found = true;
                }
            }

            return best;
        }

        public Vector3 CurrentWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return transform.position;

            int index = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
            var point = waypoints[index];

            return point != null ? point.position : transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, detectRadius);

            if (waypoints == null) return;

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;

                Gizmos.DrawSphere(waypoints[i].position, 1f);
                Gizmos.DrawLine(transform.position, waypoints[i].position);

                var next = waypoints[(i + 1) % waypoints.Length];
                if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
            }
        }
    }
}
