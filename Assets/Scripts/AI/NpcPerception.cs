using UnityEngine;

namespace LabGame.AI
{
    /// <summary>
    /// «Зрение» NPC: расстояние, прямая видимость и главное —
    /// понимает ли игрок, что на него смотрят (направление камеры).
    /// </summary>
    public class NpcPerception : MonoBehaviour
    {
        [Header("Наблюдение")]
        public Transform player;

        [Tooltip("Камера игрока. Если пусто — берётся Camera.main.")]
        public Camera playerCamera;

        public float sightRange = 26f;

        [Tooltip("Угол «в поле зрения игрока», градусы.")]
        [Range(5f, 180f)] public float noticeHalfAngle = 42f;

        [Tooltip("Слои, которые перекрывают обзор.")]
        public LayerMask obstructionMask = ~0;

        public float Distance { get; private set; } = float.MaxValue;
        public bool HasLineOfSight { get; private set; }
        public bool PlayerLookingAtMe { get; private set; }

        private void Awake()
        {
            if (player == null)
            {
                var found = GameObject.FindWithTag("Player");
                if (found != null) player = found.transform;
            }

            if (playerCamera == null) playerCamera = Camera.main;
        }

        private void Update() => Evaluate();

        public void Evaluate()
        {
            if (player == null)
            {
                Distance = float.MaxValue;
                HasLineOfSight = false;
                PlayerLookingAtMe = false;
                return;
            }

            Distance = Vector3.Distance(transform.position, player.position);

            Vector3 npcEye = transform.position + Vector3.up * 1.4f;
            Vector3 playerChest = player.position + Vector3.up * 1.2f;

            HasLineOfSight = Distance <= sightRange && !IsBlocked(npcEye, playerChest);

            if (!HasLineOfSight)
            {
                PlayerLookingAtMe = false;
                return;
            }

            if (playerCamera == null) playerCamera = Camera.main;
            if (playerCamera == null)
            {
                PlayerLookingAtMe = false;
                return;
            }

            Vector3 cameraPosition = playerCamera.transform.position;
            Vector3 toNpc = (npcEye - cameraPosition).normalized;
            float angle = Vector3.Angle(playerCamera.transform.forward, toNpc);

            PlayerLookingAtMe = angle <= noticeHalfAngle && !IsBlocked(cameraPosition, npcEye);
        }

        private bool IsBlocked(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;

            if (distance < 0.01f) return false;

            return Physics.Raycast(from, direction / distance, distance - 0.2f, obstructionMask,
                QueryTriggerInteraction.Ignore);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = HasLineOfSight ? new Color(0.4f, 1f, 0.5f, 0.5f) : new Color(1f, 0.4f, 0.4f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, sightRange);
        }
    }
}
