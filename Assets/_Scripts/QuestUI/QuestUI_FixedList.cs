using UnityEngine;

public class QuestUI_FixedList : MonoBehaviour
{
    [SerializeField] private QuestItemUI[] items;

    public void SetSteps(string[] stepTexts, int currentIndex)
    {
        for (int i = 0; i < items.Length; i++)
        {
            bool shouldShow = i <= currentIndex && i < stepTexts.Length;

            items[i].gameObject.SetActive(shouldShow);

            if (!shouldShow) continue;

            items[i].SetText(stepTexts[i]);

            bool completed = i < currentIndex;
            bool active = i == currentIndex;

            items[i].SetCompleted(completed);
            items[i].SetActive(active);
        }
    }
}