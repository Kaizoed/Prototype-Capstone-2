using UnityEngine;
using System.Collections;

public class EarthquakeController : MonoBehaviour
{
    public static EarthquakeController Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private float fadeInTime = 2f;
    [SerializeField] private float fadeOutTime = 2.5f;

    public float Intensity { get; private set; } = 0f;
    public bool IsQuaking { get; private set; } = false;
    public EnvironmentShake ActiveEnvironment { get; private set; }

    private Coroutine quakeRoutine;

    private void Awake()
    {
        Instance = this;
    }

    public void StartQuake(float targetIntensity)
    {
        if (quakeRoutine != null)
            StopCoroutine(quakeRoutine);

        quakeRoutine = StartCoroutine(FadeToIntensity(targetIntensity, fadeInTime));
    }

    public void StopQuake()
    {
        if (quakeRoutine != null)
            StopCoroutine(quakeRoutine);

        quakeRoutine = StartCoroutine(FadeToIntensity(0f, fadeOutTime, stopAtEnd: true));
    }

    private IEnumerator FadeToIntensity(float target, float duration, bool stopAtEnd = false)
    {
        IsQuaking = true;

        float start = Intensity;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            Intensity = Mathf.Lerp(start, target, lerp);
            yield return null;
        }

        Intensity = target;

        if (stopAtEnd && target == 0f)
            IsQuaking = false;

        quakeRoutine = null;
    }

    public void SetActiveEnvironment(EnvironmentShake env)
    {
        ActiveEnvironment = env;
    }
}