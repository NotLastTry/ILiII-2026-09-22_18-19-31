using LabGame.AI;
using NUnit.Framework;
using UnityEngine;

namespace LabGame.EditModeTests
{
    /// <summary>
    /// Тесты стайного поведения (ПЗ №6): силы разделения, сплочённости,
    /// выравнивания и режимы «спокойно / любопытство / паника».
    /// </summary>
    public class CrowdSteeringTests
    {
        private static CrowdSettings OnlySeparation() => new CrowdSettings
        {
            neighborRadius = 5f,
            separationRadius = 2f,
            separationWeight = 2f,
            alignmentWeight = 0f,
            cohesionWeight = 0f,
            seekWeight = 0f,
            homeWeight = 0f,
            interestWeight = 0f,
            panicWeight = 0f,
            maxForce = 100f,
            maxSpeed = 2f,
            panicSpeed = 4f,
            personalSpace = 2f
        };

        private static CrowdContext At(Vector3 position) => new CrowdContext
        {
            Position = position,
            Home = position,
            WanderTarget = position,
            Mode = CrowdMode.Calm
        };

        [Test]
        public void Seek_PointsToTarget()
        {
            var direction = CrowdSteering.Seek(Vector3.zero, new Vector3(5f, 0f, 0f));

            Assert.AreEqual(1f, direction.x, 0.001f);
            Assert.AreEqual(0f, direction.y, 0.001f);
        }

        [Test]
        public void Flee_PointsAwayFromThreat()
        {
            var direction = CrowdSteering.Flee(Vector3.zero, new Vector3(5f, 0f, 0f));

            Assert.AreEqual(-1f, direction.x, 0.001f);
        }

        [Test]
        public void Seek_AtTarget_ReturnsZero()
        {
            Assert.AreEqual(Vector3.zero, CrowdSteering.Seek(Vector3.one, Vector3.one));
        }

        [Test]
        public void Separation_PushesAwayFromCloseNeighbour()
        {
            var settings = OnlySeparation();

            // Сосед справа от нас.
            var positions = new[] { new Vector3(1f, 0f, 0f), Vector3.zero };
            var velocities = new[] { Vector3.zero, Vector3.zero };

            var force = CrowdSteering.Compute(At(Vector3.zero), positions, velocities, 2, 1, settings);

            Assert.Less(force.x, 0f, "близкий сосед должен отталкивать");
        }

        [Test]
        public void Separation_IgnoresDistantNeighbours()
        {
            var settings = OnlySeparation();

            var positions = new[] { new Vector3(3f, 0f, 0f), Vector3.zero };
            var velocities = new[] { Vector3.zero, Vector3.zero };

            var force = CrowdSteering.Compute(At(Vector3.zero), positions, velocities, 2, 1, settings);

            Assert.AreEqual(0f, force.magnitude, 0.001f, "вне радиуса расталкивания силы нет");
        }

        [Test]
        public void Cohesion_PointsToGroupCentre()
        {
            var settings = OnlySeparation();
            settings.separationWeight = 0f;
            settings.cohesionWeight = 1f;

            var positions = new[] { new Vector3(3f, 0f, 0f), Vector3.zero };
            var velocities = new[] { Vector3.zero, Vector3.zero };

            var force = CrowdSteering.Compute(At(Vector3.zero), positions, velocities, 2, 1, settings);

            Assert.Greater(force.x, 0f, "группа должна притягивать к себе");
        }

        [Test]
        public void Alignment_FollowsNeighbourVelocity()
        {
            var settings = OnlySeparation();
            settings.separationWeight = 0f;
            settings.alignmentWeight = 1f;

            var positions = new[] { new Vector3(2f, 0f, 0f), Vector3.zero };
            var velocities = new[] { new Vector3(3f, 0f, 0f), Vector3.forward };

            var force = CrowdSteering.Compute(At(Vector3.zero), positions, velocities, 2, 1, settings);

            Assert.Greater(force.x, 0f, "выравнивание повторяет направление соседа");
        }

        [Test]
        public void Panic_FleesFromThreat()
        {
            var settings = OnlySeparation();
            settings.panicWeight = 2f;

            var context = At(Vector3.zero);
            context.Mode = CrowdMode.Panic;
            context.HasThreat = true;
            context.ThreatPosition = new Vector3(5f, 0f, 0f);

            var force = CrowdSteering.Compute(context, new[] { Vector3.zero }, new[] { Vector3.zero }, 0, 0, settings);

            Assert.Less(force.x, 0f, "паника гонит от угрозы");
        }

        [Test]
        public void Curious_ApproachesInterest_ButKeepsDistance()
        {
            var settings = OnlySeparation();
            settings.interestWeight = 1f;
            settings.personalSpace = 2f;

            var context = At(Vector3.zero);
            context.Mode = CrowdMode.Curious;
            context.HasInterest = true;
            context.InterestPosition = new Vector3(8f, 0f, 0f);

            var far = CrowdSteering.Compute(context, new[] { Vector3.zero }, new[] { Vector3.zero }, 0, 0, settings);
            Assert.Greater(far.x, 0f, "интересный объект притягивает");

            context.InterestPosition = new Vector3(1f, 0f, 0f);
            var close = CrowdSteering.Compute(context, new[] { Vector3.zero }, new[] { Vector3.zero }, 0, 0, settings);

            Assert.AreEqual(0f, close.magnitude, 0.001f, "в личной зоне агент останавливается");
        }

        [Test]
        public void Calm_SeeksWanderTarget()
        {
            var settings = OnlySeparation();
            settings.seekWeight = 1f;

            var context = At(Vector3.zero);
            context.WanderTarget = new Vector3(0f, 0f, 5f);

            var force = CrowdSteering.Compute(context, new[] { Vector3.zero }, new[] { Vector3.zero }, 0, 0, settings);

            Assert.Greater(force.z, 0f, "в спокойном режиме агент идёт к своей точке");
        }

        [Test]
        public void Compute_ClampsForce()
        {
            var settings = new CrowdSettings { maxForce = 1.5f };

            var positions = new[]
            {
                new Vector3(0.4f, 0f, 0f),
                new Vector3(-0.4f, 0f, 0f),
                new Vector3(0f, 0f, 0.4f)
            };
            var velocities = new[] { Vector3.right, Vector3.left, Vector3.forward };

            var context = At(Vector3.zero);
            context.WanderTarget = new Vector3(10f, 0f, 10f);

            var force = CrowdSteering.Compute(context, positions, velocities, 3, 0, settings);

            Assert.LessOrEqual(force.magnitude, settings.maxForce + 0.001f);
        }

        [Test]
        public void Compute_WithoutSettings_ReturnsZero()
        {
            Assert.AreEqual(Vector3.zero,
                CrowdSteering.Compute(At(Vector3.zero), new Vector3[0], new Vector3[0], 0, 0, null));
        }
    }
}
