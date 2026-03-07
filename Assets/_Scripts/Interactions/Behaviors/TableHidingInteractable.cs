using UnityEngine;
using ShakySurvival.Cover;

namespace ShakySurvival.Interactions.Behaviors
{
    // Interaction for hiding under a table.
    // Integrates with the Cover System for entry/exit state management.
    public class TableHidingInteractable : MonoBehaviour, IInteractable
    {
        [Header("Cover Configuration")]
        [SerializeField] private CoverSpot coverSpot;

        [Header("Prompts")]
        [SerializeField] private string hidePrompt = "Hide";
        [SerializeField] private string exitPrompt = "Exit";

        [Header("Quest")]
        [SerializeField] private string hideDeskQuestId = "HideUnderDesk";
        [SerializeField] private bool completeQuestOnHide = true;

        private PlayerCoverController _playerController;
        private bool _questCompleted;

        public string InteractionPrompt
        {
            get
            {
                if (_playerController != null && _playerController.CurrentState == CoverState.Hidden)
                {
                    return exitPrompt;
                }
                return hidePrompt;
            }
        }

        private void Awake()
        {
            // Try to find CoverSpot if not assigned
            if (coverSpot == null)
            {
                coverSpot = GetComponentInChildren<CoverSpot>();
                if (coverSpot == null)
                {
                    Debug.LogError("No CoverSpot found!");
                }
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            if (_playerController == null)
            {
                _playerController = interactor.GetComponent<PlayerCoverController>();
            }

            if (_playerController == null)
            {
                Debug.LogWarning("Interactor has no PlayerCoverController!");
                return false;
            }

            // Check if player can interact based on their current state
            if (!_playerController.CanInteract)
            {
                return false;
            }

            // If player is Idle, check approach angle and occupancy
            if (_playerController.CurrentState == CoverState.Idle)
            {
                if (coverSpot == null) return false;

                // Block if already occupied by an NPC
                if (coverSpot.IsOccupied && coverSpot.Occupant != interactor)
                {
                    return false;
                }

                return coverSpot.IsValidApproach(interactor.transform);
            }

            // If player is Hidden (in this cover), they can exit
            if (_playerController.CurrentState == CoverState.Hidden)
            {
                return true;
            }

            return false;
        }

        public void Interact(GameObject interactor)
        {
            if (_playerController == null)
            {
                _playerController = interactor.GetComponent<PlayerCoverController>();
            }

            if (_playerController == null) return;

            // Toggle based on current state
            if (_playerController.CurrentState == CoverState.Idle)
            {
                bool enteredCover = _playerController.EnterCover(coverSpot);

                if (enteredCover && completeQuestOnHide && !_questCompleted)
                {
                    Debug.Log("Completing quest step: " + hideDeskQuestId);
                    QuestManager.Instance?.CompleteStep(hideDeskQuestId);
                    _questCompleted = true;
                }
            }
            else if (_playerController.CurrentState == CoverState.Hidden)
            {
                _playerController.ExitCover();
            }
        }
    }
}