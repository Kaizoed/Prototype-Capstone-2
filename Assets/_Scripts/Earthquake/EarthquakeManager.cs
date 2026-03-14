using UnityEngine;
using UnityEngine.InputSystem;

namespace ShakySurvival.Earthquake
{
    public class EarthquakeManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────
        private static EarthquakeManager _instance;

        public static EarthquakeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<EarthquakeManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("[EarthquakeManager] No instance found in scene!");
                    }
                }
                return _instance;
            }
        }

        // ── Inspector ────────────────────────────────────────────────
        [Header("Magnitude Settings")]
        [Tooltip("Richter-scale magnitude (3 = barely felt, 9 = catastrophic)")]
        [SerializeField, Range(3f, 9f)] private float magnitude = 5f;

        [Tooltip("Duration of the earthquake in seconds")]
        [SerializeField] private float earthquakeDuration = 30f;

        [Tooltip("Normalized time (X: 0→1) to intensity envelope (Y: 0→1). " +
                 "Controls how the earthquake ramps up and down over its duration.")]
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Preset (Optional)")]
        [Tooltip("Drag a MagnitudeSettings asset here to override the fields above.")]
        [SerializeField] private MagnitudeSettings preset;

        [Header("Advanced — Force Curve Override")]
        [Tooltip("Optional: X = Magnitude (0-10), Y = Force Multiplier. " +
                 "Leave empty to use the default logarithmic formula.")]
        [SerializeField] private AnimationCurve forceOverrideCurve;

        [Header("Testing (Input System)")]
        [SerializeField] private InputActionReference testTriggerAction;
        [SerializeField] private bool autoStartOnAwake = false;

        // ── Public State ─────────────────────────────────────────────
        /// <summary>Current magnitude-derived strength (updates every frame while active).</summary>
        public EarthquakeStrength CurrentStrength { get; private set; }

        /// <summary>Simple 0-1 intensity (shorthand for CurrentStrength.NormalizedIntensity).</summary>
        public float CurrentIntensity => CurrentStrength.NormalizedIntensity;

        public bool IsActive { get; private set; }
        public float NormalizedTime { get; private set; }

        /// <summary>The magnitude currently in use (from preset or inspector field).</summary>
        public float ActiveMagnitude => _activeMagnitude;

        // ── Private ──────────────────────────────────────────────────
        private float _elapsedTime;
        private EarthquakeStrength _previousStrength;
        private float _activeMagnitude;
        private float _activeDuration;
        private AnimationCurve _activeIntensityCurve;
        private AnimationCurve _activeForceOverride;
        private InputAction _keyboardTAction;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[EarthquakeManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }

            _instance = this;

            // Setup default keyboard input if no action reference assigned
            if (testTriggerAction == null)
            {
                _keyboardTAction = new InputAction("TestTrigger", InputActionType.Button, "<Keyboard>/t");
                _keyboardTAction.Enable();
            }

            if (autoStartOnAwake)
            {
                StartEarthquake();
            }
        }

        private void OnEnable()
        {
            if (testTriggerAction != null)
            {
                testTriggerAction.action.Enable();
                testTriggerAction.action.performed += OnTestTrigger;
            }
        }

        private void OnDisable()
        {
            if (testTriggerAction != null)
            {
                testTriggerAction.action.performed -= OnTestTrigger;
                testTriggerAction.action.Disable();
            }
        }

        private void Update()
        {
            // Check fallback T key
            if (_keyboardTAction != null && _keyboardTAction.WasPressedThisFrame())
            {
                ToggleEarthquake();
            }

            // Update earthquake simulation
            if (IsActive)
            {
                UpdateEarthquake();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            _keyboardTAction?.Dispose();
        }

        // ══════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════

        /// <summary>Start using Inspector values or the assigned preset.</summary>
        public void StartEarthquake()
        {
            if (preset != null)
            {
                StartEarthquake(preset);
            }
            else
            {
                StartEarthquake(magnitude, earthquakeDuration, intensityCurve, forceOverrideCurve);
            }
        }

        /// <summary>Start with a specific magnitude, using Inspector duration and curves.</summary>
        public void StartEarthquake(float mag)
        {
            StartEarthquake(mag, earthquakeDuration, intensityCurve, forceOverrideCurve);
        }

        /// <summary>
        /// Starts an earthquake for the experimentation zone.
        /// Higher magnitudes last longer.
        /// </summary>
        public void StartEarthquakeFromExperiment(float mag)
        {
            float clampedMag = Mathf.Clamp(mag, MagnitudePhysics.MIN_MAGNITUDE, MagnitudePhysics.MAX_MAGNITUDE);

            // Magnitude 3 = 10 seconds, Magnitude 9 = 40 seconds
            float scaledDuration = Mathf.Lerp(10f, 40f, Mathf.InverseLerp(3f, 9f, clampedMag));

            StartEarthquake(clampedMag, scaledDuration, intensityCurve, forceOverrideCurve);
        }

        /// <summary>Start using a MagnitudeSettings preset asset.</summary>
        public void StartEarthquake(MagnitudeSettings settings)
        {
            StartEarthquake(
                settings.Magnitude,
                settings.Duration,
                settings.IntensityCurve,
                settings.ForceOverrideCurve
            );
        }

        /// <summary>Full control: magnitude, duration, curves.</summary>
        public void StartEarthquake(float mag, float duration,
        AnimationCurve envelope = null, AnimationCurve forceOverride = null)
        {
            if (IsActive)
            {
                StopEarthquake();
            }

            _activeMagnitude = Mathf.Clamp(mag, MagnitudePhysics.MIN_MAGNITUDE, MagnitudePhysics.MAX_MAGNITUDE);
            _activeDuration = Mathf.Max(1f, duration);
            _activeIntensityCurve = envelope ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            _activeForceOverride = forceOverride;

            _elapsedTime = 0f;
            NormalizedTime = 0f;
            CurrentStrength = EarthquakeStrength.Zero;
            _previousStrength = EarthquakeStrength.Zero;
            IsActive = true;
            EarthquakeEvents.RaiseEarthquakeStart();
        }

        public void StopEarthquake()
        {
            if (!IsActive) return;

            IsActive = false;
            CurrentStrength = EarthquakeStrength.Zero;
            NormalizedTime = 0f;

            Debug.Log("[EarthquakeManager] Earthquake stopped.");
            EarthquakeEvents.RaiseIntensityChange(EarthquakeStrength.Zero);
            EarthquakeEvents.RaiseEarthquakeStop();
        }

        /// <summary>
        /// Override the active magnitude at runtime (e.g. for aftershocks or gameplay triggers).
        /// </summary>
        public void SetMagnitudeOverride(float mag)
        {
            _activeMagnitude = Mathf.Clamp(mag, MagnitudePhysics.MIN_MAGNITUDE, MagnitudePhysics.MAX_MAGNITUDE);

            if (IsActive)
            {
                // Recalculate strength immediately so listeners get the change
                float envelope = _activeIntensityCurve.Evaluate(NormalizedTime);
                CurrentStrength = CalculateCurrentStrength(envelope);
                EarthquakeEvents.RaiseIntensityChange(CurrentStrength);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Internal
        // ══════════════════════════════════════════════════════════════

        private void OnTestTrigger(InputAction.CallbackContext context)
        {
            ToggleEarthquake();
        }

        private void ToggleEarthquake()
        {
            if (IsActive)
            {
                StopEarthquake();
            }
            else
            {
                StartEarthquake();
            }
        }

        private void UpdateEarthquake()
        {
            _elapsedTime += Time.deltaTime;
            NormalizedTime = Mathf.Clamp01(_elapsedTime / _activeDuration);

            // Evaluate the intensity envelope (0-1 over duration)
            float envelope = _activeIntensityCurve.Evaluate(NormalizedTime);

            // Build strength from magnitude × envelope
            CurrentStrength = CalculateCurrentStrength(envelope);

            // Only dispatch event if strength changed meaningfully
            if (!Mathf.Approximately(CurrentStrength.ForceMultiplier, _previousStrength.ForceMultiplier) ||
                !Mathf.Approximately(CurrentStrength.FrequencyMultiplier, _previousStrength.FrequencyMultiplier))
            {
                EarthquakeEvents.RaiseIntensityChange(CurrentStrength);
                _previousStrength = CurrentStrength;
            }

            // Check for completion
            if (_elapsedTime >= _activeDuration)
            {
                StopEarthquake();
            }
        }

        private EarthquakeStrength CalculateCurrentStrength(float envelope)
        {
            if (_activeForceOverride != null && _activeForceOverride.length > 0)
            {
                return MagnitudePhysics.CalculateStrength(_activeMagnitude, envelope, _activeForceOverride);
            }

            return MagnitudePhysics.CalculateStrength(_activeMagnitude, envelope);
        }
    }
}
