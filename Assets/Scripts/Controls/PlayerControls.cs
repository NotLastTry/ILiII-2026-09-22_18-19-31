using UnityEngine;

namespace LabGame.Controls
{
    /// <summary>
    /// Удобные «глобальные» проверки ввода для игровых систем.
    /// Если роутер ещё не создан (например, в отдельном тесте),
    /// используется старое прямое чтение клавиатуры.
    /// </summary>
    public static class PlayerControls
    {
        public static PlayerInputState State =>
            InputRouter.Instance != null ? InputRouter.Instance.State : PlayerInputState.Neutral;

        public static Vector2 Move
        {
            get
            {
                if (InputRouter.Instance != null) return InputRouter.Instance.State.Move;
                return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            }
        }

        public static Vector2 Look
        {
            get
            {
                if (InputRouter.Instance != null) return InputRouter.Instance.State.Look;
                return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
        }

        public static bool InteractPressed
        {
            get
            {
                if (InputRouter.Instance != null) return InputRouter.Instance.State.InteractPressed;
                return Input.GetKeyDown(KeyCode.E);
            }
        }

        public static bool SecondaryPressed
        {
            get
            {
                if (InputRouter.Instance != null) return InputRouter.Instance.State.SecondaryPressed;
                return Input.GetKeyDown(KeyCode.F);
            }
        }

        public static bool ToggleInventoryPressed
        {
            get
            {
                if (InputRouter.Instance != null) return InputRouter.Instance.State.InventoryPressed;
                return Input.GetKeyDown(KeyCode.Tab);
            }
        }

        public static bool ClosePressed => Input.GetKeyDown(KeyCode.Escape);

        public static bool DropPressed =>
            Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Delete);
    }
}
