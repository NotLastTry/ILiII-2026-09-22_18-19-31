using LabGame.Controls;
using LabGame.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LabGame
{
    /// <summary>
    /// Корневой HUD лабораторных: создаёт Canvas, EventSystem,
    /// всплывающие сообщения, подсказку взаимодействия,
    /// статус хаотичного ИИ и сам экран инвентаря.
    /// Всё строится в рантайме — сцена остаётся «чистой».
    /// </summary>
    public class LabHUD : MonoBehaviour
    {
        public static LabHUD Instance { get; private set; }

        public Canvas Canvas { get; private set; }
        public RectTransform Root { get; private set; }
        public InventoryUI Inventory { get; private set; }

        private Text toastText;
        private Text promptText;
        private Text chaosText;
        private Image chaosBadge;

        private Text deviceText;
        private Text deviceDetailsText;
        private float deviceRefreshTimer;

        private float toastTimer;

        public static LabHUD Ensure(PlayerInventory player)
        {
            if (Instance != null) return Instance;

            var go = new GameObject("LabHUD");
            Instance = go.AddComponent<LabHUD>();
            Instance.Build(player);
            return Instance;
        }

        private void Build(PlayerInventory player)
        {
            EnsureEventSystem();

            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            Root = (RectTransform)transform;
            Root.anchorMin = Vector2.zero;
            Root.anchorMax = Vector2.one;
            Root.offsetMin = Vector2.zero;
            Root.offsetMax = Vector2.zero;

            BuildStatusWidgets();

            // Интерактор добавляем на игрока, если его ещё нет.
            if (player != null && player.GetComponent<PlayerInteractor>() == null)
                player.gameObject.AddComponent<PlayerInteractor>();

            // Сенсорный слой создаём раньше инвентаря, чтобы он оказался ниже него.
            TouchControlsUI.Create(Root);

            Inventory = InventoryUI.Create(this, player);
        }

        private void BuildStatusWidgets()
        {
            // Подсказка взаимодействия — по центру снизу.
            var promptPanel = UIFactory.CreatePanel(Root, "PromptPanel", 0f, 0f, 620f, 46f,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false);
            var promptRect = (RectTransform)promptPanel.transform.parent;
            promptRect.anchorMin = new Vector2(0.5f, 0f);
            promptRect.anchorMax = new Vector2(0.5f, 0f);
            promptRect.pivot = new Vector2(0.5f, 0f);
            promptRect.anchoredPosition = new Vector2(0f, 150f);
            promptRect.sizeDelta = new Vector2(620f, 46f);

            promptText = UIFactory.CreateText(promptPanel.transform, "Prompt", string.Empty, 22,
                UIFactory.TextMain, TextAnchor.MiddleCenter, 0f, 0f, 610f, 40f);

            // Всплывающие сообщения — ниже подсказки.
            var toastPanel = UIFactory.CreatePanel(Root, "ToastPanel", 0f, 0f, 700f, 44f,
                UIFactory.PanelFill, UIFactory.PanelFill, 1.5f, false);
            var toastRect = (RectTransform)toastPanel.transform.parent;
            toastRect.anchorMin = new Vector2(0.5f, 0f);
            toastRect.anchorMax = new Vector2(0.5f, 0f);
            toastRect.pivot = new Vector2(0.5f, 0f);
            toastRect.anchoredPosition = new Vector2(0f, 96f);
            toastRect.sizeDelta = new Vector2(700f, 44f);

            toastText = UIFactory.CreateText(toastPanel.transform, "Toast", string.Empty, 20,
                UIFactory.TextMain, TextAnchor.MiddleCenter, 0f, 0f, 690f, 38f);

            toastPanel.transform.parent.gameObject.SetActive(false);

            // Статус хаотичного NPC — в левом верхнем углу.
            var chaosPanel = UIFactory.CreatePanel(Root, "ChaosPanel", 24f, 24f, 460f, 74f,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false);

            chaosBadge = UIFactory.CreateImage(chaosPanel.transform, "Badge", 10f, 10f, 54f, 54f,
                new Color(0.72f, 0.72f, 0.72f, 1f));

            var badgeLetter = UIFactory.CreateText(chaosBadge.transform, "Letter", "?", 30,
                UIFactory.TextMain, TextAnchor.MiddleCenter, 0f, 0f, 54f, 54f, FontStyle.Bold);
            badgeLetter.raycastTarget = false;

            chaosText = UIFactory.CreateText(chaosPanel.transform, "Chaos", string.Empty, 17,
                UIFactory.TextDim, TextAnchor.UpperLeft, 76f, 8f, 372f, 58f);

            chaosPanel.transform.parent.gameObject.SetActive(false);

            // Индикатор устройства ввода — в правом верхнем углу (ПЗ №2).
            var devicePanel = UIFactory.CreatePanel(Root, "DevicePanel", 0f, 0f, 560f, 96f,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false);

            var deviceRect = (RectTransform)devicePanel.transform.parent;
            deviceRect.anchorMin = deviceRect.anchorMax = new Vector2(1f, 1f);
            deviceRect.pivot = new Vector2(1f, 1f);
            deviceRect.anchoredPosition = new Vector2(-24f, -24f);
            deviceRect.sizeDelta = new Vector2(560f, 96f);

            deviceText = UIFactory.CreateText(devicePanel.transform, "Device", string.Empty, 16,
                UIFactory.TextMain, TextAnchor.UpperLeft, 12f, 8f, 536f, 26f);

            deviceDetailsText = UIFactory.CreateText(devicePanel.transform, "Details", string.Empty, 13,
                UIFactory.TextDim, TextAnchor.UpperLeft, 12f, 36f, 536f, 52f);

            ChaosHud = this;
        }

        public static LabHUD ChaosHud { get; private set; }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var existing = FindAnyObjectByType<EventSystem>();
            if (existing != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;

                var panel = toastText.transform.parent.parent.gameObject;
                if (toastTimer <= 0f) panel.SetActive(false);
                else
                {
                    var color = toastText.color;
                    color.a = Mathf.Clamp01(toastTimer);
                    toastText.color = color;
                }
            }

            UpdateDeviceLabel();
        }

        /// <summary>Показывает, какое устройство сейчас активно и что вообще подключено.</summary>
        private void UpdateDeviceLabel()
        {
            deviceRefreshTimer -= Time.unscaledDeltaTime;
            if (deviceRefreshTimer > 0f) return;

            deviceRefreshTimer = 0.5f;

            var router = InputRouter.Instance;
            if (router == null || deviceText == null) return;

            string mode = router.ForcedDevice == InputDeviceKind.Auto ? "авто" : "вручную";

            deviceText.text =
                $"Устройство: {DeviceDetection.RussianName(router.ActiveDevice)} ({mode})   F1 — сменить";

            if (deviceDetailsText != null)
            {
                deviceDetailsText.text =
                    $"Клавиатура: {YesNo(Keyboard.current != null)}   Мышь: {YesNo(Mouse.current != null)}\n" +
                    $"Геймпад: {YesNo(Gamepad.current != null)}   Тачскрин: {YesNo(Touchscreen.current != null)}";
            }
        }

        private static string YesNo(bool value) => value ? "да" : "нет";

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (ChaosHud == this) ChaosHud = null;
            GameplayInput.Locked = false;
        }

        // ─────────────────────────────────────────────────────────────
        //  Публичный API для остальных систем
        // ─────────────────────────────────────────────────────────────

        public void ShowToast(string message, float duration = 2.5f)
        {
            if (toastText == null || string.IsNullOrEmpty(message)) return;

            toastText.text = message;
            toastText.color = UIFactory.TextMain;
            toastText.transform.parent.parent.gameObject.SetActive(true);
            toastTimer = duration;
        }

        public static void Toast(string message, float duration = 2.5f)
        {
            if (Instance == null) return;
            Instance.ShowToast(message, duration);
        }

        public static void SetPrompt(string text)
        {
            if (Instance == null || Instance.promptText == null) return;

            bool visible = !string.IsNullOrEmpty(text);
            Instance.promptText.transform.parent.parent.gameObject.SetActive(visible);
            if (visible) Instance.promptText.text = $"[E]  {text}";
        }

        public static void SetChaosStatus(string text, Color? badgeColor = null)
        {
            if (Instance == null || Instance.chaosText == null) return;

            bool visible = !string.IsNullOrEmpty(text);
            Instance.chaosText.transform.parent.parent.gameObject.SetActive(visible);
            if (!visible) return;

            Instance.chaosText.text = text;
            if (badgeColor.HasValue && Instance.chaosBadge != null)
                Instance.chaosBadge.color = badgeColor.Value;
        }
    }
}
