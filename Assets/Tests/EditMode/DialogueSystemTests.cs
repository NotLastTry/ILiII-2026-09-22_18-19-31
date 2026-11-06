using System.Collections.Generic;
using LabGame.Dialogue;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LabGame.EditModeTests
{
    /// <summary>
    /// Тесты разбора диалогового XML и проигрывания диалога (ПЗ №9).
    /// </summary>
    public class DialogueSystemTests
    {
        private const string Sample = @"<dialogue id='test' speaker='Тест'>
  <node id='start' text='Привет'>
    <answer text='Дальше' to='next'/>
    <answer text='Секрет' to='next' condition='flag:unlocked'/>
    <answer text='Пока' to='end'/>
  </node>
  <node id='next' text='Второй'>
    <answer text='Назад' to='start'/>
  </node>
</dialogue>";

        private readonly List<GameObject> created = new List<GameObject>();

        private sealed class FakeContext : IDialogueContext
        {
            public readonly HashSet<string> Flags = new HashSet<string>();
            public readonly List<string> Applied = new List<string>();

            public bool CheckCondition(string condition) => Flags.Contains(condition);

            public void ApplyAction(string action) => Applied.Add(action);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
                if (go != null) Object.DestroyImmediate(go);

            created.Clear();
        }

        // ─────────────────────────────────────────────────────────────
        //  Разбор XML
        // ─────────────────────────────────────────────────────────────

        [Test]
        public void Parser_ReadsNodesAnswersAndSpeaker()
        {
            var graph = DialogueParser.Parse(Sample);

            Assert.AreEqual("test", graph.Id);
            Assert.AreEqual("Тест", graph.Speaker);
            Assert.AreEqual(2, graph.Count);
            Assert.AreEqual("start", graph.Start.Id);
            Assert.AreEqual(3, graph.Find("start").Answers.Count);
            Assert.AreEqual("next", graph.Find("start").Answers[0].TargetId);
        }

        [Test]
        public void Parser_ReadsConditionAndAction()
        {
            const string xml = @"<dialogue id='d' speaker='S'>
  <node id='start' text='Так' action='quest:offer'>
    <answer text='Да' to='end' condition='quest:idle' action='quest:accept'/>
  </node>
</dialogue>";

            var graph = DialogueParser.Parse(xml);
            var node = graph.Find("start");

            Assert.AreEqual("quest:offer", node.Action);
            Assert.AreEqual("quest:idle", node.Answers[0].Condition);
            Assert.AreEqual("quest:accept", node.Answers[0].Action);
        }

        [Test]
        public void Parser_ReadsTextFromElementContent()
        {
            const string xml = @"<dialogue id='d' speaker='S'>
  <node id='start'>
    Это текст внутри элемента.
    <answer text='Ок' to='end'/>
  </node>
</dialogue>";

            var graph = DialogueParser.Parse(xml);

            StringAssert.Contains("Это текст внутри элемента", graph.Find("start").Text);
        }

        [Test]
        public void Parser_ThrowsOnUnknownTarget()
        {
            const string xml = @"<dialogue id='d' speaker='S'>
  <node id='start' text='Так'>
    <answer text='Туда' to='missing'/>
  </node>
</dialogue>";

            var error = Assert.Throws<DialogueFormatException>(() => DialogueParser.Parse(xml));
            StringAssert.Contains("missing", error.Message);
        }

        [Test]
        public void Parser_ThrowsOnDuplicateNode()
        {
            const string xml = @"<dialogue id='d' speaker='S'>
  <node id='start' text='Раз'/>
  <node id='start' text='Два'/>
</dialogue>";

            Assert.Throws<DialogueFormatException>(() => DialogueParser.Parse(xml));
        }

        [Test]
        public void Parser_ThrowsOnBrokenXml()
        {
            Assert.Throws<DialogueFormatException>(() => DialogueParser.Parse("<dialogue"));
        }

        [Test]
        public void Parser_ThrowsOnEmptyText()
        {
            const string xml = @"<dialogue id='d' speaker='S'>
  <node id='start' text=''>
    <answer text='Ок' to='end'/>
  </node>
</dialogue>";

            Assert.Throws<DialogueFormatException>(() => DialogueParser.Parse(xml));
        }

        [Test]
        public void TryParse_ReturnsErrorInsteadOfThrowing()
        {
            bool ok = DialogueParser.TryParse("<не диалог/>", out var graph, out string error);

            Assert.IsFalse(ok);
            Assert.IsNull(graph);
            Assert.IsFalse(string.IsNullOrEmpty(error));
        }

        // ─────────────────────────────────────────────────────────────
        //  Проигрывание
        // ─────────────────────────────────────────────────────────────

        private DialogueRunner CreateRunner()
        {
            var go = new GameObject("DialogueRunner");
            created.Add(go);

            return go.AddComponent<DialogueRunner>();
        }

        [Test]
        public void Runner_StartsAtFirstNode()
        {
            var runner = CreateRunner();
            var graph = DialogueParser.Parse(Sample);

            Assert.IsTrue(runner.Begin(graph, new FakeContext()));

            Assert.IsTrue(runner.IsOpen);
            Assert.AreEqual("start", runner.Current.Id);
            Assert.AreEqual("Привет", runner.Current.Text);
        }

        [Test]
        public void Runner_HidesAnswersByCondition()
        {
            var runner = CreateRunner();
            var graph = DialogueParser.Parse(Sample);
            var context = new FakeContext();

            runner.Begin(graph, context);
            Assert.AreEqual(2, runner.Answers.Count, "ответ с условием скрыт");

            context.Flags.Add("flag:unlocked");
            runner.Begin(graph, context);

            Assert.AreEqual(3, runner.Answers.Count, "после выполнения условия ответ появился");
        }

        [Test]
        public void Runner_MovesBetweenNodes()
        {
            var runner = CreateRunner();
            runner.Begin(DialogueParser.Parse(Sample), new FakeContext());

            runner.Choose(0);

            Assert.AreEqual("next", runner.Current.Id);

            runner.Choose(0);

            Assert.AreEqual("start", runner.Current.Id, "ответ «Назад» возвращает в начало");
        }

        [Test]
        public void Runner_ClosesOnExitAnswer()
        {
            var runner = CreateRunner();
            runner.Begin(DialogueParser.Parse(Sample), new FakeContext());

            runner.Choose(1);

            Assert.IsFalse(runner.IsOpen);
        }

        [Test]
        public void Runner_AppliesNodeAndAnswerActions()
        {
            const string xml = @"<dialogue id='d' speaker='S'>
  <node id='start' text='Так' action='quest:offer'>
    <answer text='Да' to='end' action='quest:accept'/>
  </node>
</dialogue>";

            var runner = CreateRunner();
            var context = new FakeContext();

            runner.Begin(DialogueParser.Parse(xml), context);

            CollectionAssert.Contains(context.Applied, "quest:offer");

            runner.Choose(0);

            CollectionAssert.Contains(context.Applied, "quest:accept");
        }

        [Test]
        public void Runner_AddsContinueWhenNoAnswersMatch()
        {
            const string xml = @"<dialogue id='d' speaker='S'>
  <node id='start' text='Тишина'>
    <answer text='Секрет' to='end' condition='never'/>
  </node>
</dialogue>";

            var runner = CreateRunner();
            runner.Begin(DialogueParser.Parse(xml), new FakeContext());

            Assert.AreEqual(1, runner.Answers.Count);
            Assert.AreEqual("Продолжить", runner.Answers[0].Text);

            runner.Choose(0);

            Assert.IsFalse(runner.IsOpen);
        }

        [Test]
        public void Rules_RequireEveryConditionInList()
        {
            var context = new FakeContext();
            context.Flags.Add("a");

            Assert.IsTrue(DialogueRules.MatchesAll("a", context));
            Assert.IsFalse(DialogueRules.MatchesAll("a;b", context));
            Assert.IsTrue(DialogueRules.MatchesAll(null, context));
        }

        [Test]
        public void Rules_ApplyEveryActionInList()
        {
            var context = new FakeContext();

            DialogueRules.ApplyAll("quest:offer;quest:accept", context);

            CollectionAssert.AreEqual(new[] { "quest:offer", "quest:accept" }, context.Applied);
        }
    }
}
