using UnityEngine;
using ShakySurvival.Player;

public class SafeAreaTrigger : MonoBehaviour
{
    [SerializeField] private GameObject endGamePanel;

    [Header("Player Control")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private PlayerLook playerLookScript;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        if (GameFlowManager.Instance == null) return;
        if (GameFlowManager.Instance.currentStep != GameFlowManager.GameStep.Evacuate) return;

        triggered = true;
        GameFlowManager.Instance.SetStep(GameFlowManager.GameStep.End);

        // Disable player controls
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (playerLookScript != null)
            playerLookScript.enabled = false;

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show ending screen
        if (endGamePanel != null)
            endGamePanel.SetActive(true);

        Time.timeScale = 0f;
    }
}