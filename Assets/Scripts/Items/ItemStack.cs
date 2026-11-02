using System;
using UnityEngine;

namespace LabGame.Items
{
    /// <summary>
    /// Стопка предметов в одном слоте инвентаря.
    /// Класс (не struct), чтобы слот можно было «переставлять» по ссылке.
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        public ItemDefinition item;
        public int amount;

        public ItemStack()
        {
            item = null;
            amount = 0;
        }

        public ItemStack(ItemDefinition item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }

        public bool IsEmpty => item == null || amount <= 0;

        public int MaxStack => item == null ? 1 : Mathf.Max(1, item.maxStack);

        public bool IsFull => !IsEmpty && amount >= MaxStack;

        public int FreeSpace => IsEmpty ? 0 : Mathf.Max(0, MaxStack - amount);

        public ItemStack Clone() => new ItemStack(item, amount);

        public void Clear()
        {
            item = null;
            amount = 0;
        }

        public override string ToString() => IsEmpty ? "<empty>" : $"{item?.SafeName} x{amount}";
    }
}
