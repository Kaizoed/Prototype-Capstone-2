using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShakySurvival.Earthquake;

namespace ShakySurvival.AI
{
    public class RoomEvacuationCoordinator : MonoBehaviour
    {
        public enum EvacuationState
        {
            Idle,
            LiningUp,
            WaitingForPlayer,
            Evacuating,
            Done
        }

        [Header("NPC Assignment")]
        [Tooltip("All NPCs that belong to this room. Each must have an NPCController.")]
        [SerializeField] private NPCController[] assignedNPCs;

        [Header("Line-Up Configuration")]
        [Tooltip("Empty GameObjects positioned outside the classroom door. " +
                 "One per NPC — first NPC gets spot [0], second gets spot [1], etc.")]
        [SerializeField] private Transform[] lineUpSpots;

        [Header("Evacuation Destination")]
        [Tooltip("The global safe zone all NPCs walk to after lining up.")]
        [SerializeField] private Transform safeZoneWaypoint;

        [Tooltip("Spots at the safe zone where NPCs form a line and crouch. " +
                 "One per NPC — works like line-up spots. If empty, NPCs just walk to safeZoneWaypoint.")]
        [SerializeField] private Transform[] safeZoneSpots;

        [Header("Player Gate")]
        [Tooltip("If true, NPCs wait at the line-up spots until the player is close enough.")]
        [SerializeField] private bool requiresPlayer;

        [Tooltip("How close (meters) the player must be before evacuation begins. " +
                 "Only used when Requires Player is checked.")]
        [SerializeField] private float playerTriggerDistance = 3f;

        [Tooltip("The player's Transform. Auto-found via 'Player' tag if left empty.")]
        [SerializeField] private Transform playerTransform;

        [Header("Auto-Trigger")]
        [Tooltip("If true, automatically begins evacuation when the earthquake ends.")]
        [SerializeField] private bool autoTriggerOnEarthquakeEnd = true;

        [Tooltip("Seconds to wait after the earthquake ends before commanding NPCs to line up. " +
                 "Gives NPCs time to finish their exit cover animation.")]
        [SerializeField] private float postEarthquakeDelay = 5f;

        [Header("Stagger")]
        [Tooltip("Seconds between each NPC's departure. Prevents them from " +
                 "bunching up at doorways. Set to 0 for simultaneous.")]
        [SerializeField] private float staggerDelay = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;
        public EvacuationState CurrentState => m_State;
        public bool IsComplete => m_State == EvacuationState.Done;
        private EvacuationState m_State = EvacuationState.Idle;
        private int m_ArrivedCount;
        private HashSet<NPCController> m_ArrivedSet = new HashSet<NPCController>();
        private Coroutine m_DelayedEvacRoutine;

        private void Start()
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    playerTransform = playerObj.transform;
            }

            ValidateSetup();
        }

        private void OnEnable()
        {
            if (autoTriggerOnEarthquakeEnd)
                EarthquakeEvents.OnEarthquakeStop += HandleEarthquakeStop;
        }

        private void OnDisable()
        {
            if (autoTriggerOnEarthquakeEnd)
                EarthquakeEvents.OnEarthquakeStop -= HandleEarthquakeStop;
        }

        private void HandleEarthquakeStop()
        {
            if (m_State != EvacuationState.Idle) return;

            if (m_DelayedEvacRoutine != null)
                StopCoroutine(m_DelayedEvacRoutine);

            m_DelayedEvacRoutine = StartCoroutine(DelayedBeginEvacuation());
        }

        private IEnumerator DelayedBeginEvacuation()
        {
            if (debugMode) Debug.Log($"[RoomEvac] Earthquake ended — waiting {postEarthquakeDelay}s for NPCs to exit cover...");

            yield return new WaitForSeconds(postEarthquakeDelay);

            if (debugMode) Debug.Log("[RoomEvac] Delay complete — beginning evacuation.");
            BeginEvacuation();

            m_DelayedEvacRoutine = null;
        }

        private void Update()
        {
            // The only state that needs per-frame work is WaitingForPlayer.
            if (m_State != EvacuationState.WaitingForPlayer) return;

            if (playerTransform == null)
            {
                if (debugMode) Debug.LogWarning("[RoomEvac] requiresPlayer is true but no player Transform found.");
                return;
            }

            // Check if the player is close to any line-up spot (where the NPCs are standing).
            float closestDist = float.MaxValue;
            for (int i = 0; i < lineUpSpots.Length; i++)
            {
                if (lineUpSpots[i] == null) continue;
                float d = Vector3.Distance(playerTransform.position, lineUpSpots[i].position);
                if (d < closestDist) closestDist = d;
            }

            if (closestDist <= playerTriggerDistance)
            {
                if (debugMode) Debug.Log($"[RoomEvac] Player is within {playerTriggerDistance}m of the line — starting evacuation.");
                BeginEvacuationPhase();
            }
        }

        public void BeginEvacuation()
        {
            if (m_State != EvacuationState.Idle)
            {
                if (debugMode) Debug.LogWarning("[RoomEvac] BeginEvacuation ignored — not in Idle state.");
                return;
            }

            if (assignedNPCs == null || assignedNPCs.Length == 0)
            {
                Debug.LogError("[RoomEvac] No NPCs assigned! Aborting.");
                return;
            }

            if (lineUpSpots == null || lineUpSpots.Length == 0)
            {
                Debug.LogError("[RoomEvac] No line-up spots assigned! Aborting.");
                return;
            }

            m_State = EvacuationState.LiningUp;
            m_ArrivedCount = 0;
            m_ArrivedSet.Clear();

            // Gate the behavior tree's idle branch so it doesn't fight us.
            SetEvacuatingOnAllNPCs(true);

            if (debugMode) Debug.Log($"[RoomEvac] {gameObject.name}: Lining up {assignedNPCs.Length} NPCs.");

            StartCoroutine(StaggeredLineUp());
        }

        private IEnumerator StaggeredLineUp()
        {
            // ── Build nearest-available assignments first ──
            HashSet<int> claimedSpots = new HashSet<int>();
            List<(NPCController npc, Transform spot, int spotIndex)> assignments
                = new List<(NPCController, Transform, int)>();

            for (int i = 0; i < assignedNPCs.Length; i++)
            {
                NPCController npc = assignedNPCs[i];
                if (npc == null) continue;

                int bestSpot = -1;
                float bestDist = float.MaxValue;

                for (int s = 0; s < lineUpSpots.Length; s++)
                {
                    if (lineUpSpots[s] == null) continue;
                    if (claimedSpots.Contains(s)) continue;

                    float d = Vector3.Distance(npc.transform.position, lineUpSpots[s].position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestSpot = s;
                    }
                }

                if (bestSpot < 0) bestSpot = lineUpSpots.Length - 1;
                claimedSpots.Add(bestSpot);
                assignments.Add((npc, lineUpSpots[bestSpot], bestSpot));
            }

            for (int i = 0; i < assignments.Count; i++)
            {
                var (npc, spot, spotIndex) = assignments[i];

                npc.OnDestinationReached += OnNPCReachedLineUp;
                npc.CommandMoveTo(spot);

                if (debugMode) Debug.Log($"[RoomEvac]   {npc.gameObject.name} → Spot {spotIndex} ('{spot.name}')");

                // Wait before sending the next NPC (skip delay after the last one).
                if (staggerDelay > 0f && i < assignments.Count - 1)
                    yield return new WaitForSeconds(staggerDelay);
            }
        }

        private void OnNPCReachedLineUp(NPCController npc)
        {
            // Guard against double-counting.
            if (!m_ArrivedSet.Add(npc)) return;

            // Unsubscribe so this callback doesn't fire again for this phase.
            npc.OnDestinationReached -= OnNPCReachedLineUp;

            m_ArrivedCount++;
            if (debugMode) Debug.Log($"[RoomEvac] {npc.gameObject.name} lined up ({m_ArrivedCount}/{assignedNPCs.Length}).");

            // Check if everyone is lined up.
            if (m_ArrivedCount >= CountValidNPCs())
            {
                if (debugMode) Debug.Log("[RoomEvac] All NPCs lined up.");
                OnAllNPCsLinedUp();
            }
        }

        private void OnNPCReachedSafeZone(NPCController npc)
        {
            if (!m_ArrivedSet.Add(npc)) return;

            npc.OnDestinationReached -= OnNPCReachedSafeZone;

            // Crouch at the safe zone spot.
            npc.SetCrouch(true);

            m_ArrivedCount++;
            if (debugMode) Debug.Log($"[RoomEvac] {npc.gameObject.name} reached safe zone and crouched ({m_ArrivedCount}/{assignedNPCs.Length}).");

            if (m_ArrivedCount >= CountValidNPCs())
            {
                m_State = EvacuationState.Done;

                // Release the behavior tree gate.
                SetEvacuatingOnAllNPCs(false);

                if (debugMode) Debug.Log($"[RoomEvac] {gameObject.name}: Evacuation COMPLETE — all NPCs crouched at safe zone.");
            }
        }

        private void OnAllNPCsLinedUp()
        {
            if (requiresPlayer)
            {
                // Wait for the player to approach before evacuating.
                m_State = EvacuationState.WaitingForPlayer;
                if (debugMode) Debug.Log("[RoomEvac] Waiting for player to approach...");
            }
            else
            {
                // No player gate — evacuate immediately.
                BeginEvacuationPhase();
            }
        }

        private void BeginEvacuationPhase()
        {
            if (safeZoneWaypoint == null && (safeZoneSpots == null || safeZoneSpots.Length == 0))
            {
                Debug.LogError("[RoomEvac] No safe zone destination set! Cannot evacuate.");
                return;
            }

            m_State = EvacuationState.Evacuating;
            m_ArrivedCount = 0;
            m_ArrivedSet.Clear();

            if (debugMode) Debug.Log($"[RoomEvac] Evacuating {assignedNPCs.Length} NPCs to safe zone (walking).");

            // If we have safe zone spots, use nearest-available assignment.
            if (safeZoneSpots != null && safeZoneSpots.Length > 0)
            {
                StartCoroutine(StaggeredEvacuation());
            }
            else
            {
                // all NPCs walk to the single safeZoneWaypoint.
                for (int i = 0; i < assignedNPCs.Length; i++)
                {
                    NPCController npc = assignedNPCs[i];
                    if (npc == null) continue;

                    npc.OnDestinationReached += OnNPCReachedSafeZone;
                    npc.CommandMoveTo(safeZoneWaypoint); 
                }
            }
        }

        private IEnumerator StaggeredEvacuation()
        {
            // Build nearest-available assignments.
            HashSet<int> claimedSpots = new HashSet<int>();
            List<(NPCController npc, Transform spot, int spotIndex)> assignments
                = new List<(NPCController, Transform, int)>();

            for (int i = 0; i < assignedNPCs.Length; i++)
            {
                NPCController npc = assignedNPCs[i];
                if (npc == null) continue;

                int bestSpot = -1;
                float bestDist = float.MaxValue;

                for (int s = 0; s < safeZoneSpots.Length; s++)
                {
                    if (safeZoneSpots[s] == null) continue;
                    if (claimedSpots.Contains(s)) continue;

                    float d = Vector3.Distance(npc.transform.position, safeZoneSpots[s].position);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestSpot = s;
                    }
                }

                if (bestSpot < 0) bestSpot = safeZoneSpots.Length - 1;
                claimedSpots.Add(bestSpot);
                assignments.Add((npc, safeZoneSpots[bestSpot], bestSpot));
            }

            // Send NPCs one by one.
            for (int i = 0; i < assignments.Count; i++)
            {
                var (npc, spot, spotIndex) = assignments[i];

                npc.OnDestinationReached += OnNPCReachedSafeZone;
                npc.CommandMoveTo(spot); 

                if (debugMode) Debug.Log($"[RoomEvac]   {npc.gameObject.name} → SafeSpot {spotIndex} ('{spot.name}')");

                if (staggerDelay > 0f && i < assignments.Count - 1)
                    yield return new WaitForSeconds(staggerDelay);
            }
        }

        private int CountValidNPCs()
        {
            int count = 0;
            for (int i = 0; i < assignedNPCs.Length; i++)
            {
                if (assignedNPCs[i] != null) count++;
            }
            return count;
        }

        /// <summary>
        /// Sets or clears the IsEvacuating flag on every assigned NPC's
        /// <see cref="NPCBehaviorBridge"/>, gating the behavior tree's
        /// idle branch during evacuation.
        /// </summary>
        private void SetEvacuatingOnAllNPCs(bool evacuating)
        {
            for (int i = 0; i < assignedNPCs.Length; i++)
            {
                if (assignedNPCs[i] == null) continue;

                NPCBehaviorBridge bridge =
                    assignedNPCs[i].GetComponent<NPCBehaviorBridge>();

                if (bridge != null)
                    bridge.SetEvacuating(evacuating);
            }

            if (debugMode) Debug.Log($"[RoomEvac] Set IsEvacuating = {evacuating} on all NPCs.");
        }

        private void ValidateSetup()
        {
            if (assignedNPCs == null || assignedNPCs.Length == 0)
                Debug.LogWarning($"[RoomEvac] {gameObject.name}: No NPCs assigned.", this);

            if (lineUpSpots == null || lineUpSpots.Length == 0)
                Debug.LogWarning($"[RoomEvac] {gameObject.name}: No line-up spots assigned.", this);

            if (safeZoneWaypoint == null)
                Debug.LogWarning($"[RoomEvac] {gameObject.name}: safeZoneWaypoint is not set.", this);

            if (requiresPlayer && playerTransform == null)
                Debug.LogWarning($"[RoomEvac] {gameObject.name}: requiresPlayer is true but no player found. " +
                                 "Tag your player 'Player' or assign the field manually.", this);

            if (assignedNPCs != null && lineUpSpots != null && assignedNPCs.Length > lineUpSpots.Length)
                Debug.LogWarning($"[RoomEvac] {gameObject.name}: More NPCs ({assignedNPCs.Length}) than " +
                                 $"line-up spots ({lineUpSpots.Length}). Extra NPCs will share the last spot.", this);
        }

        //  Gizmos
        private void OnDrawGizmosSelected()
        {
            // Draw the player trigger radius.
            if (requiresPlayer)
            {
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
                Gizmos.DrawWireSphere(transform.position, playerTriggerDistance);
            }

            // Draw lines from coordinator to each line-up spot.
            if (lineUpSpots != null)
            {
                Gizmos.color = Color.yellow;
                foreach (Transform spot in lineUpSpots)
                {
                    if (spot != null)
                    {
                        Gizmos.DrawWireSphere(spot.position, 0.3f);
                        Gizmos.DrawLine(transform.position, spot.position);
                    }
                }
            }

            // Draw line from coordinator to safe zone.
            if (safeZoneWaypoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(safeZoneWaypoint.position, 0.5f);
                Gizmos.DrawLine(transform.position, safeZoneWaypoint.position);
            }
        }
    }
}
