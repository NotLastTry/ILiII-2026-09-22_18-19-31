using System.Collections.Generic;
using UnityEngine;

namespace LabGame.AI
{
    /// <summary>
    /// Управляет толпой: собирает участников, раздаёт им соседей
    /// и запускает панику по событию (например, когда противник бьёт игрока).
    /// </summary>
    public class CrowdManager : MonoBehaviour
    {
        public static CrowdManager Instance { get; private set; }

        [Header("Поведение")]
        public CrowdSettings settings = new CrowdSettings();

        [Header("Реакция на события")]
        [Tooltip("Радиус, в котором событие пугает толпу.")]
        public float panicRadius = 14f;

        [Tooltip("Сколько секунд длится паника.")]
        public float panicDuration = 5f;

        [Header("Любопытство")]
        [Tooltip("В этом радиусе часть толпы подходит к интересному объекту.")]
        public float interestRadius = 9f;

        [Tooltip("К чему тянется толпа. Если пусто — ищется игрок.")]
        public Transform pointOfInterest;

        private readonly List<CrowdAgent> agents = new List<CrowdAgent>();

        private Vector3[] positions = new Vector3[0];
        private Vector3[] velocities = new Vector3[0];
        private int snapshotFrame = -1;

        private Vector3 threatPosition;
        private float threatTimer;

        public IReadOnlyList<CrowdAgent> Agents => agents;
        public CrowdSettings Settings => settings;

        public bool HasThreat => threatTimer > 0f;
        public Vector3 ThreatPosition => threatPosition;

        public Transform PointOfInterest => pointOfInterest;
        public float InterestRadius => interestRadius;
        public float PanicRadius => panicRadius;

        private void Awake()
        {
            Instance = this;

            agents.AddRange(Object.FindObjectsByType<CrowdAgent>(FindObjectsInactive.Exclude));

            for (int i = 0; i < agents.Count; i++)
                agents[i].AssignIndex(i);

            if (pointOfInterest == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) pointOfInterest = player.transform;
            }

            positions = new Vector3[agents.Count];
            velocities = new Vector3[agents.Count];
        }

        private void OnEnable() => GameEventBus.Disturbance += OnDisturbance;

        private void OnDisable() => GameEventBus.Disturbance -= OnDisturbance;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (threatTimer > 0f) threatTimer -= Time.deltaTime;

            // Демонстрация: H — напугать толпу без ожидания драки.
            if (Input.GetKeyDown(KeyCode.H))
            {
                Vector3 target = pointOfInterest != null ? pointOfInterest.position : transform.position;
                Alarm(target);
            }
        }

        /// <summary>Испугать толпу в указанной точке.</summary>
        public void Alarm(Vector3 position)
        {
            threatPosition = position;
            threatTimer = Mathf.Max(threatTimer, panicDuration);
        }

        private void OnDisturbance(Vector3 position) => Alarm(position);

        /// <summary>
        /// Снимок позиций и скоростей всей толпы на текущий кадр.
        /// Считается один раз, чтобы участники не обходили друг друга повторно.
        /// </summary>
        public void GetSnapshot(out Vector3[] snapshotPositions, out Vector3[] snapshotVelocities, out int count)
        {
            count = agents.Count;

            if (snapshotFrame != Time.frameCount)
            {
                snapshotFrame = Time.frameCount;

                if (positions.Length != count) positions = new Vector3[count];
                if (velocities.Length != count) velocities = new Vector3[count];

                for (int i = 0; i < count; i++)
                {
                    var agent = agents[i];

                    positions[i] = agent != null ? agent.transform.position : Vector3.zero;
                    velocities[i] = agent != null ? agent.DesiredVelocity : Vector3.zero;
                }
            }

            snapshotPositions = positions;
            snapshotVelocities = velocities;
        }
    }
}
