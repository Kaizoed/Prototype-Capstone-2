using UnityEngine;
using ShakySurvival.Earthquake;

public class DoorframeEarthquakeTrigger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool useCustomMagnitude = false;
    [SerializeField] private float customMagnitude = 6f;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered) return;

        if (!other.CompareTag("Player")) return;

        hasTriggered = true;

        if (EarthquakeManager.Instance == null)
        {
            Debug.LogWarning("EarthquakeManager not found!");
            return;
        }

        if (useCustomMagnitude)
            EarthquakeManager.Instance.StartEarthquake(customMagnitude);
        else
            EarthquakeManager.Instance.StartEarthquake();

        Debug.Log("Earthquake triggered at doorframe.");
    }
}