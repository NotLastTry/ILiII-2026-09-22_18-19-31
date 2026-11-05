using System.Collections.Generic;
using LabGame.AI;
using NUnit.Framework;

namespace LabGame.EditModeTests
{
    /// <summary>
    /// Тесты самого дерева поведения (ПЗ №5): порядок обхода,
    /// приоритет веток и возобновление выполнения.
    /// </summary>
    public class BehaviourTreeTests
    {
        private static EnemyContext Context() => new EnemyContext();

        [Test]
        public void Selector_StopsOnFirstSuccess()
        {
            var calls = new List<string>();

            var tree = new SelectorNode("root");
            tree.Add(new ActionNode("a", ctx => { calls.Add("a"); return BehaviourStatus.Failure; }));
            tree.Add(new ActionNode("b", ctx => { calls.Add("b"); return BehaviourStatus.Success; }));
            tree.Add(new ActionNode("c", ctx => { calls.Add("c"); return BehaviourStatus.Success; }));

            Assert.AreEqual(BehaviourStatus.Success, tree.Tick(Context()));
            CollectionAssert.AreEqual(new[] { "a", "b" }, calls);
        }

        [Test]
        public void Selector_FailsWhenEveryChildFails()
        {
            var tree = new SelectorNode("root");
            tree.Add(new ActionNode("a", ctx => BehaviourStatus.Failure));
            tree.Add(new ActionNode("b", ctx => BehaviourStatus.Failure));

            Assert.AreEqual(BehaviourStatus.Failure, tree.Tick(Context()));
        }

        [Test]
        public void Sequence_StopsOnFirstFailure()
        {
            var calls = new List<string>();

            var tree = new SequenceNode("root");
            tree.Add(new ActionNode("a", ctx => { calls.Add("a"); return BehaviourStatus.Success; }));
            tree.Add(new ActionNode("b", ctx => { calls.Add("b"); return BehaviourStatus.Failure; }));
            tree.Add(new ActionNode("c", ctx => { calls.Add("c"); return BehaviourStatus.Success; }));

            Assert.AreEqual(BehaviourStatus.Failure, tree.Tick(Context()));
            CollectionAssert.AreEqual(new[] { "a", "b" }, calls);
        }

        [Test]
        public void Sequence_ResumesRunningChild()
        {
            int workTicks = 0;
            var calls = new List<string>();

            var tree = new SequenceNode("root");
            tree.Add(new ConditionNode("condition", ctx => true));
            tree.Add(new ActionNode("work", ctx =>
            {
                workTicks++;
                calls.Add($"work{workTicks}");
                return workTicks < 3 ? BehaviourStatus.Running : BehaviourStatus.Success;
            }));
            tree.Add(new ActionNode("done", ctx => { calls.Add("done"); return BehaviourStatus.Success; }));

            var context = Context();

            Assert.AreEqual(BehaviourStatus.Running, tree.Tick(context));
            Assert.AreEqual(BehaviourStatus.Running, tree.Tick(context));
            Assert.AreEqual(BehaviourStatus.Success, tree.Tick(context));

            CollectionAssert.AreEqual(new[] { "work1", "work2", "work3", "done" }, calls);
        }

        [Test]
        public void Selector_ResumesRunningChild()
        {
            int ticks = 0;

            var tree = new SelectorNode("root");
            tree.Add(new ActionNode("blocked", ctx => BehaviourStatus.Failure));
            tree.Add(new ActionNode("long", ctx =>
            {
                ticks++;
                return ticks < 2 ? BehaviourStatus.Running : BehaviourStatus.Success;
            }));

            var context = Context();

            Assert.AreEqual(BehaviourStatus.Running, tree.Tick(context));
            Assert.AreEqual(BehaviourStatus.Success, tree.Tick(context));
            Assert.AreEqual(2, ticks);
        }

        [Test]
        public void Condition_MapsBoolToStatus()
        {
            var yes = new ConditionNode("yes", ctx => true);
            var no = new ConditionNode("no", ctx => false);

            Assert.AreEqual(BehaviourStatus.Success, yes.Tick(Context()));
            Assert.AreEqual(BehaviourStatus.Failure, no.Tick(Context()));
        }

        [Test]
        public void Tree_PrioritisesCombatOverChaseAndPatrol()
        {
            var calls = new List<string>();
            var tree = BuildSampleTree(calls);

            var context = Context();
            context.CanSeePlayer = true;
            context.DistanceToPlayer = 1.5f;

            tree.Tick(context);

            CollectionAssert.AreEqual(new[] { "attack" }, calls);
        }

        [Test]
        public void Tree_ChasesWhenInSightButFaraway()
        {
            var calls = new List<string>();
            var tree = BuildSampleTree(calls);

            var context = Context();
            context.CanSeePlayer = true;
            context.DistanceToPlayer = 8f;

            tree.Tick(context);

            CollectionAssert.AreEqual(new[] { "chase" }, calls);
        }

        [Test]
        public void Tree_PatrolsWhenPlayerIsNotVisible()
        {
            var calls = new List<string>();
            var tree = BuildSampleTree(calls);

            var context = Context();
            context.CanSeePlayer = false;
            context.HasLastKnown = false;

            tree.Tick(context);

            CollectionAssert.AreEqual(new[] { "patrol" }, calls);
        }

        [Test]
        public void Describe_ContainsEveryNode()
        {
            var calls = new List<string>();
            var tree = BuildSampleTree(calls);

            tree.Tick(Context());

            var dump = new System.Text.StringBuilder();
            tree.Describe(dump, 0);
            var text = dump.ToString();

            StringAssert.Contains("Корень", text);
            StringAssert.Contains("Бой", text);
            StringAssert.Contains("Преследование", text);
            StringAssert.Contains("Патрулирование", text);
            StringAssert.Contains("успех", text);
        }

        /// <summary>Упрощённая копия дерева противника для проверки приоритетов.</summary>
        private static SelectorNode BuildSampleTree(List<string> calls)
        {
            var root = new SelectorNode("Корень");

            var combat = new SequenceNode("Бой");
            combat.Add(new ConditionNode("Цель в зоне удара",
                ctx => ctx.CanSeePlayer && ctx.DistanceToPlayer <= 2f));
            combat.Add(new ActionNode("Удар", ctx => { calls.Add("attack"); return BehaviourStatus.Success; }));

            var chase = new SequenceNode("Преследование");
            chase.Add(new ConditionNode("Игрок виден", ctx => ctx.CanSeePlayer));
            chase.Add(new ActionNode("Идти к игроку", ctx => { calls.Add("chase"); return BehaviourStatus.Success; }));

            var search = new SequenceNode("Поиск");
            search.Add(new ConditionNode("Есть последняя позиция", ctx => ctx.HasLastKnown));
            search.Add(new ActionNode("Обыскать точку", ctx => { calls.Add("search"); return BehaviourStatus.Success; }));

            var patrol = new ActionNode("Патрулирование", ctx => { calls.Add("patrol"); return BehaviourStatus.Success; });

            root.Add(combat);
            root.Add(chase);
            root.Add(search);
            root.Add(patrol);

            return root;
        }
    }
}
