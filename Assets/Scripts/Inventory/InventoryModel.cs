using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabGame.Items
{
    /// <summary>
    /// Модель инвентаря: сетка слотов + отдельный набор слотов экипировки.
    /// Не знает ничего про Unity-сцену, UI и ввод — это чистая логика,
    /// поэтому её поведение целиком покрывается юнит-тестами.
    ///
    /// Реализованные механики (ПЗ №7):
    ///   • перемещение предметов между слотами (Move);
    ///   • экипировка / снятие (Equip / EquipTo / Unequip);
    ///   • выбрасывание предмета (Take / TakeEquipped);
    ///   • подбор предмета (Add / TryAdd).
    /// </summary>
    public sealed class InventoryModel
    {
        public const int DefaultCapacity = 24;
        public const int EquipSlotCount = 8;

        private readonly ItemStack[] slots;
        private readonly ItemStack[] equipped;

        /// <summary>Любое изменение содержимого (слоты или экипировка).</summary>
        public event Action Changed;

        /// <summary>Предмет добавлен (item, сколько именно добавлено).</summary>
        public event Action<ItemDefinition, int> ItemAdded;

        /// <summary>Предмет убран (item, сколько именно убрано).</summary>
        public event Action<ItemDefinition, int> ItemRemoved;

        /// <summary>Предмет надет (слот, предмет).</summary>
        public event Action<EquipSlot, ItemDefinition> ItemEquipped;

        /// <summary>Предмет снят (слот, предмет).</summary>
        public event Action<EquipSlot, ItemDefinition> ItemUnequipped;

        public InventoryModel(int capacity = DefaultCapacity)
        {
            capacity = Mathf.Max(1, capacity);

            slots = new ItemStack[capacity];
            for (int i = 0; i < capacity; i++) slots[i] = new ItemStack();

            equipped = new ItemStack[EquipSlotCount];
            for (int i = 0; i < EquipSlotCount; i++) equipped[i] = new ItemStack();
        }

        public int Capacity => slots.Length;

        public IReadOnlyList<ItemStack> Slots => slots;

        public bool IsValidIndex(int index) => index >= 0 && index < slots.Length;

        public ItemStack this[int index] => IsValidIndex(index) ? slots[index] : null;

        /// <summary>Количество занятых слотов.</summary>
        public int UsedSlots
        {
            get
            {
                int used = 0;
                for (int i = 0; i < slots.Length; i++)
                    if (!slots[i].IsEmpty) used++;
                return used;
            }
        }

        public IEnumerable<ItemStack> EquippedStacks
        {
            get
            {
                for (int i = 0; i < equipped.Length; i++)
                {
                    if (!equipped[i].IsEmpty) yield return equipped[i];
                }
            }
        }

        public ItemStack GetEquipped(EquipSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= equipped.Length) return null;
            return equipped[index];
        }

        public bool IsEquipped(ItemDefinition item)
        {
            if (item == null) return false;
            for (int i = 0; i < equipped.Length; i++)
                if (!equipped[i].IsEmpty && equipped[i].item == item) return true;
            return false;
        }

        /// <summary>Суммарный вес содержимого (для грузоподъёмности).</summary>
        public float TotalWeight(float equippedWeightFactor = 0.35f)
        {
            float total = 0f;
            for (int i = 0; i < slots.Length; i++)
                if (!slots[i].IsEmpty) total += slots[i].item.weight * slots[i].amount;

            // Надетые вещи весят меньше — они «на себе», а не в сумке.
            for (int i = 0; i < equipped.Length; i++)
                if (!equipped[i].IsEmpty) total += equipped[i].item.weight * equipped[i].amount * equippedWeightFactor;

            return total;
        }

        // ─────────────────────────────────────────────────────────────
        //  ПОДБОР (добавление)
        // ─────────────────────────────────────────────────────────────

        /// <summary>Сколько единиц предмета ещё поместится.</summary>
        public int GetFreeSpaceFor(ItemDefinition item)
        {
            if (item == null) return 0;

            int free = 0;
            int maxStack = Mathf.Max(1, item.maxStack);

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty) free += maxStack;
                else if (item.IsStackable && slot.item == item) free += Mathf.Max(0, maxStack - slot.amount);
            }
            return free;
        }

        public bool CanAdd(ItemDefinition item, int amount) =>
            item != null && amount > 0 && GetFreeSpaceFor(item) >= amount;

        /// <summary>Добавляет предмет с частичным результатом. Возвращает, сколько реально поместилось.</summary>
        public int Add(ItemDefinition item, int amount)
        {
            if (item == null || amount <= 0) return 0;

            int remaining = amount;
            int added = 0;
            int maxStack = Mathf.Max(1, item.maxStack);

            // 1. Сначала дополняем существующие стопки того же предмета.
            if (item.IsStackable)
            {
                for (int i = 0; i < slots.Length && remaining > 0; i++)
                {
                    var slot = slots[i];
                    if (slot.IsEmpty || slot.item != item) continue;

                    int space = maxStack - slot.amount;
                    if (space <= 0) continue;

                    int put = Mathf.Min(space, remaining);
                    slot.amount += put;
                    remaining -= put;
                    added += put;
                }
            }

            // 2. Затем занимаем пустые слоты.
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                if (!slots[i].IsEmpty) continue;

                int put = Mathf.Min(maxStack, remaining);
                slots[i] = new ItemStack(item, put);
                remaining -= put;
                added += put;
            }

            if (added > 0)
            {
                ItemAdded?.Invoke(item, added);
                Changed?.Invoke();
            }

            return added;
        }

        /// <summary>
        /// Атомарный подбор: либо помещается всё, либо ничего.
        /// Именно так работает подбор предмета из мира — предмет не должен
        /// «потеряться наполовину».
        /// </summary>
        public bool TryAdd(ItemDefinition item, int amount)
        {
            if (!CanAdd(item, amount)) return false;
            Add(item, amount);
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        //  ПЕРЕМЕЩЕНИЕ ВНУТРИ ИНВЕНТАРЯ
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Перемещает предмет из слота <paramref name="from"/> в слот <paramref name="to"/>.
        /// • пустой слот  → перенос;
        /// • тот же предмет и есть место → объединение стопок;
        /// • другой предмет → обмен местами.
        /// </summary>
        public bool Move(int from, int to, int amount = -1)
        {
            if (!IsValidIndex(from) || !IsValidIndex(to) || from == to) return false;

            var source = slots[from];
            var target = slots[to];
            if (source.IsEmpty) return false;

            if (amount <= 0 || amount > source.amount) amount = source.amount;

            if (target.IsEmpty)
            {
                slots[to] = new ItemStack(source.item, amount);
                if (amount >= source.amount) slots[from] = new ItemStack();
                else source.amount -= amount;
            }
            else if (target.item == source.item && source.item.IsStackable)
            {
                int space = target.MaxStack - target.amount;
                int put = Mathf.Min(space, amount);
                if (put <= 0) return false;

                target.amount += put;
                if (put >= source.amount) slots[from] = new ItemStack();
                else source.amount -= put;
            }
            else
            {
                // Обмен возможен только целыми стопками.
                if (amount < source.amount) return false;
                slots[from] = target;
                slots[to] = source;
            }

            Changed?.Invoke();
            return true;
        }

        /// <summary>Меняет местами два слота (синоним Move без частичного переноса).</summary>
        public bool Swap(int a, int b) => Move(a, b);

        // ─────────────────────────────────────────────────────────────
        //  ЭКИПИРОВКА
        // ─────────────────────────────────────────────────────────────

        /// <summary>Надеть предмет из слота инвентаря в его собственный слот экипировки.</summary>
        public bool Equip(int index)
        {
            if (!IsValidIndex(index)) return false;

            var source = slots[index];
            if (source.IsEmpty || source.item == null || !source.item.IsEquippable) return false;

            return EquipInternal(index, source.item.equipSlot);
        }

        /// <summary>
        /// Надеть предмет на конкретный слот (drag&amp;drop в панель экипировки).
        /// Тип слота должен совпадать с типом предмета.
        /// </summary>
        public bool EquipTo(int index, EquipSlot target)
        {
            if (!IsValidIndex(index) || target == EquipSlot.None) return false;

            var source = slots[index];
            if (source.IsEmpty || source.item == null) return false;
            if (!source.item.IsEquippable || source.item.equipSlot != target) return false;

            return EquipInternal(index, target);
        }

        private bool EquipInternal(int index, EquipSlot target)
        {
            int equipIndex = (int)target;
            if (equipIndex <= 0 || equipIndex >= equipped.Length) return false;

            var source = slots[index];
            var previous = equipped[equipIndex];
            bool hasPrevious = !previous.IsEmpty;

            // Куда денется ранее надетый предмет.
            int previousTarget = -1;
            if (hasPrevious)
            {
                // Если из исходного слота уходит вся стопка — кладём туда.
                previousTarget = source.amount <= 1 ? index : FindFirstFreeSlot();
                if (previousTarget < 0) return false; // некуда девать — отменяем
            }

            var item = source.item;

            // Снимаем одну единицу из слота.
            if (source.amount > 1) source.amount -= 1;
            else slots[index] = new ItemStack();

            // Возвращаем ранее надетый предмет.
            if (hasPrevious)
            {
                if (previousTarget == index && slots[index].IsEmpty)
                    slots[index] = previous;
                else
                    slots[previousTarget] = previous;
            }

            equipped[equipIndex] = new ItemStack(item, 1);

            if (hasPrevious) ItemUnequipped?.Invoke(target, previous.item);
            ItemEquipped?.Invoke(target, item);
            Changed?.Invoke();
            return true;
        }

        /// <summary>Снять предмет в первый свободный слот инвентаря.</summary>
        public bool Unequip(EquipSlot slot)
        {
            int equipIndex = (int)slot;
            if (equipIndex <= 0 || equipIndex >= equipped.Length) return false;

            var current = equipped[equipIndex];
            if (current.IsEmpty) return false;

            if (!CanAdd(current.item, current.amount)) return false;

            equipped[equipIndex] = new ItemStack();
            Add(current.item, current.amount);

            ItemUnequipped?.Invoke(slot, current.item);
            Changed?.Invoke();
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        //  ВЫБРАСЫВАНИЕ (изъятие из модели)
        // ─────────────────────────────────────────────────────────────

        /// <summary>Забирает предмет из слота и возвращает его (для выбрасывания в мир).</summary>
        public ItemStack Take(int index, int amount = -1)
        {
            if (!IsValidIndex(index)) return null;

            var slot = slots[index];
            if (slot.IsEmpty) return null;

            if (amount <= 0 || amount > slot.amount) amount = slot.amount;

            var taken = new ItemStack(slot.item, amount);

            if (amount >= slot.amount) slots[index] = new ItemStack();
            else slot.amount -= amount;

            ItemRemoved?.Invoke(taken.item, taken.amount);
            Changed?.Invoke();
            return taken;
        }

        /// <summary>Забирает надетый предмет (для выбрасывания прямо из панели экипировки).</summary>
        public ItemStack TakeEquipped(EquipSlot slot)
        {
            int equipIndex = (int)slot;
            if (equipIndex <= 0 || equipIndex >= equipped.Length) return null;

            var current = equipped[equipIndex];
            if (current.IsEmpty) return null;

            equipped[equipIndex] = new ItemStack();
            var taken = new ItemStack(current.item, current.amount);

            ItemUnequipped?.Invoke(slot, taken.item);
            ItemRemoved?.Invoke(taken.item, taken.amount);
            Changed?.Invoke();
            return taken;
        }

        // ─────────────────────────────────────────────────────────────
        //  ВСПОМОГАТЕЛЬНОЕ
        // ─────────────────────────────────────────────────────────────

        public int Count(ItemDefinition item)
        {
            if (item == null) return 0;

            int total = 0;
            for (int i = 0; i < slots.Length; i++)
                if (!slots[i].IsEmpty && slots[i].item == item) total += slots[i].amount;

            for (int i = 0; i < equipped.Length; i++)
                if (!equipped[i].IsEmpty && equipped[i].item == item) total += equipped[i].amount;

            return total;
        }

        public int FindFirstFreeSlot()
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsEmpty) return i;
            return -1;
        }

        /// <summary>Индекс первой стопки предмета (или -1).</summary>
        public int FindSlot(ItemDefinition item)
        {
            if (item == null) return -1;
            for (int i = 0; i < slots.Length; i++)
                if (!slots[i].IsEmpty && slots[i].item == item) return i;
            return -1;
        }

        public void Clear()
        {
            for (int i = 0; i < slots.Length; i++) slots[i] = new ItemStack();
            for (int i = 0; i < equipped.Length; i++) equipped[i] = new ItemStack();
            Changed?.Invoke();
        }
    }
}
