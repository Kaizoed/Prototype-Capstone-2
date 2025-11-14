using UnityEngine;

public class CameraShaking : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float maxRotation = 1.2f;
    [SerializeField] private float maxOffset = 0.3f;
    [SerializeField] private float frequency = 12f;

    [Header("Axis Multipliers")]
    [SerializeField, Range(0f, 1f)] private float verticalMultiplier = 0.3f;
    [SerializeField, Range(0f, 1f)] private float horizontalMultiplier = 1f;

    private Vector3 originalLocalPos;
    private Vector3 positionOffset;
    private Quaternion shakeRotationOffset = Quaternion.identity;

    public Quaternion ShakeRotationOffset => shakeRotationOffset;
    public Vector3 ShakePositionOffset => positionOffset;

    private void Start()
    {
        originalLocalPos = transform.localPosition;
    }

    private void LateUpdate()
    {
        if (EarthquakeController.Instance == null || !EarthquakeController.Instance.IsQuaking)
        {
            shakeRotationOffset = Quaternion.identity;
            positionOffset = Vector3.zero;
            transform.localPosition = originalLocalPos;
            return;
        }

        float intensity = EarthquakeController.Instance.Intensity;
        float time = Time.time * frequency;

        float offsetX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * maxOffset * horizontalMultiplier * intensity;

        float offsetY = (Mathf.PerlinNoise(0f, time * 0.6f) - 0.5f) * 2f * maxOffset * verticalMultiplier * intensity;

        positionOffset = new Vector3(offsetX, offsetY, 0f);

        float rotX = Mathf.Sin(time * 1.1f) * maxRotation * intensity;
        float rotZ = Mathf.Cos(time * 1.3f) * maxRotation * intensity;
        shakeRotationOffset = Quaternion.Euler(rotX, 0f, rotZ);

        Vector3 baseLocalPos = new Vector3(originalLocalPos.x, transform.localPosition.y, originalLocalPos.z);
        transform.localPosition = baseLocalPos + positionOffset;
    }
}