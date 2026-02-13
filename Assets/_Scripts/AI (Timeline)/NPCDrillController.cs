using UnityEngine;
using UnityEngine.AI;

public class NPCDrillController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Transform coverPoint; // assign in inspector

    private Vector3 originalScale;
    public void SetCoverPoint(Transform cp)
    {
        coverPoint = cp;
    }
    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        originalScale = transform.localScale;
    }

    // Call this when earthquake starts
    public void GoToCover()
    {
        if (coverPoint == null) return;

        agent.isStopped = false;
        agent.SetDestination(coverPoint.position);
    }

    public void Duck()
    {
        // Fake crouch without animations
        transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.6f, originalScale.z);
    }

    public void Hold()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    public void Stand()
    {
        transform.localScale = originalScale;
        agent.isStopped = false;
    }
}
