using LabGame.Controls;
using NUnit.Framework;
using UnityEngine;

namespace LabGame.EditModeTests
{
    /// <summary>
    /// Тесты логики определения устройства и математики джойстика (ПЗ №2).
    /// Сцена не нужна — проверяются чистые функции.
    /// </summary>
    public class ControlsTests
    {
        // ── Определение устройства ────────────────────────────────

        [Test]
        public void ResolveInitial_MobilePlatform_GivesTouch()
        {
            var kind = DeviceDetection.ResolveInitial(
                isMobile: true, touchSupported: true, gamepadConnected: false, keyboardConnected: false);

            Assert.AreEqual(InputDeviceKind.Touch, kind);
        }

        [Test]
        public void ResolveInitial_TabletWithoutKeyboard_GivesTouch()
        {
            var kind = DeviceDetection.ResolveInitial(
                isMobile: false, touchSupported: true, gamepadConnected: false, keyboardConnected: false);

            Assert.AreEqual(InputDeviceKind.Touch, kind);
        }

        [Test]
        public void ResolveInitial_OnlyGamepad_GivesGamepad()
        {
            var kind = DeviceDetection.ResolveInitial(
                isMobile: false, touchSupported: false, gamepadConnected: true, keyboardConnected: false);

            Assert.AreEqual(InputDeviceKind.Gamepad, kind);
        }

        [Test]
        public void ResolveInitial_Desktop_GivesKeyboardAndMouse()
        {
            var kind = DeviceDetection.ResolveInitial(
                isMobile: false, touchSupported: false, gamepadConnected: true, keyboardConnected: true);

            Assert.AreEqual(InputDeviceKind.KeyboardMouse, kind);
        }

        [Test]
        public void RussianName_IsNotEmpty_ForEveryDevice()
        {
            foreach (InputDeviceKind kind in System.Enum.GetValues(typeof(InputDeviceKind)))
                Assert.IsFalse(string.IsNullOrEmpty(DeviceDetection.RussianName(kind)), kind.ToString());
        }

        // ── Математика джойстика ──────────────────────────────────

        [Test]
        public void ClampToCircle_KeepsVectorInsideRadius()
        {
            var value = new Vector2(30f, 40f); // длина 50

            var clamped = JoystickMath.ClampToCircle(value, 100f);

            Assert.AreEqual(value, clamped);
        }

        [Test]
        public void ClampToCircle_LimitsVectorOutsideRadius()
        {
            var value = new Vector2(300f, 400f); // длина 500

            var clamped = JoystickMath.ClampToCircle(value, 100f);

            Assert.AreEqual(100f, clamped.magnitude, 0.01f);
            Assert.AreEqual(value.normalized.x, clamped.normalized.x, 0.001f);
            Assert.AreEqual(value.normalized.y, clamped.normalized.y, 0.001f);
        }

        [Test]
        public void ClampToCircle_ZeroRadius_ReturnsZero()
        {
            Assert.AreEqual(Vector2.zero, JoystickMath.ClampToCircle(new Vector2(5f, 5f), 0f));
        }

        [Test]
        public void ApplyDeadZone_InsideDeadZone_ReturnsZero()
        {
            var result = JoystickMath.ApplyDeadZone(new Vector2(0.1f, 0f), 0.2f);

            Assert.AreEqual(Vector2.zero, result);
        }

        [Test]
        public void ApplyDeadZone_RescalesRemainingRange()
        {
            // 0.5 при мёртвой зоне 0.2 → (0.5 − 0.2) / (1 − 0.2) = 0.375
            var result = JoystickMath.ApplyDeadZone(new Vector2(0.5f, 0f), 0.2f);

            Assert.AreEqual(0.375f, result.magnitude, 0.001f);
            Assert.AreEqual(1f, result.normalized.x, 0.001f);
        }

        [Test]
        public void ApplyDeadZone_FullTilt_StaysFull()
        {
            var result = JoystickMath.ApplyDeadZone(Vector2.right, 0.2f);

            Assert.AreEqual(1f, result.magnitude, 0.001f);
        }

        [Test]
        public void NeutralState_HasNoInput()
        {
            var state = PlayerInputState.Neutral;

            Assert.AreEqual(Vector2.zero, state.Move);
            Assert.AreEqual(Vector2.zero, state.Look);
            Assert.IsFalse(state.JumpPressed);
            Assert.IsFalse(state.InteractPressed);
            Assert.IsFalse(state.InventoryPressed);
        }
    }
}
