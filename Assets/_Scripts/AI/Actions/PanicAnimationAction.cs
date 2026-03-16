using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Behavior Graph Action — plays the panic animation once, then succeeds.
    ///
    /// Place AFTER <see cref="PanicRunToRandomPointAction"/> inside a
    /// Repeat → Sequence so the NPC alternates between running and panicking.
    ///
    /// The action:
    ///   1. Stops the NavMeshAgent (NPC stands still)
    ///   2. Sets Panic = true on the Animator → transitions to the Panic state
    ///   3. Waits for the animation to finish (normalizedTime >= 1)
    ///   4. Sets Panic = false → Animator transitions back to Locomotion
    ///   5. Returns Success so the Sequence continues to the next run
    ///
    /// On interrupt (earthquake stops / branch switch):
    ///   OnEnd() always cleans up — sets Panic = false and un-stops the agent.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Panic Animation",
        description: "Plays the panic animation once, then returns Success.",
        story: "[Agent] plays panic animation",
        category: "Action/Earthquake",
        id: "ea1c0013000000000000000000000001")]
    public partial class PanicAnimationAction : Action
    {
        // ── Blackboard Variables ─────────────────────────────────
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        // ── Animator hashes ─────────────────────────────────────
        private static readonly int s_PanicHash      = Animator.StringToHash("Panic");
        private static readonly int s_PanicStateHash = Animator.StringToHash("Panic");

        // ── Runtime ─────────────────────────────────────────────
        private NavMeshAgent m_NavAgent;
        private Animator     m_Animator;
        private bool         m_EnteredPanicState;

        // ─────────────────────────────────────────────────────────
        protected override Status OnStart()
        {
            if (Agent == null || Agent.Value == null)
                return Status.Failure;

            m_NavAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            m_Animator = Agent.Value.GetComponentInChildren<Animator>();

            if (m_Animator == null)
                return Status.Failure;

            // Stop movement so the NPC stands still during the animation.
            if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
            {
                m_NavAgent.ResetPath();
                m_NavAgent.isStopped = true;
            }

            // Trigger the Panic animation.
            m_Animator.SetBool(s_PanicHash, true);
            m_EnteredPanicState = false;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (m_Animator == null)
                return Status.Failure;

            AnimatorStateInfo stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);

            // Wait for the Animator to actually enter the Panic state.
            if (!m_EnteredPanicState)
            {
                if (stateInfo.shortNameHash == s_PanicStateHash)
                    m_EnteredPanicState = true;

                return Status.Running;
            }

            // Check if the animation has finished playing.
            if (stateInfo.shortNameHash == s_PanicStateHash && stateInfo.normalizedTime >= 1f)
            {
                // Animation done — clear the bool so Animator transitions back to Locomotion.
                m_Animator.SetBool(s_PanicHash, false);
                UnStopAgent();
                return Status.Success;
            }

            // If we've already left the Panic state (e.g. interrupted), just succeed.
            if (m_EnteredPanicState && stateInfo.shortNameHash != s_PanicStateHash)
            {
                m_Animator.SetBool(s_PanicHash, false);
                UnStopAgent();
                return Status.Success;
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            // Always clean up — handles interrupts (earthquake stop, branch switch).
            if (m_Animator != null)
                m_Animator.SetBool(s_PanicHash, false);

            UnStopAgent();
        }

        // ── Helpers ──────────────────────────────────────────────
        private void UnStopAgent()
        {
            if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
                m_NavAgent.isStopped = false;
        }
    }
}
