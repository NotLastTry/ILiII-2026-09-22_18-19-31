using UnityEngine;
using UnityEngine.AI;

namespace LabGame.AI
{
    /// <summary>
    /// Участник толпы: считает стайные силы (разделение, выравнивание,
    /// сплочённость) и ведёт себя по ним через NavMeshAgent.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CrowdAgent : MonoBehaviour
    {
        [Header("Личное поведение")]
        [Tooltip("Радиус спокойного блуждания вокруг стартовой точки.")]
        public float wanderRadius = 6f;

        [Tooltip("Насколько далеко агент смотрит вперёд при выборе точки назначения.")]
        public float lookAhead = 3f;

        public int Index { get; private set; } = -1;

        public CrowdMode Mode { get; private set; } = CrowdMode.Calm;

        public Vector3 HomePosition { get; private set; }

        public Vector3 DesiredVelocity => desiredVelocity;

        private NavMeshAgent agent;
        private Animator animator;

        private Vector3 desiredVelocity;
        private Vector3 wanderTarget;
        private float wanderTimer;
        private float repathTimer;
        private float speedFactor = 1f;
        private bool curiousTrait = true;
        private bool hasWalkParameter;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();

            HomePosition = transform.position;
            speedFactor = Random.Range(0.85f, 1.15f);
            curiousTrait = Random.value < 0.65f;

            hasWalkParameter = HasParameter(animator, "isWalking");

            PickWanderTarget();
        }

        public void AssignIndex(int index) => Index = index;

        private void Update()
        {
            var manager = CrowdManager.Instance;
            if (manager == null || agent == null || !agent.isOnNavMesh) return;

            Mode = ResolveMode(manager);

            wanderTimer -= Time.deltaTime;

            if (wanderTimer <= 0f ||
                (Mode == CrowdMode.Calm && Vector3.Distance(transform.position, wanderTarget) < 1.5f))
            {
                PickWanderTarget();
            }

            var context = new CrowdContext
            {
                Position = transform.position,
                Velocity = desiredVelocity,
                Home = HomePosition,
                WanderTarget = wanderTarget,
                Mode = Mode,
                HasThreat = manager.HasThreat,
                ThreatPosition = manager.ThreatPosition,
                HasInterest = manager.PointOfInterest != null,
                InterestPosition = manager.PointOfInterest != null
                    ? manager.PointOfInterest.position
                    : transform.position
            };

            manager.GetSnapshot(out var positions, out var velocities, out int count);

            Vector3 force = CrowdSteering.Compute(context, positions, velocities, count, Index, manager.Settings);

            float targetSpeed = (Mode == CrowdMode.Panic ? manager.Settings.panicSpeed : manager.Settings.maxSpeed) * speedFactor;

            Vector3 direction = force.sqrMagnitude > 0.0001f ? force.normalized : Vector3.zero;
            desiredVelocity = Vector3.Lerp(desiredVelocity, direction * targetSpeed, 6f * Time.deltaTime);

            Steer(targetSpeed, direction);
            UpdateAnimator();
        }

        private void Steer(float targetSpeed, Vector3 direction)
        {
            agent.speed = Mathf.Max(0.2f, targetSpeed);

            repathTimer -= Time.deltaTime;
            if (repathTimer > 0f) return;

            repathTimer = 0.2f;

            if (direction.sqrMagnitude < 0.0001f)
            {
                if (agent.hasPath) agent.ResetPath();
                return;
            }

            Vector3 ahead = transform.position + direction * lookAhead;

            if (NavMesh.SamplePosition(ahead, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        private CrowdMode ResolveMode(CrowdManager manager)
        {
            if (manager.HasThreat &&
                Vector3.Distance(transform.position, manager.ThreatPosition) <= manager.PanicRadius)
            {
                return CrowdMode.Panic;
            }

            var interest = manager.PointOfInterest;

            if (curiousTrait && interest != null &&
                Vector3.Distance(transform.position, interest.position) <= manager.InterestRadius)
            {
                return CrowdMode.Curious;
            }

            return CrowdMode.Calm;
        }

        private void PickWanderTarget()
        {
            Vector2 circle = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = HomePosition + new Vector3(circle.x, 0f, circle.y);

            wanderTarget = NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas)
                ? hit.position
                : HomePosition;

            wanderTimer = Random.Range(4f, 9f);
        }

        private void UpdateAnimator()
        {
            if (!hasWalkParameter || animator == null) return;

            bool walking = agent != null && agent.velocity.sqrMagnitude > 0.04f;
            animator.SetBool("isWalking", walking);
        }

        private static bool HasParameter(Animator target, string parameter)
        {
            if (target == null) return false;

            foreach (var item in target.parameters)
                if (item.type == AnimatorControllerParameterType.Bool && item.name == parameter)
                    return true;

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 0.4f, 0.5f);
            Gizmos.DrawWireSphere(HomePosition, wanderRadius);

            Gizmos.color = new Color(0.9f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + desiredVelocity);
        }
    }
}
