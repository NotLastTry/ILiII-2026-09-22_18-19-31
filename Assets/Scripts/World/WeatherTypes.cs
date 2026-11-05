using System;
using UnityEngine;

namespace LabGame.World
{
    /// <summary>Тип погоды.</summary>
    public enum WeatherType
    {
        Clear = 0,
        Cloudy = 1,
        Rain = 2,
        Storm = 3,
        Fog = 4,
        Snow = 5
    }

    /// <summary>
    /// Параметры погоды. Переход между состояниями — плавная интерполяция
    /// этих чисел, поэтому дождь «нарастает», а не включается рывком.
    /// </summary>
    [Serializable]
    public struct WeatherSettings
    {
        [Range(0f, 1f)] public float cloudiness;
        [Range(0f, 1f)] public float rain;
        [Range(0f, 1f)] public float snow;
        [Min(0f)] public float fogDensity;
        [Range(0f, 1f)] public float sunMultiplier;
        [Range(0f, 2f)] public float ambientMultiplier;
        [Range(0f, 1f)] public float wind;

        public static WeatherSettings Lerp(WeatherSettings a, WeatherSettings b, float t)
        {
            t = Mathf.Clamp01(t);

            return new WeatherSettings
            {
                cloudiness = Mathf.Lerp(a.cloudiness, b.cloudiness, t),
                rain = Mathf.Lerp(a.rain, b.rain, t),
                snow = Mathf.Lerp(a.snow, b.snow, t),
                fogDensity = Mathf.Lerp(a.fogDensity, b.fogDensity, t),
                sunMultiplier = Mathf.Lerp(a.sunMultiplier, b.sunMultiplier, t),
                ambientMultiplier = Mathf.Lerp(a.ambientMultiplier, b.ambientMultiplier, t),
                wind = Mathf.Lerp(a.wind, b.wind, t)
            };
        }
    }

    /// <summary>Готовые наборы параметров для каждого типа погоды.</summary>
    public static class WeatherPresets
    {
        public static readonly WeatherType[] All =
        {
            WeatherType.Clear,
            WeatherType.Cloudy,
            WeatherType.Rain,
            WeatherType.Storm,
            WeatherType.Fog,
            WeatherType.Snow
        };

        public static WeatherSettings Get(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Cloudy:
                    return new WeatherSettings
                    {
                        cloudiness = 0.75f,
                        rain = 0f,
                        snow = 0f,
                        fogDensity = 0.004f,
                        sunMultiplier = 0.55f,
                        ambientMultiplier = 0.85f,
                        wind = 0.30f
                    };

                case WeatherType.Rain:
                    return new WeatherSettings
                    {
                        cloudiness = 0.95f,
                        rain = 0.8f,
                        snow = 0f,
                        fogDensity = 0.012f,
                        sunMultiplier = 0.25f,
                        ambientMultiplier = 0.70f,
                        wind = 0.50f
                    };

                case WeatherType.Storm:
                    return new WeatherSettings
                    {
                        cloudiness = 1f,
                        rain = 1f,
                        snow = 0f,
                        fogDensity = 0.018f,
                        sunMultiplier = 0.12f,
                        ambientMultiplier = 0.55f,
                        wind = 1f
                    };

                case WeatherType.Fog:
                    return new WeatherSettings
                    {
                        cloudiness = 0.60f,
                        rain = 0f,
                        snow = 0f,
                        fogDensity = 0.045f,
                        sunMultiplier = 0.40f,
                        ambientMultiplier = 0.80f,
                        wind = 0.05f
                    };

                case WeatherType.Snow:
                    return new WeatherSettings
                    {
                        cloudiness = 0.85f,
                        rain = 0f,
                        snow = 0.9f,
                        fogDensity = 0.020f,
                        sunMultiplier = 0.35f,
                        ambientMultiplier = 0.90f,
                        wind = 0.35f
                    };

                default:
                    return new WeatherSettings
                    {
                        cloudiness = 0.05f,
                        rain = 0f,
                        snow = 0f,
                        fogDensity = 0.0015f,
                        sunMultiplier = 1f,
                        ambientMultiplier = 1f,
                        wind = 0.15f
                    };
            }
        }

        /// <summary>Относительная вероятность выбора погоды.</summary>
        public static float Weight(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Clear: return 3f;
                case WeatherType.Cloudy: return 3f;
                case WeatherType.Rain: return 2.5f;
                case WeatherType.Storm: return 1f;
                case WeatherType.Fog: return 1.5f;
                case WeatherType.Snow: return 0.8f;
                default: return 1f;
            }
        }

        public static string Russian(WeatherType type)
        {
            switch (type)
            {
                case WeatherType.Clear: return "Ясно";
                case WeatherType.Cloudy: return "Облачно";
                case WeatherType.Rain: return "Дождь";
                case WeatherType.Storm: return "Гроза";
                case WeatherType.Fog: return "Туман";
                case WeatherType.Snow: return "Снег";
                default: return type.ToString();
            }
        }
    }

    /// <summary>
    /// Выбор следующей погоды. Чистая функция: результат зависит только
    /// от текущего типа и случайного числа, поэтому легко тестируется.
    /// </summary>
    public static class WeatherSelector
    {
        /// <summary>
        /// <paramref name="roll"/> — случайное число 0…1.
        /// Текущая погода исключается, чтобы не «меняться» сама на себя.
        /// </summary>
        public static WeatherType PickNext(WeatherType current, WeatherType[] all, double roll)
        {
            if (all == null || all.Length == 0) return current;

            float total = 0f;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == current && all.Length > 1) continue;
                total += Mathf.Max(0.01f, WeatherPresets.Weight(all[i]));
            }

            if (total <= 0f) return all[0];

            double target = Mathf.Repeat((float)roll, 1f) * total;

            WeatherType last = all[0];

            for (int i = 0; i < all.Length; i++)
            {
                var type = all[i];
                if (type == current && all.Length > 1) continue;

                last = type;
                target -= Mathf.Max(0.01f, WeatherPresets.Weight(type));

                if (target <= 0d) return type;
            }

            return last;
        }
    }
}
