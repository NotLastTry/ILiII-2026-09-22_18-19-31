using LabGame.World;
using NUnit.Framework;
using UnityEngine;

namespace LabGame.EditModeTests
{
    /// <summary>
    /// Тесты логики времени суток и погоды (ПЗ №4).
    /// Сцена не нужна — проверяются чистые функции.
    /// </summary>
    public class EnvironmentTests
    {
        // ── Время суток ───────────────────────────────────────────

        [Test]
        public void WrapHour_NormalizesValue()
        {
            Assert.AreEqual(1f, TimeOfDayMath.WrapHour(25f), 0.001f);
            Assert.AreEqual(21f, TimeOfDayMath.WrapHour(-3f), 0.001f);
            Assert.AreEqual(0f, TimeOfDayMath.WrapHour(24f), 0.001f);
            Assert.AreEqual(12f, TimeOfDayMath.WrapHour(12f), 0.001f);
        }

        [Test]
        public void SunAngle_KeyHours()
        {
            Assert.AreEqual(0f, TimeOfDayMath.SunAngle(6f), 0.001f);
            Assert.AreEqual(90f, TimeOfDayMath.SunAngle(12f), 0.001f);
            Assert.AreEqual(180f, TimeOfDayMath.SunAngle(18f), 0.001f);
            Assert.AreEqual(-90f, TimeOfDayMath.SunAngle(0f), 0.001f);
        }

        [Test]
        public void SunElevation_KeyHours()
        {
            Assert.AreEqual(1f, TimeOfDayMath.SunElevation(12f), 0.001f);
            Assert.AreEqual(0f, TimeOfDayMath.SunElevation(6f), 0.001f);
            Assert.AreEqual(0f, TimeOfDayMath.SunElevation(18f), 0.001f);
            Assert.AreEqual(-1f, TimeOfDayMath.SunElevation(0f), 0.001f);
        }

        [Test]
        public void Daylight_IsZeroAtNight()
        {
            Assert.AreEqual(0f, TimeOfDayMath.Daylight(0f), 0.001f);
            Assert.AreEqual(1f, TimeOfDayMath.Daylight(12f), 0.001f);
            Assert.AreEqual(0f, TimeOfDayMath.Daylight(22f), 0.001f);
        }

        [Test]
        public void Phase_Boundaries()
        {
            Assert.AreEqual(DayPhase.Night, TimeOfDayMath.Phase(3f));
            Assert.AreEqual(DayPhase.Dawn, TimeOfDayMath.Phase(5f));
            Assert.AreEqual(DayPhase.Dawn, TimeOfDayMath.Phase(8.9f));
            Assert.AreEqual(DayPhase.Day, TimeOfDayMath.Phase(9f));
            Assert.AreEqual(DayPhase.Day, TimeOfDayMath.Phase(17.9f));
            Assert.AreEqual(DayPhase.Dusk, TimeOfDayMath.Phase(18f));
            Assert.AreEqual(DayPhase.Dusk, TimeOfDayMath.Phase(20.9f));
            Assert.AreEqual(DayPhase.Night, TimeOfDayMath.Phase(21f));
        }

        [Test]
        public void FormatTime_PadsHoursAndMinutes()
        {
            Assert.AreEqual("14:30", TimeOfDayMath.FormatTime(14.5f));
            Assert.AreEqual("00:00", TimeOfDayMath.FormatTime(0f));
            Assert.AreEqual("09:15", TimeOfDayMath.FormatTime(9.25f));
            Assert.AreEqual("23:59", TimeOfDayMath.FormatTime(23.99f));
            Assert.AreEqual("06:00", TimeOfDayMath.FormatTime(-18f));
        }

        [Test]
        public void PhaseName_IsNotEmpty()
        {
            foreach (DayPhase phase in System.Enum.GetValues(typeof(DayPhase)))
                Assert.IsFalse(string.IsNullOrEmpty(TimeOfDayMath.PhaseName(phase)));
        }

        // ── Погода ────────────────────────────────────────────────

        [Test]
        public void WeatherSettings_Lerp_Midpoint()
        {
            var clear = WeatherPresets.Get(WeatherType.Clear);
            var rain = WeatherPresets.Get(WeatherType.Rain);

            var middle = WeatherSettings.Lerp(clear, rain, 0.5f);

            Assert.AreEqual((clear.rain + rain.rain) * 0.5f, middle.rain, 0.001f);
            Assert.AreEqual((clear.fogDensity + rain.fogDensity) * 0.5f, middle.fogDensity, 0.001f);
            Assert.AreEqual((clear.sunMultiplier + rain.sunMultiplier) * 0.5f, middle.sunMultiplier, 0.001f);
        }

        [Test]
        public void Presets_MatchTheirNames()
        {
            Assert.AreEqual(0f, WeatherPresets.Get(WeatherType.Clear).rain, 0.001f);
            Assert.Greater(WeatherPresets.Get(WeatherType.Rain).rain, 0.5f);
            Assert.AreEqual(0f, WeatherPresets.Get(WeatherType.Rain).snow, 0.001f);

            Assert.Greater(WeatherPresets.Get(WeatherType.Snow).snow, 0.5f);
            Assert.AreEqual(0f, WeatherPresets.Get(WeatherType.Snow).rain, 0.001f);

            Assert.Greater(WeatherPresets.Get(WeatherType.Storm).rain,
                WeatherPresets.Get(WeatherType.Rain).rain);

            float foggiest = 0f;
            foreach (var type in WeatherPresets.All)
                foggiest = Mathf.Max(foggiest, WeatherPresets.Get(type).fogDensity);

            Assert.AreEqual(foggiest, WeatherPresets.Get(WeatherType.Fog).fogDensity, 0.0001f);
        }

        [Test]
        public void Selector_NeverReturnsCurrentWeather()
        {
            foreach (var current in WeatherPresets.All)
            {
                for (int i = 0; i < 100; i++)
                {
                    double roll = i / 100.0;
                    var next = WeatherSelector.PickNext(current, WeatherPresets.All, roll);

                    Assert.AreNotEqual(current, next, $"погода {current}, roll={roll}");
                }
            }
        }

        [Test]
        public void Selector_HeavyWeatherIsMoreLikely()
        {
            int clear = 0;
            int snow = 0;

            for (int i = 0; i < 1000; i++)
            {
                var next = WeatherSelector.PickNext(WeatherType.Storm, WeatherPresets.All, i / 1000.0);

                if (next == WeatherType.Clear) clear++;
                if (next == WeatherType.Snow) snow++;
            }

            Assert.Greater(clear, snow, "«Ясно» (вес 3) должно выпадать чаще «Снега» (вес 0.8)");
            Assert.Greater(clear, 0);
        }

        [Test]
        public void Selector_IsDeterministic()
        {
            var first = WeatherSelector.PickNext(WeatherType.Clear, WeatherPresets.All, 0.42);
            var second = WeatherSelector.PickNext(WeatherType.Clear, WeatherPresets.All, 0.42);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void Selector_WithSingleWeather_ReturnsIt()
        {
            var single = new[] { WeatherType.Fog };

            Assert.AreEqual(WeatherType.Fog, WeatherSelector.PickNext(WeatherType.Fog, single, 0.7));
        }

        // ── Осадки ────────────────────────────────────────────────

        [Test]
        public void SafeEmissionRate_KeepsSteadyStateUnderParticleLimit()
        {
            // Снег: 0.9 × 450 ≈ 405 частиц/с, время жизни 7 с.
            float snow = WeatherVisuals.SafeEmissionRate(0.9f, 450f, 7f, 4000);

            Assert.Greater(snow, 0f);
            Assert.LessOrEqual(snow * 7f, 4000f + 0.01f,
                "снежинки не должны упираться в maxParticles, иначе снег идёт с паузами");

            // Дождь: 1.0 × 2500 при времени жизни 1.6 с.
            float rain = WeatherVisuals.SafeEmissionRate(1f, 2500f, 1.6f, 6000);

            Assert.AreEqual(2500f, rain, 0.01f, "если лимит позволяет, используется запрошенная плотность");
            Assert.LessOrEqual(rain * 1.6f, 6000f + 0.01f);
        }

        [Test]
        public void SafeEmissionRate_ClampsWhenLifetimeIsLong()
        {
            // 1500 частиц при жизни 7 с → не более ~214 частиц/с.
            float rate = WeatherVisuals.SafeEmissionRate(0.9f, 450f, 7f, 1500);

            Assert.AreEqual(1500f / 7f, rate, 0.01f);
        }

        [Test]
        public void SafeEmissionRate_NoEmission_WhenWeatherIsOff()
        {
            Assert.AreEqual(0f, WeatherVisuals.SafeEmissionRate(0f, 450f, 7f, 4000), 0.001f);
        }
    }
}
