using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabGame.Dialogue
{
    /// <summary>Кто отвечает на условия и выполняет действия диалога.</summary>
    public interface IDialogueContext
    {
        /// <summary>Выполняется ли условие вида «quest:idle».</summary>
        bool CheckCondition(string condition);

        /// <summary>Выполнить действие вида «quest:accept».</summary>
        void ApplyAction(string action);
    }

    /// <summary>Заглушка: все условия истинны, действия игнорируются.</summary>
    public sealed class EmptyDialogueContext : IDialogueContext
    {
        public bool CheckCondition(string condition) => true;

        public void ApplyAction(string action)
        {
        }
    }

    /// <summary>Проверка условий и выполнение действий из строк XML.</summary>
    public static class DialogueRules
    {
        public static bool MatchesAll(string condition, IDialogueContext context)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;
            if (context == null) return true;

            foreach (var part in condition.Split(';'))
            {
                string value = part.Trim();
                if (value.Length == 0) continue;

                if (!context.CheckCondition(value)) return false;
            }

            return true;
        }

        public static void ApplyAll(string action, IDialogueContext context)
        {
            if (string.IsNullOrWhiteSpace(action) || context == null) return;

            foreach (var part in action.Split(';'))
            {
                string value = part.Trim();
                if (value.Length == 0) continue;

                context.ApplyAction(value);
            }
        }
    }

    /// <summary>
    /// Диалог, привязанный к квесту NPC: условия читают состояние квеста
    /// и инвентарь игрока, действия меняют состояние квеста.
    /// </summary>
    public sealed class QuestDialogueContext : IDialogueContext
    {
        private readonly NPCController npc;
        private readonly PlayerInventory inventory;

        public QuestDialogueContext(NPCController npc, PlayerInventory inventory)
        {
            this.npc = npc;
            this.inventory = inventory;
        }

        public bool CheckCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition)) return true;

            Split(condition, out string key, out string value);

            switch (key.ToLowerInvariant())
            {
                case "quest":
                    return npc != null &&
                           string.Equals(StateKey(npc.questState), value, StringComparison.OrdinalIgnoreCase);

                case "has":
                case "item":
                    return HasItem(value);

                case "always":
                    return true;

                default:
                    Debug.LogWarning($"[Диалог] неизвестное условие «{condition}»");
                    return false;
            }
        }

        public void ApplyAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action)) return;

            Split(action, out string key, out string value);

            switch (key.ToLowerInvariant())
            {
                case "quest":
                    ApplyQuestAction(value);
                    break;

                default:
                    Debug.LogWarning($"[Диалог] неизвестное действие «{action}»");
                    break;
            }
        }

        private void ApplyQuestAction(string value)
        {
            if (npc == null) return;

            switch (value.ToLowerInvariant())
            {
                case "offer": npc.OfferQuest(); break;
                case "accept": npc.AcceptQuest(); break;
                case "done": npc.TurnInQuest(); break;
                case "reset": npc.ResetQuest(); break;
                default: Debug.LogWarning($"[Диалог] неизвестное состояние квеста «{value}»"); break;
            }
        }

        /// <summary>Короткое имя состояния квеста для условий XML: idle, offer, active, done, rewarded.</summary>
        private static string StateKey(NPCQuestState state)
        {
            switch (state)
            {
                case NPCQuestState.Offer: return "offer";
                case NPCQuestState.QuestActive: return "active";
                case NPCQuestState.QuestDone: return "done";
                case NPCQuestState.RewardGiven: return "rewarded";
                default: return "idle";
            }
        }

        private bool HasItem(string itemId)        {
            if (inventory == null || inventory.Model == null || string.IsNullOrWhiteSpace(itemId)) return false;

            foreach (var stack in inventory.Model.Slots)
            {
                if (stack == null || stack.amount <= 0 || stack.item == null) continue;

                if (string.Equals(stack.item.itemId, itemId, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static void Split(string raw, out string key, out string value)
        {
            int separator = raw.IndexOf(':');

            if (separator < 0)
            {
                key = raw.Trim();
                value = string.Empty;
                return;
            }

            key = raw.Substring(0, separator).Trim();
            value = raw.Substring(separator + 1).Trim();
        }
    }

    /// <summary>Загрузка диалогов из Resources/Dialogues с кешем.</summary>
    public static class DialogueLibrary
    {
        private const string Folder = "Dialogues";

        private static readonly Dictionary<string, DialogueGraph> cache =
            new Dictionary<string, DialogueGraph>(StringComparer.OrdinalIgnoreCase);

        public static DialogueGraph Load(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId)) return null;

            if (cache.TryGetValue(dialogueId, out var cached)) return cached;

            var asset = Resources.Load<TextAsset>($"{Folder}/{dialogueId}");

            if (asset == null)
            {
                Debug.LogError($"[Диалог] не найден файл Resources/{Folder}/{dialogueId}.xml");
                return null;
            }

            var graph = DialogueParser.Parse(asset.text);
            cache[dialogueId] = graph;

            return graph;
        }

        public static void Clear() => cache.Clear();
    }
}
