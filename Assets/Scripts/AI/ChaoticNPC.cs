using LabGame.Items;
using UnityEngine;
using UnityEngine.AI;

namespace LabGame.AI
{
    /// <summary>
    /// Хаотичный NPC (ПЗ №8): исполняет выбранные «мозгом» действия,
    /// реагирует на события игры и показывает своё состояние в HUD.
    ///
    /// NPC намеренно не имеет «правильного» поведения: игрок не может
    /// предсказать, что будет дальше, но при этом действия всегда
    /// осмысленны по отношению к ситуации (близко/далеко, смотрят/не смотрят,
    /// игрок ранен, взял предмет и т.д.).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ChaoticNPC : MonoBehaviour
    {
        [Header("Профиль поведения")]
        public ChaosProfile profile;

        [Header("Отображение")]
        public string displayName = "Хаотичный NPC";
        public bool showHud = true;

        [Header("Отладка")]
        public bool logActions = true;

        [Header("Реакции")]
        [Tooltip("Не реагирует на события дальше этого расстояния.")]
        public float reactionRadius = 40f;

        [Tooltip("Минимальный интервал между реакциями на «взгляд»/«приближение».")]
        public float proximityReactionCooldown = 1.5f;

        private ChaosAgent agent;
        private NpcPerception perception;
        private ChaosBrain brain;

        /// <summary>Доступ к «мозгу» для тестов и отладки.</summary>
        public ChaosBrain Brain => brain;

        private Vector3 lastPlayerPosition;
        private float playerSpeed;
        private float speechTimer;
        private string speech = string.Empty;

        private float lastHealth = -1f;
        private bool questActive;
        private bool playerLookedAtMe;
        private bool playerWasClose;
        private float proximityCooldown;
        private float hudTimer;

        // ─────────────────────────────────────────────────────────────
        //  Жизненный цикл
        // ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            agent = GetComponent<ChaosAgent>();
            if (agent == null) agent = gameObject.AddComponent<ChaosAgent>();

            perception = GetComponent<NpcPerception>();
            if (perception == null) perception = gameObject.AddComponent<NpcPerception>();

            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                agent.player = player.transform;
                perception.player = player.transform;
                lastPlayerPosition = player.transform.position;
            }

            if (profile == null) profile = ScriptableObject.CreateInstance<ChaosProfile>();

            brain = new ChaosBrain(profile, ChaosActions.BuildDefault(profile),
                UnityEngine.Random.Range(1, int.MaxValue));
            brain.ActionChanged += OnActionChanged;

            agent.SpeechSink = OnSpeech;

            if (string.IsNullOrEmpty(displayName) || displayName == "Хаотичный NPC")
                displayName = name;
        }

        private void OnEnable()
        {
            GameEventBus.ItemPickedUp += HandleItemPickedUp;
            GameEventBus.ItemEquipped += HandleItemEquipped;
            GameEventBus.ItemDropped += HandleItemDropped;
            GameEventBus.PlayerHealthChanged += HandleHealthChanged;
            GameEventBus.QuestStateChanged += HandleQuestStateChanged;
        }

        private void OnDisable()
        {
            GameEventBus.ItemPickedUp -= HandleItemPickedUp;
            GameEventBus.ItemEquipped -= HandleItemEquipped;
            GameEventBus.ItemDropped -= HandleItemDropped;
            GameEventBus.PlayerHealthChanged -= HandleHealthChanged;
            GameEventBus.QuestStateChanged -= HandleQuestStateChanged;

            if (brain != null) brain.ActionChanged -= OnActionChanged;
        }

        private void Update()
        {
            if (brain == null || agent == null || perception == null) return;

            UpdatePlayerSpeed();

            var context = BuildContext();
            brain.Tick(agent, context, Time.deltaTime);

            DetectTransitions(context);
            UpdateSpeech(deltaTime: Time.deltaTime);
            UpdateHud(context);
        }

        // ─────────────────────────────────────────────────────────────
        //  Контекст
        // ─────────────────────────────────────────────────────────────

        private ChaosContext BuildContext()
        {
            float health01 = 1f;
            var stats = LabServices.Stats;
            if (stats != null) health01 = stats.HealthNormalized;

            return new ChaosContext(
                perception.Distance,
                perception.HasLineOfSight,
                perception.PlayerLookingAtMe,
                health01,
                playerSpeed,
                questActive);
        }

        private void UpdatePlayerSpeed()
        {
            if (agent.player == null) return;

            Vector3 position = agent.player.position;
            Vector3 delta = position - lastPlayerPosition;
            delta.y = 0f;

            float instant = delta.magnitude / Mathf.Max(0.0001f, Time.deltaTime);
            playerSpeed = Mathf.Lerp(playerSpeed, instant, 0.35f);
            lastPlayerPosition = position;
        }

        // ─────────────────────────────────────────────────────────────
        //  Реакции на события игры
        // ─────────────────────────────────────────────────────────────

        private void HandleItemPickedUp(ItemDefinition item, int amount) => React(ChaosSignal.PlayerPickedUpItem);
        private void HandleItemEquipped(ItemDefinition item, EquipSlot slot) => React(ChaosSignal.PlayerEquippedItem);
        private void HandleItemDropped(ItemDefinition item, int amount) => React(ChaosSignal.PlayerDroppedItem);

        private void HandleHealthChanged(float current, float max)
        {
            if (lastHealth >= 0f && current < lastHealth - 0.5f)
                React(ChaosSignal.PlayerDamaged);

            lastHealth = current;
        }

        private void HandleQuestStateChanged(int state)
        {
            // NPCQuestState: 0 Idle, 1 Offer, 2 QuestActive, 3 QuestDone, 4 RewardGiven
            questActive = state == 2 || state == 3;
            React(ChaosSignal.QuestChanged);
        }

        private void React(ChaosSignal signal)
        {
            if (perception == null || perception.Distance > reactionRadius) return;

            var context = BuildContext();
            brain.Notify(signal, agent, context);

            if (logActions)
                Debug.Log($"[Chaos] {displayName}: реакция на {signal} → {ChaosActionNames.Russian(brain.CurrentAction)}");
        }

        /// <summary>Реакции на то, что видит сам NPC (игрок посмотрел / подошёл).</summary>
        private void DetectTransitions(ChaosContext context)
        {
            proximityCooldown -= Time.deltaTime;

            bool lookedNow = context.PlayerLookingAtMe;
            if (lookedNow && !playerLookedAtMe && proximityCooldown <= 0f)
            {
                proximityCooldown = proximityReactionCooldown;
                React(ChaosSignal.PlayerLookedAtMe);
            }
            playerLookedAtMe = lookedNow;

            bool closeNow = context.PlayerIsClose;
            if (closeNow && !playerWasClose && proximityCooldown <= 0f)
            {
                proximityCooldown = proximityReactionCooldown;
                React(ChaosSignal.PlayerCameClose);
            }
            playerWasClose = closeNow;
        }

        // ─────────────────────────────────────────────────────────────
        //  Речь и HUD
        // ─────────────────────────────────────────────────────────────

        private void OnSpeech(string text, float duration)
        {
            speech = text;
            speechTimer = Mathf.Max(1f, duration);
            if (logActions) Debug.Log($"[Chaos] {displayName} говорит: «{text}»");
        }

        private void UpdateSpeech(float deltaTime)
        {
            if (speechTimer > 0f)
            {
                speechTimer -= deltaTime;
                if (speechTimer <= 0f) speech = string.Empty;
            }
        }

        private void UpdateHud(ChaosContext context)
        {
            if (!showHud) return;

            hudTimer -= Time.deltaTime;
            if (hudTimer > 0f) return;
            hudTimer = 0.1f;

            string speechLine = speechTimer > 0f
                ? $"«{speech}»"
                : $"до игрока {context.DistanceToPlayer:0.0} м";

            string text =
                $"<b>{displayName}</b>\n" +
                $"Действие: {ChaosActionNames.Russian(brain.CurrentAction)}  " +
                $"({brain.TimeLeft:0.0} с)\n" +
                speechLine;

            LabHUD.SetChaosStatus(text, BadgeColor(brain.CurrentAction));
        }

        private static Color BadgeColor(ChaosAction action)
        {
            // Оформление намеренно без цветов — только оттенки серого.
            return new Color(0.72f, 0.72f, 0.72f, 1f);
        }

        private void OnActionChanged(ChaosAction action)
        {
            if (logActions)
                Debug.Log($"[Chaos] {displayName} → {ChaosActionNames.Russian(action)} (t={Time.time:0.0})");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.3f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, reactionRadius);
        }
    }
}
