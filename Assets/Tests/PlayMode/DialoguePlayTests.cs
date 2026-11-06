using System.Collections;
using System.Collections.Generic;
using LabGame;
using LabGame.Dialogue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace LabGame.PlayModeTests
{
    /// <summary>
    /// Сквозные проверки диалогов (ПЗ №9): разговор открывается по E,
    /// ответы меняют состояние квеста, условия скрывают лишние варианты.
    /// </summary>
    public class DialoguePlayTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        private GameObject playerObject;
        private GameObject npcObject;
        private PlayerInventory inventory;
        private NPCController npc;
        private DialogueAgent agent;

        [SetUp]
        public void SetUp()
        {
            LabServices.Reset();
            LabServices.AutoCreateInterface = false;
            GameplayInput.Locked = false;
            DialogueLibrary.Clear();
            LogAssert.ignoreFailingMessages = true;

            playerObject = new GameObject("Player") { tag = "Player" };
            playerObject.transform.position = Vector3.zero;
            inventory = playerObject.AddComponent<PlayerInventory>();

            npcObject = new GameObject("NPC");
            npcObject.transform.position = new Vector3(0f, 0f, 2f);

            npc = npcObject.AddComponent<NPCController>();
            agent = npcObject.AddComponent<DialogueAgent>();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            if (DialogueRunner.Instance != null)
                Object.Destroy(DialogueRunner.Instance.gameObject);

            if (npcObject != null) Object.Destroy(npcObject);
            if (playerObject != null) Object.Destroy(playerObject);

            foreach (var go in created)
                if (go != null) Object.Destroy(go);

            created.Clear();

            GameplayInput.Locked = false;
            LabServices.Reset();
        }

        private static int FindAnswer(DialogueRunner runner, string text)
        {
            for (int i = 0; i < runner.Answers.Count; i++)
                if (runner.Answers[i].Text == text) return i;

            return -1;
        }

        [UnityTest]
        public IEnumerator Dialogue_OpensOnInteract()
        {
            yield return null;

            agent.Interact(inventory);

            var runner = DialogueRunner.Instance;

            Assert.IsNotNull(runner, "раннер должен создаться");
            Assert.IsTrue(runner.IsOpen);
            Assert.AreEqual("start", runner.Current.Id);
            Assert.AreEqual("Кузнец", runner.Current.Speaker);
        }

        [UnityTest]
        public IEnumerator DialogueAgent_IsFoundByInteractor()
        {
            yield return null;

            // Именно так PlayerInteractor ищет, с чем можно взаимодействовать.
            var found = InteractableRegistry.FindClosest(npcObject.transform.position, 5f, inventory);

            Assert.AreSame(agent, found, "собеседник должен быть в реестре интерактивных объектов");
        }

        [UnityTest]
        public IEnumerator Dialogue_TakesQuest()
        {
            yield return null;

            agent.Interact(inventory);
            var runner = DialogueRunner.Instance;

            int offer = FindAnswer(runner, "Есть работа?");
            Assert.GreaterOrEqual(offer, 0, "квест ещё не взят — вариант виден");

            runner.Choose(offer);

            Assert.AreEqual("offer", runner.Current.Id);
            Assert.AreEqual(NPCQuestState.Offer, npc.questState, "узел предложения переводит квест в Offer");

            int accept = FindAnswer(runner, "Согласен.");
            Assert.GreaterOrEqual(accept, 0);

            runner.Choose(accept);

            Assert.AreEqual(NPCQuestState.QuestActive, npc.questState, "ответ принимает квест");
            Assert.IsTrue(npc.isQuestAccepted);
        }

        [UnityTest]
        public IEnumerator Dialogue_HidesTurnInBeforeQuestDone()
        {
            yield return null;

            agent.Interact(inventory);
            var runner = DialogueRunner.Instance;

            Assert.AreEqual(-1, FindAnswer(runner, "Я принёс ягоды."),
                "пока квест не выполнен, сдавать нечего");
        }

        [UnityTest]
        public IEnumerator Dialogue_TurnsInQuest()
        {
            yield return null;

            npc.questState = NPCQuestState.QuestDone;

            agent.Interact(inventory);
            var runner = DialogueRunner.Instance;

            int turnIn = FindAnswer(runner, "Я принёс ягоды.");
            Assert.GreaterOrEqual(turnIn, 0);

            runner.Choose(turnIn);

            Assert.AreEqual("turnin", runner.Current.Id);
            Assert.AreEqual(NPCQuestState.RewardGiven, npc.questState);
        }

        [UnityTest]
        public IEnumerator Dialogue_ClosesAndUnlocksInput()
        {
            yield return null;

            agent.Interact(inventory);
            var runner = DialogueRunner.Instance;

            int goodbye = FindAnswer(runner, "Всего доброго.");
            Assert.GreaterOrEqual(goodbye, 0);

            runner.Choose(goodbye);

            Assert.IsFalse(runner.IsOpen);
            Assert.IsFalse(GameplayInput.Locked, "после диалога управление возвращается");
        }
    }
}
