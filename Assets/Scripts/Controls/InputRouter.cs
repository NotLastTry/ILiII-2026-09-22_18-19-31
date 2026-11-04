using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LabGame.Controls
{
    /// <summary>
    /// Центральный узел управления (ПЗ №2).
    ///
    /// Определяет, каким устройством играет человек (клавиатура и мышь,
    /// геймпад или сенсорный экран), и опрашивает только соответствующую
    /// схему. Все остальные системы читают готовое <see cref="PlayerInputState"/>
    /// и не знают, откуда пришёл ввод.
    ///
    /// Опрос ленивый: состояние пересчитывается один раз за кадр при первом
    /// обращении, поэтому порядок Update/LateUpdate у компонентов не важен.
    /// </summary>
    [DisallowMultipleComponent]
    public class InputRouter : MonoBehaviour
    {
        public static InputRouter Instance { get; private set; }

        [Header("Определение устройства")]
        [Tooltip("Auto — определять автоматически; остальное — принудительно (для демонстрации).")]
        [SerializeField] private InputDeviceKind forcedDevice = InputDeviceKind.Auto;

        [SerializeField] private bool logDeviceChanges = true;

        private readonly KeyboardMouseScheme keyboardMouse = new KeyboardMouseScheme();
        private readonly GamepadScheme gamepad = new GamepadScheme();
        private readonly TouchScheme touch = new TouchScheme();

        private InputDeviceKind activeDevice = InputDeviceKind.KeyboardMouse;
        private PlayerInputState state;
        private int polledFrame = -1;
        private float lastTouchTime = -10f;

        /// <summary>Устройство, которым управляют прямо сейчас.</summary>
        public InputDeviceKind ActiveDevice
        {
            get
            {
                PollIfNeeded();
                return activeDevice;
            }
        }

        /// <summary>Текущее принудительное устройство (Auto — автоопределение).</summary>
        public InputDeviceKind ForcedDevice
        {
            get => forcedDevice;
            set
            {
                forcedDevice = value;
                polledFrame = -1;
            }
        }

        /// <summary>Готовое состояние ввода на этот кадр.</summary>
        public PlayerInputState State
        {
            get
            {
                PollIfNeeded();
                return state;
            }
        }

        public event Action<InputDeviceKind> DeviceChanged;

        private void Awake()
        {
            Instance = this;

            activeDevice = DeviceDetection.ResolveInitial(
                DeviceDetection.IsMobileDevice,
                Touchscreen.current != null,
                Gamepad.current != null,
                Keyboard.current != null);

            if (logDeviceChanges)
                Debug.Log($"[Input] стартовое устройство: {DeviceDetection.RussianName(activeDevice)}");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // F1 — демонстрация: переключить устройство вручную.
            if (Input.GetKeyDown(KeyCode.F1)) CycleForcedDevice();
        }

        // ─────────────────────────────────────────────────────────────
        //  Публичный API
        // ─────────────────────────────────────────────────────────────

        /// <summary>Auto → клавиатура → геймпад → сенсор → Auto.</summary>
        public void CycleForcedDevice()
        {
            var next = forcedDevice switch
            {
                InputDeviceKind.Auto => InputDeviceKind.KeyboardMouse,
                InputDeviceKind.KeyboardMouse => InputDeviceKind.Gamepad,
                InputDeviceKind.Gamepad => InputDeviceKind.Touch,
                _ => InputDeviceKind.Auto
            };

            ForcedDevice = next;

            if (next == InputDeviceKind.Auto)
            {
                var resolved = DeviceDetection.ResolveInitial(
                    DeviceDetection.IsMobileDevice,
                    Touchscreen.current != null,
                    Gamepad.current != null,
                    Keyboard.current != null);

                SetActive(resolved);
            }
            else
            {
                SetActive(next);
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Внутреннее
        // ─────────────────────────────────────────────────────────────

        private void PollIfNeeded()
        {
            int frame = Time.frameCount;
            if (polledFrame == frame) return;
            polledFrame = frame;

            DetectDevice();

            var result = PlayerInputState.Neutral;

            // Пока открыт инвентарь, игровой ввод заблокирован.
            if (!GameplayInput.Locked)
                GetScheme(activeDevice).Poll(ref result);

            state = result;
        }

        private IPlayerInputScheme GetScheme(InputDeviceKind kind)
        {
            switch (kind)
            {
                case InputDeviceKind.Gamepad: return gamepad;
                case InputDeviceKind.Touch: return touch;
                default: return keyboardMouse;
            }
        }

        /// <summary>
        /// Автоопределение по последней активности: то устройство,
        /// которым только что воспользовались, и становится активным.
        /// </summary>
        private void DetectDevice()
        {
            if (forcedDevice != InputDeviceKind.Auto)
            {
                SetActive(forcedDevice);
                return;
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                if (touchscreen.primaryTouch.press.wasPressedThisFrame)
                {
                    lastTouchTime = Time.unscaledTime;
                    SetActive(InputDeviceKind.Touch);
                    return;
                }

                var touches = touchscreen.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (!touches[i].press.isPressed) continue;

                    lastTouchTime = Time.unscaledTime;
                    SetActive(InputDeviceKind.Touch);
                    return;
                }
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                bool stick = pad.leftStick.ReadValue().sqrMagnitude > 0.04f ||
                             pad.rightStick.ReadValue().sqrMagnitude > 0.04f;

                bool buttons = pad.buttonSouth.wasPressedThisFrame ||
                               pad.buttonEast.wasPressedThisFrame ||
                               pad.buttonWest.wasPressedThisFrame ||
                               pad.buttonNorth.wasPressedThisFrame ||
                               pad.startButton.wasPressedThisFrame;

                if (stick || buttons)
                {
                    SetActive(InputDeviceKind.Gamepad);
                    return;
                }
            }

            // На мобильных клавиатуру не считаем «основным» устройством:
            // иначе случайное событие клавиши спрячет джойстик.
            var keyboard = Keyboard.current;
            if (!DeviceDetection.IsMobileDevice && keyboard != null && keyboard.anyKey.wasPressedThisFrame)
            {
                SetActive(InputDeviceKind.KeyboardMouse);
                return;
            }

            var mouse = Mouse.current;
            if (mouse != null && !DeviceDetection.IsMobileDevice)
            {
                // На устройствах с тачскрином касание часто дублируется «виртуальной мышью».
                // Если сенсор использовали только что — мышь игнорируем, иначе джойстик
                // будет пропадать сразу после первого нажатия.
                if (Time.unscaledTime - lastTouchTime < 1f) return;

                bool clicked = mouse.leftButton.wasPressedThisFrame ||
                               mouse.rightButton.wasPressedThisFrame;

                bool moved = mouse.delta.ReadValue().sqrMagnitude > 4f;

                if (clicked || moved)
                {
                    SetActive(InputDeviceKind.KeyboardMouse);
                }
            }
        }

        private void SetActive(InputDeviceKind kind)
        {
            if (kind == activeDevice) return;

            activeDevice = kind;

            if (logDeviceChanges)
                Debug.Log($"[Input] активное устройство: {DeviceDetection.RussianName(kind)}");

            DeviceChanged?.Invoke(kind);
        }
    }
}
