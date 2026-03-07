using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Behavior Graph Action — casual idle wandering.
    ///
    /// Picks a random walkable NavMesh point within <see cref="Radius"/> meters,
    /// walks there at a relaxed speed, waits 2–5 seconds, then returns Success
    /// so the Repeat decorator re-triggers a new wander.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Idle Roam",
        description: "Casually walks to a random nearby point, pauses, then repeats.",
        story: "[Agent] roams within [Radius] meters",
        category: "Action/Earthquake",
        id: "ea1c0006000000000000000000000001")]
    public partial class IdleRoamAction : Action
    {
        // ── Blackboard Variables ─────────────────────────────────
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<float> Radius = new BlackboardVariable<float>(4f);

        // ── Tuning ──────────────────────────────────────────────
        private const float k_RoamSpeed        = 1.2f;   // slow, casual walk
        private const float k_MinWait          = 2f;
        private const float k_MaxWait          = 5f;
        private const float k_SampleMaxDist    = 10f;
        private const float k_ArrivalTolerance = 0.3f;

        // ── Runtime ─────────────────────────────────────────────
        private NavMeshAgent m_NavAgent;
        private float m_WaitTimer;
        private bool  m_IsWaiting;

        // ─────────────────────────────────────────────────────────
        protected override Status OnStart()
        {
            if (Agent == null || Agent.Value == null)
                return Status.Failure;

            m_NavAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            if (m_NavAgent == null || !m_NavAgent.isOnNavMesh)
                return Status.Failure;

            // Re-enable NavMeshAgent in case a previous cover entry disabled it.
            m_NavAgent.updatePosition = true;
            m_NavAgent.updateRotation = true;
            m_NavAgent.isStopped = false;
            m_NavAgent.speed = k_RoamSpeed;

            // Pick a random walkable destination.
            if (!TryGetRandomNavMeshPoint(out Vector3 dest))
                return Status.Failure;

            m_NavAgent.SetDestination(dest);
            m_IsWaiting = false;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (m_NavAgent == null)
                return Status.Failure;

            if (!m_IsWaiting)
            {
                if (!m_NavAgent.pathPending &&
                    m_NavAgent.hasPath &&
                    m_NavAgent.remainingDistance <= m_NavAgent.stoppingDistance + k_ArrivalTolerance)
                {
                    // Arrived — pause for a few seconds.
                    m_WaitTimer = UnityEngine.Random.Range(k_MinWait, k_MaxWait);
                    m_IsWaiting = true;
                }
                return Status.Running;
            }

            // Waiting at the point.
            m_WaitTimer -= Time.deltaTime;
            if (m_WaitTimer <= 0f)
                return Status.Success; // Repeat will re-trigger a new wander.

            return Status.Running;
        }

        protected override void OnEnd()
        {
            // Only reset path on normal completion, not tree re-evaluation.
            if (CurrentStatus == Status.Success || CurrentStatus == Status.Failure)
            {
                if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
                    m_NavAgent.ResetPath();
            }
        }

        // ── Helpers ──────────────────────────────────────────────
        private bool TryGetRandomNavMeshPoint(out Vector3 result)
        {
            Vector3 origin = Agent.Value.transform.position;

            // Use a flat circle (XZ only) so we never pick points on other floors.
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * Radius.Value;
            Vector3 randomPos = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, k_SampleMaxDist, NavMesh.AllAreas))
            {
                // Reject if the sampled point is on a different floor.
                if (Mathf.Abs(hit.position.y - origin.y) > 1.5f)
                {
                    result = Vector3.zero;
                    return false;
                }
                result = hit.position;
                return true;
            }

            result = Vector3.zero;
            return false;
        }
    }
}
