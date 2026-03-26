using UnityEngine;

public class EvacuationTrigger : MonoBehaviour
{
    [Header("Optional Next Step")]
    [SerializeField] private bool advanceToEnd = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameFlowManager.Instance == null) return;

        if (GameFlowManager.Instance.currentStep != GameFlowManager.GameStep.Evacuate)
            return;

        Debug.Log("Player reached evacuation trigger.");

        if (advanceToEnd)
        {
            GameFlowManager.Instance.SetStep(GameFlowManager.GameStep.End);
        }

        gameObject.SetActive(false);
    }
}