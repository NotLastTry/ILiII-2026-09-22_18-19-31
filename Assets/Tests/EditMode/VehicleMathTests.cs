using LabGame.Vehicles;
using NUnit.Framework;
using UnityEngine;

namespace LabGame.EditModeTests
{
    /// <summary>Тесты математики управления техникой (ПЗ №7 и №8).</summary>
    public class VehicleMathTests
    {
        [Test]
        public void SignedAngle_TargetToTheRight_IsPositive()
        {
            // В Unity положительный поворот вокруг Y — по часовой стрелке, то есть вправо.
            float angle = VehicleMath.SignedAngleToTarget(Vector3.forward, Vector3.right);

            Assert.AreEqual(90f, angle, 0.01f);
        }

        [Test]
        public void SignedAngle_TargetAhead_IsZero()
        {
            float angle = VehicleMath.SignedAngleToTarget(Vector3.forward, new Vector3(0.01f, 0f, 10f));

            Assert.AreEqual(0f, angle, 0.1f);
        }

        [Test]
        public void SignedAngle_TargetBehind_IsHalfTurn()
        {
            float angle = VehicleMath.SignedAngleToTarget(Vector3.forward, Vector3.back);

            Assert.AreEqual(180f, Mathf.Abs(angle), 0.01f);
        }

        [Test]
        public void SignedAngle_IgnoresHeight()
        {
            float angle = VehicleMath.SignedAngleToTarget(Vector3.forward, new Vector3(0f, 50f, 10f));

            Assert.AreEqual(0f, angle, 0.01f);
        }

        [Test]
        public void MoveTowardsSpeed_AcceleratesAtGivenRate()
        {
            float speed = VehicleMath.MoveTowardsSpeed(0f, 10f, 5f, 20f, 1f);

            Assert.AreEqual(5f, speed, 0.01f);
        }

        [Test]
        public void MoveTowardsSpeed_BrakesFasterThanAccelerates()
        {
            float speed = VehicleMath.MoveTowardsSpeed(10f, 0f, 5f, 20f, 0.25f);

            Assert.AreEqual(5f, speed, 0.01f, "торможение идёт по своему темпу");
        }

        [Test]
        public void SteerStep_StandingStill_DoesNotTurn()
        {
            float step = VehicleMath.SteerStep(90f, 90f, 0f, 1f, 1f);

            Assert.AreEqual(0f, step, 0.001f, "на месте машина не разворачивается");
        }

        [Test]
        public void SteerStep_AtFullSpeed_UsesFullTurnRate()
        {
            float step = VehicleMath.SteerStep(90f, 90f, 5f, 1f, 0.5f);

            Assert.AreEqual(45f, step, 0.01f);
        }

        [Test]
        public void SteerStep_SmallAngle_IsNotClamped()
        {
            float step = VehicleMath.SteerStep(10f, 90f, 5f, 1f, 0.5f);

            Assert.AreEqual(10f, step, 0.01f);
        }

        [Test]
        public void BankForTurn_TiltsIntoTheTurn()
        {
            Assert.AreEqual(-24f, VehicleMath.BankForTurn(20f, 50f), 0.01f, "крен в сторону поворота");
            Assert.AreEqual(24f, VehicleMath.BankForTurn(-20f, 50f), 0.01f);

            Assert.AreEqual(-50f, VehicleMath.BankForTurn(45f, 50f), 0.01f, "крен ограничен максимумом");
            Assert.AreEqual(50f, VehicleMath.BankForTurn(-45f, 50f), 0.01f);
        }

        [Test]
        public void ClimbAngle_UpLevelAndDown()
        {
            Assert.AreEqual(45f, VehicleMath.ClimbAngle(Vector3.zero, new Vector3(0f, 10f, 10f)), 0.01f);
            Assert.AreEqual(0f, VehicleMath.ClimbAngle(Vector3.zero, new Vector3(0f, 0f, 10f)), 0.01f);
            Assert.AreEqual(-45f, VehicleMath.ClimbAngle(Vector3.zero, new Vector3(0f, -10f, 10f)), 0.01f);
        }

        [Test]
        public void Reached_ComparesDistanceWithRadius()
        {
            Assert.IsTrue(VehicleMath.Reached(Vector3.zero, new Vector3(3f, 0f, 0f), 4f));
            Assert.IsFalse(VehicleMath.Reached(Vector3.zero, new Vector3(5f, 0f, 0f), 4f));
        }
    }
}
