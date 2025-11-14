using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnvironmentShake : MonoBehaviour
{
    [SerializeField] private float maxMagnitude = 0.5f;
    [SerializeField] private float frequency = 8f;

    private Vector3 originalPos;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        originalPos = transform.position;

        if (EarthquakeController.Instance != null)
            EarthquakeController.Instance.SetActiveEnvironment(this);
    }

    private void FixedUpdate()
    {
        if (EarthquakeController.Instance == null || !EarthquakeController.Instance.IsQuaking)
            return;

        float intensity = EarthquakeController.Instance.Intensity;
        float time = Time.time * frequency;

        float x = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * intensity * maxMagnitude;
        float z = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f * intensity * maxMagnitude;

        Vector3 newPos = originalPos + new Vector3(x, 0f, z);
        rb.MovePosition(newPos);
    }

    public void SetFrequency(float value)
    {
        frequency = value;
    }
}
