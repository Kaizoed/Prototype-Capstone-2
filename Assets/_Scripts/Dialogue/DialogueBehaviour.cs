using UnityEngine;
using UnityEngine.Playables;

public class DialogueBehaviour : PlayableBehaviour
{
    public DialogueLineSO DialogueLine;
    public DialogueUIManager UIManager;

    private bool _hasPlayed;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;
        if (_hasPlayed) return;

        Debug.Log($"[DialogueBehaviour] Playing dialogue: {DialogueLine.speakerName}");
        UIManager.PlayDialogue(DialogueLine);
        _hasPlayed = true;
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;
        if (UIManager == null) return;
        if (!_hasPlayed) return;

        double duration = playable.GetDuration();
        double time = playable.GetTime();

        if (time < duration - 0.05)
        {

        }
        _hasPlayed = false;
    }
}
