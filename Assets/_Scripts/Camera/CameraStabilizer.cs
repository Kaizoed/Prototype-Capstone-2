using UnityEngine;

namespace ShakySurvival.Camera
{
    public class CameraStabilizer : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField, Tooltip("The head bone or eye socket to follow")]
        private Transform headBoneTarget;

        [Header("Stabilization Settings")]
        [SerializeField, Tooltip("0 = No smoothing (raw follow), 1 = Maximum smoothing")]
        [Range(0f, 1f)]
        private float stabilityFactor = 0.85f;

        [SerializeField, Tooltip("How quickly position responds. Higher = less filtering, more responsive.")]
        [Range(1f, 50f)]
        private float positionCutoffFrequency = 8f;

        [SerializeField, Tooltip("How quickly rotation responds. Higher = less filtering.")]
        [Range(1f, 50f)]
        private float rotationCutoffFrequency = 12f;

        [Header("Mode")]
        [SerializeField, Tooltip("When true, follows head bone exactly with no filtering (for cutscenes)")]
        private bool cinematicMode = false;

        private Vector3 _filteredPosition;
        private Quaternion _filteredRotation;
        private bool _initialized;

        public float StabilityFactor
        {
            get => stabilityFactor;
            set => stabilityFactor = Mathf.Clamp01(value);
        }

        public bool CinematicMode
        {
            get => cinematicMode;
            set => cinematicMode = value;
        }

        public void SnapToTarget()
        {
            if (headBoneTarget != null)
            {
                _filteredPosition = headBoneTarget.position;
                _filteredRotation = headBoneTarget.rotation;
                transform.position = _filteredPosition;
                // Note: We don't set rotation here - PlayerLook handles that
            }
        }

        private void Start()
        {
            if (headBoneTarget != null)
            {
                _filteredPosition = headBoneTarget.position;
                _filteredRotation = headBoneTarget.rotation;
                _initialized = true;
            }
        }

        private void LateUpdate()
        {
            if (headBoneTarget == null) return;

            if (!_initialized)
            {
                _filteredPosition = headBoneTarget.position;
                _filteredRotation = headBoneTarget.rotation;
                _initialized = true;
            }

            Vector3 targetPosition = headBoneTarget.position;

            if (cinematicMode || stabilityFactor <= 0.001f)
            {
                // Cinematic mode: Follow exactly with no filtering (will most likely be used for cover system)
                transform.position = targetPosition;
            }
            else
            {
                // Apply low-pass filter for position
                float effectiveCutoff = Mathf.Lerp(50f, positionCutoffFrequency, stabilityFactor);
                _filteredPosition = LowPassFilter(_filteredPosition, targetPosition, effectiveCutoff);
                transform.position = _filteredPosition;
            }
        }

        private Vector3 LowPassFilter(Vector3 current, Vector3 target, float cutoffFrequency)
        {
            float dt = Time.deltaTime;
            float rc = 1f / (2f * Mathf.PI * cutoffFrequency);
            float alpha = dt / (rc + dt);

            return Vector3.Lerp(current, target, alpha);
        }

        private Quaternion LowPassFilterRotation(Quaternion current, Quaternion target, float cutoffFrequency)
        {
            float dt = Time.deltaTime;
            float rc = 1f / (2f * Mathf.PI * cutoffFrequency);
            float alpha = dt / (rc + dt);

            return Quaternion.Slerp(current, target, alpha);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Clamp values in editor
            stabilityFactor = Mathf.Clamp01(stabilityFactor);
            positionCutoffFrequency = Mathf.Clamp(positionCutoffFrequency, 1f, 50f);
            rotationCutoffFrequency = Mathf.Clamp(rotationCutoffFrequency, 1f, 50f);
        }

        private void OnDrawGizmosSelected()
        {
            if (headBoneTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(headBoneTarget.position, 0.05f);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, headBoneTarget.position);
            }
        }
#endif
    }
}
