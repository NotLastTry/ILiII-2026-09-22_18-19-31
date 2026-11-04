using System.Collections;
using LabGame;
using LabGame.Controls;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LabGame.PlayModeTests
{
    /// <summary>
    /// Сквозные тесты системы управления (ПЗ №2): роутер устройств,
    /// переключение схем и появление сенсорного слоя.
    /// </summary>
    public class ControlsPlayTests
    {
        private GameObject playerObject;

        [SetUp]
        public void SetUp()
        {
            LabServices.Reset();
            LabServices.AutoCreateInterface = false;
            GameEventBus.ClearAll();
            InteractableRegistry.Clear();
            GameplayInput.Locked = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (LabHUD.Instance != null) Object.DestroyImmediate(LabHUD.Instance.gameObject);
            if (playerObject != null) Object.Destroy(playerObject);

            GameplayInput.Locked = false;
            LabServices.Reset();
        }

        private void CreatePlayer()
        {
            playerObject = new GameObject("Player");
            playerObject.tag = "Player";

            playerObject.AddComponent<Rigidbody>();
            playerObject.AddComponent<CapsuleCollider>();
            playerObject.AddComponent<PhysicsModule>();
            playerObject.AddComponent<PlayerInventory>();

            // InputReader сам создаёт InputRouter.
            playerObject.AddComponent<InputReader>();
        }

        [UnityTest]
        public IEnumerator Router_IsCreatedOnPlayer()
        {
            CreatePlayer();
            yield return null;

            var router = playerObject.GetComponent<InputRouter>();

            Assert.IsNotNull(router, "InputReader должен создать InputRouter");
            Assert.AreEqual(InputDeviceKind.KeyboardMouse, router.ActiveDevice,
                "в редакторе на ПК активным устройством должны быть клавиатура и мышь");
        }

        [UnityTest]
        public IEnumerator ForcedDevice_SwitchesActiveDevice()
        {
            CreatePlayer();
            yield return null;

            var router = playerObject.GetComponent<InputRouter>();

            router.ForcedDevice = InputDeviceKind.Touch;
            Assert.AreEqual(InputDeviceKind.Touch, router.ActiveDevice);

            router.ForcedDevice = InputDeviceKind.Gamepad;
            Assert.AreEqual(InputDeviceKind.Gamepad, router.ActiveDevice);
        }

        [UnityTest]
        public IEnumerator CycleForcedDevice_GoesThroughAllModes()
        {
            CreatePlayer();
            yield return null;

            var router = playerObject.GetComponent<InputRouter>();
            Assert.AreEqual(InputDeviceKind.Auto, router.ForcedDevice);

            router.CycleForcedDevice();
            Assert.AreEqual(InputDeviceKind.KeyboardMouse, router.ForcedDevice);
            Assert.AreEqual(InputDeviceKind.KeyboardMouse, router.ActiveDevice);

            router.CycleForcedDevice();
            Assert.AreEqual(InputDeviceKind.Gamepad, router.ForcedDevice);
            Assert.AreEqual(InputDeviceKind.Gamepad, router.ActiveDevice);

            router.CycleForcedDevice();
            Assert.AreEqual(InputDeviceKind.Touch, router.ForcedDevice);
            Assert.AreEqual(InputDeviceKind.Touch, router.ActiveDevice);

            router.CycleForcedDevice();
            Assert.AreEqual(InputDeviceKind.Auto, router.ForcedDevice, "после Touch снова автоопределение");
        }

        [UnityTest]
        public IEnumerator TouchControls_AppearOnTouch_AndHideInInventory()
        {
            LabServices.AutoCreateInterface = true;

            CreatePlayer();
            yield return null;

            var touch = TouchControlsUI.Instance;
            Assert.IsNotNull(touch, "сенсорный слой должен создаться вместе с HUD");
            Assert.IsNotNull(touch.Joystick, "должен быть виртуальный джойстик");
            Assert.IsNotNull(touch.LookArea, "должна быть зона обзора");
            Assert.IsNotNull(touch.JumpButton, "должна быть кнопка прыжка");
            Assert.IsNotNull(touch.InventoryButton, "должна быть кнопка инвентаря");
            Assert.Greater(touch.Joystick.radius, 0f);

            Assert.IsFalse(touch.IsVisible, "в режиме клавиатуры сенсорный слой скрыт");

            var router = playerObject.GetComponent<InputRouter>();
            router.ForcedDevice = InputDeviceKind.Touch;

            yield return null;
            yield return null;

            Assert.IsTrue(touch.IsVisible, "в сенсорном режиме слой должен появиться");

            LabHUD.Instance.Inventory.Open();
            yield return null;
            yield return null;

            Assert.IsFalse(touch.IsVisible, "при открытом инвентаре сенсорное управление скрывается");
            Assert.IsTrue(GameplayInput.Locked);
        }

        [UnityTest]
        public IEnumerator InputReader_IsNeutral_WhenInputLocked()
        {
            CreatePlayer();
            yield return null;

            var reader = playerObject.GetComponent<InputReader>();

            GameplayInput.Locked = true;
            reader.Read();

            Assert.AreEqual(Vector2.zero, reader.MoveInput);
            Assert.IsFalse(reader.JumpPressed);
            Assert.IsFalse(reader.JumpHeld);
            Assert.IsFalse(reader.CrouchHeld);
        }
    }
}
