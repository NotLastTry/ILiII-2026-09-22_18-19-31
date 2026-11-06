using UnityEngine;

namespace LabGame.Dialogue
{
    /// <summary>
    /// Превращает NPC в собеседника: по клавише E открывает диалог,
    /// описанный в XML (ПЗ №9). Квестовые условия и действия диалога
    /// применяются к этому же NPC.
    /// </summary>
    [RequireComponent(typeof(NPCController))]
    public class DialogueAgent : MonoBehaviour, IInteractable
    {
        [Header("Диалог")]
        [Tooltip("Имя файла в Resources/Dialogues без расширения.")]
        [SerializeField] private string dialogueId = "smith";

        [Tooltip("С какого узла начинать. Пусто — стартовый узел диалога.")]
        [SerializeField] private string startNode = "start";

        public string DialogueId => dialogueId;

        public Vector3 InteractionPoint => transform.position;

        private void OnEnable()
        {
            // Без регистрации PlayerInteractor не увидит собеседника.
            InteractableRegistry.Register(this);
        }

        private void OnDisable()
        {
            InteractableRegistry.Unregister(this);
        }

        public string GetPrompt(PlayerInventory player) => "E — поговорить";

        public bool CanInteract(PlayerInventory player) => !string.IsNullOrWhiteSpace(dialogueId);

        public void Interact(PlayerInventory player)
        {
            var runner = DialogueRunner.Ensure();
            if (runner == null || runner.IsOpen) return;

            var graph = DialogueLibrary.Load(dialogueId);

            if (graph == null)
            {
                LabHUD.Toast($"Диалог «{dialogueId}» не найден");
                return;
            }

            var npc = GetComponent<NPCController>();
            if (npc != null) npc.isTalking = true;

            runner.Closed -= HandleClosed;
            runner.Closed += HandleClosed;

            runner.Begin(graph, new QuestDialogueContext(npc, player), startNode);
        }

        private void HandleClosed()
        {
            var npc = GetComponent<NPCController>();
            if (npc != null) npc.isTalking = false;
        }

        private void OnDestroy()
        {
            if (DialogueRunner.Instance != null)
                DialogueRunner.Instance.Closed -= HandleClosed;
        }
    }
}
