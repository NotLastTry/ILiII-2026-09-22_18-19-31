using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabGame.Dialogue
{
    /// <summary>
    /// Проигрывает диалог: хранит текущий узел и доступные ответы,
    /// применяет условия и действия. UI подписывается на события.
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        public static DialogueRunner Instance { get; private set; }

        /// <summary>Что-то изменилось: другой узел, другой набор ответов, диалог закрыт.</summary>
        public event Action Changed;

        /// <summary>Диалог закрыт.</summary>
        public event Action Closed;

        private readonly List<DialogueAnswer> answers = new List<DialogueAnswer>();

        private DialogueGraph graph;
        private IDialogueContext context;

        public bool IsOpen => graph != null;

        public DialogueGraph Graph => graph;

        public DialogueNode Current { get; private set; }

        public IReadOnlyList<DialogueAnswer> Answers => answers;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Найти раннер в сцене или создать его.</summary>
        public static DialogueRunner Ensure()
        {
            if (Instance != null) return Instance;

            var existing = FindAnyObjectByType<DialogueRunner>(FindObjectsInactive.Exclude);

            if (existing != null) return existing;

            return new GameObject("DialogueRunner").AddComponent<DialogueRunner>();
        }

        /// <summary>Начать диалог по идентификатору из Resources/Dialogues.</summary>
        public bool Begin(string dialogueId, IDialogueContext dialogueContext, string startId = null)
            => Begin(DialogueLibrary.Load(dialogueId), dialogueContext, startId);

        /// <summary>Начать диалог по уже разобранному графу.</summary>
        public bool Begin(DialogueGraph graphToPlay, IDialogueContext dialogueContext, string startId = null)
        {
            if (graphToPlay == null) return false;

            graph = graphToPlay;
            context = dialogueContext ?? new EmptyDialogueContext();

            var node = string.IsNullOrWhiteSpace(startId)
                ? graph.Start
                : graph.Find(startId) ?? graph.Start;

            if (node == null)
            {
                Close();
                return false;
            }

            EnterNode(node);
            return IsOpen;
        }

        /// <summary>Выбрать ответ по индексу.</summary>
        public void Choose(int index)
        {
            if (!IsOpen || index < 0 || index >= answers.Count) return;

            var answer = answers[index];

            DialogueRules.ApplyAll(answer.Action, context);

            if (answer.IsExit)
            {
                Close();
                return;
            }

            var next = graph.Find(answer.TargetId);

            if (next == null)
            {
                Close();
                return;
            }

            EnterNode(next);
        }

        public void Close()
        {
            bool wasOpen = IsOpen;

            graph = null;
            context = null;
            Current = null;
            answers.Clear();

            if (wasOpen) Closed?.Invoke();

            Changed?.Invoke();
        }

        private void EnterNode(DialogueNode node)
        {
            Current = node;

            DialogueRules.ApplyAll(node.Action, context);

            RebuildAnswers();
            Changed?.Invoke();
        }

        private void RebuildAnswers()
        {
            answers.Clear();

            if (Current != null)
            {
                foreach (var answer in Current.Answers)
                    if (DialogueRules.MatchesAll(answer.Condition, context))
                        answers.Add(answer);
            }

            // Без подходящих ответов игроку всегда можно просто выйти.
            if (answers.Count == 0)
                answers.Add(new DialogueAnswer { Text = "Продолжить", TargetId = DialogueParser.ExitTarget });
        }
    }
}
