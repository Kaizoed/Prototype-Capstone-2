using UnityEngine;

namespace ShakySurvival.Earthquake
{
    public static class MagnitudePhysics
    {
        // Supported range
        public const float MIN_MAGNITUDE = 3f;
        public const float MAX_MAGNITUDE = 9f;

        // Reference magnitude — the "baseline" earthquake where
        // ForceMultiplier = 1.0.  Everything below is weaker,
        // everything above grows exponentially
        public const float REFERENCE_MAGNITUDE = 5f;

        // AnimationCurve on EarthquakeManager if they want a different feel
        private const float FORCE_EXPONENT_SCALE = 1f;

        // Converts magnitude to force multiplier using logarithmic scaling
        // The result is then normalised so that the full range maps cleanly
        public static float MagnitudeToForceMultiplier(float magnitude)
        {
            magnitude = Mathf.Clamp(magnitude, MIN_MAGNITUDE, MAX_MAGNITUDE);

            float raw = Mathf.Pow(10f, (magnitude - REFERENCE_MAGNITUDE) * FORCE_EXPONENT_SCALE);

            float minRaw = Mathf.Pow(10f, (MIN_MAGNITUDE - REFERENCE_MAGNITUDE) * FORCE_EXPONENT_SCALE);
            float maxRaw = Mathf.Pow(10f, (MAX_MAGNITUDE - REFERENCE_MAGNITUDE) * FORCE_EXPONENT_SCALE);

            return Mathf.InverseLerp(minRaw, maxRaw, raw);
        }


        /// Higher magnitudes shake faster, but growth is gentler than force
        public static float MagnitudeToFrequencyMultiplier(float magnitude)
        {
            magnitude = Mathf.Clamp(magnitude, MIN_MAGNITUDE, MAX_MAGNITUDE);

            float t = Mathf.InverseLerp(MIN_MAGNITUDE, MAX_MAGNITUDE, magnitude);
            // Square-root curve: fast initial rise, gentler at high end
            return Mathf.Sqrt(t);
        }

        /// Simple linear 0-1 mapping across [MIN_MAGNITUDE, MAX_MAGNITUDE]
        /// Handy for UI bars, camera amplitude lerps, etc.
        public static float MagnitudeToNormalized(float magnitude)
        {
            return Mathf.InverseLerp(MIN_MAGNITUDE, MAX_MAGNITUDE, magnitude);
        }

        public static EarthquakeStrength CalculateStrength(float magnitude, float envelope = 1f)
        {
            float force = MagnitudeToForceMultiplier(magnitude) * envelope;
            float freq  = MagnitudeToFrequencyMultiplier(magnitude) * envelope;
            float norm  = MagnitudeToNormalized(magnitude) * envelope;

            return new EarthquakeStrength(magnitude, force, freq, norm);
        }

        public static EarthquakeStrength CalculateStrength(
            float magnitude, float envelope, AnimationCurve forceOverride)
        {
            float force, freq, norm;

            if (forceOverride != null && forceOverride.length > 0)
            {
                force = forceOverride.Evaluate(magnitude) * envelope;
            }
            else
            {
                force = MagnitudeToForceMultiplier(magnitude) * envelope;
            }

            freq = MagnitudeToFrequencyMultiplier(magnitude) * envelope;
            norm = MagnitudeToNormalized(magnitude) * envelope;

            return new EarthquakeStrength(magnitude, force, freq, norm);
        }
    }
}
