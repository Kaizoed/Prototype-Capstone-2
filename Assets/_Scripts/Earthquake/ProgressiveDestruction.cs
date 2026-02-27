using UnityEngine;

namespace ShakySurvival.Earthquake
{
    public class ProgressiveDestruction : MonoBehaviour
    {
        private enum DestructionPhase { Intact, Cracked, Collapsed }

        [Header("Wall References")]
        [SerializeField] private GameObject baseWall;
        [SerializeField] private GameObject fracturedWall;

        [Header("Magnitude Thresholds")]
        [SerializeField] private float crackingThreshold = 5.0f;
        [SerializeField] private float collapseThreshold = 7.0f;

        [Header("Reactor Settings (applied on collapse)")]
        [Tooltip("Optional weight data assigned to each fragment's EarthquakeReactor.")]
        [SerializeField] private EarthquakeWeightData weightData;

        private Rigidbody[] _fracturedBodies;
        private DestructionPhase _currentPhase = DestructionPhase.Intact;

        private void Awake()
        {
            // Cache all child rigidbodies once so we never allocate during the earthquake.
            if (fracturedWall != null)
            {
                _fracturedBodies = fracturedWall.GetComponentsInChildren<Rigidbody>(true);
            }
        }

        private void OnEnable()
        {
            EarthquakeEvents.OnIntensityChange += OnIntensityChange;
        }

        private void OnDisable()
        {
            EarthquakeEvents.OnIntensityChange -= OnIntensityChange;
        }

        private void Start()
        {
            // Ensure we begin in the Intact state.
            SetPhaseIntact();
        }

        private void OnIntensityChange(EarthquakeStrength strength)
        {
            float effectiveMagnitude = GetEffectiveMagnitude(strength);

            switch (_currentPhase)
            {
                case DestructionPhase.Intact:
                    if (effectiveMagnitude >= crackingThreshold)
                    {
                        // Jump straight to collapse if the reading exceeds both thresholds.
                        if (effectiveMagnitude >= collapseThreshold)
                        {
                            SetPhaseCracked();
                            SetPhaseCollapsed();
                        }
                        else
                        {
                            SetPhaseCracked();
                        }
                    }
                    break;

                case DestructionPhase.Cracked:
                    if (effectiveMagnitude >= collapseThreshold)
                    {
                        SetPhaseCollapsed();
                    }
                    break;

                case DestructionPhase.Collapsed:
                    break;
            }
        }

        private static float GetEffectiveMagnitude(EarthquakeStrength strength)
        {
            float fullNorm = MagnitudePhysics.MagnitudeToNormalized(strength.Magnitude);

            if (fullNorm <= 0f) return 0f;

            float envelope = Mathf.Clamp01(strength.NormalizedIntensity / fullNorm);
            return strength.Magnitude * envelope;
        }

        private void SetPhaseIntact()
        {
            _currentPhase = DestructionPhase.Intact;

            if (baseWall != null) baseWall.SetActive(true);
            if (fracturedWall != null) fracturedWall.SetActive(false);
        }

        private void SetPhaseCracked()
        {
            _currentPhase = DestructionPhase.Cracked;

            if (baseWall != null) baseWall.SetActive(false);

            if (fracturedWall != null)
            {
                fracturedWall.SetActive(true);

                // Keep every fragment frozen in place so the wall looks cracked but holds together.
                SetFracturedBodiesKinematic(true);
            }
        }

        private void SetPhaseCollapsed()
        {
            _currentPhase = DestructionPhase.Collapsed;

            // Release all fragments so they react to gravity and earthquake jolts.
            SetFracturedBodiesKinematic(false);

            // Dynamically add EarthquakeReactor to each fragment so they receive jolts.
            AddReactorsToFragments();
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void SetFracturedBodiesKinematic(bool isKinematic)
        {
            if (_fracturedBodies == null) return;

            for (int i = 0; i < _fracturedBodies.Length; i++)
            {
                _fracturedBodies[i].isKinematic = isKinematic;
            }
        }

        private void AddReactorsToFragments()
        {
            if (_fracturedBodies == null) return;

            for (int i = 0; i < _fracturedBodies.Length; i++)
            {
                GameObject fragment = _fracturedBodies[i].gameObject;

                // Skip if a reactor already exists (safety check).
                if (fragment.GetComponent<EarthquakeReactor>() != null) continue;

                EarthquakeReactor reactor = fragment.AddComponent<EarthquakeReactor>();

                if (weightData != null)
                {
                    reactor.SetWeightData(weightData);
                }
            }
        }
    }
}
