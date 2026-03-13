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

    [Header("Behavior Options")]
    [SerializeField] private bool useIntroFreeze = false;
    [SerializeField] private bool evacuateAfterEarthquake = false;

    [Header("Evacuation")]
    [SerializeField] private bool willTripAndFall = false;
    [SerializeField] private float fallDelay = 2f;

    [Header("Optional Fall Swap")]
    [SerializeField] private GameObject standingNPCVisual;
    [SerializeField] private GameObject fallenNPCVisual;

    private bool introFinished = false;
    private bool hasFallen = false;

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

    private void HandleEarthquakeStart()
    {
        if (!introFinished) return;

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

    private void HandleEarthquakeStop()
    {
        if (!introFinished) return;

        if (animator != null)
            animator.SetBool(earthquakeCrouchParam, false);

        if (!evacuateAfterEarthquake)
        {
            if (navMeshAgent != null)
                navMeshAgent.isStopped = false;

            return;
        }

        if (willTripAndFall)
        {
            if (animator != null)
                animator.SetBool(isRunningParam, true);

            Invoke(nameof(FallDown), fallDelay);
        }
        else
        {
            StartEvacuation();
        }
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
    }
}