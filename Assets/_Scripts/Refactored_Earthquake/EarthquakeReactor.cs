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
        private float _currentIntensity;
        private bool _isActive;
        private float _nextJoltTime;
        private float _joltInterval;

        public bool IsReacting => _isActive && _currentIntensity > MinimumIntensity;
        public EarthquakeWeightData WeightData => weightData;

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
            UpdateJoltInterval();

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
            if (_currentIntensity < MinimumIntensity) return;

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

            // Update interval in case weightData changed
            UpdateJoltInterval();

            // Schedule first jolt with random offset
            _nextJoltTime = Time.fixedTime + Random.Range(0f, _joltInterval);
            if (debugMode) Debug.Log($"[EarthquakeReactor] {gameObject.name}: Earthquake started.");
        }

        private void OnEarthquakeStop()
        {
            _isActive = false;
            _currentIntensity = 0f;
            if (debugMode) Debug.Log($"[EarthquakeReactor] {gameObject.name}: Earthquake stopped.");
        }

        private void OnIntensityChange(float intensity)
        {
            _currentIntensity = intensity;
        }

        private void UpdateJoltInterval()
        {
            _joltInterval = 1f / Mathf.Max(1f, VibrationFrequency);
        }

        private void ScheduleNextJolt()
        {
            float interval = _joltInterval;

            // Randomize timing by positive/negative 20% to prevent synchronized shaking
            if (randomizeJoltTiming)
            {
                float variance = interval * 0.2f;
                interval += Random.Range(-variance, variance);
            }

            _nextJoltTime = Time.fixedTime + Mathf.Max(0.02f, interval);
        }

        private void ApplyJolt()
        {
            // Calculate jolt velocity based on intensity and weight class
            float joltVelocity = Mathf.Lerp(BaseJoltVelocity, MaxJoltVelocity, _currentIntensity);
            joltVelocity *= JoltMultiplier;

            // Generate random horizontal direction using insideUnitCircle
            Vector2 horizontalDir = Random.insideUnitCircle.normalized;

            // Build the jolt velocity vector
            Vector3 joltVector = new Vector3(horizontalDir.x, 0f, horizontalDir.y) * joltVelocity;

            // Add vertical micro-hop to break static friction
            if (Random.value < VerticalHopChance)
            {
                float hopVelocity = VerticalHopVelocity * _currentIntensity;
                joltVector.y = hopVelocity;
            }
            
            // Apply velocity change
            // This ensures two differently weighted objects accelerate identically
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
                Debug.Log($"[EarthquakeReactor] {gameObject.name} ({weightClass}): Jolt vel={joltVelocity:F2}");
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
