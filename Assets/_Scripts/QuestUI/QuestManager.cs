using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [SerializeField] private QuestUI_FixedList ui;

    [System.Serializable]
    public class Step
    {
        public string id;
        [TextArea] public string text;
    }

    [SerializeField] private Step[] steps;
    private int currentIndex = 0;

    void Awake() => Instance = this;

    void Start() => RefreshUI();

    void RefreshUI()
    {
        string[] texts = new string[steps.Length];
        for (int i = 0; i < steps.Length; i++) texts[i] = steps[i].text;

        ui.SetSteps(texts, currentIndex);
    }

    public void CompleteStep(string id)
    {
        if (currentIndex >= steps.Length) return;

        if (steps[currentIndex].id == id)
        {
            currentIndex++;
            RefreshUI();
        }
    }
}
