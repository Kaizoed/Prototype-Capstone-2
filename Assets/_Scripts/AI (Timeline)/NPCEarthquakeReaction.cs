using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using ShakySurvival.Earthquake;

public class NPCEarthquakeReaction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private Transform evacuationPoint;

    [Header("Animator")]
    [SerializeField] private string earthquakeCrouchParam = "EarthquakeCrouch";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string isFallenParam = "IsFallen";
    [SerializeField] private string helpUpTriggerParam = "HelpUp";
    [SerializeField] private string isPanickingParam = "IsPanicking";

    [Header("Behavior Options")]
    [SerializeField] private bool useIntroFreeze = false;
    [SerializeField] private bool evacuateAfterEarthquake = false;

    [Header("Panic Settings")]
    [Tooltip("Chance (0-1) that this NPC panics instead of crouching during an earthquake")]
    [Range(0f, 1f)]
    [SerializeField] private float panicChance = 0.3f;

    [Tooltip("How far the NPC runs during panic run segments")]
    [SerializeField] private float panicRunRadius = 8f;

    [Tooltip("How long the panic animation plays before the NPC runs (seconds)")]
    [SerializeField] private float panicAnimDuration = 2f;

    [Header("Evacuation")]
    [SerializeField] private float evacuationStartDelay = 0f;
    [SerializeField] private bool willTripAndFall = false;
    [SerializeField] private float fallDelay = 2f;

    [Header("Optional Fall Swap")]
    [SerializeField] private GameObject standingNPCVisual;
    [SerializeField] private GameObject fallenNPCVisual;

    [Header("Quest")]
    [SerializeField] private bool triggerHelpQuestOnFall = false;
    [SerializeField] private string helpQuestStepId = "help_fallen_npc";

    [Header("Optional AI Script To Pause")]
    [SerializeField] private MonoBehaviour aiMovementScript;

    private bool introFinished = false;
    private bool hasFallen = false;
    private bool isPanicking = false;
    private Coroutine panicCoroutine;

    private void Update()
    {
        if (hasFallen) return;
        if (navMeshAgent == null) return;
        if (!evacuateAfterEarthquake) return;

        if (!navMeshAgent.pathPending &&
            navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.05f)
        {
            if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude < 0.01f)
            {
                if (animator != null)
                    animator.SetBool(isRunningParam, false);
            }
        }
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (useIntroFreeze && navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

        if (fallenNPCVisual != null)
            fallenNPCVisual.SetActive(false);

        if (!useIntroFreeze)
            introFinished = true;
    }

    private void OnEnable()
    {
        EarthquakeEvents.OnEarthquakeStart += HandleEarthquakeStart;
        EarthquakeEvents.OnEarthquakeStop += HandleEarthquakeStop;
    }

    private void OnDisable()
    {
        EarthquakeEvents.OnEarthquakeStart -= HandleEarthquakeStart;
        EarthquakeEvents.OnEarthquakeStop -= HandleEarthquakeStop;
    }

    public void EndIntro()
    {
        introFinished = true;
    }

    public bool IsCurrentlyFallen()
    {
        return hasFallen;
    }

    public void HelpUp()
    {
        if (!hasFallen)
            return;

        hasFallen = false;

        if (standingNPCVisual != null)
            standingNPCVisual.SetActive(true);

        if (fallenNPCVisual != null)
            fallenNPCVisual.SetActive(false);

        if (animator != null)
        {
            animator.SetBool(isFallenParam, false);
            animator.ResetTrigger(helpUpTriggerParam);
            animator.SetTrigger(helpUpTriggerParam);
            animator.SetBool(isRunningParam, true);
        }

        if (navMeshAgent != null && evacuationPoint != null)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(evacuationPoint.position);
        }
    }

    private void HandleEarthquakeStart()
    {
        if (!introFinished) return;

        Debug.Log($"[NPCEarthquakeReaction] {gameObject.name} - HandleEarthquakeStart called. panicChance={panicChance}");

        if (aiMovementScript != null)
            aiMovementScript.enabled = false;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

        // Roll for panic vs crouch
        float roll = Random.value;
        if (roll < panicChance)
        {
            // ── Panic path ──
            Debug.Log($"[NPCEarthquakeReaction] {gameObject.name} - PANIC (roll={roll:F2})");
            isPanicking = true;

            if (animator != null)
            {
                animator.SetBool(earthquakeCrouchParam, false);
                animator.SetBool(isRunningParam, false);
                animator.SetBool(isPanickingParam, true);
            }

            panicCoroutine = StartCoroutine(PanicLoopCoroutine());
        }
        else
        {
            // ── Crouch path (existing behavior) ──
            Debug.Log($"[NPCEarthquakeReaction] {gameObject.name} - CROUCH (roll={roll:F2})");
            if (animator != null)
            {
                animator.SetBool(earthquakeCrouchParam, true);
                animator.SetBool(isRunningParam, false);
            }
        }
    }

    private IEnumerator PanicLoopCoroutine()
    {
        // IsPanicking is already true (set by HandleEarthquakeStart).
        // Agent is already stopped.

        while (true)
        {
            // ── Phase 1: Hold panic animation (already playing) ──
            yield return new WaitForSeconds(panicAnimDuration);

            // ── Phase 2: Run to a random point ──
            if (animator != null)
            {
                animator.SetBool(isPanickingParam, false);
                animator.SetBool(isRunningParam, true);
            }

            Vector3 randomPoint = AIMovement.RandomNavSphere(transform.position, panicRunRadius);

            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(randomPoint);
            }

            // Wait until the agent arrives
            if (navMeshAgent != null)
            {
                yield return null; // wait one frame for path to start calculating
                while (navMeshAgent.pathPending ||
                       navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance + 0.1f)
                {
                    yield return null;
                }
            }

            // ── Transition back to panic animation for next loop ──
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.velocity = Vector3.zero;
                navMeshAgent.ResetPath();
            }

            if (animator != null)
            {
                animator.SetBool(isRunningParam, false);
                animator.SetBool(isPanickingParam, true);
            }
        }
    }

    private void StopPanic()
    {
        if (!isPanicking) return;

        isPanicking = false;

        if (panicCoroutine != null)
        {
            StopCoroutine(panicCoroutine);
            panicCoroutine = null;
        }

        if (animator != null)
        {
            animator.SetBool(isPanickingParam, false);
            animator.SetBool(isRunningParam, false);
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }
    }

    private void BeginEvacuationSequence()
    {
        StartEvacuation();

        if (willTripAndFall)
        {
            Invoke(nameof(FallDown), fallDelay);
        }
    }

    private void HandleEarthquakeStop()
    {
        if (!introFinished) return;

        // Immediately cut off panic if active
        StopPanic();

        if (animator != null)
            animator.SetBool(earthquakeCrouchParam, false);

        if (!evacuateAfterEarthquake)
        {
            if (navMeshAgent != null)
                navMeshAgent.isStopped = false;

            if (aiMovementScript != null)
                aiMovementScript.enabled = true;

            return;
        }

        Invoke(nameof(BeginEvacuationSequence), evacuationStartDelay);
    }

    private void StartEvacuation()
    {
        if (navMeshAgent != null && evacuationPoint != null)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(evacuationPoint.position);
        }

        if (animator != null)
            animator.SetBool(isRunningParam, true);
    }

    private void FallDown()
    {
        if (hasFallen) return;
        hasFallen = true;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetBool(isRunningParam, false);
            animator.SetBool(isFallenParam, true);
        }

        if (standingNPCVisual != null)
            standingNPCVisual.SetActive(false);

        if (fallenNPCVisual != null)
            fallenNPCVisual.SetActive(true);

        if (triggerHelpQuestOnFall && QuestManager.Instance != null)
        {
            QuestManager.Instance.ForceSetCurrentStep(helpQuestStepId);
        }
    }
}