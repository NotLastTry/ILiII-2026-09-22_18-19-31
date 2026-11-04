namespace LabGame.Controls
{
    /// <summary>
    /// Сенсорное управление: виртуальный джойстик для движения,
    /// зона обзора справа и экранные кнопки действий.
    /// Значения берутся из <see cref="TouchControlsUI"/>, который создаёт HUD.
    /// </summary>
    public sealed class TouchScheme : IPlayerInputScheme
    {
        public InputDeviceKind Kind => InputDeviceKind.Touch;

        public void Poll(ref PlayerInputState state)
        {
            var ui = TouchControlsUI.Instance;
            if (ui == null) return;

            state.Move = ui.JoystickValue;
            state.Look = ui.ConsumeLook();

            state.JumpHeld = ui.JumpButton.IsHeld;
            state.JumpPressed = ui.JumpButton.ConsumePressed();
            state.JumpReleased = ui.JumpButton.ConsumeReleased();

            state.CrouchHeld = ui.CrouchButton.IsHeld;

            state.InteractPressed = ui.InteractButton.ConsumePressed();
            state.SecondaryPressed = ui.SecondaryButton.ConsumePressed();
            state.InventoryPressed = ui.InventoryButton.ConsumePressed();
        }
    }
}
