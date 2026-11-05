using UnityEngine;

namespace LabGame.World
{
    /// <summary>
    /// Точка входа системы окружения: создаёт время суток, погоду,
    /// осадки и контроллер освещения, а также обрабатывает
    /// демонстрационные клавиши.
    ///
    ///   T — перемотать время на час;
    ///   Y — сменить погоду;
    ///   U — пауза/продолжить ход времени.
    /// </summary>
    public class LabEnvironment : MonoBehaviour
    {
        public static LabEnvironment Instance { get; private set; }

        public DayNightCycle DayNight { get; private set; }
        public WeatherSystem Weather { get; private set; }
        public EnvironmentController Controller { get; private set; }
        public WeatherVisuals Visuals { get; private set; }

        private bool built;

        /// <summary>
        /// Возвращает существующую систему окружения, находит её в сцене
        /// или создаёт новую. Компоненты времени и погоды можно заранее
        /// добавить на объект вручную — тогда используются их настройки.
        /// </summary>
        public static LabEnvironment Ensure()
        {
            var environment = Instance;

            // Система может быть заранее размещена в сцене — тогда используем её настройки.
            if (environment == null) environment = Object.FindAnyObjectByType<LabEnvironment>();

            if (environment == null)
            {
                var go = new GameObject("LabEnvironment");
                environment = go.AddComponent<LabEnvironment>();
            }

            // В редакторе Awake не вызывается, поэтому строим явно (метод идемпотентен).
            environment.Build();

            return environment;
        }

        private void Awake()
        {
            if (built) return;

            Instance = this;
            Build();
        }

        private void Build()
        {
            if (built) return;
            built = true;

            DayNight = GetComponent<DayNightCycle>();
            if (DayNight == null) DayNight = gameObject.AddComponent<DayNightCycle>();

            Weather = GetComponent<WeatherSystem>();
            if (Weather == null) Weather = gameObject.AddComponent<WeatherSystem>();

            Visuals = GetComponent<WeatherVisuals>();
            if (Visuals == null) Visuals = gameObject.AddComponent<WeatherVisuals>();

            Controller = GetComponent<EnvironmentController>();
            if (Controller == null) Controller = gameObject.AddComponent<EnvironmentController>();

            // В редакторе Awake не вызывается — инициализируем состояние явно.
            DayNight.Initialize();
            Weather.Initialize();
            Visuals.Initialize();

            Visuals.SetFollowTarget(FindPlayer());
            Controller.Bind(DayNight, Weather, Visuals, FindSun());
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // Пока открыт инвентарь, демонстрационные клавиши не срабатывают.
            if (GameplayInput.Locked) return;

            if (Input.GetKeyDown(KeyCode.T)) DayNight.AdvanceHours(1f);
            if (Input.GetKeyDown(KeyCode.Y)) Weather.CycleWeather();
            if (Input.GetKeyDown(KeyCode.U)) DayNight.TogglePause();
        }

        private static Transform FindPlayer()
        {
            var player = GameObject.FindWithTag("Player");
            return player != null ? player.transform : null;
        }

        private static Light FindSun()
        {
            if (RenderSettings.sun != null) return RenderSettings.sun;

            var lights = Object.FindObjectsByType<Light>();

            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null && lights[i].type == LightType.Directional)
                    return lights[i];

            Debug.LogWarning("[Environment] в сцене нет направленного источника света (Directional Light)");
            return null;
        }
    }
}
