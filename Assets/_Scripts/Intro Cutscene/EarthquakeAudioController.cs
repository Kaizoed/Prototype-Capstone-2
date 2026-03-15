using UnityEngine;
using ShakySurvival.Earthquake;

public class EarthquakeAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource earthquakeRumble;
    [SerializeField] private AudioSource earthquakeSiren;

    private void OnEnable()
    {
        EarthquakeEvents.OnEarthquakeStart += PlayEarthquakeAudio;
        EarthquakeEvents.OnEarthquakeStop += StopEarthquakeAudio;
    }

    private void OnDisable()
    {
        EarthquakeEvents.OnEarthquakeStart -= PlayEarthquakeAudio;
        EarthquakeEvents.OnEarthquakeStop -= StopEarthquakeAudio;
    }

    void PlayEarthquakeAudio()
    {
        if (earthquakeRumble != null)
            earthquakeRumble.Play();

        if (earthquakeSiren != null)
            earthquakeSiren.Play();
    }

    void StopEarthquakeAudio()
    {
        if (earthquakeRumble != null)
            earthquakeRumble.Stop();

        if (earthquakeSiren != null)
            earthquakeSiren.Stop();
    }
}