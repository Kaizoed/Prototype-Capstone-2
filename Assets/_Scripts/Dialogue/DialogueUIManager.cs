using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIManager : MonoBehaviour
{
    // ── Singleton ───────────────────────────────────────────────────────
    public static DialogueUIManager Instance { get; private set; }

    // ── Inspector References ────────────────────────────────────────────
    [Header("UI References")]
    [Tooltip("The TextMeshProUGUI component that displays dialogue.")]
    [SerializeField] private TextMeshProUGUI dialogueTextComponent;

    [Tooltip("The RectTransform of the background panel (must have ContentSizeFitter).")]
    [SerializeField] private RectTransform backgroundRect;

    [Tooltip("Optional: the parent GameObject to show/hide the entire dialogue panel.")]
    [SerializeField] private GameObject dialoguePanel;

    // ── Private State ───────────────────────────────────────────────────
    private Coroutine _typewriterCoroutine;

    // ── Unity Lifecycle ─────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        HideDialogue();
    }

    // ── Public API ──────────────────────────────────────────────────────

    public void PlayDialogue(DialogueLineSO line)
    {
        if (line == null) return;

        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        string hexColor = ColorUtility.ToHtmlStringRGBA(line.speakerColor);
        string speakerPrefix = $"{line.speakerName}: ";
        string formattedText = $"<color=#{hexColor}>{line.speakerName}:</color> {line.dialogueText}";

        dialogueTextComponent.text = formattedText;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);

        int speakerVisibleChars = speakerPrefix.Length;

        _typewriterCoroutine = StartCoroutine(TypewriterRoutine(
            dialogueTextComponent, speakerVisibleChars, line.typingSpeed));
    }

    public void HideDialogue()
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }

        if (dialogueTextComponent != null)
        {
            dialogueTextComponent.text = string.Empty;
            dialogueTextComponent.maxVisibleCharacters = 0;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    // ── Typewriter Coroutine ────────────────────────────────────────────

    private IEnumerator TypewriterRoutine(TextMeshProUGUI tmp, int startFrom, float speed)
    {
        tmp.ForceMeshUpdate();
        int visibleTotal = tmp.textInfo.characterCount;

        tmp.maxVisibleCharacters = startFrom;

        for (int i = startFrom; i <= visibleTotal; i++)
        {
            tmp.maxVisibleCharacters = i;
            yield return new WaitForSeconds(speed);
        }

        _typewriterCoroutine = null;
    }
}
