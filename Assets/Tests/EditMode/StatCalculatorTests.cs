using System.Collections.Generic;
using LabGame.Items;
using NUnit.Framework;
using UnityEngine;

namespace LabGame.EditModeTests
{
    /// <summary>
    /// Тесты пересчёта характеристик от экипировки
    /// (дополнительное задание ПЗ №7).
    /// </summary>
    public class StatCalculatorTests
    {
        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);

            created.Clear();
        }

        private ItemDefinition Equipment(params StatModifier[] modifiers)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = "test_item";
            item.category = ItemCategory.Equipment;
            item.equipSlot = EquipSlot.Weapon;
            item.maxStack = 1;
            item.modifiers = modifiers;

            created.Add(item);
            return item;
        }

        private static ItemStack[] Equipped(params ItemDefinition[] items)
        {
            var stacks = new ItemStack[items.Length];
            for (int i = 0; i < items.Length; i++) stacks[i] = new ItemStack(items[i], 1);
            return stacks;
        }

        [Test]
        public void Compute_WithoutEquipment_ReturnsBase()
        {
            var baseStats = StatBlock.Default;
            var total = StatCalculator.Compute(baseStats, null);

            Assert.AreEqual(baseStats.maxHealth, total.maxHealth, 0.001f);
            Assert.AreEqual(baseStats.attack, total.attack, 0.001f);
            Assert.AreEqual(baseStats.defense, total.defense, 0.001f);
            Assert.AreEqual(baseStats.moveSpeed, total.moveSpeed, 0.001f);
        }

        [Test]
        public void Compute_AddsFlatBonus()
        {
            var sword = Equipment(new StatModifier(StatType.Attack, 12f));

            var total = StatCalculator.Compute(StatBlock.Default, Equipped(sword));

            Assert.AreEqual(StatBlock.Default.attack + 12f, total.attack, 0.001f);
        }

        [Test]
        public void Compute_AppliesPercentAfterFlat()
        {
            // (10 + 10) * 1.5 = 30
            var amulet = Equipment(
                new StatModifier(StatType.Attack, 10f),
                new StatModifier(StatType.Attack, 0f, 0.5f));

            var total = StatCalculator.Compute(StatBlock.Default, Equipped(amulet));

            Assert.AreEqual(30f, total.attack, 0.001f);
        }

        [Test]
        public void Compute_SumsBonusesFromSeveralItems()
        {
            var helmet = Equipment(new StatModifier(StatType.Defense, 4f));
            var shield = Equipment(new StatModifier(StatType.Defense, 6f));

            var total = StatCalculator.Compute(StatBlock.Default, Equipped(helmet, shield));

            Assert.AreEqual(StatBlock.Default.defense + 10f, total.defense, 0.001f);
        }

        [Test]
        public void Compute_ClampsUnsafeValues()
        {
            var cursed = Equipment(
                new StatModifier(StatType.MaxHealth, -500f),
                new StatModifier(StatType.MoveSpeed, -100f),
                new StatModifier(StatType.CritChance, 5f));

            var total = StatCalculator.Compute(StatBlock.Default, Equipped(cursed));

            Assert.GreaterOrEqual(total.maxHealth, 1f);
            Assert.GreaterOrEqual(total.moveSpeed, 0.1f);
            Assert.LessOrEqual(total.critChance, 1f);
        }

        [Test]
        public void Delta_ShowsEquipmentContribution()
        {
            var boots = Equipment(new StatModifier(StatType.MoveSpeed, 1.2f));

            var total = StatCalculator.Compute(StatBlock.Default, Equipped(boots));
            var delta = StatCalculator.Delta(StatBlock.Default, total);

            Assert.AreEqual(1.2f, delta.moveSpeed, 0.001f);
            Assert.AreEqual(0f, delta.attack, 0.001f);
        }
    }
}
