using UnityEngine;
using UnityEngine.AI;

public class RovingNPC : MonoBehaviour
{
    public float wanderRadius = 10f; // Радиус поиска новой точки
    public float wanderTimer = 10f;  // Время ожидания перед сменой точки

    private NavMeshAgent agent;    
    private float timer;

    Animator animator;
    NPCController controller;

    bool isWalking=false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<NPCController>();
        timer = wanderTimer;
    }

    void Update()
    {
        if (controller.isInRadius == false && controller.isQuestAccepted == false)
        {
            timer += Time.deltaTime;

            if (timer >= wanderTimer)
            {
            
                Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
                agent.SetDestination(newPos);
                timer = 0;
            }            
        }
        isWalking = agent.remainingDistance > agent.stoppingDistance && agent.velocity.magnitude > 0.01f;
        AnimationController();
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);
        return navHit.position;
    }

    public void AnimationController()
    {
        if (isWalking)
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }
}
