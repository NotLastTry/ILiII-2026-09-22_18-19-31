using UnityEngine;

namespace LabGame.Controls
{
    /// <summary>Клавиатура и мышь (совместимо с текущими настройками Input Manager).</summary>
    public sealed class KeyboardMouseScheme : IPlayerInputScheme
    {
        public InputDeviceKind Kind => InputDeviceKind.KeyboardMouse;

        public void Poll(ref PlayerInputState state)
        {
            state.Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            // Мышь уже отдаёт прирост за кадр — камера умножит его на чувствительность.
            state.Look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            state.JumpPressed = Input.GetButtonDown("Jump");
            state.JumpHeld = Input.GetButton("Jump");
            state.JumpReleased = Input.GetButtonUp("Jump");

            state.CrouchHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);

            state.InteractPressed = Input.GetKeyDown(KeyCode.E);
            state.SecondaryPressed = Input.GetKeyDown(KeyCode.F);
            state.InventoryPressed = Input.GetKeyDown(KeyCode.Tab);
        }
    }
}
