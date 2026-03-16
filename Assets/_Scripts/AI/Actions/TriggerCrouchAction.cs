using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using ShakySurvival.Cover;
using Action = Unity.Behavior.Action;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Behavior Graph Action — scripted cover entry.
    ///
    /// After <see cref="NavigateToSafeTableAction"/> delivers the NPC to the table edge,
    /// this node:
    ///   1. Disables NavMeshAgent
    ///   2. Plays Crouch animation (blend)
    ///   3. Plays CoverCrawl + lerps toward HideAnchor under the table
    ///   4. Settles into Crouch Idle — NPC stays hidden
    ///
    /// Resilient to tree re-evaluation restarts via m_InProgress guard.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Trigger Crouch",
        description: "Crouches and crawls the NPC under a table to the HideAnchor position.",
        story: "[Agent] crouches under the table",
        category: "Action/Earthquake",
        id: "ea1c0012000000000000000000000001")]
    public partial class TriggerCrouchAction : Action
    {
        // ── Blackboard Variables ─────────────────────────────────
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<GameObject> TargetTable;

        // ── Tuning ──────────────────────────────────────────────
        private const float k_CrawlSpeed       = 2f;
        private const float k_AnimBlendTime    = 0.25f;
        private const float k_ArrivalThreshold = 0.05f;

        // ── Animator hashes ─────────────────────────────────────
        private static readonly int s_CrouchHash     = Animator.StringToHash("Crouch");
        private static readonly int s_CoverCrawlHash = Animator.StringToHash("CoverCrawl");

        // ── Runtime state ───────────────────────────────────────
        private enum Phase { BlendToCrouch, Crawling, Settling }

        private NavMeshAgent m_NavAgent;
        private Animator     m_Animator;
        private Transform    m_AgentTransform;
        private Vector3      m_HideAnchorPos;
        private Phase        m_Phase;
        private float        m_PhaseTimer;
        private bool         m_InProgress;

        // ─────────────────────────────────────────────────────────
        protected override Status OnStart()
        {
            // If already mid-process (tree restarted us), just keep going.
            if (m_InProgress)
                return Status.Running;

            if (Agent == null || Agent.Value == null)
            {
                LogFailure("Agent is null.");
                return Status.Failure;
            }

            if (TargetTable == null || TargetTable.Value == null)
            {
                LogFailure("TargetTable is null.");
                return Status.Failure;
            }

            // Get CoverSpot component to access HideAnchor.
            CoverSpot coverSpot = TargetTable.Value.GetComponentInChildren<CoverSpot>();
            if (coverSpot == null || coverSpot.HideAnchor == null)
            {
                LogFailure($"No CoverSpot component (or HideAnchor) found on '{TargetTable.Value.name}'.");
                return Status.Failure;
            }

            m_HideAnchorPos  = coverSpot.HideAnchor.position;
            m_AgentTransform = Agent.Value.transform;

            m_NavAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            m_Animator = Agent.Value.GetComponentInChildren<Animator>();

            // Disable root motion so the crawl animation doesn't fight our position lerp.
            if (m_Animator != null)
                m_Animator.applyRootMotion = false;

            // Disable NavMeshAgent so it doesn't fight the scripted movement.
            if (m_NavAgent != null)
            {
                m_NavAgent.ResetPath();
                m_NavAgent.isStopped      = true;
                m_NavAgent.updatePosition = false;
                m_NavAgent.updateRotation = false;
            }

            // ── Phase 1: Blend to Crouch Idle ──
            m_Phase      = Phase.BlendToCrouch;
            m_PhaseTimer = k_AnimBlendTime;
            m_InProgress = true;
            SetAnimatorState(crouch: true, crawl: false);

            Debug.Log("[EnterCover] Phase 1 — Blending to Crouch.");
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            m_PhaseTimer -= Time.deltaTime;

            switch (m_Phase)
            {
                case Phase.BlendToCrouch:
                    if (m_PhaseTimer <= 0f)
                    {
                        m_Phase = Phase.Crawling;
                        SetAnimatorState(crouch: true, crawl: true);
                        Debug.Log("[EnterCover] Phase 2 — Crawling to HideAnchor.");
                    }
                    break;

                case Phase.Crawling:
                {
                    Vector3 currentPos = m_AgentTransform.position;
                    Vector3 toTarget = m_HideAnchorPos - currentPos;
                    Vector3 horizontalDelta = new Vector3(toTarget.x, 0f, toTarget.z);

                    if (horizontalDelta.sqrMagnitude <= k_ArrivalThreshold * k_ArrivalThreshold)
                    {
                        m_AgentTransform.position = new Vector3(
                            m_HideAnchorPos.x, currentPos.y, m_HideAnchorPos.z);

                        m_Phase      = Phase.Settling;
                        m_PhaseTimer = k_AnimBlendTime;
                        SetAnimatorState(crouch: true, crawl: false);
                        Debug.Log("[EnterCover] Phase 3 — Settling into Crouch Idle.");
                        break;
                    }

                    Vector3 moveDir = horizontalDelta.normalized;
                    float step = k_CrawlSpeed * Time.deltaTime;

                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                        m_AgentTransform.rotation = Quaternion.RotateTowards(
                            m_AgentTransform.rotation, targetRot, 360f * Time.deltaTime);
                    }

                    m_AgentTransform.position += moveDir * Mathf.Min(step, horizontalDelta.magnitude);
                    break;
                }

                case Phase.Settling:
                    if (m_PhaseTimer <= 0f)
                    {
                        // Log once, then just hold here — keep returning Running
                        // so the Sequence doesn't complete and Repeat Forever doesn't restart.
                        // The NPC stays under cover until the tree interrupts us
                        // (earthquake stops / IsPanicking changes).
                    }
                    break;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (CurrentStatus == Status.Success || CurrentStatus == Status.Failure)
                m_InProgress = false;
        }

        private void SetAnimatorState(bool crouch, bool crawl)
        {
            if (m_Animator == null) return;
            m_Animator.SetBool(s_CrouchHash, crouch);
            m_Animator.SetBool(s_CoverCrawlHash, crawl);
        }
    }
}
