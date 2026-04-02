using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text tutorialText;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowTutorial(string message)
    {
        if (tutorialText != null)
            tutorialText.text = message;

        if (UIVisibilityManager.Instance != null)
            UIVisibilityManager.Instance.ShowTutorialPanel();
    }

    public void ShowTutorial(string message, float duration)
    {
        ShowTutorial(message);

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(HideTutorialAfterDelay(duration));
    }

    public void HideTutorial()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (UIVisibilityManager.Instance != null)
            UIVisibilityManager.Instance.HideTutorialPanel();
    }

    public void UpdateTutorial(string message)
    {
        ShowTutorial(message);
    }

    private IEnumerator HideTutorialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideTutorial();
    }
}