using UnityEngine;
using TMPro;
using System.Collections;

public class EarthquakeSequence : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI magnitudeText;

    [Header("Earthquake Settings")]
    [SerializeField] private float[] magnitudes = new float[] { 3.5f, 5.0f, 7.0f };
    [SerializeField] private float[] intensities = new float[] { 0.3f, 0.6f, 1.0f };
    [SerializeField] private float[] frequencies = new float[] { 8f, 12f, 18f };

    [Header("Timing")]
    [SerializeField] private float quakeDuration = 6f;
    [SerializeField] private float interval = 3f;

    private EarthquakeController controller;

    private void Start()
    {
        controller = EarthquakeController.Instance;
        StartCoroutine(RunEarthquakeDemo());
    }

    private IEnumerator RunEarthquakeDemo()
    {
        for (int i = 0; i < magnitudes.Length; i++)
        {
            if (magnitudeText != null)
                magnitudeText.text = "Magnitude: " + magnitudes[i].ToString("0.0");

            if (controller.ActiveEnvironment != null)
                controller.ActiveEnvironment.SetFrequency(frequencies[i]);

            controller.StartQuake(intensities[i]);

            yield return new WaitForSeconds(quakeDuration);

            controller.StopQuake();

            magnitudeText.text = "";
            yield return new WaitForSeconds(interval);
        }
    }
}