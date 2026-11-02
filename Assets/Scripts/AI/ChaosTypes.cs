using UnityEngine;

namespace LabGame.AI
{
    /// <summary>Набор действий хаотичного NPC.</summary>
    public enum ChaosAction
    {
        Wander = 0,   // бесцельно бродит
        Stalk = 1,    // крадётся за игроком на расстоянии
        Flee = 2,     // в панике убегает
        Ambush = 3,   // обходит со спины и бросается
        Mimic = 4,    // повторяет движения игрока
        Taunt = 5,    // подбегает, дразнит, убегает
        FakeGift = 6, // подбрасывает «подарок»-обманку
        Freeze = 7,   // замирает и смотрит
        Blink = 8     // исчезает и появляется в другом месте
    }

    /// <summary>Сигналы игрового процесса, на которые NPC обязан реагировать.</summary>
    public enum ChaosSignal
    {
        PlayerPickedUpItem = 0,
        PlayerEquippedItem = 1,
        PlayerDroppedItem = 2,
        PlayerDamaged = 3,
        QuestChanged = 4,
        PlayerLookedAtMe = 5,
        PlayerCameClose = 6,
        PlayerRested = 7
    }

    /// <summary>Снимок ситуации, который видит «мозг» NPC в текущем кадре.</summary>
    public readonly struct ChaosContext
    {
        public readonly float DistanceToPlayer;
        public readonly bool HasLineOfSight;
        public readonly bool PlayerLookingAtMe;
        public readonly float PlayerHealth01;
        public readonly float PlayerSpeed;
        public readonly bool QuestActive;

        public ChaosContext(float distanceToPlayer, bool hasLineOfSight, bool playerLookingAtMe,
            float playerHealth01, float playerSpeed, bool questActive)
        {
            DistanceToPlayer = distanceToPlayer;
            HasLineOfSight = hasLineOfSight;
            PlayerLookingAtMe = playerLookingAtMe;
            PlayerHealth01 = playerHealth01;
            PlayerSpeed = playerSpeed;
            QuestActive = questActive;
        }

        public bool PlayerIsClose => DistanceToPlayer <= 7f;
        public bool PlayerIsFar => DistanceToPlayer >= 14f;
        public bool PlayerWounded => PlayerHealth01 <= 0.55f;
        public bool PlayerIsMoving => PlayerSpeed >= 0.6f;

        public override string ToString() =>
            $"dist={DistanceToPlayer:0.0} los={HasLineOfSight} looked={PlayerLookingAtMe} hp={PlayerHealth01:0.00}";
    }

    /// <summary>Русские названия действий — для HUD и отчёта.</summary>
    public static class ChaosActionNames
    {
        public static string Russian(ChaosAction action)
        {
            switch (action)
            {
                case ChaosAction.Wander: return "Бродит";
                case ChaosAction.Stalk: return "Следит";
                case ChaosAction.Flee: return "Убегает";
                case ChaosAction.Ambush: return "Засада";
                case ChaosAction.Mimic: return "Повторяет";
                case ChaosAction.Taunt: return "Дразнит";
                case ChaosAction.FakeGift: return "Подкидывает обманку";
                case ChaosAction.Freeze: return "Замер";
                case ChaosAction.Blink: return "Исчез";
                default: return action.ToString();
            }
        }
    }
}
