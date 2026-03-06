using TMPro;
using UnityEngine;
using ShakySurvival.Player;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Player Control Lock")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerLook playerLook;

    private string[] _currentLines;
    private int _currentIndex;
    private bool _isDialogueActive;

    public bool IsDialogueActive => _isDialogueActive;

    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.lines == null || dialogue.lines.Length == 0)
            return;

        _isDialogueActive = true;
        _currentLines = dialogue.lines;
        _currentIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        if (speakerNameText != null)
            speakerNameText.text = dialogue.speakerName;

        if (dialogueText != null)
            dialogueText.text = _currentLines[_currentIndex];

        // Lock player controls
        playerMovement?.LockInput();
        playerLook?.LockLook();
    }

    public void NextLine()
    {
        if (!_isDialogueActive) return;

        _currentIndex++;

        if (_currentIndex >= _currentLines.Length)
        {
            EndDialogue();
            return;
        }

        if (dialogueText != null)
            dialogueText.text = _currentLines[_currentIndex];
    }

    public void EndDialogue()
    {
        _isDialogueActive = false;
        _currentLines = null;
        _currentIndex = 0;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Unlock player controls
        playerMovement?.UnlockInput();
        playerLook?.UnlockLook();
    }
}