using UnityEngine;
using ShakySurvival.Player;

public class SafeAreaTrigger : MonoBehaviour
{
    [SerializeField] private string requiredQuestStepId = "reach_safe_area";
    [SerializeField] private GameObject endGamePanel;

    [Header("Player Control")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private PlayerLook playerLookScript;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        if (QuestManager.Instance == null) return;
        if (QuestManager.Instance.CurrentStepId != requiredQuestStepId) return;

        triggered = true;

        QuestManager.Instance.CompleteStep(requiredQuestStepId);

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