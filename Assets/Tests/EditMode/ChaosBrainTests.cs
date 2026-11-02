using System.Collections.Generic;
using System.Linq;
using LabGame.AI;
using NUnit.Framework;
using UnityEngine;

namespace LabGame.EditModeTests
{
    /// <summary>
    /// Акёр-заглушка: позволяет гонять «мозг» NPC без сцены и NavMesh.
    /// </summary>
    public class FakeChaosActor : IChaosActor
    {
        public Vector3 Position { get; set; }
        public Vector3 PlayerPosition { get; set; } = new Vector3(0f, 0f, 10f);
        public Vector3 PlayerForward { get; set; } = Vector3.forward;
        public System.Random Rng { get; } = new System.Random(1234);

        public bool IsMoving { get; private set; }
        public int MoveCalls { get; private set; }
        public int StopCalls { get; private set; }
        public int Teleports { get; private set; }
        public int Gifts { get; private set; }
        public readonly List<string> Speech = new List<string>();

        public void MoveTo(Vector3 worldPosition)
        {
            Position = worldPosition;
            IsMoving = true;
            MoveCalls++;
        }

        public void Stop()
        {
            IsMoving = false;
            StopCalls++;
        }

        public void FaceTowards(Vector3 worldPosition) { }

        public void PlayTrigger(string triggerName) { }

        public void Say(string text, float duration = 3f) => Speech.Add(text);

        public bool TryRandomPoint(Vector3 origin, float radius, out Vector3 point)
        {
            point = origin;
            return true;
        }

        public void Teleport(Vector3 worldPosition)
        {
            Position = worldPosition;
            Teleports++;
        }

        public void SpawnFakeGift(Vector3 worldPosition) => Gifts++;
    }

    /// <summary>
    /// Тесты хаотичного поведения (ПЗ №8): проверяем, что NPC действительно
    /// непредсказуем, не зацикливается и реагирует на события.
    /// </summary>
    public class ChaosBrainTests
    {
        private ChaosProfile profile;
        private ChaosBrain brain;
        private FakeChaosActor actor;

        [SetUp]
        public void SetUp()
        {
            profile = ScriptableObject.CreateInstance<ChaosProfile>();
            profile.chaos = 0.9f;
            profile.reactionChance = 1f;
            profile.antiRepeatStrength = 0.75f;
            profile.historyDepth = 4;
            profile.durationScale = 1f;

            actor = new FakeChaosActor();
            brain = new ChaosBrain(profile, ChaosActions.BuildDefault(profile), 42);
        }

        [TearDown]
        public void TearDown()
        {
            if (profile != null) UnityEngine.Object.DestroyImmediate(profile);
        }

        private static ChaosContext ContextAt(int step, bool playerMoving = true, bool looking = false)
        {
            float t = step * 0.2f;
            float distance = 5f + Mathf.PingPong(t * 2f, 20f);

            return new ChaosContext(
                distance,
                distance <= 26f,
                looking,
                1f,
                playerMoving ? 3f : 0f,
                false);
        }

        private List<ChaosAction> Simulate(int steps, bool playerMoving = true, bool looking = false, float dt = 0.2f)
        {
            var sequence = new List<ChaosAction>();

            for (int i = 0; i < steps; i++)
            {
                brain.Tick(actor, ContextAt(i, playerMoving, looking), dt);

                if (sequence.Count == 0 || sequence[sequence.Count - 1] != brain.CurrentAction)
                    sequence.Add(brain.CurrentAction);
            }

            return sequence;
        }

        [Test]
        public void Simulate_UsesAllNineActions()
        {
            var sequence = Simulate(4000);
            var distinct = new HashSet<ChaosAction>(sequence);

            Assert.AreEqual(9, distinct.Count,
                "должны встретиться все действия, а встретились: " + string.Join(", ", distinct));
        }

        [Test]
        public void Simulate_AlmostNeverRepeatsSameActionTwice()
        {
            var sequence = Simulate(3000);

            int repeats = 0;
            for (int i = 1; i < sequence.Count; i++)
                if (sequence[i] == sequence[i - 1]) repeats++;

            Assert.Less(repeats, sequence.Count * 0.05f,
                $"слишком много повторов: {repeats} из {sequence.Count}");
        }

        [Test]
        public void Simulate_DoesNotFavourOneAction()
        {
            var sequence = Simulate(4000);

            var counts = sequence
                .GroupBy(a => a)
                .ToDictionary(g => g.Key, g => g.Count());

            int max = counts.Values.Max();
            ChaosAction dominant = counts.OrderByDescending(p => p.Value).First().Key;

            Assert.Less(max, sequence.Count * 0.45f,
                $"действие {dominant} выбирается слишком часто: {max} из {sequence.Count}");
        }

        [Test]
        public void Mimic_NeverChosen_WhenPlayerStandsStill()
        {
            var sequence = Simulate(1500, playerMoving: false);

            Assert.IsFalse(sequence.Contains(ChaosAction.Mimic));
        }

        [Test]
        public void Notify_ForcesReaction_WhenChanceIsOne()
        {
            brain.Tick(actor, ContextAt(0), 0.2f);

            brain.Notify(ChaosSignal.PlayerPickedUpItem, actor, ContextAt(0));

            Assert.IsNotNull(brain.LastReaction);
            Assert.IsTrue(brain.LastReaction == ChaosAction.Stalk || brain.LastReaction == ChaosAction.FakeGift);
            Assert.AreEqual(brain.LastReaction.Value, brain.CurrentAction);
            Assert.AreEqual(ChaosSignal.PlayerPickedUpItem, brain.LastSignal);
        }

        [Test]
        public void Notify_DoesNotForceReaction_WhenChanceIsZero()
        {
            profile.reactionChance = 0f;
            brain = new ChaosBrain(profile, ChaosActions.BuildDefault(profile), 7);

            brain.Tick(actor, ContextAt(0), 0.2f);
            var before = brain.CurrentAction;

            brain.Notify(ChaosSignal.PlayerEquippedItem, actor, ContextAt(0));

            Assert.IsNull(brain.LastReaction);
            Assert.AreEqual(before, brain.CurrentAction);
            Assert.AreEqual(ChaosSignal.PlayerEquippedItem, brain.LastSignal);
        }

        [Test]
        public void Notify_ReactsToEverySignalType()
        {
            var signals = (ChaosSignal[])System.Enum.GetValues(typeof(ChaosSignal));

            foreach (var signal in signals)
            {
                brain.Tick(actor, ContextAt(0), 0.2f);
                Assert.DoesNotThrow(() => brain.Notify(signal, actor, ContextAt(0)), $"сигнал {signal}");
                Assert.AreEqual(signal, brain.LastSignal);
            }
        }

        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var first = new ChaosBrain(profile, ChaosActions.BuildDefault(profile), 99);
            var second = new ChaosBrain(profile, ChaosActions.BuildDefault(profile), 99);

            var actorA = new FakeChaosActor();
            var actorB = new FakeChaosActor();

            for (int i = 0; i < 800; i++)
            {
                var context = ContextAt(i);
                first.Tick(actorA, context, 0.2f);
                second.Tick(actorB, context, 0.2f);

                Assert.AreEqual(first.CurrentAction, second.CurrentAction, $"шаг {i}");
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentSequences()
        {
            var first = new ChaosBrain(profile, ChaosActions.BuildDefault(profile), 1);
            var second = new ChaosBrain(profile, ChaosActions.BuildDefault(profile), 2);

            var a = new List<ChaosAction>();
            var b = new List<ChaosAction>();

            for (int i = 0; i < 400; i++)
            {
                var context = ContextAt(i);
                first.Tick(new FakeChaosActor(), context, 0.2f);
                second.Tick(new FakeChaosActor(), context, 0.2f);

                a.Add(first.CurrentAction);
                b.Add(second.CurrentAction);
            }

            Assert.IsFalse(a.SequenceEqual(b), "разные seed должны давать разные последовательности");
        }

        [Test]
        public void Actions_KeepChangingOverTime()
        {
            Simulate(600);

            Assert.Greater(brain.Changes, 20, "NPC должен регулярно менять действие");
        }
    }
}
