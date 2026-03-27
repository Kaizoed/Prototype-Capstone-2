using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using ShakySurvival.Player;
using ShakySurvival.AI;

public class GuardEvacuationManager : MonoBehaviour
{
    [Header("Guard")]
    [SerializeField] private NavMeshAgent guardAgent;
    [SerializeField] private Transform guardDestination;
    [SerializeField] private Transform guardLookTarget;

    [Header("Guard AI Override")]
    [SerializeField] private MonoBehaviour guardAIMovementScript;
    [SerializeField] private NPCEarthquakeReaction guardEarthquakeReaction;

    [Header("Door")]
    [SerializeField] private DoorInteractable doorToOpen;

    [Header("Player")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLook playerLook;

    [Header("Classroom NPCs")]
    [SerializeField] private ClassroomNPCManager classroomNPCManager;
    public ClassroomNPCManager ClassroomNPCManager => classroomNPCManager;

    [Header("Student Evacuation")]
    [SerializeField] private RoomEvacuationCoordinator roomEvacuationCoordinator;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private string guardLine = "Stay calm. It is now safe to evacuate in an orderly manner.";

    [Header("Timing")]
    [SerializeField] private float waitAfterArrival = 1f;
    [SerializeField] private float dialogueDuration = 3f;

    private bool isPlaying;

    public void StartGuardCutscene()
    {
        if (isPlaying) return;
        StartCoroutine(PlayGuardSequence());
    }

    private IEnumerator PlayGuardSequence()
    {
        isPlaying = true;

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.SetStep(GameFlowManager.GameStep.GuardEvacuationCutscene);

        if (guardAIMovementScript != null)
            guardAIMovementScript.enabled = false;

        if (guardEarthquakeReaction != null)
            guardEarthquakeReaction.SetEarthquakeReaction(false);

        if (playerMovement != null)
            playerMovement.LockMovementOnly();

        if (playerLook != null)
            playerLook.LockLook();

        if (guardAgent != null && guardDestination != null)
        {
            guardAgent.isStopped = false;
            guardAgent.ResetPath();
            guardAgent.SetDestination(guardDestination.position);

            while (true)
            {
                if (!guardAgent.pathPending)
                {
                    if (guardAgent.remainingDistance <= guardAgent.stoppingDistance + 0.05f)
                    {
                        if (!guardAgent.hasPath || guardAgent.velocity.sqrMagnitude < 0.01f)
                            break;
                    }
                }

                yield return null;
            }

            guardAgent.isStopped = true;
            guardAgent.velocity = Vector3.zero;
            guardAgent.ResetPath();
        }

        if (doorToOpen != null && guardAgent != null)
        {
            doorToOpen.OpenDoorFrom(guardAgent.gameObject);
        }

        if (guardAgent != null && guardLookTarget != null)
        {
            Vector3 direction = guardLookTarget.position - guardAgent.transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                guardAgent.transform.rotation = Quaternion.LookRotation(direction.normalized);
            }
        }

        yield return new WaitForSeconds(waitAfterArrival);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (dialogueText != null)
            dialogueText.text = guardLine;

        yield return new WaitForSeconds(dialogueDuration);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Start student evacuation
        if (roomEvacuationCoordinator != null)
        {
            roomEvacuationCoordinator.StartEvacuationNow();
        }

        // Start teacher evacuation
        if (classroomNPCManager != null)
        {
            classroomNPCManager.StartTeacherEvacuation();
        }

        // Unlock player after guard says it's safe
        if (playerMovement != null)
            playerMovement.UnlockMovementOnly();

        if (playerLook != null)
        {
            playerLook.UnlockLook();
            playerLook.LockCursor();
        }

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.SetStep(GameFlowManager.GameStep.Evacuate);

        isPlaying = false;
    }
}