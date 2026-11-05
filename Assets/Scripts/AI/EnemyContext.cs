using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LabGame.AI
{
    /// <summary>Состояния противника — ровно те, что требует задание.</summary>
    public enum EnemyState
    {
        /// <summary>Игрок вне зоны видимости, противник осматривается.</summary>
        OutOfSight = 0,

        /// <summary>Патрулирование по точкам (дополнительное задание).</summary>
        Patrol = 1,

        /// <summary>Преследование игрока.</summary>
        Chase = 2,

        /// <summary>Подготовка к атаке (замах).</summary>
        PrepareAttack = 3,

        /// <summary>Атака.</summary>
        Attack = 4,

        /// <summary>Поиск игрока в последней известной точке.</summary>
        Search = 5,

        /// <summary>Возвращение в промежуточную или начальную точку.</summary>
        Return = 6
    }

    public static class EnemyStates
    {
        public static string Russian(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.OutOfSight: return "Вне зоны видимости";
                case EnemyState.Patrol: return "Патрулирование";
                case EnemyState.Chase: return "Преследование";
                case EnemyState.PrepareAttack: return "Подготовка к атаке";
                case EnemyState.Attack: return "Атака";
                case EnemyState.Search: return "Поиск";
                case EnemyState.Return: return "Возвращение";
                default: return state.ToString();
            }
        }
    }

    /// <summary>
    /// «Чёрная доска» дерева поведения: всё, что узлы знают о мире
    /// и во что они могут писать.
    /// </summary>
    public class EnemyContext
    {
        public RovingNPC Owner;
        public Transform Self;
        public Transform Player;
        public NavMeshAgent Agent;

        /// <summary>Виден ли игрок прямо сейчас.</summary>
        public bool CanSeePlayer;

        public float DistanceToPlayer;

        /// <summary>Последняя известная позиция игрока.</summary>
        public Vector3 LastKnownPosition;

        /// <summary>Есть ли ещё смысл искать игрока.</summary>
        public bool HasLastKnown;

        /// <summary>Сколько секунд игрока не видно.</summary>
        public float TimeSinceSeen;

        /// <summary>Остаток перезарядки атаки.</summary>
        public float AttackCooldown;

        public EnemyState State = EnemyState.OutOfSight;

        public Vector3 HomePosition;
        public readonly List<Vector3> PatrolPoints = new List<Vector3>();
        public int PatrolIndex;
    }
}
