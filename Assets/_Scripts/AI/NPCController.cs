using System;
using UnityEngine;
using UnityEngine.AI;

namespace ShakySurvival.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCController : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────
        [Header("Movement Speeds")]
        [Tooltip("Walking speed used during calm movement (e.g. lining up).")]
        [SerializeField] private float walkSpeed = 1.5f;

        [Tooltip("Running speed used during urgent movement (e.g. evacuating).")]
        [SerializeField] private float runSpeed = 5f;

        [Header("Arrival")]
        [Tooltip("Extra tolerance added to NavMeshAgent.stoppingDistance to decide 'arrived'.")]
        [SerializeField] private float arrivalTolerance = 0.2f;

        // ── Events ──────────────────────────────────────────────

        public event Action<NPCController> OnDestinationReached;

        public bool HasCommand => m_HasCommand;

        public Transform CurrentTarget => m_CurrentTarget;

        public bool HasReachedDestination
        {
            get
            {
                if (m_NavAgent == null || !m_NavAgent.isOnNavMesh) return false;
                if (m_NavAgent.pathPending) return false;
                return m_NavAgent.remainingDistance <=
                       m_NavAgent.stoppingDistance + arrivalTolerance;
            }
        }

        private NavMeshAgent m_NavAgent;
        private Transform    m_CurrentTarget;
        private bool         m_HasCommand;
        private bool         m_ArrivalFired; // prevents double-fire

        private void Awake()
        {
            m_NavAgent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (!m_HasCommand || m_ArrivalFired) return;

            if (HasReachedDestination)
            {
                m_ArrivalFired = true;

                Debug.Log($"[NPCController] {gameObject.name} reached destination.");

                OnDestinationReached?.Invoke(this);
            }
        }

        // Public API — called by RoomEvacuationCoordinator

        public void CommandMoveTo(Transform target)
        {
            CommandMoveTo(target, walkSpeed);
        }

        public void CommandRunTo(Transform target)
        {
            CommandMoveTo(target, runSpeed);
        }

        public void CommandMoveTo(Transform target, float speed)
        {
            if (target == null)
            {
                Debug.LogWarning($"[NPCController] {gameObject.name}: CommandMoveTo called with null target.");
                return;
            }

            m_CurrentTarget = target;
            m_HasCommand    = true;
            m_ArrivalFired  = false;

            if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
            {
                m_NavAgent.isStopped       = false;
                m_NavAgent.updatePosition  = true;
                m_NavAgent.updateRotation  = true;
                m_NavAgent.speed           = speed;
                m_NavAgent.SetDestination(target.position);
            }

            Debug.Log($"[NPCController] {gameObject.name} commanded to move to '{target.name}' at speed {speed:F1}.");
        }

        public void CancelCommand()
        {
            m_HasCommand   = false;
            m_ArrivalFired = false;
            m_CurrentTarget = null;

            if (m_NavAgent != null && m_NavAgent.isOnNavMesh)
            {
                m_NavAgent.ResetPath();
                m_NavAgent.isStopped = true;
            }
        }

        // IK Rig Integration

        [Header("IK Rig")]
        [Tooltip("Reference to the NPCIKRigController. " +
                 "If assigned, crouch state is forwarded automatically.")]
        [SerializeField] private NPCIKRigController ikRigController;

        // Animator Helpers

        private static readonly int s_CrouchHash = Animator.StringToHash("Crouch");

        public void SetCrouch(bool crouch)
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.SetBool(s_CrouchHash, crouch);
                if (m_NavAgent != null) m_NavAgent.isStopped = crouch;

                Debug.Log($"[NPCController] {gameObject.name} Crouch = {crouch}");
            }

            if (ikRigController != null)
                ikRigController.SetCrouchState(crouch);
        }
    }
}
