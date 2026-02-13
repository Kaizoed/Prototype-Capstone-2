using UnityEngine;
using UnityEngine.AI;

public class NPCTimelineReceiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;

    [Header("Optional Visuals")]
    [SerializeField] private Transform crouchVisual; // optional (can be null)
    [SerializeField] private float crouchScaleY = 0.5f;

    private Vector3 originalScale;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (crouchVisual == null) crouchVisual = transform;

        originalScale = crouchVisual.localScale;
    }

    // TIMELINE SIGNAL: set a target point (cover)
    public void GoToCover(Transform coverPoint)
    {
        if (agent == null || coverPoint == null) return;

        agent.isStopped = false;
        agent.SetDestination(coverPoint.position);
    }

    // TIMELINE SIGNAL: stop moving (hold position)
    public void Hold()
    {
        if (agent == null) return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    // TIMELINE SIGNAL: crouch (no animation needed)
    public void Duck()
    {
        // simple fake crouch by scaling down
        Vector3 s = originalScale;
        s.y *= crouchScaleY;
        crouchVisual.localScale = s;
    }

    // TIMELINE SIGNAL: stand up
    public void Stand()
    {
        crouchVisual.localScale = originalScale;
    }
}
