using UnityEngine;

namespace LabGame.Controls
{
    /// <summary>
    /// Одно «чтение» ввода за кадр, не зависящее от конкретного устройства.
    /// Все системы (движение, камера, взаимодействие, инвентарь) читают
    /// именно это состояние, поэтому им не важно, откуда пришёл ввод.
    /// </summary>
    public struct PlayerInputState
    {
        /// <summary>Направление движения: x — вправо, y — вперёд. Диапазон −1…1.</summary>
        public Vector2 Move;

        /// <summary>Прирост поворота камеры за кадр.</summary>
        public Vector2 Look;

        public bool JumpHeld;
        public bool JumpPressed;
        public bool JumpReleased;

        public bool CrouchHeld;

        /// <summary>Основное действие (подобрать предмет, поговорить с NPC).</summary>
        public bool InteractPressed;

        /// <summary>Дополнительное действие (сдать/отменить квест).</summary>
        public bool SecondaryPressed;

        /// <summary>Открыть/закрыть инвентарь.</summary>
        public bool InventoryPressed;

        public static PlayerInputState Neutral => new PlayerInputState();
    }
}
