using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;
using ShakySurvival.Earthquake;
using ShakySurvival.Cover;

namespace ShakySurvival.AI
{
    /// <summary>
    /// Bridges EarthquakeEvents ↔ Behavior Tree Blackboard.
    ///
    /// Start  → delayed reaction → IsEarthquakeActive + IsPanicking
    /// Stop   → exit cover coroutine (turn, crawl out, stand) → reset Blackboard
    /// </summary>
    [RequireComponent(typeof(BehaviorGraphAgent))]
    public class NPCBehaviorBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BehaviorGraphAgent behaviorAgent;

        [Header("Panic Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float panicChance = 0.5f;

        [Header("Reaction")]
        [SerializeField] private float reactionDelay = 2f;

        [Header("Exit Cover Tuning")]
        [SerializeField] private float exitCrawlSpeed     = 1.5f;
        [SerializeField] private float exitTurnSpeed      = 180f;
        [SerializeField] private float exitBlendTime      = 0.3f;
        [SerializeField] private float exitStandBlendTime = 0.4f;

        // ── Blackboard keys ─────────────────────────────────────
        private const string KEY_IS_EARTHQUAKE_ACTIVE = "IsEarthquakeActive";
        private const string KEY_IS_PANICKING         = "IsPanicking";

        private static readonly int s_CrouchHash     = Animator.StringToHash("Crouch");
        private static readonly int s_CoverCrawlHash = Animator.StringToHash("CoverCrawl");

        private Coroutine m_DelayRoutine;
        private Coroutine m_ExitRoutine;

        private void Awake()
        {
            if (behaviorAgent == null)
                behaviorAgent = GetComponent<BehaviorGraphAgent>();
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

        // ── Event Handlers ───────────────────────────────────────

        private void HandleEarthquakeStart()
        {
            if (m_ExitRoutine != null)
            {
                StopCoroutine(m_ExitRoutine);
                m_ExitRoutine = null;
            }

            if (m_DelayRoutine != null)
                StopCoroutine(m_DelayRoutine);
            m_DelayRoutine = StartCoroutine(DelayedReaction());
        }

        private void HandleEarthquakeStop()
        {
            if (m_DelayRoutine != null)
            {
                StopCoroutine(m_DelayRoutine);
                m_DelayRoutine = null;
            }

            SetBlackboardBool(KEY_IS_PANICKING, false);

            // Start exit sequence — IsEarthquakeActive stays true until it finishes.
            if (m_ExitRoutine != null)
                StopCoroutine(m_ExitRoutine);
            m_ExitRoutine = StartCoroutine(ExitCoverSequence());
        }

        // ── Delayed Reaction ────────────────────────────────────
        private IEnumerator DelayedReaction()
        {
            yield return new WaitForSeconds(reactionDelay);

            SetBlackboardBool(KEY_IS_EARTHQUAKE_ACTIVE, true);

            bool willPanic = Random.value < panicChance;
            SetBlackboardBool(KEY_IS_PANICKING, willPanic);

            Debug.Log($"[NPCBehaviorBridge] Earthquake reaction — {gameObject.name} " +
                      $"{(willPanic ? "PANICKING" : "SEEKING COVER")} (delay: {reactionDelay}s).");

            m_DelayRoutine = null;
        }

        // ── Exit Cover Coroutine ────────────────────────────────
        private IEnumerator ExitCoverSequence()
        {
            Animator animator = GetComponentInChildren<Animator>();
            NavMeshAgent navAgent = GetComponentInChildren<NavMeshAgent>();
            Transform agentTransform = transform;

            // Get the table reference for exit position and release.
            GameObject targetTable = null;
            CoverSpot coverSpot = null;
            if (behaviorAgent != null)
            {
                behaviorAgent.BlackboardReference.GetVariableValue("TargetTable", out targetTable);
                if (targetTable != null)
                    coverSpot = targetTable.GetComponentInChildren<CoverSpot>();
            }

            bool isCrouching = animator != null && animator.GetBool(s_CrouchHash);

            if (!isCrouching)
            {
                // Not under cover (was panicking or never reached cover).
                ReEnableNavAgent(navAgent, animator, agentTransform);
                ReleaseCoverSpot(coverSpot);
                SetBlackboardBool(KEY_IS_EARTHQUAKE_ACTIVE, false);
                Debug.Log($"[NPCBehaviorBridge] {gameObject.name} not under cover — reset immediately.");
                m_ExitRoutine = null;
                yield break;
            }

            // Keep NavMeshAgent disabled during exit.
            if (navAgent != null)
            {
                navAgent.ResetPath();
                navAgent.isStopped = true;
                navAgent.updatePosition = false;
                navAgent.updateRotation = false;
            }
            if (animator != null)
                animator.applyRootMotion = false;

            // Find exit position.
            Vector3 exitPos = agentTransform.position;
            if (coverSpot != null && coverSpot.ExitPoint != null)
                exitPos = coverSpot.ExitPoint.position;

            // ── Phase 1: Turn around ──
            Debug.Log("[ExitCover] Phase 1 — Turning around.");
            Vector3 toExit = exitPos - agentTransform.position;
            toExit.y = 0f;

            if (toExit.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toExit.normalized, Vector3.up);
                while (Quaternion.Angle(agentTransform.rotation, targetRot) > 5f)
                {
                    agentTransform.rotation = Quaternion.RotateTowards(
                        agentTransform.rotation, targetRot, exitTurnSpeed * Time.deltaTime);
                    yield return null;
                }
                agentTransform.rotation = targetRot;
            }

            // ── Phase 2: Crawl to exit ──
            Debug.Log("[ExitCover] Phase 2 — Crawling to exit.");
            if (animator != null)
            {
                animator.SetBool(s_CrouchHash, true);
                animator.SetBool(s_CoverCrawlHash, true);
            }

            while (true)
            {
                Vector3 delta = exitPos - agentTransform.position;
                delta.y = 0f;

                if (delta.sqrMagnitude <= 0.01f)
                {
                    agentTransform.position = new Vector3(exitPos.x, agentTransform.position.y, exitPos.z);
                    break;
                }

                Vector3 moveDir = delta.normalized;
                float step = exitCrawlSpeed * Time.deltaTime;
                agentTransform.position += moveDir * Mathf.Min(step, delta.magnitude);
                yield return null;
            }

            // ── Phase 3: Settle crouch ──
            Debug.Log("[ExitCover] Phase 3 — Settling crouch.");
            if (animator != null)
                animator.SetBool(s_CoverCrawlHash, false);
            yield return new WaitForSeconds(exitBlendTime);

            // ── Phase 4: Stand up ──
            Debug.Log("[ExitCover] Phase 4 — Standing up.");
            if (animator != null)
                animator.SetBool(s_CrouchHash, false);
            yield return new WaitForSeconds(exitStandBlendTime);

            // ── Done ──
            ReEnableNavAgent(navAgent, animator, agentTransform);
            ReleaseCoverSpot(coverSpot);
            SetBlackboardBool(KEY_IS_EARTHQUAKE_ACTIVE, false);

            Debug.Log($"[ExitCover] Complete — {gameObject.name} is standing.");
            m_ExitRoutine = null;
        }

        // ── Helpers ──────────────────────────────────────────────
        private void ReEnableNavAgent(NavMeshAgent navAgent, Animator animator, Transform agentTransform)
        {
            if (navAgent != null)
            {
                navAgent.updatePosition = true;
                navAgent.updateRotation = true;
                navAgent.isStopped = false;
                if (navAgent.isOnNavMesh)
                    navAgent.Warp(agentTransform.position);
            }
            if (animator != null)
                animator.applyRootMotion = true;
        }

        private void SetBlackboardBool(string key, bool value)
        {
            if (behaviorAgent == null) return;
            behaviorAgent.BlackboardReference.SetVariableValue(key, value);
        }

        private void ReleaseCoverSpot(CoverSpot spot)
        {
            if (spot != null)
            {
                spot.Release(gameObject);
                Debug.Log($"[NPCBehaviorBridge] {gameObject.name} released cover spot.");
            }
        }
    }
}
