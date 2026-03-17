using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AIMovement : MonoBehaviour
{
    public enum AIState
    {
        Idle,
        Walk,
        Crouch,
        Panic
    }

    public AIState currentState;

    private NavMeshAgent agent;
    private Animator animator;

    public float wanderRadius = 10f;
    public float idleTime = 2f;
    private float idleTimer;

    [Header("Panic Settings")]
    [Tooltip("Chance (0-1) that this NPC panics instead of crouching during an earthquake")]
    [Range(0f, 1f)]
    public float panicChance = 0.3f;

    [Tooltip("How far the NPC runs during panic run segments")]
    public float panicRunRadius = 8f;

    [Tooltip("How long the panic animation plays before the NPC runs (seconds)")]
    public float panicAnimDuration = 2f;

    // Panic sub-state
    private bool isPlayingPanicAnim;
    private float panicTimer;

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

            case AIState.Panic:
                PanicState();
                break;
        }
    }

    void ChangeState(AIState newState)
    {
        // ── Clean up previous state ──
        if (currentState == AIState.Panic && newState != AIState.Panic)
        {
            animator.SetBool("IsPanicking", false);
            animator.SetBool("isWalking", false);
            isPlayingPanicAnim = false;
            panicTimer = 0f;
        }

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

        if (newState == AIState.Panic)
        {
            animator.SetBool("EarthquakeCrouch", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("IsPanicking", true);
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();

            // Start with panic animation sub-phase
            isPlayingPanicAnim = true;
            panicTimer = panicAnimDuration;
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

    void PanicState()
    {
        if (isPlayingPanicAnim)
        {
            // Playing panic animation — count down, then start running
            panicTimer -= Time.deltaTime;
            if (panicTimer <= 0f)
            {
                isPlayingPanicAnim = false;

                // Pick a random point and run to it
                animator.SetBool("IsPanicking", false);
                animator.SetBool("isWalking", true);

                Vector3 randomPoint = RandomNavSphere(transform.position, panicRunRadius);
                agent.isStopped = false;
                agent.SetDestination(randomPoint);
            }
        }
        else
        {
            // Running to random point — wait until arrival, then panic anim again
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                agent.ResetPath();

                animator.SetBool("isWalking", false);
                animator.SetBool("IsPanicking", true);

                isPlayingPanicAnim = true;
                panicTimer = panicAnimDuration;
            }
        }
    }

    public void TriggerCrouch(bool value)
    {
        if (value)
        {
            // Roll for panic vs crouch
            if (Random.value < panicChance)
                ChangeState(AIState.Panic);
            else
                ChangeState(AIState.Crouch);
        }
        else
        {
            // Earthquake ended — immediately cut off whatever state and reset
            ChangeState(AIState.Idle);
        }
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
