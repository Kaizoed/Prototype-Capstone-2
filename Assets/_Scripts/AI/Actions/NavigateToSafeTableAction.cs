using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace ShakySurvival.AI
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Navigate To Safe Table",
        description: "Moves the agent to the target CoverPoint, monitoring pathStatus for dynamic obstacles. Blacklists the table and fails on partial/invalid paths.",
        story: "[Agent] navigates to [TargetCoverPoint] under [TargetTable]",
        category: "Action/Earthquake",
        id: "ea1c0004000000000000000000000001")]
    public partial class NavigateToSafeTableAction : Action
    {
        // ── Blackboard Variables ─────────────────────────────────
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<Transform>  TargetCoverPoint;
        [SerializeReference] public BlackboardVariable<GameObject> TargetTable;
        [SerializeReference] public BlackboardVariable<List<GameObject>> BlacklistedTables;

        // ── Internal State ───────────────────────────────────────
        private NavMeshAgent m_NavAgent;
        private Transform    m_TargetTransform;   // live reference to the EntryPoint
        private Vector3      m_LastSetDestination; // avoids re-setting every frame

        private const float k_ArrivalTolerance   = 0.15f;
        private const float k_RetargetThreshold  = 0.3f;

        protected override Status OnStart()
        {
            if (Agent == null || Agent.Value == null || TargetCoverPoint == null || TargetCoverPoint.Value == null)
            {
                LogFailure("Agent or TargetCoverPoint is null.");
                return Status.Failure;
            }

            m_NavAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            if (m_NavAgent == null || !m_NavAgent.isOnNavMesh)
            {
                LogFailure("NavMeshAgent missing or not on NavMesh.");
                return Status.Failure;
            }

            m_TargetTransform = TargetCoverPoint.Value;
            m_LastSetDestination = m_TargetTransform.position;

            m_NavAgent.isStopped = false;
            m_NavAgent.speed = 5f; // Run speed
            m_NavAgent.SetDestination(m_LastSetDestination);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (m_NavAgent == null || m_TargetTransform == null)
                return Status.Failure;

            Vector3 currentTargetPos = m_TargetTransform.position;
            if (Vector3.Distance(currentTargetPos, m_LastSetDestination) > k_RetargetThreshold)
            {
                m_NavAgent.SetDestination(currentTargetPos);
                m_LastSetDestination = currentTargetPos;
            }

            if (m_NavAgent.pathPending)
                return Status.Running;

            if (m_NavAgent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning($"[NavigateToSafeTable] Path is Invalid — blacklisting table.");
                BlacklistCurrentTable();
                m_NavAgent.ResetPath();
                return Status.Failure;
            }

            float distToTarget = Vector3.Distance(
                m_NavAgent.transform.position, currentTargetPos);

            if (distToTarget <= m_NavAgent.stoppingDistance + k_ArrivalTolerance)
            {
                return Status.Success;
            }

            if (!m_NavAgent.pathPending && m_NavAgent.hasPath &&
                m_NavAgent.remainingDistance <= m_NavAgent.stoppingDistance + k_ArrivalTolerance)
            {
                return Status.Success;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (CurrentStatus == Status.Success || CurrentStatus == Status.Failure)
            {
                if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
                    m_NavAgent.ResetPath();
            }
        }

        private void BlacklistCurrentTable()
        {
            if (TargetTable?.Value == null || BlacklistedTables?.Value == null)
                return;

            if (!BlacklistedTables.Value.Contains(TargetTable.Value))
            {
                BlacklistedTables.Value.Add(TargetTable.Value);
                Debug.Log(
                    $"[NavigateToSafeTable] Blacklisted table '{TargetTable.Value.name}' — path blocked.",
                    TargetTable.Value);
            }
        }
    }
}
