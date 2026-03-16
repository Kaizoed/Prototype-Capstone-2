using UnityEngine;
using System.Collections;
using ShakySurvival.Earthquake;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [System.Serializable]
    public class Step
    {
        public string id;
        [TextArea] public string text;

        [Header("Optional Counter Objective")]
        public bool useCounter;
        public int requiredCount = 1;

        [HideInInspector] public int currentCount = 0;
    }

    [SerializeField] private QuestUI_FixedList ui;
    [SerializeField] private Step[] steps;

    [Header("Earthquake Trigger")]
    [SerializeField] private string earthquakeTriggerStepId = "collect_items";
    [SerializeField] private float earthquakeDelay = 3f;
    [SerializeField] private EarthquakeManager earthquakeManager;

    private int currentIndex = 0;
    private bool waitingForEarthquake = false;

    public int CurrentIndex => currentIndex;

    public string CurrentStepId
    {
        get
        {
            if (steps == null || steps.Length == 0) return string.Empty;
            if (currentIndex < 0 || currentIndex >= steps.Length) return string.Empty;
            return steps[currentIndex].id;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (steps != null)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i].useCounter)
                    steps[i].currentCount = 0;
            }
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (ui == null || steps == null) return;

        string[] texts = new string[steps.Length];

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].useCounter)
                texts[i] = $"{steps[i].text} ({steps[i].currentCount}/{steps[i].requiredCount})";
            else
                texts[i] = steps[i].text;
        }

        ui.SetSteps(texts, currentIndex);
    }

    public void CompleteStep(string id)
    {
        if (steps == null || currentIndex >= steps.Length) return;
        if (steps[currentIndex].id != id) return;

        currentIndex++;
        RefreshUI();
    }

    public void AddStepProgress(string id, int amount = 1)
    {
        if (steps == null || currentIndex >= steps.Length) return;
        if (waitingForEarthquake) return;

        Step currentStep = steps[currentIndex];

        if (currentStep.id != id) return;
        if (!currentStep.useCounter) return;

        currentStep.currentCount += amount;

        if (currentStep.currentCount > currentStep.requiredCount)
            currentStep.currentCount = currentStep.requiredCount;

        RefreshUI();

        if (currentStep.currentCount >= currentStep.requiredCount)
        {
            if (currentStep.id == earthquakeTriggerStepId)
            {
                StartCoroutine(CompleteStepAfterEarthquakeDelay(currentStep.id));
            }
            else
            {
                CompleteStep(currentStep.id);
            }
        }
    }

    private IEnumerator CompleteStepAfterEarthquakeDelay(string stepId)
    {
        waitingForEarthquake = true;

        yield return new WaitForSeconds(earthquakeDelay);

        if (earthquakeManager != null)
        {
            earthquakeManager.StartEarthquake();
        }
        else
        {
            Debug.LogWarning("[QuestManager] EarthquakeManager is not assigned.");
        }

        CompleteStep(stepId);
        waitingForEarthquake = false;
    }

    public void ForceSetCurrentStep(string id)
    {
        if (steps == null || steps.Length == 0) return;

        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].id == id)
            {
                currentIndex = i;
                RefreshUI();
                return;
            }
        }

        Debug.LogWarning("Quest step not found: " + id);
    }
}