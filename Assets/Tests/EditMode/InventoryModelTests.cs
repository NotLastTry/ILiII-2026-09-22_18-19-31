using System.Collections.Generic;
using LabGame.Items;
using NUnit.Framework;
using UnityEngine;

namespace LabGame.EditModeTests
{
    /// <summary>
    /// Тесты правил инвентаря (ПЗ №7). Сцена не нужна — InventoryModel
    /// не зависит от Unity-объектов, кроме ScriptableObject-описаний предметов.
    /// </summary>
    public class InventoryModelTests
    {
        private const int Capacity = 6;

        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        private InventoryModel model;

        [SetUp]
        public void SetUp()
        {
            model = new InventoryModel(Capacity);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);

            created.Clear();
        }

        private ItemDefinition Item(string id, ItemCategory category = ItemCategory.Junk,
            EquipSlot slot = EquipSlot.None, int maxStack = 1, params StatModifier[] modifiers)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.name = id;
            item.itemId = id;
            item.displayName = id;
            item.category = category;
            item.equipSlot = slot;
            item.maxStack = maxStack;
            item.modifiers = modifiers;

            created.Add(item);
            return item;
        }

        // ── Подбор ────────────────────────────────────────────────

        [Test]
        public void Add_PutsItemIntoFirstSlot()
        {
            var sword = Item("sword");

            Assert.AreEqual(1, model.Add(sword, 1));
            Assert.AreSame(sword, model[0].item);
            Assert.AreEqual(1, model[0].amount);
            Assert.AreEqual(1, model.UsedSlots);
        }

        [Test]
        public void Add_StacksUpToMaxStack()
        {
            var potion = Item("potion", maxStack: 5);

            model.Add(potion, 3);
            model.Add(potion, 4);

            Assert.AreEqual(5, model[0].amount);
            Assert.AreEqual(2, model[1].amount);
            Assert.AreEqual(2, model.UsedSlots);
        }

        [Test]
        public void CanAdd_IsFalse_WhenInventoryFull()
        {
            var item = Item("stone", maxStack: 1);

            for (int i = 0; i < Capacity; i++)
                Assert.IsTrue(model.TryAdd(item, 1), $"не удалось добавить {i}-й предмет");

            Assert.IsFalse(model.CanAdd(item, 1));
            Assert.IsFalse(model.TryAdd(item, 1));
            Assert.AreEqual(Capacity, model.UsedSlots);
        }

        [Test]
        public void TryAdd_IsAtomic()
        {
            var stones = Item("stone", maxStack: 1);

            // В инвентаре 6 слотов по одному предмету — 7 не помещается.
            Assert.IsFalse(model.TryAdd(stones, Capacity + 1));
            Assert.AreEqual(0, model.UsedSlots);

            Assert.IsTrue(model.TryAdd(stones, Capacity));
            Assert.AreEqual(Capacity, model.UsedSlots);
        }

        // ── Перемещение ───────────────────────────────────────────

        [Test]
        public void Move_ToEmptySlot_TransfersStack()
        {
            var sword = Item("sword");
            model.Add(sword, 1);

            Assert.IsTrue(model.Move(0, 3));

            Assert.IsTrue(model[0].IsEmpty);
            Assert.AreSame(sword, model[3].item);
        }

        [Test]
        public void Move_SwapsDifferentItems()
        {
            var a = Item("a");
            var b = Item("b");
            model.TryAdd(a, 1);
            model.TryAdd(b, 1);

            Assert.IsTrue(model.Move(0, 1));

            Assert.AreSame(b, model[0].item);
            Assert.AreSame(a, model[1].item);
        }

        [Test]
        public void Move_MergesSameStackableItems()
        {
            var potion = Item("potion", maxStack: 10);
            model.Add(potion, 12);

            Assert.AreEqual(10, model[0].amount);
            Assert.AreEqual(2, model[1].amount);

            // В первый слот уже не влезает — перенос невозможен.
            Assert.IsFalse(model.Move(1, 0));

            // А во второй помещается ещё 8.
            Assert.IsTrue(model.Move(0, 1, 3));
            Assert.AreEqual(7, model[0].amount);
            Assert.AreEqual(5, model[1].amount);
        }

        [Test]
        public void Move_ToSameSlot_DoesNothing()
        {
            var sword = Item("sword");
            model.Add(sword, 1);

            Assert.IsFalse(model.Move(0, 0));
            Assert.AreSame(sword, model[0].item);
        }

        [Test]
        public void Move_FromEmptySlot_Fails()
        {
            Assert.IsFalse(model.Move(0, 1));
        }

        // ── Экипировка ────────────────────────────────────────────

        [Test]
        public void Equip_MovesItemToEquipmentSlot()
        {
            var sword = Item("sword", ItemCategory.Equipment, EquipSlot.Weapon);
            model.Add(sword, 1);

            Assert.IsTrue(model.Equip(0));

            Assert.AreSame(sword, model.GetEquipped(EquipSlot.Weapon).item);
            Assert.IsTrue(model[0].IsEmpty);
            Assert.AreEqual(1, model.Count(sword));
        }

        [Test]
        public void Equip_ReturnsPreviousItemToInventory()
        {
            var sword1 = Item("sword1", ItemCategory.Equipment, EquipSlot.Weapon);
            var sword2 = Item("sword2", ItemCategory.Equipment, EquipSlot.Weapon);

            model.Add(sword1, 1);
            Assert.IsTrue(model.Equip(0));

            model.Add(sword2, 1);
            Assert.IsTrue(model.Equip(0));

            Assert.AreSame(sword2, model.GetEquipped(EquipSlot.Weapon).item);
            Assert.AreSame(sword1, model[0].item);
        }

        [Test]
        public void Equip_NonEquippableItem_Fails()
        {
            var junk = Item("junk");
            model.Add(junk, 1);

            Assert.IsFalse(model.Equip(0));
            Assert.IsTrue(model.GetEquipped(EquipSlot.Weapon).IsEmpty);
        }

        [Test]
        public void EquipTo_WrongSlot_Fails()
        {
            var helmet = Item("helmet", ItemCategory.Equipment, EquipSlot.Head);
            model.Add(helmet, 1);

            Assert.IsFalse(model.EquipTo(0, EquipSlot.Weapon));
            Assert.IsTrue(model.EquipTo(0, EquipSlot.Head));
        }

        [Test]
        public void Unequip_ReturnsItemToInventory()
        {
            var sword = Item("sword", ItemCategory.Equipment, EquipSlot.Weapon);
            model.Add(sword, 1);
            model.Equip(0);

            Assert.IsTrue(model.Unequip(EquipSlot.Weapon));

            Assert.AreEqual(1, model.Count(sword));
            Assert.IsTrue(model.GetEquipped(EquipSlot.Weapon).IsEmpty);
        }

        [Test]
        public void Unequip_WhenInventoryFull_Fails()
        {
            var sword = Item("sword", ItemCategory.Equipment, EquipSlot.Weapon);
            var junk = Item("junk");

            model.Add(sword, 1);
            model.Equip(0);

            for (int i = 0; i < Capacity; i++) model.TryAdd(junk, 1);

            Assert.IsFalse(model.Unequip(EquipSlot.Weapon));
            Assert.AreSame(sword, model.GetEquipped(EquipSlot.Weapon).item);
        }

        // ── Выбрасывание ──────────────────────────────────────────

        [Test]
        public void Take_RemovesAndReturnsStack()
        {
            var sword = Item("sword");
            model.Add(sword, 1);

            var taken = model.Take(0);

            Assert.IsNotNull(taken);
            Assert.AreSame(sword, taken.item);
            Assert.AreEqual(1, taken.amount);
            Assert.IsTrue(model[0].IsEmpty);
        }

        [Test]
        public void TakeEquipped_RemovesFromEquipmentSlot()
        {
            var sword = Item("sword", ItemCategory.Equipment, EquipSlot.Weapon);
            model.Add(sword, 1);
            model.Equip(0);

            var taken = model.TakeEquipped(EquipSlot.Weapon);

            Assert.IsNotNull(taken);
            Assert.AreSame(sword, taken.item);
            Assert.IsTrue(model.GetEquipped(EquipSlot.Weapon).IsEmpty);
            Assert.AreEqual(0, model.Count(sword));
        }

        // ── События и суммарные данные ────────────────────────────

        [Test]
        public void Changed_FiresOnEveryMutation()
        {
            int changes = 0;
            model.Changed += () => changes++;

            var sword = Item("sword", ItemCategory.Equipment, EquipSlot.Weapon);
            model.Add(sword, 1);
            model.Move(0, 1);
            model.Equip(1);
            model.Unequip(EquipSlot.Weapon);
            model.Take(0);

            Assert.GreaterOrEqual(changes, 5);
        }

        [Test]
        public void Count_IncludesEquippedItems()
        {
            var sword = Item("sword", ItemCategory.Equipment, EquipSlot.Weapon);
            var potion = Item("potion", maxStack: 5);

            model.Add(sword, 1);
            model.Equip(0);
            model.Add(potion, 3);

            Assert.AreEqual(1, model.Count(sword));
            Assert.AreEqual(3, model.Count(potion));
        }

        [Test]
        public void TotalWeight_CountsInventoryAndEquipment()
        {
            var sword = Item("sword", ItemCategory.Equipment, EquipSlot.Weapon);
            sword.weight = 4f;
            model.Add(sword, 1);

            float inBag = model.TotalWeight();
            Assert.AreEqual(4f, inBag, 0.001f);

            model.Equip(0);
            Assert.Less(model.TotalWeight(), inBag);
        }
    }
}
