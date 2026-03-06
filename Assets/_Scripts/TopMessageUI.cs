using TMPro;
using UnityEngine;
using System.Collections;

public class TopMessageUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text messageText;

    public void ShowMessage(string message)
    {
        if (panel != null) panel.SetActive(true);
        if (messageText != null) messageText.text = message;
    }

    public void HideMessage()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void ShowMessageForSeconds(string message, float seconds)
    {
        StopAllCoroutines();
        StartCoroutine(ShowRoutine(message, seconds));
    }

    private IEnumerator ShowRoutine(string message, float seconds)
    {
        ShowMessage(message);
        yield return new WaitForSeconds(seconds);
        HideMessage();
    }
}