using UnityEngine;

namespace LabGame.AI
{
    /// <summary>Фабрика стандартного набора действий.</summary>
    public static class ChaosActions
    {
        public static IChaosAction[] BuildDefault(ChaosProfile profile)
        {
            return new IChaosAction[]
            {
                new WanderAction(profile),
                new StalkAction(profile),
                new FleeAction(profile),
                new AmbushAction(profile),
                new MimicAction(profile),
                new TauntAction(profile),
                new FakeGiftAction(profile),
                new FreezeAction(),
                new BlinkAction(profile)
            };
        }

        internal static Vector3 RandomFlatDirection(System.Random rng)
        {
            double angle = rng.NextDouble() * System.Math.PI * 2.0;
            return new Vector3((float)System.Math.Cos(angle), 0f, (float)System.Math.Sin(angle));
        }
    }

    /// <summary>Бесцельное блуждание вокруг «дома». Базовое состояние NPC.</summary>
    public sealed class WanderAction : IChaosAction
    {
        private readonly ChaosProfile profile;
        private Vector3 home;
        private bool hasHome;
        private float retarget;

        public WanderAction(ChaosProfile profile) => this.profile = profile;

        public ChaosAction Id => ChaosAction.Wander;
        public float MinDuration => 2.5f;
        public float MaxDuration => 6f;

        public bool IsAvailable(ChaosContext context) => true;

        public float Weight(ChaosContext context) => context.PlayerIsFar ? 1.5f : 0.85f;

        public void Enter(IChaosActor actor, ChaosContext context)
        {
            if (!hasHome)
            {
                home = actor.Position;
                hasHome = true;
            }

            Retarget(actor);
            retarget = 2.5f;
        }

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            retarget -= deltaTime;
            if (retarget > 0f && actor.IsMoving) return;

            Retarget(actor);
            retarget = 2f + (float)actor.Rng.NextDouble() * 2.5f;
        }

        private void Retarget(IChaosActor actor)
        {
            if (actor.TryRandomPoint(home, profile.homeRadius, out var point))
                actor.MoveTo(point);
        }

        public void Exit(IChaosActor actor) { }
    }

    /// <summary>Слежка: держится на расстоянии и замирает, когда на него смотрят.</summary>
    public sealed class StalkAction : IChaosAction
    {
        private readonly ChaosProfile profile;
        private float adjust;

        public StalkAction(ChaosProfile profile) => this.profile = profile;

        public ChaosAction Id => ChaosAction.Stalk;
        public float MinDuration => 3f;
        public float MaxDuration => 7f;

        public bool IsAvailable(ChaosContext context) => context.DistanceToPlayer <= 30f;

        public float Weight(ChaosContext context)
        {
            if (context.PlayerIsFar) return 1.6f;
            if (context.PlayerIsClose) return 0.6f;
            return 1.2f;
        }

        public void Enter(IChaosActor actor, ChaosContext context) => adjust = 0f;

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            // Пока игрок смотрит — стоит неподвижно. Стоит отвернуться — снова крадётся.
            if (context.PlayerLookingAtMe && context.HasLineOfSight)
            {
                actor.Stop();
                actor.FaceTowards(actor.PlayerPosition);
                return;
            }

            adjust -= deltaTime;
            if (adjust > 0f) return;

            adjust = 0.5f;

            Vector3 away = actor.Position - actor.PlayerPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = ChaosActions.RandomFlatDirection(actor.Rng);
            away.Normalize();

            Vector3 target = actor.PlayerPosition + away * Mathf.Max(2.5f, profile.stalkDistance);
            if (actor.TryRandomPoint(target, 2.5f, out var point))
                actor.MoveTo(point);
        }

        public void Exit(IChaosActor actor) { }
    }

    /// <summary>Паническое бегство от игрока.</summary>
    public sealed class FleeAction : IChaosAction
    {
        private readonly ChaosProfile profile;
        private float adjust;

        public FleeAction(ChaosProfile profile) => this.profile = profile;

        public ChaosAction Id => ChaosAction.Flee;
        public float MinDuration => 2f;
        public float MaxDuration => 4f;

        public bool IsAvailable(ChaosContext context) => context.DistanceToPlayer <= profile.fleeRadius * 1.8f;

        public float Weight(ChaosContext context)
        {
            if (context.PlayerWounded) return 0.4f;
            return context.PlayerIsClose ? 1.8f : 0.7f;
        }

        public void Enter(IChaosActor actor, ChaosContext context)
        {
            adjust = 0f;
            actor.Say(profile.Pick(actor.Rng, profile.fleeLines), 2f);
            RunAway(actor);
        }

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            adjust -= deltaTime;
            if (adjust > 0f) return;

            adjust = 0.6f;
            RunAway(actor);
        }

        private void RunAway(IChaosActor actor)
        {
            Vector3 away = actor.Position - actor.PlayerPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = ChaosActions.RandomFlatDirection(actor.Rng);
            away.Normalize();

            Vector3 target = actor.Position + away * 10f;
            if (actor.TryRandomPoint(target, 4f, out var point))
                actor.MoveTo(point);
        }

        public void Exit(IChaosActor actor) { }
    }

    /// <summary>Засада: обходит со спины, ждёт и бросается.</summary>
    public sealed class AmbushAction : IChaosAction
    {
        private readonly ChaosProfile profile;
        private int phase;
        private float timer;

        public AmbushAction(ChaosProfile profile) => this.profile = profile;

        public ChaosAction Id => ChaosAction.Ambush;
        public float MinDuration => 4f;
        public float MaxDuration => 8f;

        public bool IsAvailable(ChaosContext context) => context.DistanceToPlayer <= 22f;

        public float Weight(ChaosContext context)
        {
            if (context.PlayerLookingAtMe) return 0.35f;
            return context.PlayerIsFar ? 1.3f : 1.0f;
        }

        public void Enter(IChaosActor actor, ChaosContext context)
        {
            phase = 0;
            timer = 0f;
        }

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            switch (phase)
            {
                case 0:
                    timer -= deltaTime;
                    if (timer <= 0f)
                    {
                        timer = 1.2f;

                        Vector3 behind = actor.PlayerPosition - actor.PlayerForward * profile.ambushDistance;
                        if (actor.TryRandomPoint(behind, 3f, out var point))
                            actor.MoveTo(point);
                    }

                    if (!context.PlayerLookingAtMe &&
                        Vector3.Distance(actor.Position, actor.PlayerPosition) <= profile.ambushDistance + 2f)
                    {
                        phase = 1;
                        actor.Stop();
                    }
                    break;

                case 1:
                    // Стоит и ждёт, пока игрок отвернётся.
                    actor.Stop();
                    actor.FaceTowards(actor.PlayerPosition);

                    if (!context.PlayerLookingAtMe)
                    {
                        phase = 2;
                        actor.Say("Поймал!", 2f);
                        actor.PlayTrigger("Taunt");
                    }
                    break;

                case 2:
                    actor.MoveTo(actor.PlayerPosition);
                    break;
            }
        }

        public void Exit(IChaosActor actor) { }
    }

    /// <summary>Мимикрия: повторяет направление движения игрока.</summary>
    public sealed class MimicAction : IChaosAction
    {
        private readonly ChaosProfile profile;
        private float adjust;

        public MimicAction(ChaosProfile profile) => this.profile = profile;

        public ChaosAction Id => ChaosAction.Mimic;
        public float MinDuration => 3f;
        public float MaxDuration => 6f;

        public bool IsAvailable(ChaosContext context) =>
            context.DistanceToPlayer <= 22f && context.PlayerIsMoving;

        public float Weight(ChaosContext context) => context.PlayerIsMoving ? 1.3f : 0f;

        public void Enter(IChaosActor actor, ChaosContext context) => adjust = 0f;

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            adjust -= deltaTime;
            if (adjust > 0f) return;

            adjust = 0.35f;

            // Идёт следом, сохраняя дистанцию по линии взгляда игрока.
            Vector3 target = actor.PlayerPosition - actor.PlayerForward * 5f;
            if (actor.TryRandomPoint(target, 2.5f, out var point))
                actor.MoveTo(point);

            actor.FaceTowards(actor.PlayerPosition);
        }

        public void Exit(IChaosActor actor) { }
    }

    /// <summary>Подбегает, дразнит и убегает.</summary>
    public sealed class TauntAction : IChaosAction
    {
        private readonly ChaosProfile profile;
        private bool taunted;
        private float timer;

        public TauntAction(ChaosProfile profile) => this.profile = profile;

        public ChaosAction Id => ChaosAction.Taunt;
        public float MinDuration => 3f;
        public float MaxDuration => 5f;

        public bool IsAvailable(ChaosContext context) => context.DistanceToPlayer <= 18f;
        public float Weight(ChaosContext context) => context.PlayerIsClose ? 1.5f : 1.0f;

        public void Enter(IChaosActor actor, ChaosContext context)
        {
            taunted = false;
            timer = 0f;
        }

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            if (!taunted)
            {
                if (context.DistanceToPlayer > 6f)
                {
                    actor.MoveTo(actor.PlayerPosition);
                    return;
                }

                actor.Stop();
                actor.FaceTowards(actor.PlayerPosition);
                actor.Say(profile.Pick(actor.Rng, profile.taunts), 2.5f);
                actor.PlayTrigger("Taunt");

                taunted = true;
                timer = 1.0f;
                return;
            }

            timer -= deltaTime;
            if (timer > 0f) return;

            timer = 1.2f;

            Vector3 away = actor.Position - actor.PlayerPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = ChaosActions.RandomFlatDirection(actor.Rng);

            Vector3 target = actor.Position + away.normalized * 8f;
            if (actor.TryRandomPoint(target, 4f, out var point))
                actor.MoveTo(point);
        }

        public void Exit(IChaosActor actor) { }
    }

    /// <summary>Подкидывает предмет-обманку рядом с игроком и уходит.</summary>
    public sealed class FakeGiftAction : IChaosAction
    {
        private readonly ChaosProfile profile;
        private bool placed;
        private float timer;

        public FakeGiftAction(ChaosProfile profile) => this.profile = profile;

        public ChaosAction Id => ChaosAction.FakeGift;
        public float MinDuration => 3f;
        public float MaxDuration => 6f;

        public bool IsAvailable(ChaosContext context) => context.DistanceToPlayer <= 20f;

        public float Weight(ChaosContext context) => context.QuestActive ? 1.5f : 0.9f;

        public void Enter(IChaosActor actor, ChaosContext context)
        {
            placed = false;
            timer = 0f;
        }

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            timer -= deltaTime;

            if (!placed)
            {
                if (timer > 0f) return;

                Vector3 direction = ChaosActions.RandomFlatDirection(actor.Rng);
                Vector3 spot = actor.PlayerPosition + direction * 2.5f;

                if (actor.TryRandomPoint(spot, 3f, out var point))
                {
                    actor.SpawnFakeGift(point);
                    actor.Say(profile.Pick(actor.Rng, profile.giftLines), 2.5f);
                    placed = true;
                    timer = 1.5f;
                }
                else
                {
                    timer = 0.5f;
                }

                return;
            }

            if (timer > 0f) return;

            timer = 1.0f;

            Vector3 away = actor.Position - actor.PlayerPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = ChaosActions.RandomFlatDirection(actor.Rng);

            if (actor.TryRandomPoint(actor.Position + away.normalized * 6f, 4f, out var runPoint))
                actor.MoveTo(runPoint);
        }

        public void Exit(IChaosActor actor) { }
    }

    /// <summary>Замирает и пристально смотрит на игрока. Классический «пугающий» приём.</summary>
    public sealed class FreezeAction : IChaosAction
    {
        public ChaosAction Id => ChaosAction.Freeze;
        public float MinDuration => 2.5f;
        public float MaxDuration => 5f;

        public bool IsAvailable(ChaosContext context) => true;

        public float Weight(ChaosContext context)
        {
            // Особенно охотно замирает, когда игрок смотрит прямо на него.
            return context.PlayerLookingAtMe && context.HasLineOfSight ? 1.8f : 0.45f;
        }

        public void Enter(IChaosActor actor, ChaosContext context) => actor.Stop();

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            actor.Stop();
            actor.FaceTowards(actor.PlayerPosition);
        }

        public void Exit(IChaosActor actor) { }
    }

    /// <summary>
    /// «Мигание»: пока на NPC не смотрят, он переносится в другое место.
    /// Главный источник ощущения «он был здесь секунду назад».
    /// </summary>
    public sealed class BlinkAction : IChaosAction
    {
        private readonly ChaosProfile profile;
        private bool done;

        public BlinkAction(ChaosProfile profile) => this.profile = profile;

        public ChaosAction Id => ChaosAction.Blink;
        public float MinDuration => 1.2f;
        public float MaxDuration => 2.2f;

        public bool IsAvailable(ChaosContext context) => context.DistanceToPlayer <= profile.blinkRadius * 2f;

        public float Weight(ChaosContext context)
        {
            // Если на него смотрят — почти не мигает (иначе это выглядело бы как баг).
            return context.PlayerLookingAtMe ? 0.15f : 1.7f;
        }

        public void Enter(IChaosActor actor, ChaosContext context) => done = false;

        public void Tick(IChaosActor actor, ChaosContext context, float deltaTime)
        {
            if (done) return;
            done = true;

            if (context.PlayerLookingAtMe) return;

            Vector3 direction = ChaosActions.RandomFlatDirection(actor.Rng);
            Vector3 anchor = actor.PlayerPosition + direction * 4f;

            if (!actor.TryRandomPoint(anchor, profile.blinkRadius * 0.6f, out var point)) return;
            if (Vector3.Distance(point, actor.Position) < 3f) return;

            actor.Teleport(point);
            actor.Say("*шорох за спиной*", 2f);
        }

        public void Exit(IChaosActor actor) { }
    }
}
