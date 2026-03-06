using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AIMovement : MonoBehaviour
{
 public enum AIState
    {
        Idle,
        Walk,
        Crouch
    }

    public AIState currentState;

    private NavMeshAgent agent;
    private Animator animator;

    public float wanderRadius = 10f;
    public float idleTime = 2f;
    private float idleTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        ChangeState(AIState.Idle);
    }

    void Update()
    {
        switch (currentState)
        {
            case AIState.Idle:
                IdleState();
                break;

            case AIState.Walk:
                WalkState();
                break;

            case AIState.Crouch:
                CrouchState();
                break;
        }
    }

    void ChangeState(AIState newState)
    {
        currentState = newState;

        if (newState == AIState.Idle)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("EarthquakeCrouch", false);
            agent.isStopped = true;
            idleTimer = idleTime;
        }

        if (newState == AIState.Walk)
        {
            animator.SetBool("isWalking", true);
            animator.SetBool("EarthquakeCrouch", false);
            agent.isStopped = false;

            Vector3 randomPoint = RandomNavSphere(transform.position, wanderRadius);
            agent.SetDestination(randomPoint);
        }

        if (newState == AIState.Crouch)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("EarthquakeCrouch", true);
            agent.isStopped = true;
        }
    }

    void IdleState()
    {
        idleTimer -= Time.deltaTime;

        if (idleTimer <= 0)
        {
            ChangeState(AIState.Walk);
        }
    }

    void WalkState()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            ChangeState(AIState.Idle);
        }
    }

    void CrouchState()
    {
        // stays crouched until trigger exits
    }

    public void TriggerCrouch(bool value)
    {
        if (value)
            ChangeState(AIState.Crouch);
        else
            ChangeState(AIState.Idle);
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float distance)
    {
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randomDirection, out navHit, distance, NavMesh.AllAreas);

        return navHit.position;
    }
}
