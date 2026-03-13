using UnityEngine;
using UnityEngine.AI;
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

    [Header("Behavior Options")]
    [SerializeField] private bool useIntroFreeze = false;
    [SerializeField] private bool evacuateAfterEarthquake = false;

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

        if (aiMovementScript != null)
            aiMovementScript.enabled = false;

        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            navMeshAgent.ResetPath();
        }

        if (animator != null)
        {
            animator.SetBool(earthquakeCrouchParam, true);
            animator.SetBool(isRunningParam, false);
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