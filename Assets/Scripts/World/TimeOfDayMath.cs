using UnityEngine;

namespace LabGame.World
{
    /// <summary>Часть суток.</summary>
    public enum DayPhase
    {
        Night = 0,
        Dawn = 1,
        Day = 2,
        Dusk = 3
    }

    /// <summary>
    /// Чистая математика суточного цикла: положение солнца, высота,
    /// часть суток и форматирование времени. Вынесена отдельно,
    /// чтобы её можно было проверить тестами без сцены.
    /// </summary>
    public static class TimeOfDayMath
    {
        public const float HoursPerDay = 24f;

        /// <summary>Приводит час к диапазону 0…24.</summary>
        public static float WrapHour(float hour)
        {
            hour %= HoursPerDay;
            if (hour < 0f) hour += HoursPerDay;
            return hour;
        }

        /// <summary>
        /// Угол поворота солнца вокруг оси X: 6:00 → 0°, 12:00 → 90°,
        /// 18:00 → 180°, 0:00 → −90° (солнце под горизонтом).
        /// </summary>
        public static float SunAngle(float hour) => (WrapHour(hour) - 6f) * 15f;

        /// <summary>Высота солнца: 1 в полдень, 0 на рассвете и закате, −1 в полночь.</summary>
        public static float SunElevation(float hour) =>
            Mathf.Sin((WrapHour(hour) - 6f) / 12f * Mathf.PI);

        /// <summary>Освещённость 0…1 (сколько света даёт солнце).</summary>
        public static float Daylight(float hour) => Mathf.Clamp01(SunElevation(hour));

        public static DayPhase Phase(float hour)
        {
            float h = WrapHour(hour);

            if (h >= 5f && h < 9f) return DayPhase.Dawn;
            if (h >= 9f && h < 18f) return DayPhase.Day;
            if (h >= 18f && h < 21f) return DayPhase.Dusk;

            return DayPhase.Night;
        }

        public static string PhaseName(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.Dawn: return "Рассвет";
                case DayPhase.Day: return "День";
                case DayPhase.Dusk: return "Закат";
                default: return "Ночь";
            }
        }

        /// <summary>Часы и минуты в формате ЧЧ:ММ.</summary>
        public static string FormatTime(float hour)
        {
            float h = WrapHour(hour);

            int hours = Mathf.FloorToInt(h);
            int minutes = Mathf.FloorToInt((h - hours) * 60f);

            if (minutes > 59) minutes = 59;
            if (hours > 23) hours = 23;

            return $"{hours:00}:{minutes:00}";
        }
    }
}
