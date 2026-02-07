using UnityEngine;

namespace ShakySurvival.Interactions
{
    // component responsible for detecting interactable objects in the world.
    // Uses a raycast from the camera/eyes center.
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 3.0f;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private Transform detectionOrigin; // Usually the Camera

        private IInteractable _currentInteractable;

        // The currently detected interactable, if any.
        public IInteractable CurrentInteractable => _currentInteractable;

        private void Update()
        {
            DetectInteractable();
        }

        private void DetectInteractable()
        {
            if (detectionOrigin == null) return;

            Ray ray = new Ray(detectionOrigin.position, detectionOrigin.forward);
            RaycastHit hit;

            // Perform raycast
            if (Physics.Raycast(ray, out hit, detectionRange, interactableLayer))
            {
                // Check if the object has an IInteractable component
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                
                if (interactable == null)
                {
                    interactable = hit.collider.GetComponentInParent<IInteractable>();
                }

                if (interactable != null && interactable != _currentInteractable)
                {
                    _currentInteractable = interactable;
                }
                else if (interactable == null)
                {
                    _currentInteractable = null;
                }
            }
            else
            {
                _currentInteractable = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (detectionOrigin != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(detectionOrigin.position, detectionOrigin.forward * detectionRange);
            }
        }
    }
}
