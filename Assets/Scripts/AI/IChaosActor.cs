using UnityEngine;

namespace LabGame.AI
{
    /// <summary>
    /// Всё, что действию разрешено делать с NPC.
    /// Благодаря этому интерфейсу логику поведения можно тестировать
    /// подменой актёра, без запуска сцены и NavMesh.
    /// </summary>
    public interface IChaosActor
    {
        Vector3 Position { get; }

        /// <summary>Позиция игрока.</summary>
        Vector3 PlayerPosition { get; }

        /// <summary>Направление взгляда игрока (камера).</summary>
        Vector3 PlayerForward { get; }

        /// <summary>Генератор случайных чисел NPC (детерминированный при фиксированном seed).</summary>
        System.Random Rng { get; }

        bool IsMoving { get; }

        void MoveTo(Vector3 worldPosition);

        void Stop();

        void FaceTowards(Vector3 worldPosition);

        void PlayTrigger(string triggerName);

        void Say(string text, float duration = 3f);

        bool TryRandomPoint(Vector3 origin, float radius, out Vector3 point);

        void Teleport(Vector3 worldPosition);

        /// <summary>Подбросить «подарок» — предмет-обманку, который рассыпается при подборе.</summary>
        void SpawnFakeGift(Vector3 worldPosition);
    }
}
