namespace LabGame.Controls
{
    /// <summary>
    /// Одна схема управления (клавиатура, геймпад, сенсор).
    /// Опрашивается только у активной схемы.
    /// </summary>
    public interface IPlayerInputScheme
    {
        InputDeviceKind Kind { get; }

        void Poll(ref PlayerInputState state);
    }
}
