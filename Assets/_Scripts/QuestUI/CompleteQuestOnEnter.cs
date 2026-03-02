using UnityEngine;

public class CompleteQuestOnEnter : MonoBehaviour
{
    [SerializeField] private string stepId;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        QuestManager.Instance.CompleteStep(stepId);
        gameObject.SetActive(false);
    }
}