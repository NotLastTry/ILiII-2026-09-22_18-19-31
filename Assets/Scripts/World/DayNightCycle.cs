using System;
using UnityEngine;

namespace LabGame.World
{
    /// <summary>
    /// Смена времени суток: часы идут с заданной скоростью, поддерживаются
    /// пауза, ручная перемотка и события смены часа/части суток.
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Цикл")]
        [Tooltip("Сколько реальных секунд длится полный игровой день.")]
        [SerializeField, Min(10f)] private float dayLengthSeconds = 600f;

        [SerializeField, Range(0f, 24f)] private float startHour = 9f;

        [SerializeField] private bool paused;

        /// <summary>Текущий игровой час, 0…24.</summary>
        public float Hour { get; private set; }

        /// <summary>Номер игрового дня (начинается с 1).</summary>
        public int DayNumber { get; private set; } = 1;

        public DayPhase Phase => TimeOfDayMath.Phase(Hour);

        public bool Paused => paused;

        public float DayLengthSeconds => dayLengthSeconds;

        public event Action<float> HourChanged;
        public event Action<DayPhase> PhaseChanged;

        private DayPhase lastPhase;
        private bool initialized;

        private void Awake() => Initialize();

        /// <summary>
        /// Выставляет стартовое время. Идемпотентно: в редакторе Awake
        /// не вызывается, поэтому инициализацию запускает LabEnvironment.
        /// </summary>
        public void Initialize()
        {
            if (initialized) return;

            initialized = true;
            Hour = TimeOfDayMath.WrapHour(startHour);
            lastPhase = Phase;
        }

        private void Update()
        {
            if (paused) return;

            float hoursPerSecond = TimeOfDayMath.HoursPerDay / Mathf.Max(1f, dayLengthSeconds);
            AdvanceHours(hoursPerSecond * Time.deltaTime);
        }

        /// <summary>Перемотать время вперёд на указанное число часов.</summary>
        public void AdvanceHours(float hours)
        {
            if (hours <= 0f) return;

            float before = Hour;
            Hour = TimeOfDayMath.WrapHour(Hour + hours);

            if (Hour < before) DayNumber++;

            HourChanged?.Invoke(Hour);
            RaisePhaseIfChanged();
        }

        /// <summary>Задать время напрямую (например, 6.5 — половина седьмого).</summary>
        public void SetHour(float hour)
        {
            Hour = TimeOfDayMath.WrapHour(hour);
            HourChanged?.Invoke(Hour);
            RaisePhaseIfChanged();
        }

        public void SetPaused(bool value) => paused = value;

        public void TogglePause() => paused = !paused;

        public void SetDayLength(float seconds) => dayLengthSeconds = Mathf.Max(10f, seconds);

        private void RaisePhaseIfChanged()
        {
            var phase = Phase;
            if (phase == lastPhase) return;

            lastPhase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
