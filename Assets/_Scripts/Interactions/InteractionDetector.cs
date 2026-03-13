using UnityEngine;
using System;
using ShakySurvival.Interactions;

namespace ShakySurvival.Interactions
{
    public class InteractionDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [SerializeField] private float detectionRange = 3.0f;
        [SerializeField] private LayerMask interactableLayer;
        [SerializeField] private Transform detectionOrigin;

        private IInteractable _currentInteractable;

        public IInteractable CurrentInteractable => _currentInteractable;

        private void Update()
        {
            DetectInteractable();
        }

        private void DetectInteractable()
        {
            if (detectionOrigin == null)
            {
                _currentInteractable = null;
                return;
            }

            Ray ray = new Ray(detectionOrigin.position, detectionOrigin.forward);
            RaycastHit[] hits = Physics.RaycastAll(ray, detectionRange, interactableLayer, QueryTriggerInteraction.Collide);

            if (hits.Length == 0)
            {
                _currentInteractable = null;
                return;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            _currentInteractable = null;

            foreach (RaycastHit hit in hits)
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();

                if (interactable == null)
                    interactable = hit.collider.GetComponentInParent<IInteractable>();

                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    _currentInteractable = interactable;
                    return;
                }
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