using UnityEngine;
using UnityEngine.AI;

public enum NavState { Patrol, Chase, Return }

[RequireComponent(typeof(NavMeshAgent))]
public class RovingNPC : MonoBehaviour
{
    [Header("Wander")]
    public float wanderRadius = 10f;
    public float wanderTimer = 10f;

    [Header("Chase")]
    public float chaseRadius = 6f;
    public float loseTime = 3f;

    private NavMeshAgent agent;
    private Animator animator;
    private NPCController controller;
    private Transform playerTransform;   // ← добавили

    private float timer;
    private float loseTimer;
    private NavState navState = NavState.Patrol;
    private bool isWalking;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<NPCController>();
        timer = 0f;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
        else
            Debug.LogError($"{name}: игрок с тегом 'Player' не найден!");

        if (!agent.isOnNavMesh)
            Debug.LogWarning($"{name}: NavMeshAgent не на NavMesh!");
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        UpdateNavState();
        HandleNavigation();
        UpdateAnimation();

        Debug.Log($"[{name}] navState={navState}, " +
                  $"isInRadius={controller.isInRadius}, " +
                  $"isQuestAccepted={controller.isQuestAccepted}, " +
                  $"timer={timer:F2}/{wanderTimer}, " +
                  $"loseTimer={loseTimer:F2}, " +
                  $"hasPath={agent.hasPath}, " +
                  $"remaining={agent.remainingDistance:F2}");
    }

    private void UpdateNavState()
    {
        if (controller == null) return;

        switch (navState)
        {
            case NavState.Patrol:
                if (PlayerInChaseRadius())
                {
                    navState = NavState.Chase;
                    loseTimer = 0f;
                    Debug.Log($"[{name}] → Chase");
                }
                break;

            case NavState.Chase:
                if (!PlayerInChaseRadius())
                {
                    loseTimer += Time.deltaTime;
                    if (loseTimer >= loseTime)
                    {
                        navState = NavState.Return;
                        Debug.Log($"[{name}] → Return");
                    }
                }
                else loseTimer = 0f;
                break;

            case NavState.Return:
                if (AgentReachedDestination())
                {
                    navState = NavState.Patrol;
                    Debug.Log($"[{name}] → Patrol");
                }
                break;
        }
    }

    private void HandleNavigation()
    {
        switch (navState)
        {
            case NavState.Patrol:
                if (controller != null && controller.isQuestAccepted)
                {
                    if (agent.hasPath) agent.ResetPath();
                    return;
                }

                timer += Time.deltaTime;
                bool reached = agent.hasPath && AgentReachedDestination();
                if (timer >= wanderTimer || reached)
                {
                    Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                    if (Vector3.Distance(newPos, transform.position) < 1f)
                    {
                        timer = wanderTimer;
                        return;
                    }
                    agent.SetDestination(newPos);
                    timer = 0;
                }
                break;

            case NavState.Chase:
                if (playerTransform != null)
                    agent.SetDestination(playerTransform.position);
                break;

            case NavState.Return:
                if (!agent.hasPath || AgentReachedDestination())
                {
                    Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                    agent.SetDestination(newPos);
                }
                break;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;
        isWalking = !agent.pathPending &&
                    agent.remainingDistance > agent.stoppingDistance &&
                    agent.velocity.magnitude > 0.01f;
        animator.SetBool("isWalking", isWalking);
    }

    private bool PlayerInChaseRadius()
    {
        if (playerTransform == null) return false;
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        return dist <= chaseRadius;
    }

    private bool AgentReachedDestination()
    {
        if (agent.pathPending) return false;
        if (!agent.hasPath) return true;
        if (agent.remainingDistance > agent.stoppingDistance) return false;
        return true;
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist + origin;
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(randDirection, out navHit, dist, layermask))
            return navHit.position;
        return origin;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRadius);
    }
}