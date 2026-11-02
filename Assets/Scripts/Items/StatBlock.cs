using System;
using UnityEngine;

namespace LabGame.Items
{
    /// <summary>Список характеристик персонажа, которые могут менять предметы.</summary>
    public enum StatType
    {
        MaxHealth = 0,
        Attack = 1,
        Defense = 2,
        MoveSpeed = 3,
        CarryWeight = 4,
        CritChance = 5
    }

    /// <summary>
    /// Набор характеристик. Обычный struct, чтобы его можно было
    /// складывать без аллокаций и без обращения к сцене.
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        public float maxHealth;
        public float attack;
        public float defense;
        public float moveSpeed;
        public float carryWeight;
        public float critChance;

        public static readonly StatType[] All =
        {
            StatType.MaxHealth,
            StatType.Attack,
            StatType.Defense,
            StatType.MoveSpeed,
            StatType.CarryWeight,
            StatType.CritChance
        };

        public static StatBlock Default => new StatBlock
        {
            maxHealth = 100f,
            attack = 10f,
            defense = 5f,
            moveSpeed = 6f,
            carryWeight = 20f,
            critChance = 0.05f
        };

        public float Get(StatType type)
        {
            switch (type)
            {
                case StatType.MaxHealth: return maxHealth;
                case StatType.Attack: return attack;
                case StatType.Defense: return defense;
                case StatType.MoveSpeed: return moveSpeed;
                case StatType.CarryWeight: return carryWeight;
                case StatType.CritChance: return critChance;
                default: return 0f;
            }
        }

        public void Set(StatType type, float value)
        {
            switch (type)
            {
                case StatType.MaxHealth: maxHealth = value; break;
                case StatType.Attack: attack = value; break;
                case StatType.Defense: defense = value; break;
                case StatType.MoveSpeed: moveSpeed = value; break;
                case StatType.CarryWeight: carryWeight = value; break;
                case StatType.CritChance: critChance = value; break;
            }
        }

        public void Add(StatType type, float value) => Set(type, Get(type) + value);

        public float this[StatType type]
        {
            get => Get(type);
            set => Set(type, value);
        }

        public static string ShortName(StatType type)
        {
            switch (type)
            {
                case StatType.MaxHealth: return "Здоровье";
                case StatType.Attack: return "Атака";
                case StatType.Defense: return "Защита";
                case StatType.MoveSpeed: return "Скорость";
                case StatType.CarryWeight: return "Грузоподъёмность";
                case StatType.CritChance: return "Крит. шанс";
                default: return type.ToString();
            }
        }

        public override string ToString() =>
            $"HP {maxHealth:0.#} | ATK {attack:0.#} | DEF {defense:0.#} | SPD {moveSpeed:0.##}";
    }
}
