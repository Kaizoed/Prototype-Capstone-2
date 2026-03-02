using TMPro;
using UnityEngine;

public class QuestItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private GameObject strikeLine;

    public void SetText(string text)
    {
        if (label) label.text = text;
    }

    public void SetCompleted(bool completed)
    {
        if (strikeLine) strikeLine.SetActive(completed);

        if (label)
        {
            var c = label.color;
            c.a = completed ? 0.6f : 1f;
            label.color = c;
        }
    }

    public void SetActive(bool active)
    {
        if (label)
            label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
    }
}