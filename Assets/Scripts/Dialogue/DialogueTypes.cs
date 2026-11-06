using System;
using System.Collections.Generic;

namespace LabGame.Dialogue
{
    /// <summary>Вариант ответа игрока в узле диалога.</summary>
    public sealed class DialogueAnswer
    {
        /// <summary>Текст на кнопке.</summary>
        public string Text = string.Empty;

        /// <summary>Куда ведёт ответ. «end» или пусто — закрыть диалог.</summary>
        public string TargetId = string.Empty;

        /// <summary>Условие показа; пусто — показывать всегда.</summary>
        public string Condition;

        /// <summary>Действие при выборе; пусто — нет.</summary>
        public string Action;

        public bool IsExit =>
            string.IsNullOrEmpty(TargetId) ||
            string.Equals(TargetId, "end", StringComparison.OrdinalIgnoreCase);

        public override string ToString() => $"{Text} → {TargetId}";
    }

    /// <summary>Одна реплика диалога со своими ответами.</summary>
    public sealed class DialogueNode
    {
        public string Id = string.Empty;
        public string Speaker = string.Empty;
        public string Text = string.Empty;

        /// <summary>Действие при входе в узел.</summary>
        public string Action;

        public readonly List<DialogueAnswer> Answers = new List<DialogueAnswer>();

        public override string ToString() => $"{Id}: {Text}";
    }

    /// <summary>Разобранный XML-диалог: набор узлов и точка входа.</summary>
    public sealed class DialogueGraph
    {
        private readonly Dictionary<string, DialogueNode> nodes =
            new Dictionary<string, DialogueNode>(StringComparer.OrdinalIgnoreCase);

        public string Id = string.Empty;
        public string Speaker = string.Empty;

        /// <summary>С какого узла начинать.</summary>
        public string StartId = "start";

        public IReadOnlyCollection<DialogueNode> Nodes => nodes.Values;

        public int Count => nodes.Count;

        public void Add(DialogueNode node)
        {
            if (node == null) return;

            nodes[node.Id] = node;
        }

        public bool Contains(string id) => !string.IsNullOrEmpty(id) && nodes.ContainsKey(id);

        public DialogueNode Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            return nodes.TryGetValue(id, out var node) ? node : null;
        }

        public DialogueNode Start => Find(StartId) ?? FirstNode();

        public DialogueNode FirstNode()
        {
            foreach (var node in nodes.Values) return node;

            return null;
        }
    }

    /// <summary>Ошибка формата диалогового XML.</summary>
    public sealed class DialogueFormatException : Exception
    {
        public DialogueFormatException(string message) : base(message)
        {
        }
    }
}
