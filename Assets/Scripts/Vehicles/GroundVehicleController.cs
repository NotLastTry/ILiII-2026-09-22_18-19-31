using UnityEngine;

namespace LabGame.Vehicles
{
    /// <summary>
    /// Контроллер наземной техники (ПЗ №7).
    ///
    /// Машина едет по маршруту, объезжает препятствия, замечает игрока
    /// и переходит к преследованию, а потеряв его — возвращается к маршруту.
    /// Кузов всегда остаётся ровным, меняется только высота над дорогой.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundVehicleController : MonoBehaviour
    {
        public enum VehicleMode
        {
            Patrol = 0,
            Waiting = 1,
            Chase = 2
        }

        [Header("Маршрут")]
        public Transform[] waypoints;
        public float waypointReach = 4f;
        public float waypointWait = 1.5f;

        [Header("Движение")]
        public float patrolSpeed = 7f;
        public float chaseSpeed = 12f;
        public float acceleration = 6f;
        public float braking = 12f;
        public float turnRate = 85f;
        public float minTurnSpeed = 1.5f;

        [Header("Погоня")]
        public float detectRadius = 16f;
        public float chaseStopDistance = 7f;

        [Header("Дорога")]
        [Tooltip("Автоматически подобрать высоту посадки по габаритам модели.")]
        public bool autoRideHeight = true;

        public float rideHeight = 0.15f;
        public float groundRayLength = 10f;
        public LayerMask groundMask = ~0;

        [Header("Объезд препятствий")]
        public float avoidDistance = 8f;
        public float avoidHalfWidth = 1.2f;
        public float avoidStrength = 60f;

        [Header("Модель")]
        [Tooltip("Доворот модели относительно корня, если она смотрит не вперёд. " +
                 "Движение всегда идёт по forward корня; меняется только вид.")]
        public float modelYawOffset = 180f;

        public VehicleMode Mode { get; private set; } = VehicleMode.Patrol;

        public float Speed => speed;

        public float TargetSpeed { get; private set; }

        private float speed;
        private float waitTimer;
        private int waypointIndex;
        private Transform player;
        private float resolvedRideHeight;
        private readonly RaycastHit[] hits = new RaycastHit[8];

        private Transform model;
        private Quaternion modelBaseRotation;
        private float appliedYaw;

        private void Awake()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null) player = playerObject.transform;

            resolvedRideHeight = rideHeight;

            if (autoRideHeight) ResolveRideHeight();

            CacheModel();
            UpdateModelOffset();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            UpdateMode(dt);
            UpdateTargetSpeed();
            UpdateSteering(dt);

            transform.position += transform.forward * speed * dt;

            SnapToGround();
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
        /// Разворачивает только визуальную модель, не трогая направление движения.
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

        private void UpdateMode(float dt)
        {
            if (player != null &&
                Vector3.Distance(transform.position, player.position) <= detectRadius)
            {
                Mode = VehicleMode.Chase;
                waitTimer = 0f;
                return;
            }

            if (Mode == VehicleMode.Chase)
            {
                // Потеряли игрока — возвращаемся к маршруту.
                Mode = VehicleMode.Patrol;
            }

            if (Mode == VehicleMode.Waiting)
            {
                waitTimer -= dt;

                if (waitTimer <= 0f)
                {
                    Mode = VehicleMode.Patrol;
                    waypointIndex = NextWaypoint();
                }

                return;
            }

            if (waypoints != null && waypoints.Length > 0 &&
                VehicleMath.Reached(transform.position, CurrentWaypoint(), waypointReach))
            {
                Mode = VehicleMode.Waiting;
                waitTimer = waypointWait;
            }
        }

        private void UpdateTargetSpeed()
        {
            if (Mode == VehicleMode.Waiting)
            {
                TargetSpeed = 0f;
                return;
            }

            if (Mode == VehicleMode.Chase)
            {
                bool close = player != null &&
                             Vector3.Distance(transform.position, player.position) <= chaseStopDistance;

                TargetSpeed = close ? 0f : chaseSpeed;
                return;
            }

            TargetSpeed = patrolSpeed;
        }

        private void UpdateSteering(float dt)
        {
            speed = VehicleMath.MoveTowardsSpeed(speed, TargetSpeed, acceleration, braking, dt);

            Vector3 target = Mode == VehicleMode.Chase && player != null
                ? player.position
                : CurrentWaypoint();

            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;

            float angle = VehicleMath.SignedAngleToTarget(transform.forward, toTarget);
            float avoid = ComputeAvoidance();

            float normalized = Mathf.Clamp((angle + avoid) / 90f, -1f, 1f);
            float maxStep = turnRate * dt * Mathf.Clamp01(Mathf.Abs(speed) / Mathf.Max(0.01f, minTurnSpeed));

            transform.Rotate(0f, normalized * maxStep, 0f, Space.World);
        }

        // ─────────────────────────────────────────────────────────────
        //  Высота над землёй
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Держит машину на высоте дороги. Кузов при этом остаётся ровным:
        /// наклон по рельефу намеренно не применяется.
        /// </summary>
        private void SnapToGround()
        {
            if (!TryRaycastGround(out RaycastHit hit)) return;

            Vector3 position = transform.position;
            position.y = hit.point.y + resolvedRideHeight;
            transform.position = position;
        }

        private void ResolveRideHeight()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            resolvedRideHeight = Mathf.Max(0f, transform.position.y - bounds.min.y);
        }

        private bool TryRaycastGround(out RaycastHit ground)
        {
            ground = default;

            Vector3 origin = transform.position + Vector3.up * 2f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits,
                groundRayLength + 2f, groundMask, QueryTriggerInteraction.Ignore);

            float best = float.MaxValue;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.transform.IsChildOf(transform)) continue;
                if (hits[i].distance >= best) continue;

                best = hits[i].distance;
                ground = hits[i];
                found = true;
            }

            return found;
        }

        // ─────────────────────────────────────────────────────────────
        //  Препятствия
        // ─────────────────────────────────────────────────────────────

        private float ComputeAvoidance()
        {
            Vector3 origin = transform.position + Vector3.up * 0.6f;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            float bias = 0f;

            if (Blocked(origin - right * avoidHalfWidth, forward)) bias += avoidStrength;
            if (Blocked(origin + right * avoidHalfWidth, forward)) bias -= avoidStrength;
            if (Blocked(origin, forward)) bias += avoidStrength * 0.5f;

            return bias;
        }

        private bool Blocked(Vector3 origin, Vector3 direction)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, hits, avoidDistance,
                groundMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
                if (!hits[i].collider.transform.IsChildOf(transform))
                    return true;

            return false;
        }

        // ─────────────────────────────────────────────────────────────
        //  Маршрут
        // ─────────────────────────────────────────────────────────────

        public Vector3 CurrentWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return transform.position;

            int index = Mathf.Clamp(waypointIndex, 0, waypoints.Length - 1);
            var point = waypoints[index];

            return point != null ? point.position : transform.position;
        }

        private int NextWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return 0;

            return (waypointIndex + 1) % waypoints.Length;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, detectRadius);

            if (waypoints == null) return;

            Gizmos.color = new Color(0.9f, 0.9f, 0.2f, 0.9f);

            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;

                Gizmos.DrawSphere(waypoints[i].position, 0.6f);

                var next = waypoints[(i + 1) % waypoints.Length];
                if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
            }
        }
    }
}
