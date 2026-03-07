using System;
using UnityEngine;
using UnityEngine.AI;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Lightweight movement controller that sits on every NPC.
    /// Provides a clean public API for external systems (like
    /// <see cref="RoomEvacuationCoordinator"/>) to command movement
    /// and receive arrival callbacks.
    ///
    /// Works alongside <see cref="NPCLocomotionSync"/> — the Speed
    /// parameter is driven by NavMeshAgent.velocity automatically.
    ///
    /// Integration with Behavior Graph:
    ///   Your behavior tree can check <see cref="HasCommand"/> to know
    ///   if the coordinator has issued an order, and read
    ///   <see cref="CurrentTarget"/> to get the destination.
    ///   Alternatively, just let this script drive the NavMeshAgent
    ///   directly — it won't conflict as long as your tree doesn't
    ///   set a destination on the same frame.
    /// </summary>
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

        /// <summary>
        /// Fired ONCE when the NPC reaches the destination set by
        /// <see cref="CommandMoveTo"/>. Subscribers receive this
        /// NPCController as the argument so the coordinator knows
        /// which NPC arrived.
        /// </summary>
        public event Action<NPCController> OnDestinationReached;

        // ── Public Read-Only State ──────────────────────────────

        /// <summary>True while the NPC is walking toward a command target.</summary>
        public bool HasCommand => m_HasCommand;

        /// <summary>The transform the NPC was last commanded to move to (may be null).</summary>
        public Transform CurrentTarget => m_CurrentTarget;

        /// <summary>True if the agent is at (or very near) its destination.</summary>
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

        // ── Private ─────────────────────────────────────────────
        private NavMeshAgent m_NavAgent;
        private Transform    m_CurrentTarget;
        private bool         m_HasCommand;
        private bool         m_ArrivalFired; // prevents double-fire

        // ─────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────

        private void Awake()
        {
            m_NavAgent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            // Only check arrival when we have an active command and
            // haven't already fired the event for this command.
            if (!m_HasCommand || m_ArrivalFired) return;

            if (HasReachedDestination)
            {
                m_ArrivalFired = true;

                Debug.Log($"[NPCController] {gameObject.name} reached destination.");

                // Fire the event — the coordinator (or any listener) will handle it.
                OnDestinationReached?.Invoke(this);
            }
        }

        // ─────────────────────────────────────────────────────────
        // Public API — called by RoomEvacuationCoordinator
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Commands the NPC to walk to <paramref name="target"/> at the
        /// calm <see cref="walkSpeed"/>.
        /// Fires <see cref="OnDestinationReached"/> when it arrives.
        /// </summary>
        public void CommandMoveTo(Transform target)
        {
            CommandMoveTo(target, walkSpeed);
        }

        /// <summary>
        /// Commands the NPC to move to <paramref name="target"/> at
        /// <see cref="runSpeed"/> (urgent evacuation).
        /// </summary>
        public void CommandRunTo(Transform target)
        {
            CommandMoveTo(target, runSpeed);
        }

        /// <summary>
        /// Core movement command. Sets NavMeshAgent destination and speed.
        /// Resets arrival tracking so the event fires again for this new target.
        /// </summary>
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

            // Ensure the NavMeshAgent is ready to move.
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

        /// <summary>
        /// Stops the NPC and clears the current command.
        /// Does NOT fire <see cref="OnDestinationReached"/>.
        /// </summary>
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
    }
}
