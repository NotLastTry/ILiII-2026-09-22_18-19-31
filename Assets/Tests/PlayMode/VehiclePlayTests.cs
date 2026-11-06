using System.Collections;
using System.Collections.Generic;
using LabGame;
using LabGame.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LabGame.PlayModeTests
{
    /// <summary>
    /// Сквозные проверки техники (ПЗ №7 и №8): машина едет и преследует,
    /// самолёт летит, набирает высоту и переходит в погоню.
    /// </summary>
    public class VehiclePlayTests
    {
        private readonly List<GameObject> created = new List<GameObject>();
        private GameObject playerObject;

        [SetUp]
        public void SetUp()
        {
            LabServices.Reset();
            LabServices.AutoCreateInterface = false;
            GameplayInput.Locked = false;
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            for (int i = 0; i < created.Count; i++)
                if (created[i] != null) Object.Destroy(created[i]);

            created.Clear();

            if (playerObject != null) Object.Destroy(playerObject);
            playerObject = null;

            LabServices.Reset();
        }

        private void CreatePlayer(Vector3 position)
        {
            playerObject = new GameObject("Player") { tag = "Player" };
            playerObject.transform.position = position;
        }

        private GroundVehicleController CreateCar(Vector3 position)
        {
            var go = new GameObject("Car");
            go.transform.position = position;
            created.Add(go);

            return go.AddComponent<GroundVehicleController>();
        }

        private AircraftController CreatePlane(Vector3 position)
        {
            var go = new GameObject("Plane");
            go.transform.position = position;
            created.Add(go);

            return go.AddComponent<AircraftController>();
        }

        [UnityTest]
        public IEnumerator Car_ChasesPlayer_WhenClose()
        {
            CreatePlayer(new Vector3(0f, 0f, 6f));
            var car = CreateCar(Vector3.zero);

            yield return null;
            yield return null;

            Assert.AreEqual(GroundVehicleController.VehicleMode.Chase, car.Mode,
                "в радиусе обнаружения машина переходит к погоне");
        }

        [UnityTest]
        public IEnumerator Car_Patrols_WhenPlayerIsFar()
        {
            CreatePlayer(new Vector3(0f, 0f, 200f));
            var car = CreateCar(Vector3.zero);

            yield return null;
            yield return null;

            Assert.AreEqual(GroundVehicleController.VehicleMode.Patrol, car.Mode);
        }

        [UnityTest]
        public IEnumerator Car_DrivesForward()
        {
            CreatePlayer(new Vector3(0f, 0f, 200f));
            var car = CreateCar(Vector3.zero);

            float startZ = car.transform.position.z;

            yield return new WaitForSeconds(1f);

            Assert.Greater(car.Speed, 0.5f, "машина должна разгоняться");
            Assert.Greater(car.transform.position.z, startZ + 0.5f, "машина должна ехать вперёд");
        }

        [UnityTest]
        public IEnumerator Aircraft_ClimbsAndFliesForward()
        {
            CreatePlayer(new Vector3(0f, 0f, 200f));
            var plane = CreatePlane(new Vector3(0f, 2f, 0f));

            float startY = plane.transform.position.y;
            float startZ = plane.transform.position.z;

            yield return new WaitForSeconds(1f);

            Assert.Greater(plane.Speed, 0.5f, "самолёт должен набирать скорость");
            Assert.Greater(plane.transform.position.y, startY + 0.2f, "самолёт должен уходить от земли");
            Assert.Greater(plane.transform.position.z, startZ + 0.2f, "самолёт летит вперёд");
        }

        [UnityTest]
        public IEnumerator Aircraft_ChasesPlayer_WhenClose()
        {
            CreatePlayer(new Vector3(0f, 0f, 20f));
            var plane = CreatePlane(new Vector3(0f, 16f, 0f));

            yield return null;
            yield return null;

            Assert.AreEqual(AircraftController.FlightMode.Chase, plane.Mode);
        }

        [UnityTest]
        public IEnumerator Aircraft_Patrols_WhenPlayerIsFar()
        {
            CreatePlayer(new Vector3(0f, 0f, 200f));
            var plane = CreatePlane(new Vector3(0f, 16f, 0f));

            yield return null;
            yield return null;

            Assert.AreEqual(AircraftController.FlightMode.Patrol, plane.Mode);
        }
    }
}
