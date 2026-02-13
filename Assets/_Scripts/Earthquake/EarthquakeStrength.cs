namespace ShakySurvival.Earthquake
{
    public readonly struct EarthquakeStrength
    {
        // Raw Richter magnitude (e.g. 4.5, 7.0)
        public readonly float Magnitude;

        // Logarithmic force multiplier (0 → 1+)
        // Drives jolt velocity / hop velocity on reactors
        public readonly float ForceMultiplier;


        // Drives vibration frequency on reactors and camera shake frequency
        public readonly float FrequencyMultiplier;

        // Simple linear 0-1 mapping of the magnitude across the supported range.
        // Useful for UI, camera amplitude lerps, or anything that just needs a percentage.
        public readonly float NormalizedIntensity;

        public EarthquakeStrength(float magnitude, float forceMultiplier,
            float frequencyMultiplier, float normalizedIntensity)
        {
            Magnitude = magnitude;
            ForceMultiplier = forceMultiplier;
            FrequencyMultiplier = frequencyMultiplier;
            NormalizedIntensity = normalizedIntensity;
        }

        // zero-strength value (no earthquake).
        public static readonly EarthquakeStrength Zero =
            new EarthquakeStrength(0f, 0f, 0f, 0f);

        public override string ToString() =>
            $"[Mag {Magnitude:F1} | Force {ForceMultiplier:F3} | Freq {FrequencyMultiplier:F3} | Norm {NormalizedIntensity:F3}]";
    }
}
