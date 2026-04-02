using TMPro;
using UnityEngine;

public class TutorialObjectiveUI : MonoBehaviour
{
    public static TutorialObjectiveUI Instance;

    [System.Serializable]
    public class ObjectiveEntry
    {
        public string id;
        [TextArea] public string text;
        public bool completed;
    }

    [Header("UI")]
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private TMP_Text objectivesText;

    [Header("Objectives")]
    [SerializeField] private ObjectiveEntry[] objectives;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        RefreshUI();

        if (objectivePanel != null)
            objectivePanel.SetActive(false);
    }

    private void Update()
    {
        if (GameFlowManager.Instance == null || objectivePanel == null)
            return;

        var step = GameFlowManager.Instance.currentStep;

        bool shouldShow =
            step == GameFlowManager.GameStep.GoBag ||
            step == GameFlowManager.GameStep.EarthquakeResponse ||
            step == GameFlowManager.GameStep.FallInLine ||
            step == GameFlowManager.GameStep.Evacuate;

        if (objectivePanel.activeSelf != shouldShow)
            objectivePanel.SetActive(shouldShow);
    }

    public void CompleteObjective(string id)
    {
        Debug.Log("Trying to complete objective: " + id);

        for (int i = 0; i < objectives.Length; i++)
        {
            Debug.Log("Checking objective ID: " + objectives[i].id);

            if (objectives[i].id == id)
            {
                if (!objectives[i].completed)
                {
                    objectives[i].completed = true;
                    Debug.Log("Objective completed successfully: " + id);
                    RefreshUI();
                }
                return;
            }
        }

        Debug.LogWarning("Objective not found: " + id);
    }

    public void ResetObjectives()
    {
        for (int i = 0; i < objectives.Length; i++)
        {
            objectives[i].completed = false;
        }

        RefreshUI();

        if (objectivePanel != null)
            objectivePanel.SetActive(false);
    }

    private void RefreshUI()
    {
        if (objectivesText == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        for (int i = 0; i < objectives.Length; i++)
        {
            string status = objectives[i].completed ? "[DONE]" : "[ ]";
            sb.AppendLine(status + " " + objectives[i].text);
        }

        objectivesText.text = sb.ToString();
    }
}