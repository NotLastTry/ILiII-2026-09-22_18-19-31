using UnityEngine;

namespace LabGame.AI
{
    /// <summary>
    /// Настройки «хаотичности» NPC. Вынесены в ScriptableObject,
    /// чтобы поведение можно было крутить из инспектора
    /// и иметь несколько разных характеров.
    /// </summary>
    [CreateAssetMenu(fileName = "ChaosProfile", menuName = "Lab 8/Chaos Profile")]
    public class ChaosProfile : ScriptableObject
    {
        [Header("Хаотичность")]
        [Tooltip("0 — предсказуемый NPC, 1 — максимальная непредсказуемость.")]
        [Range(0f, 1f)] public float chaos = 0.75f;

        [Tooltip("Вероятность мгновенно сменить действие в ответ на игровое событие.")]
        [Range(0f, 1f)] public float reactionChance = 0.8f;

        [Tooltip("Насколько сильно наказывается повторение недавних действий.")]
        [Range(0f, 0.95f)] public float antiRepeatStrength = 0.72f;

        [Tooltip("Сколько последних действий помнить.")]
        [Range(1, 8)] public int historyDepth = 4;

        [Tooltip("Множитель длительности действий.")]
        [Range(0.3f, 3f)] public float durationScale = 1f;

        [Header("Радиусы поведения")]
        public float homeRadius = 18f;
        public float stalkDistance = 8f;
        public float fleeRadius = 9f;
        public float ambushDistance = 7f;
        public float blinkRadius = 12f;

        [Header("Реплики")]
        public string[] taunts =
        {
            "Ты видишь меня?",
            "Я знаю, что ты взял.",
            "Не оборачивайся.",
            "Почти получилось!",
            "Ещё разок?"
        };

        public string[] giftLines =
        {
            "Держи, это тебе.",
            "Бесплатно. Совсем.",
            "Нашёл у тебя за спиной."
        };

        public string[] fleeLines =
        {
            "Слишком сильный!",
            "Я передумал.",
            "Не сейчас!"
        };

        public string Pick(System.Random rng, string[] source)
        {
            if (source == null || source.Length == 0) return string.Empty;
            return source[rng.Next(source.Length)];
        }
    }
}
