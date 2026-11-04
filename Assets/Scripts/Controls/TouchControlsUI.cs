using LabGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace LabGame.Controls
{
    /// <summary>
    /// Сенсорный слой управления: джойстик слева, зона обзора справа,
    /// кнопки действий. Показывается только когда активна сенсорная схема.
    ///
    /// Слой создаётся в рантайме и ставится первым в иерархии Canvas,
    /// чтобы не перекрывать окно инвентаря.
    /// </summary>
    public class TouchControlsUI : MonoBehaviour
    {
        public static TouchControlsUI Instance { get; private set; }

        public VirtualJoystick Joystick { get; private set; }
        public TouchLookArea LookArea { get; private set; }

        public TouchButton JumpButton { get; private set; }
        public TouchButton CrouchButton { get; private set; }
        public TouchButton InteractButton { get; private set; }
        public TouchButton SecondaryButton { get; private set; }
        public TouchButton InventoryButton { get; private set; }

        private RectTransform layer;
        private bool visible;

        /// <summary>Показан ли сенсорный слой прямо сейчас.</summary>
        public bool IsVisible => visible;

        public Vector2 JoystickValue => Joystick != null ? Joystick.Value : Vector2.zero;

        public static TouchControlsUI Create(RectTransform parent)
        {
            var go = new GameObject("TouchControls", typeof(RectTransform));

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var ui = go.AddComponent<TouchControlsUI>();
            ui.Build();

            go.transform.SetAsFirstSibling();
            return ui;
        }

        private void Build()
        {
            Instance = this;

            layer = CreateFullScreen(transform, "Layer");

            BuildLookArea();
            BuildJoystick();
            BuildButtons();

            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            bool shouldShow = InputRouter.Instance != null &&
                              InputRouter.Instance.ActiveDevice == InputDeviceKind.Touch &&
                              !GameplayInput.Locked;

            if (shouldShow == visible) return;

            SetVisible(shouldShow);
        }

        public Vector2 ConsumeLook() => LookArea != null ? LookArea.ConsumeLook() : Vector2.zero;

        private void SetVisible(bool value)
        {
            visible = value;

            if (layer != null) layer.gameObject.SetActive(value);
            if (!value) ResetInput();

            if (Application.isMobilePlatform) return;

            if (value)
            {
                // Сенсорный режим включается на ПК вручную (F1): нужен свободный
                // курсор, иначе мышь «заперта» в центре и джойстик не нажать.
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (!GameplayInput.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void ResetInput()
        {
            if (Joystick != null) Joystick.ResetValue();
            if (LookArea != null) LookArea.ResetInput();

            if (JumpButton != null) JumpButton.ResetState();
            if (CrouchButton != null) CrouchButton.ResetState();
            if (InteractButton != null) InteractButton.ResetState();
            if (SecondaryButton != null) SecondaryButton.ResetState();
            if (InventoryButton != null) InventoryButton.ResetState();
        }

        // ─────────────────────────────────────────────────────────────
        //  Построение
        // ─────────────────────────────────────────────────────────────

        private void BuildLookArea()
        {
            var rect = new GameObject("LookArea", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(layer, false);

            // Правая половина экрана.
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UIFactory.RoundedSprite;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            LookArea = rect.gameObject.AddComponent<TouchLookArea>();
        }

        private void BuildJoystick()
        {
            const float size = 300f;

            var background = CreateAnchored(layer, "Joystick", new Vector2(0f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(size, size), new Vector2(240f, 240f));

            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.sprite = UIFactory.RoundedSprite;
            backgroundImage.color = new Color(0.78f, 0.78f, 0.78f, 0.45f);
            backgroundImage.raycastTarget = true;

            var knob = CreateAnchored(background, "Knob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(120f, 120f), Vector2.zero);

            var knobImage = knob.gameObject.AddComponent<Image>();
            knobImage.sprite = UIFactory.RoundedSprite;
            knobImage.color = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            knobImage.raycastTarget = false;

            var joystick = background.gameObject.AddComponent<VirtualJoystick>();
            joystick.radius = size * 0.5f;
            joystick.deadZone = 0.15f;
            joystick.knob = knob;

            Joystick = joystick;
        }

        private void BuildButtons()
        {
            JumpButton = CreateButton("Jump", "Прыжок", new Vector2(-180f, 200f), 150f);
            InteractButton = CreateButton("Interact", "Действие", new Vector2(-380f, 140f), 140f);
            CrouchButton = CreateButton("Crouch", "Присесть", new Vector2(-140f, 400f), 130f);
            SecondaryButton = CreateButton("Secondary", "Квест", new Vector2(-330f, 330f), 130f);
            InventoryButton = CreateButton("Inventory", "Инвентарь", new Vector2(-520f, 250f), 120f);
        }

        private TouchButton CreateButton(string name, string label, Vector2 position, float size)
        {
            var rect = CreateAnchored(layer, name, new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(size, size), position);

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = UIFactory.RoundedSprite;
            image.color = new Color(0.78f, 0.78f, 0.78f, 0.9f);
            image.raycastTarget = true;

            var text = UIFactory.CreateText(rect, "Label", label, size < 140f ? 16 : 18,
                UIFactory.TextMain, TextAnchor.MiddleCenter, 0f, 0f, size, size);

            text.raycastTarget = false;

            var button = rect.gameObject.AddComponent<TouchButton>();
            button.background = image;

            return button;
        }

        private static RectTransform CreateFullScreen(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform CreateAnchored(Transform parent, string name, Vector2 anchor, Vector2 pivot,
            Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }
    }
}
