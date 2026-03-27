using UnityEngine;
using ShakySurvival.Cover;
using ShakySurvival.Earthquake;

namespace ShakySurvival.Interactions.Behaviors
{
    public class TableHidingInteractable : MonoBehaviour, IInteractable
    {
        [Header("Cover Configuration")]
        [SerializeField] private CoverSpot coverSpot;

        [Header("Prompts")]
        [SerializeField] private string hidePrompt = "Hide";
        [SerializeField] private string exitPrompt = "Exit";

        [Header("Guard")]
        [SerializeField] private GuardEvacuationManager guardEvacuationManager;

        private PlayerCoverController _playerController;

        private bool _earthquakeActive;
        private bool _playerSuccessfullyHidden;
        private bool _guardSequenceStarted;

        public string InteractionPrompt
        {
            get
            {
                if (!_earthquakeActive)
                    return string.Empty;

                if (GameFlowManager.Instance == null)
                    return string.Empty;

                if (GameFlowManager.Instance.currentStep != GameFlowManager.GameStep.EarthquakeResponse)
                    return string.Empty;

                if (_playerController != null && _playerController.CurrentState == CoverState.Hidden)
                    return exitPrompt;

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
            _playerSuccessfullyHidden = false;
            _guardSequenceStarted = false;

            if (GameFlowManager.Instance != null)
            {
                GameFlowManager.Instance.SetStep(GameFlowManager.GameStep.EarthquakeResponse);
            }

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ShowTutorial("Press CTRL to crouch.");
            }
        }

        private void HandleEarthquakeStop()
        {
            _earthquakeActive = false;

            if (_guardSequenceStarted)
                return;

            if (_playerSuccessfullyHidden)
            {
                Debug.Log("Player successfully took cover. Starting guard cutscene.");

                _guardSequenceStarted = true;

                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.HideTutorial();
                }

                if (guardEvacuationManager != null && guardEvacuationManager.ClassroomNPCManager != null)
                {
                    guardEvacuationManager.ClassroomNPCManager.DisableNPCBehaviors();
                    guardEvacuationManager.ClassroomNPCManager.FreezeEarthquakeNPCsForCutscene();
                }

                if (guardEvacuationManager != null)
                {
                    guardEvacuationManager.StartGuardCutscene();
                }
                else
                {
                    Debug.LogWarning("GuardEvacuationManager is not assigned.");
                }
            }
            else
            {
                Debug.Log("Player did NOT take cover properly.");

                if (TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.HideTutorial();
                }
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            if (!_earthquakeActive)
                return false;

            if (GameFlowManager.Instance == null)
                return false;

            if (GameFlowManager.Instance.currentStep != GameFlowManager.GameStep.EarthquakeResponse)
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
                return false;

            if (_playerController.CurrentState == CoverState.Idle)
            {
                if (coverSpot == null)
                    return false;

                if (coverSpot.IsOccupied && coverSpot.Occupant != interactor)
                    return false;

                return true;
            }

            if (_playerController.CurrentState == CoverState.Hidden)
                return true;

            return false;
        }

        public void Interact(GameObject interactor)
        {
            if (_playerController == null)
            {
                _playerController = interactor.GetComponent<PlayerCoverController>();
            }

            if (_playerController == null)
                return;

            if (GameFlowManager.Instance == null)
                return;

            if (GameFlowManager.Instance.currentStep != GameFlowManager.GameStep.EarthquakeResponse)
                return;

            if (_playerController.CurrentState == CoverState.Idle)
            {
                bool enteredCover = _playerController.EnterCover(coverSpot);

                if (enteredCover)
                {
                    _playerSuccessfullyHidden = true;

                    Debug.Log("Player is now safely under the table.");

                    if (TutorialManager.Instance != null)
                    {
                        TutorialManager.Instance.ShowTutorial("Stay under the table until shaking stops.");
                    }
                }
            }
            else if (_playerController.CurrentState == CoverState.Hidden)
            {
                _playerController.ExitCover();
                _playerSuccessfullyHidden = false;

                if (_earthquakeActive && TutorialManager.Instance != null)
                {
                    TutorialManager.Instance.ShowTutorial("Press F near the table to hide.");
                }
            }
        }
    }
}