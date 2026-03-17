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

        [Header("State (read-only at runtime)")]
        [SerializeField] private bool m_IsCrouching;
        [SerializeField] private bool m_IsCrawling;
        [SerializeField] private bool m_IsEventActive;
        [SerializeField] private bool m_IsPostEarthquake;

        // ── Cached ──────────────────────────────────────────────
        private static readonly int s_CrouchHash    = Animator.StringToHash("Crouch");
        private static readonly int s_CoverCrawlHash = Animator.StringToHash("CoverCrawl");
        private float m_TargetWeight;

        private void Awake()
        {
            if (npcAnimator == null)
                npcAnimator = GetComponentInParent<Animator>()
                           ?? GetComponentInChildren<Animator>();
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

            if (m_IsPostEarthquake && !m_IsCrawling)
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
