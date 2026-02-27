using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject tutorialPanel;
    public TMP_Text tutorialText;

    [Header("Steps")]
    public TutorialStep[] steps;

    private int stepIndex = 0;
    private bool waitingForKey = false;

    void Start()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("No tutorial steps set.");
            return;
        }

        StartStep(0);
    }

    void Update()
    {
        if (!waitingForKey) return;

        // Only allow the keys listed in the current step
        var keys = steps[stepIndex].advanceKeys;
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

        // Freeze gameplay
        Time.timeScale = 0f;

        // Show UI
        tutorialPanel.SetActive(true);
        tutorialText.text = steps[stepIndex].instruction;

        waitingForKey = true;
    }

    IEnumerator AdvanceRoutine()
    {
        // Hide tutorial UI and unfreeze
        tutorialPanel.SetActive(false);
        Time.timeScale = 1f;

        float playTime = steps[stepIndex].playSecondsAfterPress;

        // If you want a short "play window" before next freeze:
        if (playTime > 0f)
        {
            yield return new WaitForSecondsRealtime(playTime);
        }
        else
        {
            // If 0, we instantly go to next step (still works, but gameplay barely runs)
            yield return null;
        }

        int next = stepIndex + 1;

        if (next >= steps.Length)
        {
            // Tutorial done: keep gameplay running
            Time.timeScale = 1f;
            tutorialPanel.SetActive(false);
            yield break;
        }

        // Freeze again and show next instruction
        StartStep(next);
    }
}