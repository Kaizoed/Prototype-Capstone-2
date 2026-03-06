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
        RefreshUI();
    }

    private void RefreshUI()
    {
        string[] texts = new string[steps.Length];
        for (int i = 0; i < steps.Length; i++)
            texts[i] = steps[i].text;

        ui.SetSteps(texts, currentIndex);
    }

    public void CompleteStep(string id)
    {
        if (currentIndex >= steps.Length) return;

        if (steps[currentIndex].id != id) return;

        currentIndex++;
        RefreshUI();
    }
}