using System;

namespace ShakySurvival.Earthquake
{
    public static class EarthquakeEvents
    {
        public static event Action OnEarthquakeStart;
        public static event Action OnEarthquakeStop;
        public static event Action<float> OnIntensityChange;

        internal static void RaiseEarthquakeStart()
        {
            OnEarthquakeStart?.Invoke();
        }

        internal static void RaiseEarthquakeStop()
        {
            OnEarthquakeStop?.Invoke();
        }

        internal static void RaiseIntensityChange(float intensity)
        {
            OnIntensityChange?.Invoke(intensity);
        }

        public static void ClearAllSubscribers()
        {
            OnEarthquakeStart = null;
            OnEarthquakeStop = null;
            OnIntensityChange = null;
        }
    }
}
