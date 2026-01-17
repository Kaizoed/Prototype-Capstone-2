using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private TMP_Text tutorialText;

    [Header("References")]
    [SerializeField] private PlayerController player;

    [Header("Hold Step Settings")]
    [SerializeField] private float holdDuration = 3f;

    public int CurrentStep { get; private set; } = 0;

    private Coroutine holdRoutine;

    private readonly string[] stepMessages =
    {
        "Use WASD to move.",
        "Move the mouse to look around.",
        "Press CTRL to crouch.",
        "Go under the desk.",
        "Stay crouched and do not move for 3 seconds."
    };

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartTutorial();
    }

    public void StartTutorial()
    {
        CurrentStep = 0;
        tutorialUI.SetActive(true);
        UpdateUI();
    }

    public void CompleteStep()
    {
        // Stop hold routine when leaving/finishing the hold step
        if (holdRoutine != null)
        {
            StopCoroutine(holdRoutine);
            holdRoutine = null;
        }

        CurrentStep++;

        if (CurrentStep >= stepMessages.Length)
        {
            EndTutorial();
            return;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (tutorialText != null && CurrentStep < stepMessages.Length)
        {
            tutorialText.text = stepMessages[CurrentStep];
        }

        Debug.Log("Tutorial Step: " + CurrentStep);
    }

    public void StartHoldCheck()
    {
        // Only start during step 4
        if (CurrentStep != 4) return;

        if (holdRoutine == null)
            holdRoutine = StartCoroutine(HoldRoutine());
    }

    private IEnumerator HoldRoutine()
    {
        float timer = 0f;

        while (timer < holdDuration)
        {
            // Must be crouched and not moving
            if (!player.IsCrouched() || player.IsMoving())
            {
                timer = 0f;
            }
            else
            {
                timer += Time.deltaTime;
            }

            yield return null;
        }

        holdRoutine = null;
        CompleteStep();
    }

    private void EndTutorial()
    {
        tutorialUI.SetActive(false);
        Debug.Log("Tutorial Finished");

        // Start earthquake gameplay
        if (EarthquakeController.Instance != null)
        {
            EarthquakeController.Instance.StartQuake(1f);
        }
    }
}
