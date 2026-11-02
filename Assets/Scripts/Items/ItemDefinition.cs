using System;
using UnityEngine;

namespace LabGame.Items
{
    /// <summary>Категория предмета. Влияет на правила инвентаря.</summary>
    public enum ItemCategory
    {
        Quest = 0,       // квестовый предмет (ягоды и т.п.)
        Consumable = 1,  // расходуемый
        Equipment = 2,   // экипировка
        Junk = 3         // прочий хлам
    }

    /// <summary>Слот экипировки. Значения совпадают с индексами массива в InventoryModel.</summary>
    public enum EquipSlot
    {
        None = 0,
        Head = 1,
        Chest = 2,
        Hands = 3,
        Feet = 4,
        Weapon = 5,
        Shield = 6,
        Amulet = 7
    }

    /// <summary>
    /// Один стат-модификатор предмета.
    /// Итоговое значение = (база + сумма flat) * (1 + сумма percent).
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public StatType stat;
        public float flat;
        public float percent;

        public StatModifier(StatType stat, float flat = 0f, float percent = 0f)
        {
            this.stat = stat;
            this.flat = flat;
            this.percent = percent;
        }

        public static StatModifier Flat(StatType stat, float value) => new StatModifier(stat, value, 0f);
        public static StatModifier Percent(StatType stat, float value) => new StatModifier(stat, 0f, value);

        public override string ToString()
        {
            string result = string.Empty;
            if (!Mathf.Approximately(flat, 0f)) result += $"{flat:+0.##;-0.##} {stat}";
            if (!Mathf.Approximately(percent, 0f))
            {
                if (result.Length > 0) result += ", ";
                result += $"{percent * 100f:+0;-0}% {stat}";
            }
            return result.Length == 0 ? "—" : result;
        }
    }

    /// <summary>
    /// Описание предмета (ScriptableObject). Один ассет = один тип предмета.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_New", menuName = "Lab 7/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Идентификация")]
        public string itemId;
        public string displayName;
        [TextArea(2, 5)] public string description;

        [Header("Правила инвентаря")]
        public ItemCategory category = ItemCategory.Junk;
        public EquipSlot equipSlot = EquipSlot.None;
        [Min(1)] public int maxStack = 1;
        [Min(0f)] public float weight = 0.5f;
        [Min(0)] public int goldValue = 1;

        [Header("Внешний вид")]
        [Tooltip("Если пусто — иконка будет сгенерирована из цвета (tint) и первой буквы названия.")]
        public Sprite icon;
        public Color tint = new Color(0.86f, 0.72f, 0.36f, 1f);
        [Tooltip("Необязательный префаб, который создаётся при выбрасывании предмета в мир.")]
        public GameObject worldPrefab;

        [Header("Экипировка")]
        public StatModifier[] modifiers = Array.Empty<StatModifier>();

        public bool IsEquippable => category == ItemCategory.Equipment && equipSlot != EquipSlot.None;
        public bool IsStackable => maxStack > 1;

        public string SafeId => string.IsNullOrEmpty(itemId) ? name : itemId;
        public string SafeName => string.IsNullOrEmpty(displayName) ? name : displayName;

        private void OnValidate()
        {
            if (maxStack < 1) maxStack = 1;
            if (itemId != null) itemId = itemId.Trim();
            if (category != ItemCategory.Equipment) equipSlot = EquipSlot.None;
        }
    }
}
