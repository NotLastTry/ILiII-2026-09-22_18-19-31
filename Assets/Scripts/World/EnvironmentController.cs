using UnityEngine;
using UnityEngine.Rendering;

namespace LabGame.World
{
    /// <summary>
    /// Применяет состояние времени суток и погоды к сцене:
    /// поворот и яркость солнца, цвет ambient-света, туман и осадки.
    /// </summary>
    public class EnvironmentController : MonoBehaviour
    {
        [Header("Солнце")]
        [SerializeField] private Light sun;
        [SerializeField] private float maxSunIntensity = 2f;
        [SerializeField] private float sunYaw = 170f;
        [SerializeField] private Color daySunColor = new Color(1f, 0.97f, 0.90f);
        [SerializeField] private Color warmSunColor = new Color(1f, 0.55f, 0.28f);

        [Header("Свет окружения")]
        [SerializeField] private Color dayAmbient = new Color(0.55f, 0.58f, 0.66f);
        [SerializeField] private Color nightAmbient = new Color(0.05f, 0.07f, 0.13f);

        [Header("Туман")]
        [SerializeField] private Color dayFog = new Color(0.70f, 0.78f, 0.88f);
        [SerializeField] private Color nightFog = new Color(0.04f, 0.06f, 0.10f);
        [SerializeField] private float nightFogBoost = 1.8f;

        private DayNightCycle dayNight;
        private WeatherSystem weather;
        private WeatherVisuals visuals;

        public Light Sun => sun;

        /// <summary>Подключить источники данных и сразу применить состояние.</summary>
        public void Bind(DayNightCycle time, WeatherSystem weatherSystem, WeatherVisuals weatherVisuals, Light sunLight)
        {
            dayNight = time;
            weather = weatherSystem;
            visuals = weatherVisuals;

            if (sunLight != null) sun = sunLight;

            // Скайбокс ориентируется по этой лампе, поэтому она должна быть «солнцем» сцены.
            if (sun != null) RenderSettings.sun = sun;

            Apply();
        }

        private void LateUpdate() => Apply();

        public void Apply()
        {
            if (dayNight == null || weather == null) return;

            var settings = weather.Current;

            float elevation = TimeOfDayMath.SunElevation(dayNight.Hour);
            float daylight = Mathf.Clamp01(elevation);

            ApplySun(elevation, settings);
            ApplyAmbient(daylight, settings);
            ApplyFog(daylight, settings);

            if (visuals != null)
                visuals.Apply(settings.rain, settings.snow, settings.wind);
        }

        private void ApplySun(float elevation, WeatherSettings settings)
        {
            if (sun == null) return;

            sun.transform.rotation = Quaternion.Euler(TimeOfDayMath.SunAngle(dayNight.Hour), sunYaw, 0f);

            float intensity = maxSunIntensity * Mathf.Clamp01(elevation * 1.5f) * settings.sunMultiplier;

            sun.intensity = intensity;
            sun.color = Color.Lerp(warmSunColor, daySunColor, Mathf.Clamp01(elevation * 1.3f));
            sun.enabled = intensity > 0.02f;
        }

        private void ApplyAmbient(float daylight, WeatherSettings settings)
        {
            Color color = Color.Lerp(nightAmbient, dayAmbient, daylight) *
                          Mathf.Clamp(settings.ambientMultiplier, 0f, 2f);

            color.a = 1f;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = color;
        }

        private void ApplyFog(float daylight, WeatherSettings settings)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(nightFog, dayFog, daylight);
            RenderSettings.fogDensity = Mathf.Max(0.0004f,
                settings.fogDensity * Mathf.Lerp(nightFogBoost, 1f, daylight));
        }
    }
}
