namespace LabGame.AI
{
    /// <summary>
    /// «Или»: перебирает детей, пока какой-нибудь не завершится успехом.
    /// Используется для выбора ветки поведения по приоритету.
    /// </summary>
    public class SelectorNode : CompositeNode
    {
        private int runningIndex = -1;

        public SelectorNode(string name) : base(name) { }

        protected override string Kind => "Селектор";

        protected override BehaviourStatus OnTick(EnemyContext context)
        {
            int start = runningIndex >= 0 ? runningIndex : 0;

            for (int i = start; i < Children.Count; i++)
            {
                var status = Children[i].Tick(context);

                if (status == BehaviourStatus.Running)
                {
                    runningIndex = i;
                    return BehaviourStatus.Running;
                }

                if (status == BehaviourStatus.Success)
                {
                    runningIndex = -1;
                    return BehaviourStatus.Success;
                }
            }

            runningIndex = -1;
            return BehaviourStatus.Failure;
        }

        public override void Reset()
        {
            runningIndex = -1;
            base.Reset();
        }
    }

    /// <summary>
    /// «И»: выполняет детей по порядку. Провал любого ребёнка — провал всего
    /// узла, успех всех — успех. Используется для цепочек «условие → действие».
    /// </summary>
    public class SequenceNode : CompositeNode
    {
        private int current;

        public SequenceNode(string name) : base(name) { }

        protected override string Kind => "Последовательность";

        protected override BehaviourStatus OnTick(EnemyContext context)
        {
            while (current < Children.Count)
            {
                var status = Children[current].Tick(context);

                if (status == BehaviourStatus.Running) return BehaviourStatus.Running;

                if (status == BehaviourStatus.Failure)
                {
                    Reset();
                    return BehaviourStatus.Failure;
                }

                current++;
            }

            // Ветка завершена — в следующий раз начинаем её с начала.
            current = 0;
            return BehaviourStatus.Success;
        }

        public override void Reset()
        {
            current = 0;
            base.Reset();
        }
    }
}
