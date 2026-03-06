using UnityEngine;
using ShakySurvival.Earthquake;

public class EvacuationMessageTrigger : MonoBehaviour
{
    [SerializeField] private TopMessageUI topMessageUI;
    [SerializeField] private string evacuationMessage = "Evacuate. Head outside to the parking lot.";
    [SerializeField] private float messageDuration = 5f;

    private void OnEnable()
    {
        EarthquakeEvents.OnEarthquakeStop += HandleEarthquakeStop;
    }

    private void OnDisable()
    {
        EarthquakeEvents.OnEarthquakeStop -= HandleEarthquakeStop;
    }

    private void HandleEarthquakeStop()
    {
        Debug.Log("Earthquake ended -> showing evacuation message");

        if (topMessageUI != null)
        {
            topMessageUI.ShowMessageForSeconds(evacuationMessage, messageDuration);
        }
        else
        {
            Debug.LogWarning("TopMessageUI is not assigned.");
        }
    }
}