using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LooseObject : MonoBehaviour
{
    private Rigidbody rb;
    private EnvironmentShake ground;
    private Vector3 lastGroundPos;
    private bool initialized = false;

    // Override the environment in inspector if you have multiple grounds or special cases, leave it empty if we only have one EnvironmentShake
    [SerializeField] private EnvironmentShake inspectorGroundOverride = null;

    [SerializeField, Range(0.0f, 5.0f)] private float weightMultiplier = 1.0f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (inspectorGroundOverride != null)
        {
            ground = inspectorGroundOverride;
        }
        else if (EarthquakeController.Instance != null)
        {
            ground = EarthquakeController.Instance.ActiveEnvironment;
        }

        if (ground != null)
        {
            lastGroundPos = ground.transform.position;
            initialized = true;
        }
    }

    private void FixedUpdate()
    {
        if (!initialized)
        {
            if (inspectorGroundOverride != null)
                ground = inspectorGroundOverride;
            else if (EarthquakeController.Instance != null)
                ground = EarthquakeController.Instance.ActiveEnvironment;

            if (ground != null)
            {
                lastGroundPos = ground.transform.position;
                initialized = true;
            }
        }

        if (!initialized || EarthquakeController.Instance == null || !EarthquakeController.Instance.IsQuaking || ground == null)
            return;

        Vector3 currentGroundPos = ground.transform.position;
        Vector3 groundVelocity = (currentGroundPos - lastGroundPos) / Time.fixedDeltaTime;
        lastGroundPos = currentGroundPos;

        // avoid dividing it by zero
        float mass = Mathf.Max(0.0001f, rb.mass); 
        float weightFactor = Mathf.Clamp01((1f / mass) * weightMultiplier);

        float intensity = EarthquakeController.Instance.Intensity;

        rb.AddForce(groundVelocity * weightFactor * intensity, ForceMode.Acceleration);
    }
}
