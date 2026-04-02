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
    [Range(0f, 1f)]
    [SerializeField] private float panicChance = 0.3f;
    [SerializeField] private float panicRunRadius = 8f;
    [SerializeField] private float panicAnimDuration = 2f;

    [Header("Evacuation")]
    [SerializeField] private float evacuationStartDelay = 0f;
    [SerializeField] private bool willTripAndFall = false;
    [SerializeField] private float fallDelay = 2f;

    [Header("Optional Multi-Step Evacuation")]
    [SerializeField] private bool useIntermediateEvacuationPoint = false;
    [SerializeField] private Transform intermediateEvacuationPoint;

    [Header("Optional Fall Swap")]
    [SerializeField] private GameObject standingNPCVisual;
    [SerializeField] private GameObject fallenNPCVisual;

    [Header("Optional AI Script To Pause")]
    [SerializeField] private MonoBehaviour aiMovementScript;

    private bool introFinished = false;
    private bool hasFallen = false;
    private bool isPanicking = false;
    private bool goingToFinalEvacuationPoint = false;
    private bool evacuationStarted = false;
    private Coroutine panicCoroutine;

    private void Update()
    {
        if (hasFallen) return;
        if (navMeshAgent == null) return;
        if (isGuard) return;
        if (!evacuationStarted) return;

        if (!navMeshAgent.pathPending &&
            navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance + 0.05f)
        {
            if (navMeshAgent.velocity.sqrMagnitude < 0.01f)
            {
                if (useIntermediateEvacuationPoint &&
                    !goingToFinalEvacuationPoint &&
                    evacuationPoint != null)
                {
                    goingToFinalEvacuationPoint = true;
                    navMeshAgent.isStopped = false;
                    navMeshAgent.ResetPath();
                    navMeshAgent.SetDestination(evacuationPoint.position);
                    return;
                }

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
        evacuationStarted = true;
        goingToFinalEvacuationPoint = false;

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

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = false;
            navMeshAgent.ResetPath();

            if (useIntermediateEvacuationPoint && intermediateEvacuationPoint != null)
                navMeshAgent.SetDestination(intermediateEvacuationPoint.position);
            else if (evacuationPoint != null)
            {
                goingToFinalEvacuationPoint = true;
                navMeshAgent.SetDestination(evacuationPoint.position);
            }
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

        if (aiMovementScript != null)
            aiMovementScript.enabled = false;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

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
        if (navMeshAgent == null) return;

        evacuationStarted = true;
        goingToFinalEvacuationPoint = false;

        navMeshAgent.isStopped = false;
        navMeshAgent.ResetPath();

        if (useIntermediateEvacuationPoint && intermediateEvacuationPoint != null)
        {
            navMeshAgent.SetDestination(intermediateEvacuationPoint.position);
        }
        else if (evacuationPoint != null)
        {
            goingToFinalEvacuationPoint = true;
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
        evacuationStarted = false;

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
        evacuationStarted = false;

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