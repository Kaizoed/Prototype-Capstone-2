using TMPro;
using UnityEngine;
using ShakySurvival.Interactions;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private PlayerInteractor playerInteractor;
    [SerializeField] private TMP_Text promptText;

    void Update()
    {
        if (playerInteractor == null || promptText == null) return;

        string prompt = playerInteractor.GetCurrentInteractionPrompt();

        if (string.IsNullOrEmpty(prompt))
        {
            promptText.text = "";
        }
        else
        {
            promptText.text = prompt;
        }
    }
}