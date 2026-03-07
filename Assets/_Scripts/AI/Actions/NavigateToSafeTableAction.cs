using System;
using System.Collections.Generic;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Behavior Graph Action — navigates the NPC to the chosen safe table's CoverPoint.
    /// 
    /// Continuously monitors <see cref="NavMeshAgent.pathStatus"/>:
    ///   • If the path becomes Partial or Invalid (e.g. debris carved a hole),
    ///     the current table is added to <see cref="BlacklistedTables"/>
    ///     and the node returns <b>Failure</b>, prompting the parent Sequence
    ///     to re-run <see cref="FindNearestSafeTableAction"/>.
    ///   • If the agent successfully reaches the destination, returns <b>Success</b>.
    /// </summary>
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

        // A small tolerance added to stoppingDistance when deciding "arrived".
        private const float k_ArrivalTolerance = 0.15f;

        // ─────────────────────────────────────────────────────────
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

            m_NavAgent.isStopped = false;
            m_NavAgent.speed = 5f; // Run speed — triggers Run animation via NPCLocomotionSync
            m_NavAgent.SetDestination(TargetCoverPoint.Value.position);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (m_NavAgent == null)
                return Status.Failure;

            // While the path is still being computed, just wait.
            if (m_NavAgent.pathPending)
                return Status.Running;

            // ── Dynamic obstacle check ───────────────────────────
            NavMeshPathStatus pathStatus = m_NavAgent.pathStatus;

            // Only fail on truly invalid paths (e.g. agent fell off NavMesh).
            // PathPartial is expected because CoverPoints are inside the
            // table's NavMeshObstacle carve — the agent gets close, and
            // the scripted crawl handles the rest.
            if (pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning($"[NavigateToSafeTable] Path is Invalid — blacklisting table.");

                BlacklistCurrentTable();

                m_NavAgent.ResetPath();
                return Status.Failure;
            }

            // ── Arrival check ────────────────────────────────────
            // For PathPartial the agent stops at the closest reachable point.
            // Check if the agent has finished moving (velocity near zero + has a path).
            if (!m_NavAgent.pathPending && m_NavAgent.hasPath &&
                m_NavAgent.remainingDistance <= m_NavAgent.stoppingDistance + k_ArrivalTolerance)
            {
                return Status.Success;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            // Only reset the path if this node completed normally.
            // If the tree is re-evaluating branches (interrupting us),
            // we must NOT cancel the agent's path — that causes flickering.
            if (CurrentStatus == Status.Success || CurrentStatus == Status.Failure)
            {
                if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
                    m_NavAgent.ResetPath();
            }
        }

        // ── Helpers ──────────────────────────────────────────────
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
