using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabGame.AI
{
    /// <summary>
    /// «Мозг» хаотичного NPC (ПЗ №8).
    ///
    /// Как достигается непредсказуемость:
    ///   1. взвешенный случайный выбор действия (а не жёсткая FSM);
    ///   2. память о недавних действиях — повтор сильно штрафуется,
    ///      поэтому NPC не зацикливается;
    ///   3. случайная длительность каждого действия;
    ///   4. реакция на события игры: сигнал временно меняет веса
    ///      и может мгновенно переключить действие.
    ///
    /// Класс не зависит от сцены: работает с IChaosActor,
    /// поэтому его легко протестировать подменой актёра.
    /// </summary>
    public sealed class ChaosBrain
    {
        private readonly ChaosProfile profile;
        private readonly IChaosAction[] actions;
        private readonly System.Random rng;
        private readonly List<ChaosAction> history = new List<ChaosAction>();
        private readonly Dictionary<ChaosAction, float> bias = new Dictionary<ChaosAction, float>();
        private readonly List<ChaosAction> biasKeys = new List<ChaosAction>();

        private IChaosAction current;
        private float timer;
        private float duration;

        /// <summary>Смены действия (счётчик для статистики/тестов).</summary>
        public int Changes { get; private set; }

        /// <summary>Сколько раз «мозг» обновлялся (для тестов и отладки).</summary>
        public int Ticks { get; private set; }

        public ChaosBrain(ChaosProfile profile, IChaosAction[] actions, int seed)
        {
            this.profile = profile != null ? profile : ScriptableObject.CreateInstance<ChaosProfile>();
            this.actions = actions ?? Array.Empty<IChaosAction>();
            rng = new System.Random(seed);
        }

        public ChaosAction CurrentAction => current != null ? current.Id : ChaosAction.Wander;

        public float TimeLeft => Mathf.Max(0f, duration - timer);

        public ChaosSignal? LastSignal { get; private set; }

        public ChaosAction? LastReaction { get; private set; }

        public IReadOnlyList<ChaosAction> History => history;

        public ChaosProfile Profile => profile;

        public event Action<ChaosAction> ActionChanged;

        // ─────────────────────────────────────────────────────────────
        //  Основной цикл
        // ─────────────────────────────────────────────────────────────

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            if (actor == null) return;

            Ticks++;

            if (current == null)
            {
                ChooseNext(actor, context);
                if (current == null) return;
            }
            else
            {
                timer += deltaTime;
                DecayBias(deltaTime);

                bool expired = timer >= duration;
                bool impossible = !current.IsAvailable(context);

                if (expired || impossible) ChooseNext(actor, context);
            }

            current.Tick(actor, context, deltaTime);
        }

        /// <summary>
        /// Реакция на изменение игрового процесса.
        /// Сначала событие сдвигает веса, затем с заданной вероятностью
        /// действие меняется немедленно — поэтому реакция «живая»,
        /// но не выглядит скриптованной.
        /// </summary>
        public void Notify(ChaosSignal signal, IChaosActor actor, ChaosContext context)
        {
            if (actor == null) return;

            LastSignal = signal;
            ApplyBias(signal);

            if (rng.NextDouble() > profile.reactionChance) return;

            var forced = MapSignalToAction(signal);
            if (forced == null || !forced.IsAvailable(context)) return;

            ForceAction(forced, actor, context);
        }

        private void ForceAction(IChaosAction action, IChaosActor actor, ChaosContext context)
        {
            if (current != null) current.Exit(actor);

            current = action;
            timer = 0f;
            duration = RollDuration(action);

            PushHistory(action.Id);

            Changes++;
            LastReaction = action.Id;

            action.Enter(actor, context);
            ActionChanged?.Invoke(action.Id);
        }

        // ─────────────────────────────────────────────────────────────
        //  Выбор действия
        // ─────────────────────────────────────────────────────────────

        private void ChooseNext(IChaosActor actor, ChaosContext context)
        {
            ChaosAction previous = current != null ? current.Id : (ChaosAction)(-1);
            if (current != null) current.Exit(actor);

            int count = actions.Length;
            if (count == 0) return;

            var weights = new float[count];
            float total = 0f;

            for (int i = 0; i < count; i++)
            {
                var action = actions[i];
                float weight = 0f;

                if (action.IsAvailable(context))
                {
                    weight = Mathf.Max(0.0001f, action.Weight(context));
                    weight *= RepetitionPenalty(action.Id);

                    if (bias.TryGetValue(action.Id, out float boost)) weight *= boost;

                    // Мгновенный повтор почти запрещён, тем сильнее, чем выше хаос.
                    if (action.Id == previous && count > 1)
                        weight *= Mathf.Lerp(0.30f, 0.02f, Mathf.Clamp01(profile.chaos));
                }

                weights[i] = weight;
                total += weight;
            }

            int chosenIndex;
            if (total <= 0f)
            {
                // Теоретически невозможно, но лучше не ронять NPC.
                chosenIndex = rng.Next(count);
            }
            else
            {
                double roll = rng.NextDouble() * total;
                chosenIndex = count - 1;

                for (int i = 0; i < count; i++)
                {
                    roll -= weights[i];
                    if (roll <= 0d)
                    {
                        chosenIndex = i;
                        break;
                    }
                }
            }

            current = actions[chosenIndex];
            timer = 0f;
            duration = RollDuration(current);

            PushHistory(current.Id);

            Changes++;
            current.Enter(actor, context);
            ActionChanged?.Invoke(current.Id);
        }

        private float RollDuration(IChaosAction action)
        {
            float t = (float)rng.NextDouble();
            return Mathf.Lerp(action.MinDuration, action.MaxDuration, t) * Mathf.Max(0.1f, profile.durationScale);
        }

        private float RepetitionPenalty(ChaosAction id)
        {
            int occurrences = 0;
            for (int i = 0; i < history.Count; i++)
                if (history[i] == id) occurrences++;

            if (occurrences == 0) return 1f;

            float penalty = Mathf.Pow(1f - Mathf.Clamp01(profile.antiRepeatStrength), occurrences);
            return Mathf.Max(0.001f, penalty);
        }

        private void PushHistory(ChaosAction id)
        {
            history.Add(id);

            int depth = Mathf.Clamp(profile.historyDepth, 1, 16);
            while (history.Count > depth) history.RemoveAt(0);
        }

        // ─────────────────────────────────────────────────────────────
        //  Смещение весов под влиянием событий
        // ─────────────────────────────────────────────────────────────

        private void ApplyBias(ChaosSignal signal)
        {
            switch (signal)
            {
                case ChaosSignal.PlayerPickedUpItem:
                    Boost(ChaosAction.Stalk, 1.8f);
                    Boost(ChaosAction.FakeGift, 2.2f);
                    break;

                case ChaosSignal.PlayerEquippedItem:
                    Boost(ChaosAction.Flee, 2.0f);
                    Boost(ChaosAction.Ambush, 1.6f);
                    break;

                case ChaosSignal.PlayerDroppedItem:
                    Boost(ChaosAction.FakeGift, 2.4f);
                    Boost(ChaosAction.Taunt, 1.5f);
                    break;

                case ChaosSignal.PlayerDamaged:
                    Boost(ChaosAction.Taunt, 2.0f);
                    Boost(ChaosAction.Ambush, 1.5f);
                    break;

                case ChaosSignal.QuestChanged:
                    Boost(ChaosAction.Taunt, 2.0f);
                    Boost(ChaosAction.Blink, 1.4f);
                    break;

                case ChaosSignal.PlayerLookedAtMe:
                    Boost(ChaosAction.Freeze, 2.2f);
                    Boost(ChaosAction.Flee, 1.5f);
                    break;

                case ChaosSignal.PlayerCameClose:
                    Boost(ChaosAction.Ambush, 1.8f);
                    Boost(ChaosAction.Mimic, 1.5f);
                    break;

                case ChaosSignal.PlayerRested:
                    Boost(ChaosAction.Stalk, 1.6f);
                    break;
            }
        }

        private void Boost(ChaosAction id, float multiplier)
        {
            if (bias.TryGetValue(id, out float currentValue))
            {
                if (multiplier > currentValue) bias[id] = multiplier;
            }
            else
            {
                bias[id] = multiplier;
                biasKeys.Add(id);
            }
        }

        private void DecayBias(float deltaTime)
        {
            if (biasKeys.Count == 0) return;

            for (int i = biasKeys.Count - 1; i >= 0; i--)
            {
                var key = biasKeys[i];
                float value = Mathf.MoveTowards(bias[key], 1f, deltaTime * 0.30f);

                if (value <= 1.0001f)
                {
                    bias.Remove(key);
                    biasKeys.RemoveAt(i);
                }
                else
                {
                    bias[key] = value;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Таблица реакций «событие → действие»
        // ─────────────────────────────────────────────────────────────

        private IChaosAction MapSignalToAction(ChaosSignal signal)
        {
            switch (signal)
            {
                case ChaosSignal.PlayerPickedUpItem:
                    return Pick(ChaosAction.Stalk, ChaosAction.FakeGift);

                case ChaosSignal.PlayerEquippedItem:
                    return Pick(ChaosAction.Flee, ChaosAction.Ambush);

                case ChaosSignal.PlayerDroppedItem:
                    return Pick(ChaosAction.FakeGift, ChaosAction.Taunt);

                case ChaosSignal.PlayerDamaged:
                    return Pick(ChaosAction.Taunt, ChaosAction.Ambush);

                case ChaosSignal.QuestChanged:
                    return Pick(ChaosAction.Taunt, ChaosAction.Blink);

                case ChaosSignal.PlayerLookedAtMe:
                    return Pick(ChaosAction.Freeze, ChaosAction.Flee);

                case ChaosSignal.PlayerCameClose:
                    return Pick(ChaosAction.Ambush, ChaosAction.Mimic);

                case ChaosSignal.PlayerRested:
                    return Pick(ChaosAction.Stalk, ChaosAction.Wander);

                default:
                    return null;
            }
        }

        private IChaosAction Pick(ChaosAction first, ChaosAction second)
        {
            var a = Find(first);
            var b = Find(second);

            if (a == null) return b;
            if (b == null) return a;

            return rng.NextDouble() < 0.5 ? a : b;
        }

        private IChaosAction Find(ChaosAction id)
        {
            for (int i = 0; i < actions.Length; i++)
                if (actions[i].Id == id) return actions[i];
            return null;
        }
    }
}
