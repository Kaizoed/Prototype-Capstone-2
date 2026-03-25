using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.98f, 0.76f, 0.18f)]
[TrackBindingType(typeof(DialogueUIManager))]
[TrackClipType(typeof(DialogueClip))]
public class DialogueTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        DialogueUIManager uiManager = null;
        var director = graph.GetResolver() as PlayableDirector;
        if (director != null)
        {
            uiManager = director.GetGenericBinding(this) as DialogueUIManager;
        }

        if (uiManager == null)
        {
            Debug.LogWarning("[DialogueTrack] No DialogueUIManager bound to this track. " +
                "Drag your DialogueUIManager into the track binding slot in the Timeline window.");
        }

        foreach (var clip in GetClips())
        {
            var dialogueClip = clip.asset as DialogueClip;
            if (dialogueClip == null) continue;

            dialogueClip.UiManager = uiManager;
        }

        var mixer = base.CreateTrackMixer(graph, go, inputCount);

        if (uiManager != null)
        {
            int inputs = mixer.GetInputCount();
            for (int i = 0; i < inputs; i++)
            {
                Playable input = mixer.GetInput(i);
                if (!input.IsValid()) continue;

                if (input.GetPlayableType() == typeof(DialogueBehaviour))
                {
                    var scriptPlayable = (ScriptPlayable<DialogueBehaviour>)input;
                    var behaviour = scriptPlayable.GetBehaviour();
                    if (behaviour != null)
                        behaviour.UIManager = uiManager;
                }
            }
        }

        return mixer;
    }
}
