using System.Collections.Generic;
using UnityEngine;

namespace LabGame.Items
{
    /// <summary>
    /// Пересчёт итоговых характеристик персонажа с учётом надетых предметов.
    /// Отдельный статический класс — его легко покрыть юнит-тестами
    /// без запуска сцены.
    /// </summary>
    public static class StatCalculator
    {
        /// <summary>
        /// Формула: итог = (база + сумма flat) * (1 + сумма percent).
        /// Проценты складываются, а не перемножаются — так проще
        /// предсказывать результат и объяснять его игроку.
        /// </summary>
        public static StatBlock Compute(StatBlock baseStats, IEnumerable<ItemStack> equipment)
        {
            var flat = new StatBlock();
            var percent = new StatBlock();

            if (equipment != null)
            {
                foreach (var stack in equipment)
                {
                    if (stack == null || stack.IsEmpty || stack.item == null) continue;

                    var mods = stack.item.modifiers;
                    if (mods == null) continue;

                    for (int i = 0; i < mods.Length; i++)
                    {
                        flat.Add(mods[i].stat, mods[i].flat);
                        percent.Add(mods[i].stat, mods[i].percent);
                    }
                }
            }

            var result = baseStats;
            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                var type = StatBlock.All[i];
                result.Set(type, (baseStats.Get(type) + flat.Get(type)) * (1f + percent.Get(type)));
            }

            // Защита от «отрицательных» характеристик, полученных суммой модификаторов.
            result.maxHealth = Mathf.Max(1f, result.maxHealth);
            result.attack = Mathf.Max(0f, result.attack);
            result.defense = Mathf.Max(0f, result.defense);
            result.moveSpeed = Mathf.Max(0.1f, result.moveSpeed);
            result.carryWeight = Mathf.Max(1f, result.carryWeight);
            result.critChance = Mathf.Clamp01(result.critChance);

            return result;
        }

        /// <summary>Разница между итогом и базой — для подсветки изменений в UI.</summary>
        public static StatBlock Delta(StatBlock baseStats, StatBlock total)
        {
            var delta = new StatBlock();
            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                var type = StatBlock.All[i];
                delta.Set(type, total.Get(type) - baseStats.Get(type));
            }
            return delta;
        }
    }
}
