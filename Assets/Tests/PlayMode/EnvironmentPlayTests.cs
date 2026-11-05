using System.Collections;
using LabGame;
using LabGame.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LabGame.PlayModeTests
{
    /// <summary>
    /// Сквозные тесты системы окружения (ПЗ №4): ход времени,
    /// применение погоды к сцене и появление индикатора в HUD.
    /// </summary>
    public class EnvironmentPlayTests
    {
        private GameObject playerObject;
        private GameObject sunObject;

        [SetUp]
        public void SetUp()
        {
            LabServices.Reset();
            LabServices.AutoCreateInterface = false;
            GameplayInput.Locked = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (LabHUD.Instance != null) Object.DestroyImmediate(LabHUD.Instance.gameObject);
            if (playerObject != null) Object.Destroy(playerObject);
            if (sunObject != null) Object.Destroy(sunObject);

            LabServices.Reset();
        }

        [UnityTest]
        public IEnumerator Environment_BuildsAndAdvancesTime()
        {
            var environment = LabEnvironment.Ensure();

            Assert.IsNotNull(environment, "система окружения должна создаться");
            Assert.IsNotNull(environment.DayNight, "должен быть цикл суток");
            Assert.IsNotNull(environment.Weather, "должна быть система погоды");
            Assert.IsNotNull(environment.Controller, "должен быть контроллер освещения");

            environment.Weather.SetAutoChange(false);
            environment.DayNight.SetDayLength(24f); // сутки за 24 секунды → 1 час в секунду

            float before = environment.DayNight.Hour;

            yield return new WaitForSeconds(0.6f);

            Assert.Greater(environment.DayNight.Hour, before + 0.2f,
                "время должно идти вперёд");
        }

        [UnityTest]
        public IEnumerator Weather_ChangesAndAppliesFogAndRain()
        {
            var environment = LabEnvironment.Ensure();
            environment.Weather.SetAutoChange(false);

            environment.Weather.SetWeather(WeatherType.Rain, instant: true);

            yield return null;
            yield return null;

            Assert.AreEqual(WeatherType.Rain, environment.Weather.TargetType);
            Assert.Greater(environment.Weather.Current.rain, 0.5f, "интенсивность дождя должна вырасти");
            Assert.Greater(RenderSettings.fogDensity, 0.005f, "в дождь туман должен сгуститься");
            Assert.IsTrue(RenderSettings.fog, "туман должен быть включён");
            Assert.IsTrue(environment.Visuals.RainActive, "система частиц дождя должна включиться");
        }

        [UnityTest]
        public IEnumerator Weather_TransitionIsSmooth()
        {
            var environment = LabEnvironment.Ensure();
            environment.Weather.SetAutoChange(false);
            environment.Weather.SetTransitionSeconds(10f);

            environment.Weather.SetWeather(WeatherType.Storm);

            yield return null;

            float early = environment.Weather.Current.rain;
            Assert.IsTrue(environment.Weather.IsTransitioning, "переход должен ещё идти");
            Assert.Less(early, 0.9f, "дождь не должен включаться мгновенно");

            yield return new WaitForSeconds(0.5f);

            Assert.Greater(environment.Weather.Current.rain, early,
                "интенсивность должна расти постепенно");
        }

        [UnityTest]
        public IEnumerator Sun_IsBrighterAtNoonThanAtMidnight()
        {
            // В тестовой сцене нет своего света — создаём «солнце» вручную.
            sunObject = new GameObject("TestSun");
            sunObject.AddComponent<Light>().type = LightType.Directional;

            var environment = LabEnvironment.Ensure();
            environment.Weather.SetAutoChange(false);
            environment.Weather.SetWeather(WeatherType.Clear, instant: true);

            Assert.IsNotNull(environment.Controller.Sun, "контроллер должен найти направленный свет");

            environment.DayNight.SetHour(12f);
            yield return null;
            yield return null;

            float noon = environment.Controller.Sun.intensity;

            environment.DayNight.SetHour(0f);
            yield return null;
            yield return null;

            float midnight = environment.Controller.Sun.intensity;

            Assert.Greater(noon, midnight, "в полдень солнце должно светить ярче, чем в полночь");
            Assert.Greater(noon, 0.2f, "в полдень солнце должно быть включено");
        }

        [UnityTest]
        public IEnumerator Hud_ShowsEnvironment()
        {
            LabServices.AutoCreateInterface = true;

            playerObject = new GameObject("Player") { tag = "Player" };
            playerObject.AddComponent<PlayerInventory>();

            yield return null;

            Assert.IsNotNull(LabHUD.Instance, "HUD должен создаться");
            Assert.IsNotNull(LabHUD.Instance.Environment, "HUD должен знать о системе окружения");
            Assert.IsNotNull(LabHUD.Instance.Environment.DayNight);
            Assert.IsNotNull(LabHUD.Instance.Environment.Weather);
        }
    }
}
