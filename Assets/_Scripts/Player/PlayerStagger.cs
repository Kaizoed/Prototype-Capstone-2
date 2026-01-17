using System.Collections;
using UnityEngine;
using ShakySurvival.Earthquake;
using Unity.Cinemachine;

namespace ShakySurvival.Player
{
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerStagger : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField] private float checkInterval = 0.75f;
        [SerializeField] private float staggerCooldown = 2f;
        [SerializeField] private float staggerDuration = 1.2f;

        [Header("Stagger Chance (Percentage)")]
        [SerializeField, Range(0f, 1f)] private float idleChance = 0.02f;
        [SerializeField, Range(0f, 1f)] private float walkChance = 0.08f;
        [SerializeField, Range(0f, 1f)] private float runChance = 0.25f;

        [Header("Intensity Scaling")]
        [SerializeField] private bool scaleWithIntensity = true;
        [SerializeField, Range(0f, 1f)] private float minimumIntensity = 0.2f;

        [Header("Stagger Effects")]
        [SerializeField, Range(0.1f, 0.8f)] private float slowdownMultiplier = 0.35f;
        [SerializeField] private float driftStrength = 1.5f;
        [SerializeField] private float driftRampSpeed = 3f;

        // Impulse is optional, it helps enhance the stagger effect
        [Header("Cinemachine Impulse")]
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private float impulseForce = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        private PlayerMovement _movement;
        private float _lastCheckTime;
        private float _lastStaggerTime;
        private bool _isStaggering;
        private Coroutine _staggerCoroutine;
        private int _driftDirection;

        public bool IsStaggering => _isStaggering;
        public float CurrentChance { get; private set; }

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            
            if (impulseSource == null)
            {
                impulseSource = GetComponentInChildren<CinemachineImpulseSource>();
            }
        }

        private void OnEnable()
        {
            EarthquakeEvents.OnEarthquakeStart += OnEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop += OnEarthquakeStop;
        }

        private void OnDisable()
        {
            EarthquakeEvents.OnEarthquakeStart -= OnEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop -= OnEarthquakeStop;
        }

        private void Update()
        {
            if (EarthquakeManager.Instance == null || !EarthquakeManager.Instance.IsActive)
            {
                CurrentChance = 0f;
                return;
            }

            if (_movement.IsCrouching)
            {
                CurrentChance = 0f;
                return;
            }

            if (EarthquakeManager.Instance.CurrentIntensity < minimumIntensity)
            {
                CurrentChance = 0f;
                return;
            }

            if (Time.time - _lastCheckTime < checkInterval) return;
            if (Time.time - _lastStaggerTime < staggerCooldown) return;
            if (_isStaggering) return;

            _lastCheckTime = Time.time;
            CheckForStagger();
        }

        private void OnEarthquakeStart()
        {
            _lastCheckTime = Time.time;
            if (debugMode) Debug.Log("[PlayerStagger] Earthquake started - stagger checks active.");
        }

        private void OnEarthquakeStop()
        {
            CurrentChance = 0f;
            
            if (_staggerCoroutine != null)
            {
                StopCoroutine(_staggerCoroutine);
                EndStagger();
            }
            
            if (debugMode) Debug.Log("[PlayerStagger] Earthquake stopped - stagger checks disabled.");
        }

        private void CheckForStagger()
        {
            float intensity = EarthquakeManager.Instance.CurrentIntensity;
            float baseChance = GetBaseChance();

            CurrentChance = scaleWithIntensity ? baseChance * intensity : baseChance;

            if (debugMode)
            {
                Debug.Log($"[PlayerStagger] Check: State={_movement.CurrentMoveState}, Intensity={intensity:F2}, Chance={CurrentChance:P1}");
            }

            float roll = Random.value;
            if (roll < CurrentChance)
            {
                if (debugMode) Debug.Log($"[PlayerStagger] STAGGER TRIGGERED! Roll={roll:F3} < Chance={CurrentChance:F3}");
                TriggerStagger();
            }
        }

        private float GetBaseChance()
        {
            return _movement.CurrentMoveState switch
            {
                MoveState.Idle => idleChance,
                MoveState.Walking => walkChance,
                MoveState.Running => runChance,
                MoveState.Crouching => 0f,
                _ => walkChance
            };
        }

        private void TriggerStagger()
        {
            if (_staggerCoroutine != null)
            {
                StopCoroutine(_staggerCoroutine);
            }

            _staggerCoroutine = StartCoroutine(StaggerRoutine());
        }

        private IEnumerator StaggerRoutine()
        {
            _isStaggering = true;
            _lastStaggerTime = Time.time;

            _driftDirection = Random.value < 0.5f ? -1 : 1;

            if (debugMode) 
            {
                string dirName = _driftDirection < 0 ? "LEFT" : "RIGHT";
                Debug.Log($"[PlayerStagger] Player staggering {dirName}!");
            }

            _movement.SetSpeedMultiplier(slowdownMultiplier);

            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(impulseForce);
            }

            float elapsed = 0f;
            float currentDrift = 0f;

            while (elapsed < staggerDuration)
            {
                if (_movement.IsCrouching)
                {
                    if (debugMode) Debug.Log("[PlayerStagger] Crouch detected - ending stagger early.");
                    break;
                }

                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / staggerDuration;

                float driftEnvelope = Mathf.Sin(normalizedTime * Mathf.PI);
                currentDrift = Mathf.Lerp(currentDrift, driftEnvelope * driftStrength, driftRampSpeed * Time.deltaTime);

                Vector3 driftVector = transform.right * _driftDirection * currentDrift;
                _movement.SetExternalDrift(driftVector);

                yield return null;
            }

            EndStagger();

            if (debugMode) Debug.Log("[PlayerStagger] Stagger ended.");
        }

        private void EndStagger()
        {
            _movement.SetSpeedMultiplier(1f);
            _movement.SetExternalDrift(Vector3.zero);
            _isStaggering = false;
        }
    }
}
