using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueLine", menuName = "Dialogue/Dialogue Line")]
public class DialogueLineSO : ScriptableObject
{
    [Tooltip("Name displayed before the dialogue text.")]
    public string speakerName;

    [Tooltip("Color applied to the speaker's name via TMP rich text.")]
    public Color speakerColor = Color.white;

    [Tooltip("The dialogue sentence shown with the typewriter effect.")]
    [TextArea(3, 8)]
    public string dialogueText;

    [Tooltip("Seconds between each character reveal. Lower = faster.")]
    [Range(0.01f, 0.2f)]
    public float typingSpeed = 0.05f;
}
