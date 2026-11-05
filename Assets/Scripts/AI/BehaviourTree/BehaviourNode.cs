using System;
using System.Text;
using UnityEngine;

namespace LabGame.AI
{
    /// <summary>Результат одного тика узла дерева поведения.</summary>
    public enum BehaviourStatus
    {
        Failure = 0,
        Success = 1,
        Running = 2
    }

    /// <summary>
    /// Узел дерева поведения. Дерево опрашивается заново каждый кадр,
    /// поэтому NPC мгновенно реагирует на изменение обстановки.
    /// </summary>
    public abstract class BehaviourNode
    {
        protected BehaviourNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public BehaviourStatus LastStatus { get; protected set; } = BehaviourStatus.Failure;

        public BehaviourStatus Tick(EnemyContext context)
        {
            LastStatus = OnTick(context);
            return LastStatus;
        }

        protected abstract BehaviourStatus OnTick(EnemyContext context);

        /// <summary>Сброс накопленного состояния (таймеров, индексов).</summary>
        public virtual void Reset()
        {
            LastStatus = BehaviourStatus.Failure;
        }

        /// <summary>Текстовое представление дерева с текущими статусами.</summary>
        public abstract void Describe(StringBuilder builder, int depth);

        protected string StatusLabel => StatusName(LastStatus);

        public static string StatusName(BehaviourStatus status)
        {
            switch (status)
            {
                case BehaviourStatus.Success: return "успех";
                case BehaviourStatus.Running: return "выполняется";
                default: return "провал";
            }
        }

        protected static void Indent(StringBuilder builder, int depth)
        {
            if (depth > 0) builder.Append(' ', depth * 2);
        }
    }

    /// <summary>Узел-композит: содержит список дочерних узлов.</summary>
    public abstract class CompositeNode : BehaviourNode
    {
        protected readonly System.Collections.Generic.List<BehaviourNode> Children =
            new System.Collections.Generic.List<BehaviourNode>();

        protected CompositeNode(string name) : base(name) { }

        public CompositeNode Add(BehaviourNode child)
        {
            Children.Add(child);
            return this;
        }

        public override void Reset()
        {
            base.Reset();

            for (int i = 0; i < Children.Count; i++)
                Children[i].Reset();
        }

        public override void Describe(StringBuilder builder, int depth)
        {
            Indent(builder, depth);
            builder.Append(Kind).Append(" «").Append(Name).Append("» — ").Append(StatusLabel).AppendLine();

            for (int i = 0; i < Children.Count; i++)
                Children[i].Describe(builder, depth + 1);
        }

        protected abstract string Kind { get; }
    }
}
