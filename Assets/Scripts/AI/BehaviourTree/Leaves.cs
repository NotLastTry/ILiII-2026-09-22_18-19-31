using System;
using System.Text;
using UnityEngine;

namespace LabGame.AI
{
    /// <summary>Лист-условие: проверяет обстановку, ничего не делая.</summary>
    public class ConditionNode : BehaviourNode
    {
        private readonly Func<EnemyContext, bool> predicate;

        public ConditionNode(string name, Func<EnemyContext, bool> predicate) : base(name)
        {
            this.predicate = predicate;
        }

        protected override BehaviourStatus OnTick(EnemyContext context)
        {
            return predicate != null && predicate(context)
                ? BehaviourStatus.Success
                : BehaviourStatus.Failure;
        }

        public override void Describe(StringBuilder builder, int depth)
        {
            Indent(builder, depth);
            builder.Append("Условие «").Append(Name).Append("» — ").Append(StatusLabel).AppendLine();
        }
    }

    /// <summary>Лист-действие: меняет мир и возвращает результат.</summary>
    public class ActionNode : BehaviourNode
    {
        private readonly Func<EnemyContext, BehaviourStatus> action;

        public ActionNode(string name, Func<EnemyContext, BehaviourStatus> action) : base(name)
        {
            this.action = action;
        }

        protected override BehaviourStatus OnTick(EnemyContext context)
        {
            return action != null ? action(context) : BehaviourStatus.Failure;
        }

        public override void Describe(StringBuilder builder, int depth)
        {
            Indent(builder, depth);
            builder.Append("Действие «").Append(Name).Append("» — ").Append(StatusLabel).AppendLine();
        }
    }

    /// <summary>Пауза: возвращает Running, пока не истечёт время.</summary>
    public class WaitNode : BehaviourNode
    {
        private readonly Func<EnemyContext, float> duration;
        private float elapsed;
        private bool started;

        public WaitNode(string name, Func<EnemyContext, float> duration) : base(name)
        {
            this.duration = duration;
        }

        protected override BehaviourStatus OnTick(EnemyContext context)
        {
            if (!started)
            {
                started = true;
                elapsed = 0f;
            }

            elapsed += Time.deltaTime;

            float limit = duration != null ? duration(context) : 0f;

            if (elapsed < limit) return BehaviourStatus.Running;

            started = false;
            elapsed = 0f;
            return BehaviourStatus.Success;
        }

        public override void Reset()
        {
            started = false;
            elapsed = 0f;
            base.Reset();
        }

        public override void Describe(StringBuilder builder, int depth)
        {
            Indent(builder, depth);
            builder.Append("Пауза «").Append(Name).Append("» — ").Append(StatusLabel).AppendLine();
        }
    }
}
