using UnityEngine;
using UnityEngine.AI;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Syncs NavMeshAgent velocity → Animator "Speed" float for
    /// a 1D Blend Tree (Idle / Walk / Run).
    /// Snaps to idle when the agent stops to prevent lingering steps.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Animator))]
    public class NPCLocomotionSync : MonoBehaviour
    {
        [Header("Smoothing")]
        [Tooltip("Blend rate when accelerating. Higher = snappier.")]
        [SerializeField] private float accelSmoothing = 8f;

        [Tooltip("Blend rate when decelerating. Higher = stops animation faster.")]
        [SerializeField] private float decelSmoothing = 15f;

        [Tooltip("Below this velocity the animation snaps to Idle immediately.")]
        [SerializeField] private float idleThreshold = 0.15f;

        private NavMeshAgent m_NavAgent;
        private Animator     m_Animator;
        private float        m_SmoothedSpeed;

        private static readonly int s_SpeedHash = Animator.StringToHash("Speed");

        private void Awake()
        {
            m_NavAgent = GetComponentInChildren<NavMeshAgent>();
            m_Animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (m_NavAgent == null || m_Animator == null) return;

            float rawSpeed = m_NavAgent.velocity.magnitude;

            if (rawSpeed < idleThreshold)
            {
                // Agent has stopped — quickly damp to idle (not instant snap).
                m_SmoothedSpeed = Mathf.MoveTowards(m_SmoothedSpeed, 0f, decelSmoothing * Time.deltaTime);
            }
            else
            {
                // Smooth toward the actual speed.
                float rate = rawSpeed > m_SmoothedSpeed ? accelSmoothing : decelSmoothing;
                m_SmoothedSpeed = Mathf.Lerp(m_SmoothedSpeed, rawSpeed, Time.deltaTime * rate);
            }

            m_Animator.SetFloat(s_SpeedHash, m_SmoothedSpeed);
        }
    }
}
