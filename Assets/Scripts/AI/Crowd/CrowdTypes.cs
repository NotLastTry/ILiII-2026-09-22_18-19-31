using System;
using UnityEngine;

namespace LabGame.AI
{
    /// <summary>Как ведёт себя участник толпы прямо сейчас.</summary>
    public enum CrowdMode
    {
        /// <summary>Спокойно бродит вокруг своего дома.</summary>
        Calm = 0,

        /// <summary>Заинтересовался игроком и подходит ближе.</summary>
        Curious = 1,

        /// <summary>Испуган и разбегается от угрозы.</summary>
        Panic = 2
    }

    public static class CrowdModes
    {
        public static string Russian(CrowdMode mode)
        {
            switch (mode)
            {
                case CrowdMode.Curious: return "любопытство";
                case CrowdMode.Panic: return "паника";
                default: return "спокойствие";
            }
        }
    }

    /// <summary>Настройки стайного поведения.</summary>
    [Serializable]
    public class CrowdSettings
    {
        [Header("Соседи")]
        [Tooltip("Радиус, в котором участники видят друг друга.")]
        public float neighborRadius = 5f;

        [Tooltip("Радиус расталкивания: ближе — сильнее отталкивает.")]
        public float separationRadius = 2f;

        [Header("Веса сил")]
        public float separationWeight = 1.8f;
        public float alignmentWeight = 0.5f;
        public float cohesionWeight = 0.7f;
        public float seekWeight = 1f;
        public float homeWeight = 0.35f;
        public float interestWeight = 1.2f;
        public float panicWeight = 2.5f;

        [Header("Движение")]
        public float maxForce = 3f;
        public float maxSpeed = 2.2f;
        public float panicSpeed = 4.6f;

        [Tooltip("Личная дистанция при подходе к интересному объекту.")]
        public float personalSpace = 2.5f;
    }
}
