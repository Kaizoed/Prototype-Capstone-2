using UnityEngine;
using UnityEngine.Animations.Rigging;
using ShakySurvival.Earthquake;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Smoothly blends an Animation Rigging <see cref="Rig"/> weight based on
    /// crouch + earthquake-event state.
    ///
    /// IK weight → 1  only when the Animator's "Crouch" bool is true,
    /// "CoverCrawl" is false (not mid-crawl), AND the earthquake event
    /// is active (hands-on-head pose).
    /// Otherwise weight → 0 (normal animation plays through).
    ///
    /// The crouch state is read directly from the Animator every frame,
    /// so it stays in sync no matter what sets the bool (behavior tree,
    /// <see cref="NPCController.SetCrouch"/>, etc.).
    ///
    /// Auto-subscribes to <see cref="EarthquakeEvents"/> so the event state
    /// is handled for you.
    /// </summary>
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

        // ─────────────────────────────────────────────────────────
        // Lifecycle
        // ─────────────────────────────────────────────────────────

        private void Awake()
        {
            // Auto-find the Animator if not assigned in the Inspector.
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

            // Read states directly from the Animator.
            if (npcAnimator != null)
            {
                m_IsCrouching = npcAnimator.GetBool(s_CrouchHash);
                m_IsCrawling  = npcAnimator.GetBool(s_CoverCrawlHash);
            }

            // After earthquake: IK always on EXCEPT while crawling.
            // During earthquake: only when crouching idle (not mid-crawl).
            if (m_IsPostEarthquake && !m_IsCrawling)
                m_TargetWeight = 1f;
            else if (m_IsEventActive && m_IsCrouching && !m_IsCrawling)
                m_TargetWeight = 1f;
            else
                m_TargetWeight = 0f;

            // Smoothly move toward target — never snaps.
            ikRig.weight = Mathf.MoveTowards(
                ikRig.weight,
                m_TargetWeight,
                blendSpeed * Time.deltaTime
            );
        }

        // ─────────────────────────────────────────────────────────
        // Public API — called by gameplay scripts
        // ─────────────────────────────────────────────────────────

        /// <summary>
        /// Manual override for the crouch state. Normally the script
        /// reads the Animator's "Crouch" bool automatically each frame,
        /// but you can call this if you need to force a value.
        /// </summary>
        public void SetCrouchState(bool crouching)
        {
            m_IsCrouching = crouching;
        }

        /// <summary>
        /// Tell the IK controller whether a disaster event is currently active.
        /// </summary>
        public void SetEventState(bool active)
        {
            m_IsEventActive = active;
        }

        // ─────────────────────────────────────────────────────────
        // Read-Only Properties
        // ─────────────────────────────────────────────────────────

        /// <summary>True when the NPC's Animator has Crouch = true.</summary>
        public bool IsCrouching => m_IsCrouching;

        /// <summary>True when the disaster event is active.</summary>
        public bool IsEventActive => m_IsEventActive;

        // ─────────────────────────────────────────────────────────
        // EarthquakeEvents handlers (auto-drive event state)
        // ─────────────────────────────────────────────────────────

        private void HandleEarthquakeStart()
        {
            SetEventState(true);
        }

        private void HandleEarthquakeStop()
        {
            // Switch to post-earthquake mode — IK stays on permanently.
            m_IsPostEarthquake = true;
        }
    }
}
