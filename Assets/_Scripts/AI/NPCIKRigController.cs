using UnityEngine;
using UnityEngine.Animations.Rigging;
using ShakySurvival.Earthquake;

namespace ShakySurvival.AI
{
    public class NPCIKRigController : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────
        [Header("Rig Reference")]
        [Tooltip("The Rig component on the IK_Rig child GameObject.")]
        [SerializeField] private Rig ikRig;

        [Header("Animator Reference")]
        [Tooltip("The Animator on the NPC (AIController). " +
                 "Auto-found via GetComponentInChildren if left empty.")]
        [SerializeField] private Animator npcAnimator;

        [Header("Blend Settings")]
        [Tooltip("How fast the rig weight transitions (units per second). " +
                 "Lower = slower, smoother blend.")]
        [SerializeField] private float blendSpeed = 2f;

        [Header("IK Hand Targets")]
        [Tooltip("The Two-Bone IK target Transform for the left hand.")]
        [SerializeField] private Transform leftHandTarget;
        [Tooltip("The Two-Bone IK target Transform for the right hand.")]
        [SerializeField] private Transform rightHandTarget;
        [Tooltip("Curve controlling the outward/forward arc offset during the blend. " +
                 "X axis = ikRig.weight (0-1), Y axis = offset multiplier. " +
                 "Should start and end at 0, peaking in the middle.")]
        [SerializeField] private AnimationCurve armArcCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
        [Tooltip("Local-space direction the left hand arcs toward during the blend.")]
        [SerializeField] private Vector3 leftHandArcDirection  = Vector3.zero;
        [Tooltip("Local-space direction the right hand arcs toward during the blend.")]
        [SerializeField] private Vector3 rightHandArcDirection = Vector3.zero;

        [Header("State (read-only at runtime)")]
        [SerializeField] private bool m_IsCrouching;
        [SerializeField] private bool m_IsCrawling;
        [SerializeField] private bool m_IsEventActive;
        [SerializeField] private bool m_IsPostEarthquake;
        [SerializeField] private bool m_IsDemonstrating;

        // ── Cached ──────────────────────────────────────────────
        private static readonly int s_CrouchHash    = Animator.StringToHash("Crouch");
        private static readonly int s_CoverCrawlHash = Animator.StringToHash("CoverCrawl");
        private float m_TargetWeight;
        private Vector3 m_LeftHandRestLocalPos;
        private Vector3 m_RightHandRestLocalPos;

        private void Awake()
        {
            if (npcAnimator == null)
                npcAnimator = GetComponentInParent<Animator>()
                           ?? GetComponentInChildren<Animator>();

            if (leftHandTarget  != null) m_LeftHandRestLocalPos  = leftHandTarget.localPosition;
            if (rightHandTarget != null) m_RightHandRestLocalPos = rightHandTarget.localPosition;
        }

        private void OnEnable()
        {
            EarthquakeEvents.OnEarthquakeStart += HandleEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop  += HandleEarthquakeStop;
        }

        private void OnDisable()
        {
            EarthquakeEvents.OnEarthquakeStart -= HandleEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop  -= HandleEarthquakeStop;
        }

        private void Update()
        {
            if (ikRig == null) return;

            if (npcAnimator != null)
            {
                m_IsCrouching = npcAnimator.GetBool(s_CrouchHash);
                m_IsCrawling  = npcAnimator.GetBool(s_CoverCrawlHash);
            }

            if (m_IsDemonstrating)
                m_TargetWeight = 1f;
            else if (m_IsPostEarthquake && !m_IsCrawling)
                m_TargetWeight = 1f;
            else if (m_IsEventActive && m_IsCrouching && !m_IsCrawling)
                m_TargetWeight = 1f;
            else
                m_TargetWeight = 0f;

            ikRig.weight = Mathf.MoveTowards(
                ikRig.weight,
                m_TargetWeight,
                blendSpeed * Time.deltaTime
            );

            // ── Arm-arc offset to prevent hands phasing through the chest ──
            float arcOffset = armArcCurve.Evaluate(ikRig.weight);

            if (leftHandTarget != null)
                leftHandTarget.localPosition = m_LeftHandRestLocalPos
                    + leftHandArcDirection * arcOffset;

            if (rightHandTarget != null)
                rightHandTarget.localPosition = m_RightHandRestLocalPos
                    + rightHandArcDirection * arcOffset;
        }

        // Public API — called by gameplay scripts

        public void SetCrouchState(bool crouching)
        {
            m_IsCrouching = crouching;
        }

        public void SetEventState(bool active)
        {
            m_IsEventActive = active;
        }

        public void SetDemonstration(bool active)
        {
            m_IsDemonstrating = active;
        }

        // Read-Only Properties

        public bool IsCrouching => m_IsCrouching;

        public bool IsEventActive => m_IsEventActive;

        // EarthquakeEvents handlers (auto-drive event state)

        private void HandleEarthquakeStart()
        {
            SetEventState(true);
        }

        private void HandleEarthquakeStop()
        {
            m_IsPostEarthquake = true;
        }
    }
}
