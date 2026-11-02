using LabGame.Items;
using UnityEngine;

namespace LabGame
{
    /// <summary>
    /// Предмет, лежащий в мире. Игрок подходит и нажимает E.
    /// Крутится и «покачивается», чтобы его было видно на земле.
    /// </summary>
    public class ItemPickup : MonoBehaviour, IInteractable
    {
        [Header("Содержимое")]
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount = 1;

        [Tooltip("Обманка хаотичного NPC: выглядит как предмет, но ничего не даёт.")]
        [SerializeField] private bool isTrap;

        [Header("Анимация")]
        [SerializeField] private float spinSpeed = 55f;
        [SerializeField] private float bobAmplitude = 0.1f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 basePosition;
        private float phase;

        public ItemDefinition Item => item;
        public int Amount => amount;
        public bool IsTrap => isTrap;

        public Vector3 InteractionPoint => transform.position;

        private void Start()
        {
            basePosition = transform.position;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void OnEnable() => InteractableRegistry.Register(this);

        private void OnDisable() => InteractableRegistry.Unregister(this);

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            if (bobAmplitude <= 0f) return;

            var position = basePosition;
            position.y += Mathf.Sin(Time.time * bobSpeed + phase) * bobAmplitude;
            transform.position = position;
        }

        /// <summary>Настроить предмет (вызывается спавнером или инспектором).</summary>
        public void Setup(ItemDefinition definition, int count, bool trap = false)
        {
            item = definition;
            amount = Mathf.Max(1, count);
            isTrap = trap;

            if (item != null && item.worldPrefab == null)
                ApplyProceduralVisual(item);
        }

        private void ApplyProceduralVisual(ItemDefinition definition)
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            var material = new Material(renderer.sharedMaterial)
            {
                color = definition.tint
            };
            renderer.sharedMaterial = material;
        }

        public string GetPrompt(PlayerInventory player)
        {
            if (item == null) return string.Empty;
            return isTrap ? "Подобрать (кажется, это ловушка…)" : $"Подобрать: {item.SafeName} x{amount}";
        }

        public bool CanInteract(PlayerInventory player) => item != null && player != null;

        public void Interact(PlayerInventory player)
        {
            if (isTrap)
            {
                LabHUD.Toast("Обманка! Предмет рассыпался в пыль…");
                GameEventBus.RaiseItemPickedUp(item, 0);
                Destroy(gameObject);
                return;
            }

            if (player != null && player.TryPickUp(this))
                Destroy(gameObject);
        }
    }
}
