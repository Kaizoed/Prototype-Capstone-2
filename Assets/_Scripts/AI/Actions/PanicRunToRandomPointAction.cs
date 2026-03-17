using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace ShakySurvival.AI
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Panic Run To Random Point",
        description: "Picks a random NavMesh point within a radius and moves the agent to it with a brief random pause.",
        story: "[Agent] panics and runs to a random point within [Radius] meters",
        category: "Action/Earthquake",
        id: "ea1c0002000000000000000000000001")]
    public partial class PanicRunToRandomPointAction : Action
    {
        // ── Blackboard Variables ─────────────────────────────────
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<float> Radius = new BlackboardVariable<float>(5f);

        // ── Internal State ───────────────────────────────────────
        private NavMeshAgent m_NavAgent;
        private float m_WaitTimer;
        private bool  m_IsWaiting;

        private const float k_SampleMaxDistance = 10f;

        protected override Status OnStart()
        {
            if (Agent.Value == null)
                return Status.Failure;

            m_NavAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            if (m_NavAgent == null || !m_NavAgent.isOnNavMesh)
                return Status.Failure;

            if (!TryGetRandomNavMeshPoint(out Vector3 destination))
                return Status.Failure;

            m_NavAgent.isStopped = false;
            m_NavAgent.speed = 5f;
            m_NavAgent.SetDestination(destination);
            m_IsWaiting = false;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (m_NavAgent == null)
                return Status.Failure;

            // Phase 1 — Moving to the random point.
            if (!m_IsWaiting)
            {
                if (!m_NavAgent.pathPending &&
                    m_NavAgent.remainingDistance <= m_NavAgent.stoppingDistance + 0.1f)
                {
                    m_WaitTimer = UnityEngine.Random.Range(0.2f, 0.5f);
                    m_IsWaiting = true;
                }

                return Status.Running;
            }

            // Phase 2 — Waiting at the point.
            m_WaitTimer -= Time.deltaTime;
            if (m_WaitTimer <= 0f)
                return Status.Success;

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
            {
                m_NavAgent.ResetPath();
            }
        }

        private bool TryGetRandomNavMeshPoint(out Vector3 result)
        {
            Vector3 origin = Agent.Value.transform.position;

            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * Radius.Value;
            Vector3 randomPos = origin + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPos, out NavMeshHit hit, k_SampleMaxDistance, NavMesh.AllAreas))
            {
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
