using System.Collections;
using UnityEngine;
using TMPro;
using ShakySurvival.Camera;
using ShakySurvival.Player;

public class IntroCutsceneManager : MonoBehaviour
{
    [Header("Player Control")]
    [SerializeField] private MonoBehaviour playerMovementScript;
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
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string[] introLines;
    [SerializeField] private float lineDuration = 2.5f;

    [Header("Tutorial")]
    [SerializeField] private TutorialManager tutorialManager;

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

        if (questPanel != null)
            questPanel.SetActive(false);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        yield return StartCoroutine(RotatePlayerToTeacher());

        if (dialogueText != null && introLines != null && introLines.Length > 0)
        {
            for (int i = 0; i < introLines.Length; i++)
            {
                dialogueText.text = introLines[i];
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

        if (playerMovementScript != null)
            playerMovementScript.enabled = true;

        if (playerLookScript != null)
        {
            playerLookScript.enabled = true;
            playerLookScript.UnlockLook();
            playerLookScript.LockCursor();
        }

        if (tutorialManager != null)
        {
            Debug.Log("[IntroCutsceneManager] Calling StartTutorial on object: " + tutorialManager.gameObject.name);
            Debug.Log("[IntroCutsceneManager] TutorialManager enabled: " + tutorialManager.enabled);
            Debug.Log("[IntroCutsceneManager] TutorialManager activeInHierarchy: " + tutorialManager.gameObject.activeInHierarchy);
            tutorialManager.StartTutorial();
        }
        else
        {
            Debug.LogWarning("[IntroCutsceneManager] tutorialManager is NULL");
        }

        if (classroomNPCManager != null)
        {
            classroomNPCManager.EndIntroForAllNPCs();
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