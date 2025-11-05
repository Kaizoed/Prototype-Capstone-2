using UnityEngine;
using System.Collections;

public class EarthquakeController : MonoBehaviour
{
    public static EarthquakeController Instance { get; private set; }

    [Header("Earthquake Settings")]
    [SerializeField] private float totalDuration = 8f;
    [SerializeField] private float fadeInTime = 2f;
    [SerializeField] private float fadeOutTime = 2.5f;
    [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public float Intensity { get; private set; } = 0f;
    public bool IsQuaking { get; private set; } = false;

    public EnvironmentShake ActiveEnvironment { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(EarthquakeRoutine());
    }

    private IEnumerator EarthquakeRoutine()
    {
        // While (true) statement to infinite loop for demo purposes
        while (true)
        {
            IsQuaking = true;
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                float normalizedTime = elapsed / totalDuration;
                float curveValue = intensityCurve.Evaluate(normalizedTime);

                // Smooth transitions using fade-in/out
                if (elapsed < fadeInTime)
                    Intensity = Mathf.SmoothStep(0f, curveValue, elapsed / fadeInTime);
                else if (elapsed > totalDuration - fadeOutTime)
                {
                    float fadeOutProgress = (elapsed - (totalDuration - fadeOutTime)) / fadeOutTime;
                    Intensity = Mathf.SmoothStep(curveValue, 0f, fadeOutProgress);
                }
                else
                    Intensity = curveValue;

                elapsed += Time.deltaTime;
                yield return null;
            }

            Intensity = 0f;
            IsQuaking = false;

            // 2 seconds interval between quakes
            yield return new WaitForSeconds(2f);
        }

    }
    
    public void SetActiveEnvironment(EnvironmentShake env)
    {
        ActiveEnvironment = env;
    }   

}
