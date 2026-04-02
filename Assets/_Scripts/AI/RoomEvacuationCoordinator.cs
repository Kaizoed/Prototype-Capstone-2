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
        [SerializeField] private NPCController[] assignedNPCs;

        [Header("Line-Up Configuration")]
        [SerializeField] private Transform[] lineUpSpots;

        [Header("Evacuation Destination")]
        [SerializeField] private Transform safeZoneWaypoint;
        [SerializeField] private Transform[] safeZoneSpots;

        [Header("Player Gate")]
        [SerializeField] private bool requiresPlayer;
        [SerializeField] private float playerTriggerDistance = 3f;
        [SerializeField] private Transform playerTransform;

        [Header("Auto-Trigger")]
        [SerializeField] private bool autoTriggerOnEarthquakeEnd = true;
        [SerializeField] private float postEarthquakeDelay = 5f;

        [Header("Stagger")]
        [SerializeField] private float staggerDelay = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        public EvacuationState CurrentState => m_State;
        public bool IsComplete => m_State == EvacuationState.Done;

        private EvacuationState m_State = EvacuationState.Idle;
        private int m_ArrivedCount;
        private HashSet<NPCController> m_ArrivedSet = new HashSet<NPCController>();
        private Coroutine m_DelayedEvacRoutine;

        // ✅ NEW FLAGS (prevents duplicate objective triggers)
        private bool lineUpCompleted;

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
            yield return new WaitForSeconds(postEarthquakeDelay);
            BeginEvacuation();
            m_DelayedEvacRoutine = null;
        }

        private void Update()
        {
            if (m_State != EvacuationState.WaitingForPlayer) return;

            if (playerTransform == null) return;

            float closestDist = float.MaxValue;

            for (int i = 0; i < lineUpSpots.Length; i++)
            {
                if (lineUpSpots[i] == null) continue;
                float d = Vector3.Distance(playerTransform.position, lineUpSpots[i].position);
                if (d < closestDist) closestDist = d;
            }

            if (closestDist <= playerTriggerDistance)
            {
                BeginEvacuationPhase();
            }
        }

        public void StartEvacuationNow()
        {
            if (m_DelayedEvacRoutine != null)
            {
                StopCoroutine(m_DelayedEvacRoutine);
                m_DelayedEvacRoutine = null;
            }

            BeginEvacuation();
        }

        public void BeginEvacuation()
        {
            if (m_State != EvacuationState.Idle) return;

            m_State = EvacuationState.LiningUp;
            m_ArrivedCount = 0;
            m_ArrivedSet.Clear();

            SetEvacuatingOnAllNPCs(true);
            StartCoroutine(StaggeredLineUp());
        }

        private IEnumerator StaggeredLineUp()
        {
            for (int i = 0; i < assignedNPCs.Length; i++)
            {
                NPCController npc = assignedNPCs[i];
                if (npc == null) continue;

                npc.OnDestinationReached += OnNPCReachedLineUp;
                npc.CommandMoveTo(lineUpSpots[i]);

                if (staggerDelay > 0f && i < assignedNPCs.Length - 1)
                    yield return new WaitForSeconds(staggerDelay);
            }
        }

        private void OnNPCReachedLineUp(NPCController npc)
        {
            if (!m_ArrivedSet.Add(npc)) return;

            npc.OnDestinationReached -= OnNPCReachedLineUp;

            m_ArrivedCount++;

            if (m_ArrivedCount >= CountValidNPCs())
            {
                OnAllNPCsLinedUp();
            }
        }

        private void OnAllNPCsLinedUp()
        {
            // ✅ COMPLETE LINE UP OBJECTIVE
            if (!lineUpCompleted)
            {
                lineUpCompleted = true;

                if (TutorialObjectiveUI.Instance != null)
                {
                    TutorialObjectiveUI.Instance.CompleteObjective("line_up");
                }
            }

            if (requiresPlayer)
            {
                m_State = EvacuationState.WaitingForPlayer;
            }
            else
            {
                BeginEvacuationPhase();
            }
        }

        private void BeginEvacuationPhase()
        {
            m_State = EvacuationState.Evacuating;
            m_ArrivedCount = 0;
            m_ArrivedSet.Clear();

            for (int i = 0; i < assignedNPCs.Length; i++)
            {
                NPCController npc = assignedNPCs[i];
                if (npc == null) continue;

                npc.OnDestinationReached += OnNPCReachedSafeZone;
                npc.CommandMoveTo(safeZoneWaypoint);
            }
        }

        private void OnNPCReachedSafeZone(NPCController npc)
        {
            if (!m_ArrivedSet.Add(npc)) return;

            npc.OnDestinationReached -= OnNPCReachedSafeZone;

            npc.SetCrouch(true);

            m_ArrivedCount++;

            if (m_ArrivedCount >= CountValidNPCs())
            {
                m_State = EvacuationState.Done;
                SetEvacuatingOnAllNPCs(false);

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
        }

        private void ValidateSetup()
        {
            // (kept as-is)
        }
    }
}