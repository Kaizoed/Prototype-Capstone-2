using UnityEngine;
using UnityEngine.AI;

public class ActionMoveToCover : GOAPAction
{
    private NavMeshAgent agent;
    private Transform targetCover;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override bool CanRun()
    {
        return WorldState.Instance.earthquakeActive &&
               !WorldState.Instance.npcIsUnderCover;
    }

    public override void Run()
    {
        if (targetCover == null)
            targetCover = CoverManager.Instance.GetNearestCover(transform.position);

        if (targetCover != null)
            agent.SetDestination(targetCover.position);
    }

    public override bool IsComplete()
    {
        if (targetCover == null) return true;

        float dist = Vector3.Distance(transform.position, targetCover.position);
        return dist < 0.6f;
    }

    public override void Stop()
    {
        agent.ResetPath();
        WorldState.Instance.npcIsUnderCover = true;
    }
}
