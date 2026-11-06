using LabGame.AI;
using LabGame.Controls;
using LabGame.Dialogue;
using LabGame.UI;
using LabGame.Vehicles;
using LabGame.World;
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

        /// <summary>Система времени суток и погоды (ПЗ №4).</summary>
        public LabEnvironment Environment { get; private set; }

        private Text toastText;
        private Text promptText;
        private Text chaosText;
        private Image chaosBadge;

        private Text deviceText;
        private Text deviceDetailsText;
        private float deviceRefreshTimer;

        private Text worldText;
        private float worldRefreshTimer;

        private Text enemyText;
        private GameObject enemyPanelObject;
        private float enemyRefreshTimer;

        private Text crowdText;
        private float crowdRefreshTimer;

        private Text vehicleText;
        private float vehicleRefreshTimer;
        private GroundVehicleController car;
        private AircraftController aircraft;

        private float toastTimer;

        private bool bound;

        public static LabHUD Ensure(PlayerInventory player)
        {
            var hud = Instance;

            // HUD может быть заранее размещён в сцене — тогда используем его.
            if (hud == null) hud = Object.FindAnyObjectByType<LabHUD>();

            if (hud == null)
            {
                var go = new GameObject("LabHUD", typeof(RectTransform));
                hud = go.AddComponent<LabHUD>();
            }

            hud.Bind(player);
            return hud;
        }

        /// <summary>Подключить игрока и (при необходимости) достроить интерфейс.</summary>
        public void Bind(PlayerInventory player)
        {
            if (bound) return;

            bound = true;
            Instance = this;

            Build(player);
        }

        private void Build(PlayerInventory player)
        {
            EnsureEventSystem();

            Environment = LabEnvironment.Ensure();

            Canvas = GetComponent<Canvas>();

            if (Canvas == null)
            {
                Canvas = gameObject.AddComponent<Canvas>();
                Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Canvas.sortingOrder = 100;
            }

            var scaler = GetComponent<CanvasScaler>();

            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

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
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false, out bool promptCreated);

            var promptRect = (RectTransform)promptPanel.transform.parent;

            if (promptCreated)
            {
                promptRect.anchorMin = new Vector2(0.5f, 0f);
                promptRect.anchorMax = new Vector2(0.5f, 0f);
                promptRect.pivot = new Vector2(0.5f, 0f);
                promptRect.anchoredPosition = new Vector2(0f, 150f);
                promptRect.sizeDelta = new Vector2(620f, 46f);
            }

            promptText = UIFactory.CreateText(promptPanel.transform, "Prompt", string.Empty, 22,
                UIFactory.TextMain, TextAnchor.MiddleCenter, 0f, 0f, 610f, 40f);

            // Всплывающие сообщения — ниже подсказки.
            var toastPanel = UIFactory.CreatePanel(Root, "ToastPanel", 0f, 0f, 700f, 44f,
                UIFactory.PanelFill, UIFactory.PanelFill, 1.5f, false, out bool toastCreated);

            var toastRect = (RectTransform)toastPanel.transform.parent;

            if (toastCreated)
            {
                toastRect.anchorMin = new Vector2(0.5f, 0f);
                toastRect.anchorMax = new Vector2(0.5f, 0f);
                toastRect.pivot = new Vector2(0.5f, 0f);
                toastRect.anchoredPosition = new Vector2(0f, 96f);
                toastRect.sizeDelta = new Vector2(700f, 44f);
            }

            toastText = UIFactory.CreateText(toastPanel.transform, "Toast", string.Empty, 20,
                UIFactory.TextMain, TextAnchor.MiddleCenter, 0f, 0f, 690f, 38f);

            if (Application.isPlaying) toastPanel.transform.parent.gameObject.SetActive(false);

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

            if (Application.isPlaying) chaosPanel.transform.parent.gameObject.SetActive(false);

            // Индикатор устройства ввода — в правом верхнем углу (ПЗ №2).
            var devicePanel = UIFactory.CreatePanel(Root, "DevicePanel", 0f, 0f, 560f, 96f,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false, out bool deviceCreated);

            var deviceRect = (RectTransform)devicePanel.transform.parent;

            if (deviceCreated)
            {
                deviceRect.anchorMin = deviceRect.anchorMax = new Vector2(1f, 1f);
                deviceRect.pivot = new Vector2(1f, 1f);
                deviceRect.anchoredPosition = new Vector2(-24f, -24f);
                deviceRect.sizeDelta = new Vector2(560f, 96f);
            }

            deviceText = UIFactory.CreateText(devicePanel.transform, "Device", string.Empty, 16,
                UIFactory.TextMain, TextAnchor.UpperLeft, 12f, 8f, 536f, 26f);

            deviceDetailsText = UIFactory.CreateText(devicePanel.transform, "Details", string.Empty, 13,
                UIFactory.TextDim, TextAnchor.UpperLeft, 12f, 36f, 536f, 52f);

            // Время суток и погода — слева, под статусом NPC (ПЗ №4).
            var worldPanel = UIFactory.CreatePanel(Root, "WorldPanel", 24f, 110f, 460f, 96f,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false);

            worldText = UIFactory.CreateText(worldPanel.transform, "World", string.Empty, 16,
                UIFactory.TextMain, TextAnchor.UpperLeft, 12f, 8f, 436f, 82f);

            // Дерево поведения противника — слева, под временем и погодой (ПЗ №5).
            var enemyPanel = UIFactory.CreatePanel(Root, "EnemyPanel", 24f, 214f, 460f, 380f,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false);

            enemyPanelObject = enemyPanel.transform.parent.gameObject;

            enemyText = UIFactory.CreateText(enemyPanel.transform, "Enemy", string.Empty, 11,
                UIFactory.TextMain, TextAnchor.UpperLeft, 12f, 8f, 436f, 364f);

            if (Application.isPlaying) enemyPanelObject.SetActive(false);

            // Состояние толпы (ПЗ №6).
            var crowdPanel = UIFactory.CreatePanel(Root, "CrowdPanel", 24f, 602f, 460f, 78f,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false);

            crowdText = UIFactory.CreateText(crowdPanel.transform, "Crowd", string.Empty, 14,
                UIFactory.TextMain, TextAnchor.UpperLeft, 12f, 8f, 436f, 62f);

            // Техника — под индикатором устройства, справа (ПЗ №7 и №8).
            var vehiclePanel = UIFactory.CreatePanel(Root, "VehiclePanel", 0f, 0f, 560f, 92f,
                UIFactory.PanelFill, UIFactory.PanelFill, 2f, false);

            var vehicleRect = (RectTransform)vehiclePanel.transform.parent;

            if (vehicleRect != null)
            {
                vehicleRect.anchorMin = vehicleRect.anchorMax = new Vector2(1f, 1f);
                vehicleRect.pivot = new Vector2(1f, 1f);
                vehicleRect.anchoredPosition = new Vector2(-24f, -128f);
                vehicleRect.sizeDelta = new Vector2(560f, 92f);
            }

            vehicleText = UIFactory.CreateText(vehiclePanel.transform, "Vehicles", string.Empty, 14,
                UIFactory.TextMain, TextAnchor.UpperLeft, 12f, 8f, 536f, 76f);

            // Окно диалогов (ПЗ №9).
            DialogueUI.Ensure(this);

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
            UpdateWorldLabel();
            UpdateEnemyLabel();
            UpdateCrowdLabel();
            UpdateVehicleLabel();
        }

        /// <summary>Показывает состояние машины и самолёта (ПЗ №7 и №8).</summary>
        private void UpdateVehicleLabel()
        {
            vehicleRefreshTimer -= Time.unscaledDeltaTime;
            if (vehicleRefreshTimer > 0f) return;

            vehicleRefreshTimer = 0.2f;

            if (vehicleText == null) return;

            if (car == null) car = FindAnyObjectByType<GroundVehicleController>();
            if (aircraft == null) aircraft = FindAnyObjectByType<AircraftController>();

            string carLine = car != null
                ? $"Машина: {CarModeName(car.Mode)} · {car.Speed:0.0} м/с · нос {car.modelYawOffset:0}°"
                : "Машина: не найдена";

            string planeLine = aircraft != null
                ? $"Самолёт: {FlightModeName(aircraft.Mode)} · {aircraft.Speed:0.0} м/с · H {aircraft.Altitude:0} м · нос {aircraft.modelYawOffset:0}°"
                : "Самолёт: не найден";

            vehicleText.text = carLine + "\n" + planeLine;
        }

        private static string CarModeName(GroundVehicleController.VehicleMode mode)
        {
            switch (mode)
            {
                case GroundVehicleController.VehicleMode.Chase: return "погоня";
                case GroundVehicleController.VehicleMode.Waiting: return "остановка";
                default: return "патруль";
            }
        }

        private static string FlightModeName(AircraftController.FlightMode mode)
        {
            return mode == AircraftController.FlightMode.Chase ? "погоня" : "патруль";
        }

        /// <summary>Показывает состояние толпы (ПЗ №6).</summary>
        private void UpdateCrowdLabel()
        {
            crowdRefreshTimer -= Time.unscaledDeltaTime;
            if (crowdRefreshTimer > 0f) return;

            crowdRefreshTimer = 0.2f;

            if (crowdText == null) return;

            var crowd = CrowdManager.Instance;

            if (crowd == null)
            {
                crowdText.text = "Толпа: не найдена";
                return;
            }

            int calm = 0, curious = 0, panic = 0;

            var agents = crowd.Agents;

            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i] == null) continue;

                switch (agents[i].Mode)
                {
                    case CrowdMode.Panic: panic++; break;
                    case CrowdMode.Curious: curious++; break;
                    default: calm++; break;
                }
            }

            crowdText.text = $"Толпа: {agents.Count}   " +
                             $"спокойно {calm} · любопытно {curious} · паника {panic}\n" +
                             "H — напугать толпу";
        }

        /// <summary>Показывает состояние и дерево поведения противника (ПЗ №5).</summary>
        private void UpdateEnemyLabel()
        {
            enemyRefreshTimer -= Time.unscaledDeltaTime;
            if (enemyRefreshTimer > 0f) return;

            enemyRefreshTimer = 0.15f;

            var enemy = RovingNPC.Primary;

            if (enemy == null)
            {
                if (enemyPanelObject != null && enemyPanelObject.activeSelf)
                    enemyPanelObject.SetActive(false);

                return;
            }

            if (enemyPanelObject != null && !enemyPanelObject.activeSelf)
                enemyPanelObject.SetActive(true);

            if (enemyText == null) return;

            enemyText.text = $"Состояние: {EnemyStates.Russian(enemy.State)}\n" +
                             "Дерево поведения:\n" +
                             enemy.BuildTreeDebug();
        }

        /// <summary>Показывает игровое время, часть суток и текущую погоду.</summary>
        private void UpdateWorldLabel()
        {
            worldRefreshTimer -= Time.unscaledDeltaTime;
            if (worldRefreshTimer > 0f) return;

            worldRefreshTimer = 0.2f;

            if (Environment == null || Environment.DayNight == null || worldText == null) return;

            var time = Environment.DayNight;
            var weather = Environment.Weather;

            worldText.text =
                $"Время: {TimeOfDayMath.FormatTime(time.Hour)}  ({TimeOfDayMath.PhaseName(time.Phase)}), " +
                $"день {time.DayNumber}\n" +
                $"Погода: {WeatherPresets.Russian(weather.TargetType)}\n" +
                (time.Paused
                    ? "T — +1 час   Y — погода   U — продолжить"
                    : "T — +1 час   Y — погода   U — пауза");
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
