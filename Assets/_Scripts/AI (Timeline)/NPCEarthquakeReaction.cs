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

    [Header("Role")]
    [SerializeField] private bool isGuard = false;
    [SerializeField] private bool allowEarthquakeReaction = true;

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
        if (isGuard) return; // Guard should not use this auto-evacuation flow

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

    public void SetEarthquakeReaction(bool value)
    {
        allowEarthquakeReaction = value;
    }

    private void HandleEarthquakeStart()
    {
        if (!introFinished) return;
        if (!allowEarthquakeReaction) return;

        Debug.Log($"[NPCEarthquakeReaction] {gameObject.name} - HandleEarthquakeStart called. panicChance={panicChance}");

        if (aiMovementScript != null)
            aiMovementScript.enabled = false;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

        // Guard should not do random panic/crouch if you plan to control it later
        if (isGuard)
        {
            if (animator != null)
            {
                animator.SetBool(earthquakeCrouchParam, false);
                animator.SetBool(isRunningParam, false);
                animator.SetBool(isPanickingParam, false);
            }
            return;
        }

        float roll = Random.value;
        if (roll < panicChance)
        {
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
            Debug.Log($"[NPCEarthquakeReaction] {gameObject.name} - CROUCH (roll={roll:F2})");
            if (animator != null)
            {
                animator.SetBool(earthquakeCrouchParam, true);
                animator.SetBool(isRunningParam, false);
                animator.SetBool(isPanickingParam, false);
            }
        }
    }

    private IEnumerator PanicLoopCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(panicAnimDuration);

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

            if (navMeshAgent != null)
            {
                yield return null;
                while (navMeshAgent.pathPending ||
                       navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance + 0.1f)
                {
                    yield return null;
                }
            }

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
        if (!allowEarthquakeReaction) return;

        StopPanic();

        if (animator != null)
            animator.SetBool(earthquakeCrouchParam, false);

        // Guard should not auto-resume or auto-evacuate through this script
        if (isGuard)
            return;

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
        if (isGuard) return;

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
        if (isGuard) return;

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
    }

    public void FreezeForCutscene()
    {
        StopPanic();

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetBool(isRunningParam, false);
            animator.SetBool(isPanickingParam, false);
            animator.SetBool(earthquakeCrouchParam, false);
        }

        if (aiMovementScript != null)
        {
            aiMovementScript.enabled = false;
        }
    }

    public void ResumeAfterCutscene()
    {
        if (aiMovementScript != null)
        {
            aiMovementScript.enabled = true;
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = false;
        }
    }

    public void StartManualEvacuation()
    {
        if (isGuard) return;
        StartEvacuation();
    }
}