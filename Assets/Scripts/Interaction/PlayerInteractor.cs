using UnityEngine;

namespace LabGame
{
    /// <summary>
    /// Ищет ближайший интерактивный объект и реагирует на клавишу E.
    /// Подсказка выводится в HUD.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Взаимодействие")]
        public float radius = 3f;
        public KeyCode interactKey = KeyCode.E;

        private PlayerInventory inventory;
        private IInteractable current;

        public IInteractable Current => current;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();
        }

        private void Update()
        {
            if (GameplayInput.Locked)
            {
                SetCurrent(null);
                return;
            }

            current = InteractableRegistry.FindClosest(transform.position, radius, inventory);

            if (current != null && Input.GetKeyDown(interactKey))
            {
                var target = current;
                target.Interact(inventory);
                current = null;
            }

            UpdatePrompt();
        }

        private void SetCurrent(IInteractable value)
        {
            if (current == value) return;
            current = value;
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            string prompt = current != null ? current.GetPrompt(inventory) : null;
            LabHUD.SetPrompt(prompt);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
