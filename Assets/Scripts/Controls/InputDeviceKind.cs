using UnityEngine;

namespace LabGame.Controls
{
    /// <summary>Каким устройством игрок управляет персонажем.</summary>
    public enum InputDeviceKind
    {
        /// <summary>Определять автоматически по последней активности.</summary>
        Auto = 0,

        KeyboardMouse = 1,
        Gamepad = 2,
        Touch = 3
    }

    /// <summary>
    /// Чистая логика определения устройства (без обращения к Unity),
    /// чтобы её можно было проверить юнит-тестами.
    /// </summary>
    public static class DeviceDetection
    {
        /// <summary>Мобильная платформа (Android/iOS или портативное устройство).</summary>
        public static bool IsMobileDevice =>
            Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;

        /// <summary>
        /// Какое устройство использовать при запуске:
        ///   • мобильная платформа → сенсорный экран;
        ///   • есть только геймпад → геймпад;
        ///   • иначе → клавиатура и мышь.
        /// </summary>
        public static InputDeviceKind ResolveInitial(bool isMobile, bool touchSupported,
            bool gamepadConnected, bool keyboardConnected)
        {
            if (isMobile) return InputDeviceKind.Touch;

            // Планшет без клавиатуры, но с тачскрином (например, Windows-планшет).
            if (touchSupported && !keyboardConnected && !gamepadConnected) return InputDeviceKind.Touch;

            if (gamepadConnected && !keyboardConnected) return InputDeviceKind.Gamepad;

            return InputDeviceKind.KeyboardMouse;
        }

        public static string RussianName(InputDeviceKind kind)
        {
            switch (kind)
            {
                case InputDeviceKind.Touch: return "сенсорный экран";
                case InputDeviceKind.Gamepad: return "геймпад";
                case InputDeviceKind.KeyboardMouse: return "клавиатура и мышь";
                default: return "авто";
            }
        }
    }
}
