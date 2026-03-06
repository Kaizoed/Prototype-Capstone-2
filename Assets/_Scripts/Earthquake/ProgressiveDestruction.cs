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

        [Header("NPC Reaction On Collapse")]
        [SerializeField] private ClassmateSequenceController classmateSequenceController;

        private Rigidbody[] _fracturedBodies;
        private DestructionPhase _currentPhase = DestructionPhase.Intact;

        private void Awake()
        {
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
                SetFracturedBodiesKinematic(true);
            }
        }

        private void SetPhaseCollapsed()
        {
            _currentPhase = DestructionPhase.Collapsed;

            SetFracturedBodiesKinematic(false);
            AddReactorsToFragments();

            // Tell the classmate NPC to fall when the wall collapses
            if (classmateSequenceController != null)
            {
                classmateSequenceController.OnWallExplosionHit();
            }
        }

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