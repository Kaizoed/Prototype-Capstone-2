using UnityEngine;
using TMPro;
using System.Collections;
using ShakySurvival.Earthquake;

public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject tutorialPanel;
    public TMP_Text tutorialText;

    [Header("Steps")]
    public TutorialStep[] steps;

    [Header("Objectives")]
    [SerializeField] private GameObject questPanel;

    [Header("Earthquake")]
    [SerializeField] private EarthquakeManager earthquakeManager;
    [SerializeField] private float earthquakeDelayAfterTutorial = 2f;

    private int stepIndex = 0;
    private bool waitingForKey = false;

    void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (questPanel != null)
            questPanel.SetActive(false);
    }

    public void StartTutorial()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("No tutorial steps set.");
            return;
        }

        stepIndex = 0;
        StartStep(0);
    }

    void Update()
    {
        if (!waitingForKey) return;

        var keys = steps[stepIndex].GetKeys();

        for (int i = 0; i < keys.Length; i++)
        {
            if (Input.GetKeyDown(keys[i]))
            {
                waitingForKey = false;
                StartCoroutine(AdvanceRoutine());
                break;
            }
        }
    }

    void StartStep(int index)
    {
        stepIndex = index;

        Time.timeScale = 0f;

        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        if (tutorialText != null)
            tutorialText.text = steps[stepIndex].instruction;

        waitingForKey = true;
    }

    IEnumerator AdvanceRoutine()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        Time.timeScale = 1f;

        float playTime = steps[stepIndex].playSecondsAfterPress;

        if (playTime > 0f)
        {
            yield return new WaitForSecondsRealtime(playTime);
        }
        else
        {
            yield return null;
        }

        int next = stepIndex + 1;

        if (next >= steps.Length)
        {
            Time.timeScale = 1f;

            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);

            yield return new WaitForSecondsRealtime(earthquakeDelayAfterTutorial);

            if (questPanel != null)
                questPanel.SetActive(true);

            if (earthquakeManager != null)
            {
                earthquakeManager.StartEarthquake();
            }
            else
            {
                Debug.LogWarning("EarthquakeManager is not assigned.");
            }

            yield break;
        }

        StartStep(next);
    }
}