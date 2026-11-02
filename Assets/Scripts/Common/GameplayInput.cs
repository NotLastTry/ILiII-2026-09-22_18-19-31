namespace LabGame
{
    /// <summary>
    /// Глобальный «замок» игрового ввода.
    /// Пока инвентарь открыт, персонаж не должен бегать и вращать камеру,
    /// поэтому все системы движения проверяют этот флаг.
    /// </summary>
    public static class GameplayInput
    {
        public static bool Locked { get; set; }

        public static void Reset() => Locked = false;
    }
}
