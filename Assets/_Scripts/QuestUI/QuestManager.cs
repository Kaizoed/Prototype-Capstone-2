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

        if (steps[currentIndex].id != id) return; // prevents skipping

        currentIndex++;
        RefreshUI();
    }
}