using UnityEngine;

public class UIVisibilityManager : MonoBehaviour
{
    public static UIVisibilityManager Instance;

    [Header("Panels")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private GameObject objectivePanel;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowTutorialPanel()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        if (objectivePanel != null)
            objectivePanel.SetActive(false);
    }

    public void HideTutorialPanel()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }

    public void ShowObjectivePanel()
    {
        if (objectivePanel != null)
            objectivePanel.SetActive(true);
    }

    public void HideObjectivePanel()
    {
        if (objectivePanel != null)
            objectivePanel.SetActive(false);
    }

    public void HideAllPanels()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (objectivePanel != null)
            objectivePanel.SetActive(false);
    }
}