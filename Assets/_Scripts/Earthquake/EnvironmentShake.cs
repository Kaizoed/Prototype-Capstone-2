using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnvironmentShake : MonoBehaviour
{
    [SerializeField] private float maxMagnitude = 0.5f;
    [SerializeField] private float frequency = 8f;

    private Vector3 originalLocalPos;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Use LOCAL position so parent motion doesn't break it
        originalLocalPos = transform.localPosition;

        if (EarthquakeController.Instance != null)
            EarthquakeController.Instance.SetActiveEnvironment(this);
    }

    private void FixedUpdate()
    {
        // Always hold the base position if not quaking
        if (EarthquakeController.Instance == null || !EarthquakeController.Instance.IsQuaking)
        {
            rb.MovePosition(transform.parent
                ? transform.parent.TransformPoint(originalLocalPos)
                : originalLocalPos);
            return;
        }

        float intensity = EarthquakeController.Instance.Intensity;
        float t = Time.time * frequency;

        float x = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * intensity * maxMagnitude;
        float z = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * intensity * maxMagnitude;

        Vector3 localOffset = new Vector3(x, 0f, z);
        Vector3 targetWorldPos = transform.parent
            ? transform.parent.TransformPoint(originalLocalPos + localOffset)
            : (originalLocalPos + localOffset);

        rb.MovePosition(targetWorldPos);
    }

    // Called when quake stops (optional but useful)
    public void ResetShake()
    {
        Vector3 targetWorldPos = transform.parent
            ? transform.parent.TransformPoint(originalLocalPos)
            : originalLocalPos;

        rb.MovePosition(targetWorldPos);
    }

    public void SetFrequency(float value) => frequency = value;
}
