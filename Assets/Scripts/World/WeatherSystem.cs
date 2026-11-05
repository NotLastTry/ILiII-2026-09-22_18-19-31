using System;
using UnityEngine;

namespace LabGame.World
{
    /// <summary>
    /// Система погоды: хранит текущее и целевое состояние, плавно
    /// переводит параметры за <see cref="transitionSeconds"/> и умеет
    /// менять погоду сама через случайные интервалы.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        [Header("Переход")]
        [SerializeField, Min(0.1f)] private float transitionSeconds = 8f;

        [Header("Автосмена")]
        [SerializeField] private bool autoChange = true;
        [SerializeField] private Vector2 changeInterval = new Vector2(45f, 110f);

        [Header("Старт")]
        [SerializeField] private WeatherType startWeather = WeatherType.Clear;
        [SerializeField] private int randomSeed;

        private WeatherSettings from;
        private WeatherSettings to;

        private float blend = 1f;
        private float autoTimer;
        private System.Random random;
        private bool initialized;

        /// <summary>Погода, к которой идёт переход (или уже установленная).</summary>
        public WeatherType TargetType { get; private set; }

        /// <summary>Сглаженные параметры, которые применяет EnvironmentController.</summary>
        public WeatherSettings Current { get; private set; }

        public bool IsTransitioning => blend < 1f;

        public bool AutoChange
        {
            get => autoChange;
            set => autoChange = value;
        }

        public event Action<WeatherType> WeatherChanged;

        private void Awake() => Initialize();

        /// <summary>
        /// Готовит стартовую погоду. Идемпотентно: в редакторе Awake
        /// не вызывается, поэтому инициализацию запускает LabEnvironment.
        /// </summary>
        public void Initialize()
        {
            if (initialized) return;

            initialized = true;

            random = new System.Random(randomSeed != 0 ? randomSeed : Environment.TickCount);

            Current = WeatherPresets.Get(startWeather);
            TargetType = startWeather;
            from = Current;
            to = Current;
            blend = 1f;

            autoTimer = RollInterval();
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (blend < 1f)
            {
                blend = Mathf.Min(1f, blend + dt / Mathf.Max(0.1f, transitionSeconds));
                Current = WeatherSettings.Lerp(from, to, Mathf.SmoothStep(0f, 1f, blend));
            }

            if (!autoChange) return;

            autoTimer -= dt;
            if (autoTimer > 0f) return;

            autoTimer = RollInterval();
            CycleWeather();
        }

        /// <summary>Включить погоду. <paramref name="instant"/> — без плавного перехода.</summary>
        public void SetWeather(WeatherType type, bool instant = false)
        {
            TargetType = type;

            from = Current;
            to = WeatherPresets.Get(type);

            blend = instant ? 1f : 0f;
            if (instant) Current = to;

            WeatherChanged?.Invoke(type);
        }

        /// <summary>Случайная следующая погода (текущая исключается).</summary>
        public void CycleWeather()
        {
            double roll = random.NextDouble();
            var next = WeatherSelector.PickNext(TargetType, WeatherPresets.All, roll);
            SetWeather(next);
        }

        public void SetAutoChange(bool value) => autoChange = value;

        public void SetTransitionSeconds(float seconds) => transitionSeconds = Mathf.Max(0.1f, seconds);

        private float RollInterval()
        {
            float min = Mathf.Max(1f, changeInterval.x);
            float max = Mathf.Max(min, changeInterval.y);

            return Mathf.Lerp(min, max, (float)random.NextDouble());
        }
    }
}
