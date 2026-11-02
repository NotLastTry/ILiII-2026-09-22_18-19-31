using LabGame.Items;
using UnityEngine;
using UnityEngine.AI;

namespace LabGame.AI
{
    /// <summary>
    /// Мост между «мозгом» NPC и Unity: NavMeshAgent, аниматор, звук.
    /// Действия не знают про Unity-компоненты напрямую — только про IChaosActor.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ChaosAgent : MonoBehaviour, IChaosActor
    {
        [Header("Ссылки")]
        public Transform player;

        [Tooltip("Предметы, которые NPC может подбросить как «подарок»-обманку.")]
        public ItemDefinition[] fakeGiftItems;

        [Header("Движение")]
        public float walkSpeed = 3.6f;
        public float runSpeed = 5.2f;

        private NavMeshAgent nav;
        private Animator animator;
        private System.Random rng;

        private readonly System.Collections.Generic.HashSet<string> boolParameters =
            new System.Collections.Generic.HashSet<string>();

        private readonly System.Collections.Generic.HashSet<string> triggerParameters =
            new System.Collections.Generic.HashSet<string>();

        private readonly System.Collections.Generic.HashSet<string> missingTriggers =
            new System.Collections.Generic.HashSet<string>();

        public System.Random Rng => rng ?? (rng = new System.Random(UnityEngine.Random.Range(1, int.MaxValue)));

        public Vector3 Position => transform.position;

        public Vector3 PlayerPosition => player != null ? player.position : transform.position;

        public Vector3 PlayerForward
        {
            get
            {
                var camera = Camera.main;
                if (camera != null) return camera.transform.forward;
                return player != null ? player.forward : Vector3.forward;
            }
        }

        public bool IsMoving
        {
            get
            {
                if (nav == null || !nav.isOnNavMesh) return false;
                if (nav.pathPending) return true;
                if (!nav.hasPath) return false;
                return nav.remainingDistance > nav.stoppingDistance + 0.2f;
            }
        }

        /// <summary>Кто произносит реплику — подписывается ChaoticNPC.</summary>
        public System.Action<string, float> SpeechSink;

        private void Awake()
        {
            nav = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            rng = new System.Random(UnityEngine.Random.Range(1, int.MaxValue));

            CacheAnimatorParameters();
        }

        /// <summary>
        /// Animator.SetBool/SetTrigger на несуществующем параметре пишет
        /// предупреждение в консоль каждый вызов. Кэшируем список параметров,
        /// чтобы не засорять лог на каждом кадре.
        /// </summary>
        private void CacheAnimatorParameters()
        {
            boolParameters.Clear();
            triggerParameters.Clear();

            if (animator == null) return;

            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Bool)
                    boolParameters.Add(parameter.name);
                else if (parameter.type == AnimatorControllerParameterType.Trigger)
                    triggerParameters.Add(parameter.name);
            }
        }

        public void SetSeed(int seed)
        {
            rng = new System.Random(seed);
        }

        public void MoveTo(Vector3 worldPosition)
        {
            if (nav == null || !nav.isOnNavMesh) return;

            if (!NavMesh.SamplePosition(worldPosition, out var hit, 4f, NavMesh.AllAreas)) return;

            nav.isStopped = false;
            nav.speed = walkSpeed;
            nav.SetDestination(hit.position);

            SetWalking(true);
        }

        public void SprintTo(Vector3 worldPosition)
        {
            if (nav == null || !nav.isOnNavMesh) return;

            if (!NavMesh.SamplePosition(worldPosition, out var hit, 4f, NavMesh.AllAreas)) return;

            nav.isStopped = false;
            nav.speed = runSpeed;
            nav.SetDestination(hit.position);

            SetWalking(true);
        }

        public void Stop()
        {
            if (nav != null && nav.isOnNavMesh)
            {
                nav.isStopped = true;
                if (nav.hasPath) nav.ResetPath();
            }

            SetWalking(false);
        }

        public void FaceTowards(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.01f) return;

            Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 10f);
        }

        public void PlayTrigger(string triggerName)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName)) return;

            if (triggerParameters.Contains(triggerName))
            {
                animator.SetTrigger(triggerName);
                return;
            }

            // В контроллере NPC из ПЗ №6 такого триггера нет — сообщаем один раз.
            if (missingTriggers.Add(triggerName))
                Debug.Log($"[ChaosAgent] в аниматоре нет триггера «{triggerName}» — действие выполняется без анимации");
        }

        public void Say(string text, float duration = 3f)
        {
            if (string.IsNullOrEmpty(text)) return;
            SpeechSink?.Invoke(text, duration);
        }

        public bool TryRandomPoint(Vector3 origin, float radius, out Vector3 point)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                Vector3 candidate = origin + ChaosActions.RandomFlatDirection(Rng) * (radius * (float)Rng.NextDouble());

                if (NavMesh.SamplePosition(candidate, out var hit, 3f, NavMesh.AllAreas))
                {
                    point = hit.position;
                    return true;
                }
            }

            if (NavMesh.SamplePosition(origin, out var fallback, 3f, NavMesh.AllAreas))
            {
                point = fallback.position;
                return true;
            }

            point = origin;
            return false;
        }

        public void Teleport(Vector3 worldPosition)
        {
            if (nav == null) return;

            if (nav.isOnNavMesh) nav.Warp(worldPosition);
            else transform.position = worldPosition;
        }

        public void SpawnFakeGift(Vector3 worldPosition)
        {
            if (fakeGiftItems == null || fakeGiftItems.Length == 0)
            {
                Debug.LogWarning("[ChaosAgent] не заданы предметы для обманок");
                return;
            }

            var item = fakeGiftItems[Rng.Next(fakeGiftItems.Length)];
            var pickup = WorldItemSpawner.Spawn(item, 1, worldPosition + Vector3.up * 0.2f);
            if (pickup != null) pickup.Setup(item, 1, trap: true);
        }

        private void SetWalking(bool walking)
        {
            if (animator == null) return;

            // В аниматоре NPC из ПЗ №6 используется параметр isWalking,
            // но имя может отличаться — ставим только существующие.
            if (boolParameters.Contains("isWalking")) animator.SetBool("isWalking", walking);
            if (boolParameters.Contains("Walking")) animator.SetBool("Walking", walking);
        }
    }
}
