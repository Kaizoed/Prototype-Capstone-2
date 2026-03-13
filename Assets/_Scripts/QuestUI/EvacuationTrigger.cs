using UnityEngine;

public class EvacuationTrigger : MonoBehaviour
{
    [SerializeField] private string questStepId = "evacuate_classroom";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.CompleteStep(questStepId);
        }

        gameObject.SetActive(false);
    }
}