using UnityEngine;
using UnityEngine.InputSystem;

namespace ShakySurvival.Earthquake
{
    public class EarthquakeManager : MonoBehaviour
    {
        // SINGLETON
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

        [Header("Earthquake Settings")]
        [SerializeField] private float earthquakeDuration = 30f;
        [SerializeField] private AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float maxIntensity = 1f;

        [Header("Testing (Input System)")]
        [SerializeField] private InputActionReference testTriggerAction;
        [SerializeField] private bool autoStartOnAwake = false;

        public float CurrentIntensity { get; private set; }
        public bool IsActive { get; private set; }
        public float NormalizedTime { get; private set; }

        private float _elapsedTime;
        private float _previousIntensity;
        private InputAction _keyboardTAction; // Input action for testing (T key), defo replaceable

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

        public void StartEarthquake()
        {
            StartEarthquake(earthquakeDuration);
        }

        public void StartEarthquake(float duration)
        {
            earthquakeDuration = duration;
            _elapsedTime = 0f;
            NormalizedTime = 0f;
            CurrentIntensity = 0f;
            _previousIntensity = 0f;
            IsActive = true;

            Debug.Log($"[EarthquakeManager] Earthquake started! Duration: {duration}s");
            EarthquakeEvents.RaiseEarthquakeStart();
        }

        public void StopEarthquake()
        {
            if (!IsActive) return;

            IsActive = false;
            CurrentIntensity = 0f;
            NormalizedTime = 0f;

            Debug.Log("[EarthquakeManager] Earthquake stopped.");
            EarthquakeEvents.RaiseIntensityChange(0f);
            EarthquakeEvents.RaiseEarthquakeStop();
        }

        public void SetIntensityOverride(float intensity)
        {
            CurrentIntensity = Mathf.Clamp01(intensity);
            EarthquakeEvents.RaiseIntensityChange(CurrentIntensity);
        }

        private void UpdateEarthquake()
        {
            _elapsedTime += Time.deltaTime;
            NormalizedTime = Mathf.Clamp01(_elapsedTime / earthquakeDuration);

            // Evaluate intensity from curve
            float curveValue = intensityCurve.Evaluate(NormalizedTime);
            CurrentIntensity = curveValue * maxIntensity;

            // Only dispatch event if intensity changed significantly
            if (!Mathf.Approximately(CurrentIntensity, _previousIntensity))
            {
                EarthquakeEvents.RaiseIntensityChange(CurrentIntensity);
                _previousIntensity = CurrentIntensity;
            }

            // Check for completion
            if (_elapsedTime >= earthquakeDuration)
            {
                StopEarthquake();
            }
        }
    }
}
