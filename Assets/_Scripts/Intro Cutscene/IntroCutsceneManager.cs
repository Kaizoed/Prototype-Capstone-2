using ShakySurvival.Camera;
using ShakySurvival.Earthquake;
using ShakySurvival.Player;
using System.Collections;
using TMPro;
using UnityEngine;

public class IntroCutsceneManager : MonoBehaviour
{
    [Header("Player Control")]
    [SerializeField] private PlayerMovement playerMovementScript;
    [SerializeField] private PlayerLook playerLookScript;

    [Header("Player References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private CameraStabilizer cameraStabilizer;

    [Header("Teacher Target")]
    [SerializeField] private Transform teacherLookTarget;
    [SerializeField] private float rotateDuration = 2f;

    [Header("Camera Height")]
    [SerializeField] private bool forceSeatedAtStart = true;
    [SerializeField] private Vector3 seatedLocalPosition = new Vector3(0f, 0.45f, 0.147f);
    [SerializeField] private Vector3 standingLocalPosition = new Vector3(0f, 0.6867536f, 0.147f);
    [SerializeField] private float standUpDuration = 0.75f;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string[] introLinesPart1;
    [SerializeField] private string[] introLinesPart2;
    [SerializeField] private float lineDuration = 2.5f;

    [Header("Earthquake Trigger")]
    [SerializeField] private EarthquakeManager earthquakeManager;
    [SerializeField] private int earthquakeTriggerLineIndex = 1;
    [SerializeField] private bool earthquakeTriggered = false;

    [Header("Go Bag")]
    [SerializeField] private bool allowMovementDuringGoBag = true;

    [Header("Classroom NPCs")]
    [SerializeField] private ClassroomNPCManager classroomNPCManager;

    private void Start()
    {
        StartCoroutine(PlayIntroScene());
    }

    private IEnumerator PlayIntroScene()
    {
        if (forceSeatedAtStart && cameraRoot != null)
        {
            cameraRoot.localPosition = seatedLocalPosition;
            cameraRoot.localRotation = Quaternion.identity;
        }

        // Full lock during intro cutscene
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (playerLookScript != null)
        {
            playerLookScript.LockLook();
            playerLookScript.enabled = false;
        }

        if (cameraStabilizer != null)
        {
            cameraStabilizer.CinematicMode = true;
            cameraStabilizer.SnapToTarget();
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        yield return StartCoroutine(RotatePlayerToTeacher());

        // PART 1 DIALOGUE
        if (dialogueText != null && introLinesPart1 != null && introLinesPart1.Length > 0)
        {
            for (int i = 0; i < introLinesPart1.Length; i++)
            {
                dialogueText.text = introLinesPart1[i];
                yield return new WaitForSeconds(lineDuration);
            }
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (cameraRoot != null)
            yield return StartCoroutine(MoveCameraRoot(standingLocalPosition, standUpDuration));

        if (cameraStabilizer != null)
        {
            cameraStabilizer.CinematicMode = false;
            cameraStabilizer.SnapToTarget();
        }

        // Go Bag phase
        if (playerLookScript != null)
        {
            playerLookScript.enabled = true;
            playerLookScript.UnlockLook();
            playerLookScript.LockCursor();
        }

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;

            if (allowMovementDuringGoBag)
                playerMovementScript.UnlockMovementOnly();
            else
                playerMovementScript.LockMovementOnly();
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetStep(GameFlowManager.GameStep.GoBag);
        }

        if (GoBagUIManager.Instance != null)
        {
            GoBagUIManager.Instance.ShowGoBagPanel(true);
        }
    }

    public void ContinueDialoguePart2()
    {
        StartCoroutine(PlayDialoguePart2());
    }

    private IEnumerator PlayDialoguePart2()
    {
        // Full lock again for dialogue
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (playerLookScript != null)
        {
            playerLookScript.LockLook();
            playerLookScript.enabled = false;
        }

        // Put camera back into cinematic mode
        if (cameraStabilizer != null)
        {
            cameraStabilizer.CinematicMode = true;
            cameraStabilizer.SnapToTarget();
        }

        // IMPORTANT: reset camera local look rotation
        if (cameraRoot != null)
        {
            cameraRoot.localRotation = Quaternion.identity;
        }

        // Rotate player back to teacher
        yield return StartCoroutine(RotatePlayerToTeacher());

        if (cameraStabilizer != null)
            cameraStabilizer.SnapToTarget();

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetStep(GameFlowManager.GameStep.DialoguePart2);
        }

        // PART 2 DIALOGUE
        if (dialogueText != null && introLinesPart2 != null && introLinesPart2.Length > 0)
        {
            for (int i = 0; i < introLinesPart2.Length; i++)
            {
                dialogueText.text = introLinesPart2[i];

                if (!earthquakeTriggered && i == earthquakeTriggerLineIndex)
                {
                    TriggerEarthquakeResponse();
                }

                yield return new WaitForSeconds(lineDuration);
            }
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Back to gameplay camera
        if (cameraStabilizer != null)
        {
            cameraStabilizer.CinematicMode = false;
            cameraStabilizer.SnapToTarget();
        }

        // Re-enable player system, but lock movement only
        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
            playerMovementScript.LockMovementOnly();
        }

        // Allow look so player can crouch + find table
        if (playerLookScript != null)
        {
            playerLookScript.enabled = true;
            playerLookScript.UnlockLook();
            playerLookScript.LockCursor();
        }

    }

    private void TriggerEarthquakeResponse()
    {
        earthquakeTriggered = true;

        Debug.Log("Earthquake triggered during Dialogue Part 2.");

        // IMPORTANT: let NPCs exit intro freeze first
        if (classroomNPCManager != null)
        {
            classroomNPCManager.EndIntroForAllNPCs();
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.SetStep(GameFlowManager.GameStep.EarthquakeResponse);
        }

        if (earthquakeManager != null)
        {
            earthquakeManager.StartEarthquake();
        }
        else
        {
            Debug.LogWarning("EarthquakeManager is not assigned in IntroCutsceneManager.");
        }

    }

    private IEnumerator RotatePlayerToTeacher()
    {
        if (playerTransform == null || teacherLookTarget == null)
            yield break;

        Vector3 direction = teacherLookTarget.position - playerTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            yield break;

        Quaternion startRotation = playerTransform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        float elapsed = 0f;

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateDuration;
            playerTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            if (cameraStabilizer != null)
                cameraStabilizer.SnapToTarget();

            yield return null;
        }

        playerTransform.rotation = targetRotation;

        if (cameraStabilizer != null)
            cameraStabilizer.SnapToTarget();
    }

    private IEnumerator MoveCameraRoot(Vector3 targetLocalPos, float duration)
    {
        Vector3 startPos = cameraRoot.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cameraRoot.localPosition = Vector3.Lerp(startPos, targetLocalPos, t);

            if (cameraStabilizer != null)
                cameraStabilizer.SnapToTarget();

            yield return null;
        }

        cameraRoot.localPosition = targetLocalPos;

        if (cameraStabilizer != null)
            cameraStabilizer.SnapToTarget();
    }
}