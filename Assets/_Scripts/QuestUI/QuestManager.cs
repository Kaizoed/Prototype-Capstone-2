using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [System.Serializable]
    public class Step
    {
        public string id;
        [TextArea] public string text;
    }

    [SerializeField] private QuestUI_FixedList ui;
    [SerializeField] private Step[] steps;

    private int currentIndex = 0;

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
        Debug.Log("Quest steps count: " + (steps != null ? steps.Length : 0));

        if (steps != null)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                Debug.Log("Quest Step " + i + ": " + steps[i].id);
            }
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (ui == null || steps == null) return;

        string[] texts = new string[steps.Length];
        for (int i = 0; i < steps.Length; i++)
            texts[i] = steps[i].text;

        ui.SetSteps(texts, currentIndex);
    }

    public void CompleteStep(string id)
    {
        if (steps == null || currentIndex >= steps.Length) return;
        if (steps[currentIndex].id != id) return;

        currentIndex++;
        RefreshUI();
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