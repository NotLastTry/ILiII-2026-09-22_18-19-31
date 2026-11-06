using System.Collections;
using System.Collections.Generic;
using LabGame;
using LabGame.AI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LabGame.PlayModeTests
{
    /// <summary>
    /// Сквозные проверки толпы (ПЗ №6): менеджер собирает участников,
    /// паника включается по событию и сама заканчивается.
    /// </summary>
    public class CrowdPlayTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            LabServices.Reset();
            LabServices.AutoCreateInterface = false;
            GameEventBus.ClearAll();
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            for (int i = 0; i < created.Count; i++)
                if (created[i] != null) Object.Destroy(created[i]);

            created.Clear();
            GameEventBus.ClearAll();
        }

        private CrowdAgent CreateAgent(Vector3 position)
        {
            var go = new GameObject("CrowdAgent");
            go.transform.position = position;
            created.Add(go);

            go.AddComponent<NavMeshAgent>();
            return go.AddComponent<CrowdAgent>();
        }

        private CrowdManager CreateManager(float panicDuration = 5f)
        {
            var go = new GameObject("Crowd");
            created.Add(go);

            var manager = go.AddComponent<CrowdManager>();
            manager.panicDuration = panicDuration;
            manager.panicRadius = 10f;

            return manager;
        }

        [UnityTest]
        public IEnumerator Manager_CollectsAgents()
        {
            CreateAgent(new Vector3(0f, 0f, 0f));
            CreateAgent(new Vector3(2f, 0f, 0f));
            CreateAgent(new Vector3(4f, 0f, 0f));

            var manager = CreateManager();

            yield return null;

            Assert.AreEqual(3, manager.Agents.Count, "менеджер должен найти всех участников");
            Assert.AreEqual(0, manager.Agents[0].Index);
            Assert.AreEqual(2, manager.Agents[2].Index);
        }

        [UnityTest]
        public IEnumerator Alarm_StartsAndEndsPanic()
        {
            CreateAgent(Vector3.zero);
            var manager = CreateManager(panicDuration: 0.3f);

            yield return null;

            Assert.IsFalse(manager.HasThreat);

            manager.Alarm(new Vector3(1f, 0f, 0f));

            Assert.IsTrue(manager.HasThreat, "тревога должна включиться");
            Assert.AreEqual(new Vector3(1f, 0f, 0f), manager.ThreatPosition);

            yield return new WaitForSeconds(0.5f);

            Assert.IsFalse(manager.HasThreat, "паника должна закончиться сама");
        }

        [UnityTest]
        public IEnumerator Disturbance_Event_TriggersPanic()
        {
            CreateAgent(Vector3.zero);
            var manager = CreateManager();

            yield return null;

            GameEventBus.RaiseDisturbance(new Vector3(0f, 0f, 3f));

            Assert.IsTrue(manager.HasThreat, "событие драки должно пугать толпу");
            Assert.AreEqual(new Vector3(0f, 0f, 3f), manager.ThreatPosition);
        }

        [UnityTest]
        public IEnumerator Manager_ProvidesSnapshotForSteering()
        {
            CreateAgent(new Vector3(1f, 0f, 0f));
            CreateAgent(new Vector3(2f, 0f, 0f));

            var manager = CreateManager();

            yield return null;

            manager.GetSnapshot(out var positions, out var velocities, out int count);

            Assert.AreEqual(2, count);
            Assert.AreEqual(2, positions.Length);
            Assert.AreEqual(2, velocities.Length);

            // Порядок участников не гарантирован — проверяем набор координат.
            float minX = Mathf.Min(positions[0].x, positions[1].x);
            float maxX = Mathf.Max(positions[0].x, positions[1].x);

            Assert.AreEqual(1f, minX, 0.01f);
            Assert.AreEqual(2f, maxX, 0.01f);
        }
    }
}
