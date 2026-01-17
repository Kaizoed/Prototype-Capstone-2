using UnityEngine;
using Unity.Cinemachine;
using ShakySurvival.Earthquake;
using ShakySurvival.Player;

namespace ShakySurvival.Camera
{
    public class EarthquakeCameraShaker : MonoBehaviour
    {
        [Header("Cinemachine References")]
        [SerializeField] private CinemachineCamera virtualCamera;
        [SerializeField] private NoiseSettings noiseProfile;

        [Header("Shake Settings")]
        [SerializeField] private float baseAmplitude = 0.5f;
        [SerializeField] private float baseFrequency = 0.5f;
        [SerializeField] private float maxAmplitude = 2f;
        [SerializeField] private float maxFrequency = 2f;

        [Header("Stability")]
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private float crouchStabilityFactor = 0.5f;

        [Header("Smoothing")]
        [SerializeField] private float intensityLerpSpeed = 5f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;
        [SerializeField] private bool verboseLogging = false;

        private CinemachineBasicMultiChannelPerlin _noise;
        private float _currentAmplitude;
        private float _currentFrequency;
        private float _targetIntensity;
        private bool _isActive;
        private bool _noiseInitialized;
        private float _debugLogTimer;

        private void Awake()
        {
            Debug.Log("[EarthquakeCameraShaker] Awake - Starting initialization...");
            InitializeCamera();
        }

        private void Start()
        {
            // Try again in Start if Awake failed (camera might not be ready)
            if (!_noiseInitialized)
            {
                Debug.Log("[EarthquakeCameraShaker] Start - Retrying initialization...");
                InitializeCamera();
            }
            
            RunDiagnostics();
        }

        private void OnEnable()
        {
            EarthquakeEvents.OnEarthquakeStart += OnEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop += OnEarthquakeStop;
            EarthquakeEvents.OnIntensityChange += OnIntensityChange;
            Debug.Log("[EarthquakeCameraShaker] Subscribed to EarthquakeEvents");
        }

        private void OnDisable()
        {
            EarthquakeEvents.OnEarthquakeStart -= OnEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop -= OnEarthquakeStop;
            EarthquakeEvents.OnIntensityChange -= OnIntensityChange;
        }

        private void Update()
        {
            // Keep trying to initialize if not ready
            if (!_noiseInitialized)
            {
                InitializeCamera();
                if (!_noiseInitialized) return;
            }

            UpdateShake();

            // Periodic debug logging
            if (debugMode && _isActive)
            {
                _debugLogTimer += Time.deltaTime;
                if (_debugLogTimer >= 1f || verboseLogging)
                {
                    _debugLogTimer = 0f;
                    Debug.Log($"[EarthquakeCameraShaker] UPDATE: Amplitude={_currentAmplitude:F3}, Frequency={_currentFrequency:F3}");
                }
            }
        }

        private void InitializeCamera()
        {
            // Try to find virtual camera if not assigned
            if (virtualCamera == null)
            {
                virtualCamera = FindFirstObjectByType<CinemachineCamera>();
                if (virtualCamera != null)
                {
                    Debug.Log($"[EarthquakeCameraShaker] Auto-found CinemachineCamera: {virtualCamera.name}");
                }
            }

            if (virtualCamera == null)
            {
                Debug.LogError("[EarthquakeCameraShaker] ERROR: No CinemachineCamera found!");
                return;
            }

            // Look for the noise component on the virtual camera
            _noise = virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();

            if (_noise == null)
            {
                // Assign noise profile if provided
                Debug.LogWarning("[EarthquakeCameraShaker] No noise component - adding one...");
                _noise = virtualCamera.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
            }

            if (_noise != null)
            {
                if (noiseProfile != null)
                {
                    _noise.NoiseProfile = noiseProfile;
                    Debug.Log($"[EarthquakeCameraShaker] Assigned noise profile: {noiseProfile.name}");
                }
                
                // Start with zero shake
                _noise.AmplitudeGain = 0f;
                _noise.FrequencyGain = baseFrequency;
                _noiseInitialized = true;

                Debug.Log("[EarthquakeCameraShaker] SUCCESS: Noise component initialized!");
            }

            // Try to find player locomotion if not assigned
            if (playerMovement == null)
            {
                playerMovement = FindFirstObjectByType<PlayerMovement>();
            }
        }

        private void RunDiagnostics()
        {
            Debug.Log("========== EARTHQUAKE CAMERA SHAKER DIAGNOSTICS ==========");
            
            // Check virtual camera
            if (virtualCamera == null)
            {
                Debug.LogError("Virtual Camera: NOT ASSIGNED");
            }
            else
            {
                Debug.Log($"Virtual Camera: {virtualCamera.name}");
                Debug.Log($"  - IsLive: {virtualCamera.IsLive}");
            }

            // Check noise component
            if (_noise == null)
            {
                Debug.LogError("Noise Component: NOT FOUND");
            }
            else
            {
                Debug.Log($"Noise Component: Found");
                Debug.Log($" - NoiseProfile: {(_noise.NoiseProfile != null ? _noise.NoiseProfile.name : "NONE")}");
            }

            // Check CinemachineBrain
            var brain = FindFirstObjectByType<CinemachineBrain>();
            if (brain == null)
            {
                Debug.LogError("CinemachineBrain: NOT FOUND IN SCENE");
            }
            else
            {
                Debug.Log($"CinemachineBrain: Found on {brain.gameObject.name}");
            }
            
            // Check EarthquakeManager
            if (EarthquakeManager.Instance == null)
            {
                Debug.LogError("EarthquakeManager: NOT FOUND");
            }
            else
            {
                Debug.Log($"EarthquakeManager: Found, IsActive={EarthquakeManager.Instance.IsActive}");
            }

            Debug.Log("============================================================");
        }

        private void OnEarthquakeStart()
        {
            _isActive = true;
            Debug.Log("[EarthquakeCameraShaker] EARTHQUAKE START ");
        }

        private void OnEarthquakeStop()
        {
            _isActive = false;
            _targetIntensity = 0f;
            Debug.Log("[EarthquakeCameraShaker] EARTHQUAKE STOP ");
        }

        private void OnIntensityChange(float intensity)
        {
            _targetIntensity = intensity;
        }

        private void UpdateShake()
        {
            if (_noise == null) return;

            // Calculate stability factor based on crouch state
            float stabilityMultiplier = 1f;
            if (playerMovement != null && playerMovement.IsCrouching)
            {
                stabilityMultiplier = crouchStabilityFactor;
            }

            // Calculate target values
            float targetAmplitude = 0f;
            float targetFrequency = baseFrequency;

            if (_isActive)
            {
                targetAmplitude = Mathf.Lerp(baseAmplitude, maxAmplitude, _targetIntensity) * stabilityMultiplier;
                targetFrequency = Mathf.Lerp(baseFrequency, maxFrequency, _targetIntensity);
            }

            // Smoothly interpolate current values
            _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, intensityLerpSpeed * Time.deltaTime);
            _currentFrequency = Mathf.Lerp(_currentFrequency, targetFrequency, intensityLerpSpeed * Time.deltaTime);

            // Apply to noise component
            _noise.AmplitudeGain = _currentAmplitude;
            _noise.FrequencyGain = _currentFrequency;
        }

        [ContextMenu("Run Diagnostics")]
        public void RunDiagnosticsManual() { RunDiagnostics(); }
    }
}
