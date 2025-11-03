using UnityEngine;
using System.Collections;

public class EnvironmentShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private Transform environmentRoot;
    [SerializeField] private float duration = 5f;
    [SerializeField] private float horizontalMagnitude = 0.3f;
    [SerializeField] private float rotationMagnitude = 0.8f;
    [SerializeField] private float frequency = 10f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Start()
    {
        // Temporary trigger for now for demo purposes
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        originalPosition = environmentRoot.localPosition;
        originalRotation = environmentRoot.localRotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Mathf.Sin(Time.time * frequency) * horizontalMagnitude;
            float z = Mathf.Cos(Time.time * (frequency * 0.9f)) * horizontalMagnitude * 0.7f;

            environmentRoot.localPosition = originalPosition + new Vector3(x, 0f, z);

            float rotX = Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f;
            float rotZ = Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f;
            environmentRoot.localRotation = Quaternion.Euler(rotX * rotationMagnitude, 0f, rotZ * rotationMagnitude);

            elapsed += Time.deltaTime;
            yield return null;
        }

        environmentRoot.localPosition = originalPosition;
        environmentRoot.localRotation = originalRotation;
    }
}
