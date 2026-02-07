using UnityEngine;

namespace ShakySurvival.Earthquake
{
    [CreateAssetMenu(fileName = "EarthquakeWeightData", menuName = "Shaky Survival/Earthquake Weight Data")]
    public class EarthquakeWeightData : ScriptableObject
    {
        [Header("Classification")]
        [SerializeField] private string weightClassName = "Medium";

        [Header("Jolt Response")]
        [SerializeField, Range(0.1f, 3f)] private float joltMultiplier = 1f;
        [SerializeField] private float baseJoltVelocity = 0.3f;
        [SerializeField] private float maxJoltVelocity = 1.2f;

        [Header("Vibration Characteristics")]
        [SerializeField] private float vibrationFrequency = 10f;
        [SerializeField, Range(0f, 2f)] private float angularJoltMultiplier = 0.3f;

        [Header("P-Wave (Vertical Hop)")]
        [SerializeField] private float verticalHopVelocity = 0.15f;
        [SerializeField, Range(0f, 1f)] private float verticalHopChance = 0.7f;

        [Header("Thresholds")]
        [SerializeField, Range(0f, 0.5f)] private float minimumIntensity = 0.1f;

        public string WeightClassName => weightClassName;
        public float JoltMultiplier => joltMultiplier;
        public float BaseJoltVelocity => baseJoltVelocity;
        public float MaxJoltVelocity => maxJoltVelocity;
        public float VibrationFrequency => vibrationFrequency;
        public float AngularJoltMultiplier => angularJoltMultiplier;
        public float VerticalHopVelocity => verticalHopVelocity;
        public float VerticalHopChance => verticalHopChance;
        public float MinimumIntensity => minimumIntensity;
    }
}
