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

        protected override Status OnStart()
        {
            if (Agent == null || Agent.Value == null)
                return Status.Failure;

            m_NavAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            m_Animator = Agent.Value.GetComponentInChildren<Animator>();

            if (m_Animator == null)
                return Status.Failure;

            if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
            {
                m_NavAgent.ResetPath();
                m_NavAgent.isStopped = true;
            }

            m_Animator.SetBool(s_PanicHash, true);
            m_EnteredPanicState = false;

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (m_Animator == null)
                return Status.Failure;

            AnimatorStateInfo stateInfo = m_Animator.GetCurrentAnimatorStateInfo(0);

            if (!m_EnteredPanicState)
            {
                if (stateInfo.shortNameHash == s_PanicStateHash)
                    m_EnteredPanicState = true;

                return Status.Running;
            }

            if (stateInfo.shortNameHash == s_PanicStateHash && stateInfo.normalizedTime >= 1f)
            {
                m_Animator.SetBool(s_PanicHash, false);
                UnStopAgent();
                return Status.Success;
            }

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
            if (m_Animator != null)
                m_Animator.SetBool(s_PanicHash, false);

            UnStopAgent();
        }

        private void UnStopAgent()
        {
            if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
                m_NavAgent.isStopped = false;
        }
    }
}
