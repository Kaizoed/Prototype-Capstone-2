using UnityEngine;
using UnityEngine.Playables;

public class DialogueClip : PlayableAsset
{
    [Tooltip("The dialogue data ScriptableObject for this clip.")]
    public DialogueLineSO dialogueLine;

    [System.NonSerialized]
    public DialogueUIManager UiManager;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DialogueBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        behaviour.DialogueLine = dialogueLine;
        behaviour.UIManager = UiManager;

        return playable;
    }
}
