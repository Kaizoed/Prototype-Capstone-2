using UnityEngine;

public class TutorialDeskTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.CurrentStep == 3)
        {
            TutorialManager.Instance.CompleteStep();
        }
    }
}
