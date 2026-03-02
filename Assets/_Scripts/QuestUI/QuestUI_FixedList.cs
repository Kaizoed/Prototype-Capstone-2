using UnityEngine;

public class QuestUI_FixedList : MonoBehaviour
{
    [SerializeField] private QuestItemUI[] items; // drag your QuestItemUI objects here in order

    public void SetSteps(string[] stepTexts, int currentIndex)
    {
        for (int i = 0; i < items.Length; i++)
        {
            bool hasStep = i < stepTexts.Length;

            // hide unused rows if fewer steps than UI slots
            items[i].gameObject.SetActive(hasStep);

            if (!hasStep) continue;

            items[i].SetText(stepTexts[i]);

            bool completed = i < currentIndex;
            bool active = i == currentIndex;

            items[i].SetCompleted(completed);
            items[i].SetActive(active);
        }
    }
}