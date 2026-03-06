using UnityEngine;
using UnityEngine.InputSystem;
using ShakySurvival.Cover;

namespace ShakySurvival.Interactions
{
    /// <summary>
    /// Handles player input for interactions and delegates execution to the detector's target.
    /// Updated to use Unity's New Input System.
    /// </summary>
    [RequireComponent(typeof(InteractionDetector))]
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string interactActionName = "Interact";
        [SerializeField] private DialogueManager dialogueManager;

        private InteractionDetector _detector;
        private InputActionMap _actionMap;
        private InputAction _interactAction;
        private PlayerCoverController _coverController;

        private void Awake()
        {
            _detector = GetComponent<InteractionDetector>();
            _coverController = GetComponent<PlayerCoverController>();
            SetupInput();
        }

        private void OnEnable()
        {
            if (_interactAction != null)
            {
                _interactAction.Enable();
                _interactAction.performed += OnInteractPerformed;
            }
        }

        private void OnDisable()
        {
            if (_interactAction != null)
            {
                _interactAction.performed -= OnInteractPerformed;
                _interactAction.Disable();
            }
        }

        private void SetupInput()
        {
            if (inputActions == null)
            {
                Debug.LogError("[PlayerInteractor] No InputActionAsset assigned!", this);
                return;
            }

            _actionMap = inputActions.FindActionMap(actionMapName);
            if (_actionMap == null)
            {
                Debug.LogError($"[PlayerInteractor] Could not find Action Map '{actionMapName}'!", this);
                return;
            }

            _interactAction = _actionMap.FindAction(interactActionName);
            if (_interactAction == null)
            {
                Debug.LogError($"[PlayerInteractor] Could not find Action '{interactActionName}' in map '{actionMapName}'! Please add it in the Input Actions Editor.", this);
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            TryInteract();
        }

        private void TryInteract()
        {
            // If dialogue is active, continue dialogue first
            if (dialogueManager != null && dialogueManager.IsDialogueActive)
            {
                dialogueManager.NextLine();
                return;
            }

            // Cover exit bypass: if player is hidden, exit directly without raycast
            if (_coverController != null && _coverController.CurrentState == CoverState.Hidden)
            {
                _coverController.ExitCover();
                return;
            }

            // Normal interaction via raycast
            if (_detector == null) return;

            IInteractable target = _detector.CurrentInteractable;

            if (target != null)
            {
                if (target.CanInteract(this.gameObject))
                {
                    target.Interact(this.gameObject);
                }
            }
        }

        // Optional: Method to get current prompt for UI
        public string GetCurrentInteractionPrompt()
        {
            // Hide interaction prompt while dialogue is active
            if (dialogueManager != null && dialogueManager.IsDialogueActive)
            {
                return string.Empty;
            }

            // Show exit prompt if in cover
            if (_coverController != null && _coverController.CurrentState == CoverState.Hidden)
            {
                return "Exit";
            }

            if (_detector != null &&
                _detector.CurrentInteractable != null &&
                _detector.CurrentInteractable.CanInteract(this.gameObject))
            {
                return _detector.CurrentInteractable.InteractionPrompt;
            }

            return string.Empty;
        }
    }
}

