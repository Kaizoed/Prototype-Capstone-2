using UnityEngine;

public class SafeAreaTrigger : MonoBehaviour
{
    [SerializeField] private string requiredQuestStepId = "reach_safe_area";
    [SerializeField] private GameObject endGamePanel;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        if (QuestManager.Instance == null) return;
        if (QuestManager.Instance.CurrentStepId != requiredQuestStepId) return;

        triggered = true;

        QuestManager.Instance.CompleteStep(requiredQuestStepId);

        if (endGamePanel != null)
            endGamePanel.SetActive(true);

        Time.timeScale = 0f;
    }
}