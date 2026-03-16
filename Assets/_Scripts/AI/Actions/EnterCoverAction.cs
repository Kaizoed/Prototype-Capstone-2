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
    /// Behavior Graph Action — scripted cover entry that mirrors the player's mechanic.
    ///
    /// After <see cref="NavigateToSafeTableAction"/> delivers the NPC to the table edge (CoverPoint),
    /// this node takes over:
    ///   1. Disables NavMeshAgent
    ///   2. Plays Crouch animation (blend time)
    ///   3. Plays CoverCrawl + lerps toward HideAnchor under the table
    ///   4. Settles into Crouch Idle — NPC stays hidden
    ///
    /// Resilient to Behavior Tree re-evaluating/restarting the node mid-process.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Enter Cover",
        description: "Crouches and crawls the NPC under a table to the HideAnchor position.",
        story: "[Agent] enters cover under [TargetTable]",
        category: "Action/Earthquake",
        id: "ea1c0015000000000000000000000001")]
    public partial class EnterCoverAction : Action
    {
        // ── Blackboard Variables ─────────────────────────────────
        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<GameObject> TargetTable;

        // ── Tuning ──────────────────────────────────────────────
        private const float k_CrawlSpeed        = 2f;
        private const float k_AnimBlendTime     = 0.25f;
        private const float k_ArrivalThreshold  = 0.05f;
        private const float k_MaxCrawlDistance  = 3f;  // prevents phasing through walls

        // ── Animator hashes ─────────────────────────────────────
        private static readonly int s_CrouchHash     = Animator.StringToHash("Crouch");
        private static readonly int s_CoverCrawlHash = Animator.StringToHash("CoverCrawl");

        // ── Runtime state ───────────────────────────────────────
        private enum Phase { BlendToCrouch, Crawling, Settling, Settled }

        private NavMeshAgent m_NavAgent;
        private Animator     m_Animator;
        private Transform    m_AgentTransform;
        private Vector3      m_HideAnchorPos;
        private Phase        m_Phase;
        private float        m_PhaseTimer;

        // Guard flag — survives tree re-evaluation restarts.
        private bool m_InProgress;

        // ─────────────────────────────────────────────────────────
        protected override Status OnStart()
        {
            // If we're already mid-process (tree restarted us), just keep going.
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

            // Safety check — if the NPC is too far from the HideAnchor,
            // the scripted crawl would phase through geometry.
            float crawlDist = Vector3.Distance(
                new Vector3(m_AgentTransform.position.x, 0f, m_AgentTransform.position.z),
                new Vector3(m_HideAnchorPos.x, 0f, m_HideAnchorPos.z));
            if (crawlDist > k_MaxCrawlDistance)
            {
                Debug.LogWarning($"[EnterCover] {Agent.Value.name} is {crawlDist:F1}m from HideAnchor — too far, aborting cover entry.");
                return Status.Failure;
            }

            // Get components.
            m_NavAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            m_Animator = Agent.Value.GetComponentInChildren<Animator>();

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
                // ── Phase 1: Wait for crouch blend ──
                case Phase.BlendToCrouch:
                    if (m_PhaseTimer <= 0f)
                    {
                        m_Phase = Phase.Crawling;
                        SetAnimatorState(crouch: true, crawl: true);
                        Debug.Log("[EnterCover] Phase 2 — Crawling to HideAnchor.");
                    }
                    break;

                // ── Phase 2: Crawl toward HideAnchor ──
                case Phase.Crawling:
                {
                    Vector3 currentPos = m_AgentTransform.position;
                    Vector3 toTarget = m_HideAnchorPos - currentPos;
                    Vector3 horizontalDelta = new Vector3(toTarget.x, 0f, toTarget.z);

                    if (horizontalDelta.sqrMagnitude <= k_ArrivalThreshold * k_ArrivalThreshold)
                    {
                        // Arrived — snap to position.
                        m_AgentTransform.position = new Vector3(
                            m_HideAnchorPos.x, currentPos.y, m_HideAnchorPos.z);

                        m_Phase      = Phase.Settling;
                        m_PhaseTimer = k_AnimBlendTime;
                        SetAnimatorState(crouch: true, crawl: false);
                        Debug.Log("[EnterCover] Phase 3 — Settling into Crouch Idle.");
                        break;
                    }

                    // Face and move toward the hide anchor.
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

                // ── Phase 3: Wait for settle blend ──
                case Phase.Settling:
                    if (m_PhaseTimer <= 0f)
                    {
                        Debug.Log("[EnterCover] Complete — NPC is under cover (staying put).");
                        m_Phase = Phase.Settled;
                    }
                    break;

                // ── Phase 4: Stay under cover forever ──
                // Return Running so the Sequence never completes
                // and Repeat doesn't re-trigger the whole branch.
                case Phase.Settled:
                    break;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            // Only reset state if the node actually completed.
            // If the tree just re-evaluated/interrupted us, keep m_InProgress alive
            // so OnStart doesn't reinitialize on the next call.
            if (CurrentStatus == Status.Success || CurrentStatus == Status.Failure)
            {
                m_InProgress = false;
            }
        }

        // ── Helpers ──────────────────────────────────────────────
        private void SetAnimatorState(bool crouch, bool crawl)
        {
            if (m_Animator == null) return;
            m_Animator.SetBool(s_CrouchHash, crouch);
            m_Animator.SetBool(s_CoverCrawlHash, crawl);
        }
    }
}
