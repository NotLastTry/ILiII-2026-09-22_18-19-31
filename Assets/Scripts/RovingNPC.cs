using System.Collections.Generic;
using System.Text;
using LabGame;
using LabGame.AI;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Противник с системой поиска персонажа (ПЗ №5).
///
/// Поведение описано деревом: каждый кадр дерево опрашивается заново,
/// поэтому противник мгновенно реагирует на изменения обстановки.
///
///   Корень (Селектор)
///   ├── Бой
///   │    ├── Цель в зоне удара
///   │    └── Атака или ожидание
///   │         ├── Удар  (замах → пауза → урон → восстановление)
///   │         └── Держать дистанцию
///   ├── Преследование
///   │    ├── Игрок виден
///   │    ├── Запомнить позицию
///   │    └── Идти к игроку
///   ├── Поиск
///   │    ├── Есть последняя позиция
///   │    └── Обыскать точку
///   ├── Возвращение
///   │    ├── Далеко от дома
///   │    └── Вернуться к точке
///   └── Патрулирование
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RovingNPC : MonoBehaviour
{
    /// <summary>Первый созданный противник — используется HUD для отладки дерева.</summary>
    public static RovingNPC Primary { get; private set; }

    [Header("Обзор")]
    [Tooltip("Максимальная дальность видимости.")]
    public float viewDistance = 16f;

    [Range(10f, 360f)]
    [Tooltip("Полный угол обзора в градусах.")]
    public float viewAngle = 110f;

    [Tooltip("Вблизи противник слышит шаги даже за препятствием.")]
    public float hearDistance = 6f;

    [Tooltip("С какой скорости шаги считаются слышимыми.")]
    public float hearSpeedThreshold = 1.5f;

    [Header("Бой")]
    public float attackRange = 2.2f;
    public float prepareDuration = 0.6f;
    public float recoverDuration = 0.8f;
    public float attackCooldown = 1.6f;
    public float damage = 8f;

    [Header("Память и поиск")]
    [Tooltip("Сколько секунд противник помнит, где видел игрока.")]
    public float memoryDuration = 6f;

    [Tooltip("Сколько секунд осматривается в последней точке.")]
    public float searchDuration = 4f;

    [Header("Патрулирование")]
    public int patrolPointCount = 4;
    public float patrolRadius = 9f;
    public float patrolPause = 1.5f;

    [Tooltip("Необязательные точки патруля. Если пусто — строятся вокруг старта.")]
    public Transform[] patrolPoints;

    [Header("Движение")]
    public float walkSpeed = 2.4f;
    public float chaseSpeed = 4.4f;

    public EnemyState State => context != null ? context.State : EnemyState.OutOfSight;

    public bool CanSeePlayer => context != null && context.CanSeePlayer;

    public float DistanceToPlayer => context != null ? context.DistanceToPlayer : 0f;

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyContext context;
    private BehaviourNode root;

    private float patrolPauseTimer;
    private float searchTimer;

    private Vector3 lastPlayerPosition;
    private float playerSpeed;

    private bool hasWalkParameter;
    private readonly HashSet<string> triggers = new HashSet<string>();

    // ─────────────────────────────────────────────────────────────
    //  Жизненный цикл
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        Primary = this;

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        CacheAnimatorParameters();

        var player = GameObject.FindWithTag("Player");

        context = new EnemyContext
        {
            Owner = this,
            Self = transform,
            Player = player != null ? player.transform : null,
            Agent = agent,
            HomePosition = transform.position,
            LastKnownPosition = transform.position
        };

        if (player != null) lastPlayerPosition = player.transform.position;

        BuildPatrolPoints();
        root = BuildTree();
    }

    private void OnDestroy()
    {
        if (Primary == this) Primary = null;
    }

    private void Update()
    {
        if (context == null || context.Player == null || root == null) return;

        UpdateSenses();

        context.AttackCooldown = Mathf.Max(0f, context.AttackCooldown - Time.deltaTime);

        root.Tick(context);

        UpdateAnimator();
    }

    // ─────────────────────────────────────────────────────────────
    //  Дерево поведения
    // ─────────────────────────────────────────────────────────────

    private BehaviourNode BuildTree()
    {
        var rootSelector = new SelectorNode("Корень");

        // 1. Бой — высший приоритет, если игрок в зоне удара.
        var combat = new SequenceNode("Бой");
        combat.Add(new ConditionNode("Цель в зоне удара",
            ctx => ctx.CanSeePlayer && ctx.DistanceToPlayer <= attackRange));

        var attackChoice = new SelectorNode("Атака или ожидание");

        var strike = new SequenceNode("Удар");
        strike.Add(new ConditionNode("Атака готова", ctx => ctx.AttackCooldown <= 0f));
        strike.Add(new ActionNode("Замах", BeginPrepareAttack));
        strike.Add(new WaitNode("Пауза замаха", ctx => prepareDuration));
        strike.Add(new ActionNode("Нанести урон", PerformAttack));
        strike.Add(new WaitNode("Восстановление", ctx => recoverDuration));

        attackChoice.Add(strike);
        attackChoice.Add(new ActionNode("Держать дистанцию", HoldInCombat));

        combat.Add(attackChoice);

        // 2. Преследование.
        var chase = new SequenceNode("Преследование");
        chase.Add(new ConditionNode("Игрок виден", ctx => ctx.CanSeePlayer));
        chase.Add(new ActionNode("Запомнить позицию", RememberPlayer));
        chase.Add(new ActionNode("Идти к игроку", ChasePlayer));

        // 3. Поиск в последней известной точке.
        var search = new SequenceNode("Поиск");
        search.Add(new ConditionNode("Есть последняя позиция", ctx => ctx.HasLastKnown));
        search.Add(new ActionNode("Обыскать точку", SearchLastKnown));

        // 4. Возвращение в промежуточную или начальную точку.
        var returnHome = new SequenceNode("Возвращение");
        returnHome.Add(new ConditionNode("Далеко от дома", IsFarFromHome));
        returnHome.Add(new ActionNode("Вернуться к точке", ReturnToPoint));

        // 5. Патрулирование (дополнительное задание).
        var patrol = new ActionNode("Патрулирование", Patrol);

        rootSelector.Add(combat);
        rootSelector.Add(chase);
        rootSelector.Add(search);
        rootSelector.Add(returnHome);
        rootSelector.Add(patrol);

        return rootSelector;
    }

    // ─────────────────────────────────────────────────────────────
    //  Восприятие
    // ─────────────────────────────────────────────────────────────

    private void UpdateSenses()
    {
        var player = context.Player;

        float instantSpeed = Vector3.Distance(player.position, lastPlayerPosition) / Mathf.Max(0.0001f, Time.deltaTime);
        playerSpeed = Mathf.Lerp(playerSpeed, instantSpeed, 0.35f);
        lastPlayerPosition = player.position;

        context.DistanceToPlayer = Vector3.Distance(transform.position, player.position);

        bool visible = context.DistanceToPlayer <= viewDistance &&
                       IsInViewAngle(player) &&
                       HasLineOfSight(player);

        context.CanSeePlayer = visible;

        if (visible)
        {
            RememberPlayerPosition(player.position);
            return;
        }

        context.TimeSinceSeen += Time.deltaTime;

        // Слух: бегущего игрока слышно даже за препятствием.
        if (!context.HasLastKnown &&
            context.DistanceToPlayer <= hearDistance &&
            playerSpeed >= hearSpeedThreshold)
        {
            RememberPlayerPosition(player.position);
            return;
        }

        if (context.HasLastKnown && context.TimeSinceSeen > memoryDuration)
            context.HasLastKnown = false;
    }

    private void RememberPlayerPosition(Vector3 position)
    {
        context.LastKnownPosition = position;
        context.HasLastKnown = true;
        context.TimeSinceSeen = 0f;
        searchTimer = 0f;
    }

    private bool IsInViewAngle(Transform player)
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.01f) return true;

        return Vector3.Angle(transform.forward, toPlayer) <= viewAngle * 0.5f;
    }

    private bool HasLineOfSight(Transform player)
    {
        Vector3 eye = transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up * 1.0f;
        Vector3 direction = target - eye;
        float distance = direction.magnitude;

        if (distance < 0.01f) return true;

        if (Physics.Raycast(eye, direction / distance, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider.transform.IsChildOf(player);

        return true;
    }

    // ─────────────────────────────────────────────────────────────
    //  Действия дерева
    // ─────────────────────────────────────────────────────────────

    private BehaviourStatus BeginPrepareAttack(EnemyContext ctx)
    {
        ctx.State = EnemyState.PrepareAttack;

        StopMoving();
        FacePlayer();
        TryTrigger("PrepareAttack");

        return BehaviourStatus.Success;
    }

    private BehaviourStatus PerformAttack(EnemyContext ctx)
    {
        ctx.State = EnemyState.Attack;

        ctx.AttackCooldown = attackCooldown;
        FacePlayer();
        TryTrigger("Attack");

        if (ctx.Player != null)
        {
            var stats = ctx.Player.GetComponent<CharacterStats>();
            if (stats != null) stats.ApplyDamage(damage);

            // Драка — событие для толпы: находящиеся рядом NPC пугаются.
            GameEventBus.RaiseDisturbance(ctx.Player.position);
        }

        return BehaviourStatus.Success;
    }

    private BehaviourStatus HoldInCombat(EnemyContext ctx)
    {
        ctx.State = EnemyState.PrepareAttack;

        StopMoving();
        FacePlayer();

        return BehaviourStatus.Success;
    }

    private BehaviourStatus RememberPlayer(EnemyContext ctx)
    {
        RememberPlayerPosition(ctx.Player.position);
        return BehaviourStatus.Success;
    }

    private BehaviourStatus ChasePlayer(EnemyContext ctx)
    {
        ctx.State = EnemyState.Chase;
        MoveTo(ctx.Player.position, chaseSpeed);
        return BehaviourStatus.Success;
    }

    private BehaviourStatus SearchLastKnown(EnemyContext ctx)
    {
        ctx.State = EnemyState.Search;

        Vector3 target = ctx.LastKnownPosition;

        if (Vector3.Distance(transform.position, target) > 1.2f)
        {
            searchTimer = 0f;
            MoveTo(target, chaseSpeed);
            return BehaviourStatus.Success;
        }

        // Дошёл — осматривается на месте.
        StopMoving();
        transform.Rotate(Vector3.up, 90f * Time.deltaTime);

        searchTimer += Time.deltaTime;

        if (searchTimer >= searchDuration)
        {
            searchTimer = 0f;
            ctx.HasLastKnown = false;
        }

        return BehaviourStatus.Success;
    }

    private BehaviourStatus ReturnToPoint(EnemyContext ctx)
    {
        ctx.State = EnemyState.Return;

        Vector3 target = CurrentPatrolPoint(ctx);

        if (Vector3.Distance(transform.position, target) <= 1.2f)
        {
            ctx.HasLastKnown = false;
            return BehaviourStatus.Success;
        }

        MoveTo(target, walkSpeed);
        return BehaviourStatus.Success;
    }

    private BehaviourStatus Patrol(EnemyContext ctx)
    {
        // Точек нет — стоим на месте и осматриваемся.
        if (ctx.PatrolPoints.Count == 0)
        {
            ctx.State = EnemyState.OutOfSight;
            StopMoving();
            transform.Rotate(Vector3.up, 20f * Time.deltaTime);
            return BehaviourStatus.Success;
        }

        if (patrolPauseTimer > 0f)
        {
            ctx.State = EnemyState.OutOfSight;
            patrolPauseTimer -= Time.deltaTime;
            StopMoving();
            transform.Rotate(Vector3.up, 45f * Time.deltaTime);
            return BehaviourStatus.Success;
        }

        ctx.State = EnemyState.Patrol;

        Vector3 target = CurrentPatrolPoint(ctx);

        if (Vector3.Distance(transform.position, target) <= 1.0f)
        {
            ctx.PatrolIndex = (ctx.PatrolIndex + 1) % ctx.PatrolPoints.Count;
            patrolPauseTimer = patrolPause;
            return BehaviourStatus.Success;
        }

        MoveTo(target, walkSpeed);
        return BehaviourStatus.Success;
    }

    private bool IsFarFromHome(EnemyContext ctx)
    {
        return Vector3.Distance(transform.position, ctx.HomePosition) > patrolRadius + 3f;
    }

    private Vector3 CurrentPatrolPoint(EnemyContext ctx)
    {
        if (ctx.PatrolPoints.Count == 0) return ctx.HomePosition;

        int index = Mathf.Clamp(ctx.PatrolIndex, 0, ctx.PatrolPoints.Count - 1);
        return ctx.PatrolPoints[index];
    }

    // ─────────────────────────────────────────────────────────────
    //  Движение
    // ─────────────────────────────────────────────────────────────

    private void MoveTo(Vector3 target, float speed)
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.speed = speed;
        agent.isStopped = false;
        agent.SetDestination(target);
    }

    private void StopMoving()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = true;
        if (agent.hasPath) agent.ResetPath();
    }

    private void FacePlayer()
    {
        if (context.Player == null) return;

        Vector3 direction = context.Player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 10f);
    }

    private void BuildPatrolPoints()
    {
        context.PatrolPoints.Clear();

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
                if (patrolPoints[i] != null)
                    context.PatrolPoints.Add(patrolPoints[i].position);

            return;
        }

        int count = Mathf.Max(0, patrolPointCount);

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 candidate = context.HomePosition +
                                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * patrolRadius;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                context.PatrolPoints.Add(hit.position);
        }

        if (context.PatrolPoints.Count == 0)
            Debug.LogWarning($"{name}: не удалось построить точки патруля (нет NavMesh?)");
    }

    // ─────────────────────────────────────────────────────────────
    //  Анимация и отладка
    // ─────────────────────────────────────────────────────────────

    private void CacheAnimatorParameters()
    {
        triggers.Clear();

        if (animator == null) return;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger)
                triggers.Add(parameter.name);
            else if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == "isWalking")
                hasWalkParameter = true;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null || !hasWalkParameter) return;

        bool walking = agent != null && agent.velocity.sqrMagnitude > 0.04f;
        animator.SetBool("isWalking", walking);
    }

    private void TryTrigger(string triggerName)
    {
        if (animator == null || !triggers.Contains(triggerName)) return;

        animator.SetTrigger(triggerName);
    }

    /// <summary>Текстовая версия дерева с текущими статусами узлов.</summary>
    public string BuildTreeDebug()
    {
        if (root == null) return "дерево не построено";

        var builder = new StringBuilder(1024);
        root.Describe(builder, 0);
        return builder.ToString();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, hearDistance);
    }
}
