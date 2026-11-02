using System;
using LabGame.Items;
using UnityEngine;

namespace LabGame
{
    /// <summary>
    /// Характеристики персонажа: базовые значения + бонусы от надетых предметов.
    /// Пересчёт автоматически запускается по событию изменения инвентаря,
    /// поэтому «надел меч — выросла атака» происходит без ручных вызовов.
    /// </summary>
    [RequireComponent(typeof(PlayerInventory))]
    public class CharacterStats : MonoBehaviour
    {
        [Header("Базовые характеристики (без экипировки)")]
        public StatBlock baseStats = StatBlock.Default;

        [Header("Отладка")]
        [SerializeField] private bool logChanges = true;

        private PlayerInventory inventory;
        private bool initialized;

        /// <summary>Итоговые характеристики с учётом экипировки.</summary>
        public StatBlock TotalStats { get; private set; }

        /// <summary>Бонус от экипировки (итог − база).</summary>
        public StatBlock EquipmentBonus => StatCalculator.Delta(baseStats, TotalStats);

        public float CurrentHealth { get; private set; }

        public float HealthNormalized => TotalStats.maxHealth <= 0f ? 0f : Mathf.Clamp01(CurrentHealth / TotalStats.maxHealth);

        /// <summary>Срабатывает после любого пересчёта характеристик.</summary>
        public event Action<CharacterStats> Changed;

        public PlayerInventory Inventory => inventory;

        private void Awake()
        {
            inventory = GetComponent<PlayerInventory>();

            // InventoryModel создаётся в Awake самого PlayerInventory.
            // Порядок Awake на одном объекте не гарантирован, поэтому
            // если модели ещё нет — пересчёт произойдёт в Start.
            if (inventory != null && inventory.Model != null) Recalculate(true);
        }

        private void Start()
        {
            if (!initialized) Recalculate(true);
        }

        private void OnEnable()
        {
            if (inventory != null && inventory.Model != null) inventory.Model.Changed += OnInventoryChanged;
        }

        private void OnDisable()
        {
            if (inventory != null && inventory.Model != null) inventory.Model.Changed -= OnInventoryChanged;
        }

        private void OnInventoryChanged()
        {
            Recalculate(false);
        }

        /// <summary>Пересчитать итоговые характеристики (например, после загрузки).</summary>
        public void Recalculate(bool resetHealth)
        {
            if (inventory == null) inventory = GetComponent<PlayerInventory>();

            var equipment = inventory != null && inventory.Model != null
                ? inventory.Model.EquippedStacks
                : null;

            TotalStats = StatCalculator.Compute(baseStats, equipment);

            if (resetHealth) CurrentHealth = TotalStats.maxHealth;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, TotalStats.maxHealth);

            initialized = true;

            if (logChanges)
            {
                Debug.Log($"[CharacterStats] {TotalStats} | экипировка: {DescribeBonus()}");
            }

            Changed?.Invoke(this);
            GameEventBus.RaisePlayerHealthChanged(CurrentHealth, TotalStats.maxHealth);
        }

        private string DescribeBonus()
        {
            var bonus = EquipmentBonus;
            bool any = false;
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                var type = StatBlock.All[i];
                float value = bonus.Get(type);
                if (Mathf.Abs(value) < 0.001f) continue;

                if (any) sb.Append(", ");
                sb.Append($"{StatBlock.ShortName(type)} {value:+0.##;-0.##}");
                any = true;
            }

            return any ? sb.ToString() : "нет";
        }

        /// <summary>Нанести урон с учётом защиты.</summary>
        public void ApplyDamage(float rawDamage)
        {
            if (rawDamage <= 0f) return;

            // Простая формула: защита снижает урон по убывающей.
            float mitigated = rawDamage * (100f / (100f + Mathf.Max(0f, TotalStats.defense)));
            CurrentHealth = Mathf.Max(0f, CurrentHealth - mitigated);

            GameEventBus.RaisePlayerHealthChanged(CurrentHealth, TotalStats.maxHealth);
            Changed?.Invoke(this);

            Debug.Log($"[CharacterStats] урон {rawDamage:0.#} → {mitigated:0.#}, HP {CurrentHealth:0.#}/{TotalStats.maxHealth:0.#}");
        }

        public void Heal(float amount)
        {
            if (amount <= 0f) return;
            CurrentHealth = Mathf.Min(TotalStats.maxHealth, CurrentHealth + amount);
            GameEventBus.RaisePlayerHealthChanged(CurrentHealth, TotalStats.maxHealth);
            Changed?.Invoke(this);
        }

        private void Update()
        {
            // Небольшой регенерации достаточно, чтобы прототип не заканчивался
            // за 10 секунд после первого урона.
            if (CurrentHealth > 0f && CurrentHealth < TotalStats.maxHealth)
                Heal(1.5f * Time.deltaTime);
        }
    }
}
