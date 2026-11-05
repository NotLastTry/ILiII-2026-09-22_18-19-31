using LabGame.Items;
using LabGame.World;
using UnityEngine;

namespace LabGame
{
    /// <summary>
    /// Небольшой сервис-локатор: хранит ссылку на игрока и настройки
    /// автосоздания интерфейса. Нужен, чтобы код Lab 8 мог найти
    /// игрока, не разыскивая объекты по тегам на каждом кадре.
    /// </summary>
    public static class LabServices
    {
        /// <summary>Отключается в PlayMode-тестах, чтобы не строить UI.</summary>
        public static bool AutoCreateInterface = true;

        public static PlayerInventory Player { get; private set; }

        public static CharacterStats Stats => Player != null ? Player.Stats : null;

        public static InventoryModel Model => Player != null ? Player.Model : null;

        public static void RegisterPlayer(PlayerInventory player)
        {
            if (player != null) Player = player;
        }

        public static void UnregisterPlayer(PlayerInventory player)
        {
            if (Player == player) Player = null;
        }

        public static void Reset()
        {
            Player = null;
            AutoCreateInterface = true;
            GameEventBus.ClearAll();
            InteractableRegistry.Clear();

            // Окружение (ПЗ №4) живёт отдельным объектом — убираем его между тестами.
            if (LabEnvironment.Instance != null)
                Object.DestroyImmediate(LabEnvironment.Instance.gameObject);
        }
    }
}
