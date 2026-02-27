using UnityEngine;

namespace ShakySurvival.Earthquake
{
    [RequireComponent(typeof(Rigidbody))]
    public class EarthquakeReactor : MonoBehaviour
    {
        [Header("Weight Configuration")]
        [SerializeField] private EarthquakeWeightData weightData;

        [Header("Fallback Settings (if no WeightData)")]
        [SerializeField, Range(0.1f, 3f)] private float fallbackJoltMultiplier = 1f;
        [SerializeField] private float fallbackBaseJoltVelocity = 0.3f;
        [SerializeField] private float fallbackMaxJoltVelocity = 1.2f;
        [SerializeField] private float fallbackVibrationFrequency = 10f;
        [SerializeField, Range(0f, 2f)] private float fallbackAngularJoltMultiplier = 0.3f;
        [SerializeField] private float fallbackVerticalHopVelocity = 0.15f;
        [SerializeField, Range(0f, 1f)] private float fallbackVerticalHopChance = 0.7f;
        [SerializeField, Range(0f, 0.5f)] private float fallbackMinimumIntensity = 0.1f;

        [Header("Timing")]
        [SerializeField] private bool randomizeJoltTiming = true;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        private float JoltMultiplier => weightData != null ? weightData.JoltMultiplier : fallbackJoltMultiplier;
        private float BaseJoltVelocity => weightData != null ? weightData.BaseJoltVelocity : fallbackBaseJoltVelocity;
        private float MaxJoltVelocity => weightData != null ? weightData.MaxJoltVelocity : fallbackMaxJoltVelocity;
        private float VibrationFrequency => weightData != null ? weightData.VibrationFrequency : fallbackVibrationFrequency;
        private float AngularJoltMultiplier => weightData != null ? weightData.AngularJoltMultiplier : fallbackAngularJoltMultiplier;
        private float VerticalHopVelocity => weightData != null ? weightData.VerticalHopVelocity : fallbackVerticalHopVelocity;
        private float VerticalHopChance => weightData != null ? weightData.VerticalHopChance : fallbackVerticalHopChance;
        private float MinimumIntensity => weightData != null ? weightData.MinimumIntensity : fallbackMinimumIntensity;

        private Rigidbody _rigidbody;
        private EarthquakeStrength _currentStrength;
        private bool _isActive;
        private float _nextJoltTime;
        private float _joltInterval;

        public bool IsReacting => _isActive && _currentStrength.NormalizedIntensity > MinimumIntensity;
        public EarthquakeWeightData WeightData => weightData;

        /// Assign weight data at runtime (used by ProgressiveDestruction to add dynamically to fragments).
        public void SetWeightData(EarthquakeWeightData data) => weightData = data;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // Ensure rigidbody is not kinematic
            if (_rigidbody.isKinematic)
            {
                if (debugMode) Debug.LogWarning($"[EarthquakeReactor] {gameObject.name}: Setting Rigidbody to non-kinematic.");
                _rigidbody.isKinematic = false;
            }

            // Calculate base jolt interval
            UpdateJoltInterval(1f);

            // Initialize with randomized timing offset (prevents synchronized shaking)
            _nextJoltTime = Time.fixedTime + Random.Range(0f, _joltInterval);

            if (weightData != null && debugMode)
            {
                Debug.Log($"[EarthquakeReactor] {gameObject.name}: Using weight class '{weightData.WeightClassName}'");
            }
        }

        private void OnEnable()
        {
            EarthquakeEvents.OnEarthquakeStart += OnEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop += OnEarthquakeStop;
            EarthquakeEvents.OnIntensityChange += OnIntensityChange;

            // If added mid-earthquake (e.g. by ProgressiveDestruction during collapse),
            // the OnEarthquakeStart event was already fired before this component existed.
            // Catch up by self-activating with the manager's current state.
            if (!_isActive && EarthquakeManager.Instance != null && EarthquakeManager.Instance.IsActive)
            {
                OnEarthquakeStart();
                OnIntensityChange(EarthquakeManager.Instance.CurrentStrength);
            }
        }

        private void OnDisable()
        {
            EarthquakeEvents.OnEarthquakeStart -= OnEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop -= OnEarthquakeStop;
            EarthquakeEvents.OnIntensityChange -= OnIntensityChange;
        }

        private void FixedUpdate()
        {
            if (!_isActive) return;
            if (_currentStrength.NormalizedIntensity < MinimumIntensity) return;

            // Check if it's time for the next jolt
            if (Time.fixedTime >= _nextJoltTime)
            {
                ApplyJolt();
                ScheduleNextJolt();
            }
        }

        private void OnEarthquakeStart()
        {
            _isActive = true;

            // Wake up the rigidbody (prevent sleep mode)
            _rigidbody.WakeUp();

            // Update interval with neutral frequency multiplier until first strength arrives
            UpdateJoltInterval(1f);

            // Schedule first jolt with random offset
            _nextJoltTime = Time.fixedTime + Random.Range(0f, _joltInterval);
            if (debugMode) Debug.Log($"[EarthquakeReactor] {gameObject.name}: Earthquake started.");
        }

        private void OnEarthquakeStop()
        {
            _isActive = false;
            _currentStrength = EarthquakeStrength.Zero;
            if (debugMode) Debug.Log($"[EarthquakeReactor] {gameObject.name}: Earthquake stopped.");
        }

        private void OnIntensityChange(EarthquakeStrength strength)
        {
            _currentStrength = strength;

            // Recalculate jolt interval: higher frequency multiplier → shorter intervals
            UpdateJoltInterval(strength.FrequencyMultiplier);
        }

        private void UpdateJoltInterval(float frequencyMultiplier)
        {
            // Scale the per-object vibration frequency by the global magnitude-derived factor
            float effectiveFrequency = VibrationFrequency * Mathf.Max(0.1f, frequencyMultiplier);
            _joltInterval = 1f / Mathf.Max(1f, effectiveFrequency);
        }

        private void ScheduleNextJolt()
        {
            float interval = _joltInterval;

            // Randomize timing by ±20% to prevent synchronized shaking
            if (randomizeJoltTiming)
            {
                float variance = interval * 0.2f;
                interval += Random.Range(-variance, variance);
            }

            _nextJoltTime = Time.fixedTime + Mathf.Max(0.02f, interval);
        }

        private void ApplyJolt()
        {
            // Calculate jolt velocity using the global force multiplier + weight-class tuning
            float joltVelocity = Mathf.Lerp(BaseJoltVelocity, MaxJoltVelocity, _currentStrength.ForceMultiplier);
            joltVelocity *= JoltMultiplier;

            // Generate random horizontal direction using insideUnitCircle
            Vector2 horizontalDir = Random.insideUnitCircle.normalized;

            // Build the jolt velocity vector
            Vector3 joltVector = new Vector3(horizontalDir.x, 0f, horizontalDir.y) * joltVelocity;

            // Add vertical micro-hop to break static friction
            if (Random.value < VerticalHopChance)
            {
                float hopVelocity = VerticalHopVelocity * _currentStrength.ForceMultiplier;
                joltVector.y = hopVelocity;
            }

            // Apply velocity change (mass-independent)
            _rigidbody.AddForce(joltVector, ForceMode.VelocityChange);

            // Apply angular jolt for natural tumbling
            if (AngularJoltMultiplier > 0)
            {
                Vector3 angularJolt = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(-1f, 1f)
                ) * joltVelocity * AngularJoltMultiplier;

                _rigidbody.AddTorque(angularJolt, ForceMode.VelocityChange);
            }

            if (debugMode && Random.value < 0.1f)
            {
                string weightClass = weightData != null ? weightData.WeightClassName : "Default";
                Debug.Log($"[EarthquakeReactor] {gameObject.name} ({weightClass}): " +
                          $"Jolt vel={joltVelocity:F2}, Strength={_currentStrength}");
            }
        }


        [ContextMenu("Test Jolt")]
        public void TestJolt()
        {
            _rigidbody.WakeUp();
            Vector2 dir = Random.insideUnitCircle.normalized;
            float velocity = MaxJoltVelocity * JoltMultiplier;
            Vector3 jolt = new Vector3(dir.x, VerticalHopVelocity, dir.y) * velocity;
            _rigidbody.AddForce(jolt, ForceMode.VelocityChange);
            Debug.Log($"[EarthquakeReactor] {gameObject.name}: Test jolt applied!");
        }

        [ContextMenu("Wake Up Rigidbody")]
        public void WakeUpRigidbody()
        {
            _rigidbody.WakeUp();
            Debug.Log($"[EarthquakeReactor] {gameObject.name}: Rigidbody awakened.");
        }
    }
}
