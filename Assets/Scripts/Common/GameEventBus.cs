using System;
using LabGame.Items;
using UnityEngine;

namespace LabGame
{
    /// <summary>
    /// Шина игровых событий (ПЗ №8): на изменения в игровом процессе
    /// подписывается «хаотичный» ИИ, HUD и любые другие системы.
    /// Статика, потому что событий мало, а подписчиков удобно
    /// регистрировать из любого места без ссылок на сцену.
    /// </summary>
    public static class GameEventBus
    {
        /// <summary>Игрок подобрал предмет.</summary>
        public static event Action<ItemDefinition, int> ItemPickedUp;

        /// <summary>Игрок экипировал предмет.</summary>
        public static event Action<ItemDefinition, EquipSlot> ItemEquipped;

        /// <summary>Игрок выбросил предмет в мир.</summary>
        public static event Action<ItemDefinition, int> ItemDropped;

        /// <summary>Изменилось здоровье игрока (текущее, максимальное).</summary>
        public static event Action<float, float> PlayerHealthChanged;

        /// <summary>Сменилось состояние квеста (числовое значение NPCQuestState).</summary>
        public static event Action<int> QuestStateChanged;

        /// <summary>Игрок посмотрел на NPC (используется для реакции «замер»/«побег»).</summary>
        public static event Action<GameObject> PlayerLookedAtNpc;

        /// <summary>Громкое событие в точке — толпа пугается (ПЗ №6).</summary>
        public static event Action<Vector3> Disturbance;

        public static void RaiseItemPickedUp(ItemDefinition item, int amount) => ItemPickedUp?.Invoke(item, amount);
        public static void RaiseItemEquipped(ItemDefinition item, EquipSlot slot) => ItemEquipped?.Invoke(item, slot);
        public static void RaiseItemDropped(ItemDefinition item, int amount) => ItemDropped?.Invoke(item, amount);
        public static void RaisePlayerHealthChanged(float current, float max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaiseQuestStateChanged(int state) => QuestStateChanged?.Invoke(state);
        public static void RaisePlayerLookedAtNpc(GameObject npc) => PlayerLookedAtNpc?.Invoke(npc);
        public static void RaiseDisturbance(Vector3 position) => Disturbance?.Invoke(position);

        /// <summary>Сбрасывает подписки. Нужно в тестах, чтобы не протекало состояние.</summary>
        public static void ClearAll()
        {
            ItemPickedUp = null;
            ItemEquipped = null;
            ItemDropped = null;
            PlayerHealthChanged = null;
            QuestStateChanged = null;
            PlayerLookedAtNpc = null;
            Disturbance = null;
        }
    }
}
