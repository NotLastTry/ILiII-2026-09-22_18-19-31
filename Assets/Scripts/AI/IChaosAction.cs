namespace LabGame.AI
{
    /// <summary>
    /// Одно действие хаотичного NPC (паттерн «Стратегия»).
    /// </summary>
    public interface IChaosAction
    {
        ChaosAction Id { get; }

        /// <summary>Базовый вес: чем больше, тем чаще действие выбирается.</summary>
        float Weight(ChaosContext context);

        /// <summary>Может ли действие выполняться в текущей ситуации.</summary>
        bool IsAvailable(ChaosContext context);

        float MinDuration { get; }

        float MaxDuration { get; }

        void Enter(IChaosActor actor, ChaosContext context);

        void Tick(IChaosActor actor, ChaosContext context, float deltaTime);

        void Exit(IChaosActor actor);
    }
}
