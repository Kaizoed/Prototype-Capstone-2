using UnityEngine;
using ShakySurvival.Cover;
using ShakySurvival.Earthquake;

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
        [SerializeField] private string hideDeskQuestId = "hide_under_desk";
        [SerializeField] private bool completeQuestOnHide = true;

        private PlayerCoverController _playerController;
        private bool _questCompleted;
        private bool _earthquakeActive;

        public string InteractionPrompt
        {
            get
            {
                if (!_earthquakeActive)
                    return string.Empty;

                if (_playerController != null && _playerController.CurrentState == CoverState.Hidden)
                {
                    return exitPrompt;
                }

                return hidePrompt;
            }
        }

        private void Awake()
        {
            if (coverSpot == null)
            {
                coverSpot = GetComponentInChildren<CoverSpot>();
                if (coverSpot == null)
                {
                    Debug.LogError("No CoverSpot found!");
                }
            }
        }

        private void OnEnable()
        {
            EarthquakeEvents.OnEarthquakeStart += HandleEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop += HandleEarthquakeStop;
        }

        private void OnDisable()
        {
            EarthquakeEvents.OnEarthquakeStart -= HandleEarthquakeStart;
            EarthquakeEvents.OnEarthquakeStop -= HandleEarthquakeStop;
        }

        private void HandleEarthquakeStart()
        {
            _earthquakeActive = true;
        }

        private void HandleEarthquakeStop()
        {
            _earthquakeActive = false;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (!_earthquakeActive)
                return false;

            if (_playerController == null)
            {
                _playerController = interactor.GetComponent<PlayerCoverController>();
            }

            if (_playerController == null)
            {
                Debug.LogWarning("Interactor has no PlayerCoverController!");
                return false;
            }

            if (!_playerController.CanInteract)
            {
                return false;
            }

            if (_playerController.CurrentState == CoverState.Idle)
            {
                if (coverSpot == null) return false;

                if (coverSpot.IsOccupied && coverSpot.Occupant != interactor)
                {
                    return false;
                }

                return coverSpot.IsValidApproach(interactor.transform);
            }

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