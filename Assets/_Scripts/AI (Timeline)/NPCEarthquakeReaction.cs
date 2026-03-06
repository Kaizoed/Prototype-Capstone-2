using UnityEngine;
using UnityEngine.AI;
using ShakySurvival.Earthquake;

public class NPCEarthquakeReaction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Header("Animator")]
    [SerializeField] private string earthquakeCrouchParam = "EarthquakeCrouch";

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();
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

    private void HandleEarthquakeStart()
    {
        // Stop movement only if this NPC has a NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
        }

        // Force crouch animation
        if (animator != null)
        {
            animator.SetBool(earthquakeCrouchParam, true);
        }
    }

    private void HandleEarthquakeStop()
    {
        // Stop crouch animation
        if (animator != null)
        {
            animator.SetBool(earthquakeCrouchParam, false);
        }

        // Resume movement only if this NPC has a NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.isStopped = false;
        }
    }
}