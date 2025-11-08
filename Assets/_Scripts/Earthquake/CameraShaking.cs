using UnityEngine;

public class CameraShaking : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float maxRotation = 1.2f;
    [SerializeField] private float maxOffset = 0.3f;
    [SerializeField] private float frequency = 12f;

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

        // Position shake
        float offsetX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * maxOffset * intensity;
        float offsetY = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f * maxOffset * 0.5f * intensity;
        positionOffset = new Vector3(offsetX, offsetY, 0f);

        // Rotation shake
        float rotX = Mathf.Sin(time * 1.1f) * maxRotation * intensity;
        float rotZ = Mathf.Cos(time * 1.3f) * maxRotation * intensity;
        shakeRotationOffset = Quaternion.Euler(rotX, 0f, rotZ);

        Vector3 baseLocalPos = new Vector3(originalLocalPos.x, transform.localPosition.y, originalLocalPos.z);
        transform.localPosition = baseLocalPos + positionOffset;
    }

}