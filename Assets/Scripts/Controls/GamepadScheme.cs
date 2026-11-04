using UnityEngine;
using UnityEngine.InputSystem;

namespace LabGame.Controls
{
    /// <summary>Геймпад (читается через Input System, если он подключён).</summary>
    public sealed class GamepadScheme : IPlayerInputScheme
    {
        private const float LookSpeed = 0.35f;
        private const float DeadZone = 0.15f;

        public InputDeviceKind Kind => InputDeviceKind.Gamepad;

        public void Poll(ref PlayerInputState state)
        {
            var pad = Gamepad.current;
            if (pad == null) return;

            Vector2 move = pad.leftStick.ReadValue();
            if (move.magnitude < DeadZone) move = Vector2.zero;
            state.Move = move;

            Vector2 look = pad.rightStick.ReadValue();
            if (look.magnitude < DeadZone) look = Vector2.zero;
            state.Look = look * LookSpeed;

            state.JumpPressed = pad.buttonSouth.wasPressedThisFrame;
            state.JumpHeld = pad.buttonSouth.isPressed;
            state.JumpReleased = pad.buttonSouth.wasReleasedThisFrame;

            state.CrouchHeld = pad.buttonEast.isPressed;

            state.InteractPressed = pad.buttonWest.wasPressedThisFrame;
            state.SecondaryPressed = pad.buttonNorth.wasPressedThisFrame;
            state.InventoryPressed = pad.startButton.wasPressedThisFrame ||
                                     pad.selectButton.wasPressedThisFrame;
        }
    }
}
