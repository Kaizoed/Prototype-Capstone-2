using UnityEngine;

namespace ShakySurvival.Earthquake
{
    [CreateAssetMenu(fileName = "MagnitudeSettings", menuName = "Shaky Survival/Magnitude Settings")]
    public class MagnitudeSettings : ScriptableObject
    {
        [Header("Preset Info")]
        [Tooltip("Human-readable label (e.g. 'Level 1 — Mild Tremor')")]
        [SerializeField] private string presetName = "New Preset";

        [Header("Earthquake Parameters")]
        [Tooltip("Richter-scale magnitude (3 = barely felt, 9 = catastrophic)")]
        [SerializeField, Range(3f, 9f)] private float magnitude = 5f;

        [Tooltip("Duration of the earthquake in seconds")]
        [SerializeField, Min(1f)] private float duration = 30f;

        [Header("Intensity Envelope")]
        [Tooltip("Normalized time (X: 0→1) to intensity envelope (Y: 0→1). " +
                 "Controls how the earthquake ramps up and down over its duration.")]
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Force Curve Override (Optional)")]
        [Tooltip("If set, overrides the built-in logarithmic force formula. " +
                 "X = Magnitude (0-10), Y = Force Multiplier (0-1).")]
        [SerializeField] private AnimationCurve forceOverrideCurve;

        public string PresetName => presetName;
        public float Magnitude => magnitude;
        public float Duration => duration;
        public AnimationCurve IntensityCurve => intensityCurve;

        public AnimationCurve ForceOverrideCurve =>
            forceOverrideCurve != null && forceOverrideCurve.length > 0
                ? forceOverrideCurve
                : null;
    }
}
