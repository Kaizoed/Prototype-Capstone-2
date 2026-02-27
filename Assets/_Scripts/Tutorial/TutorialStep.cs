using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    [TextArea] public string instruction;

    // Only these keys can advance this step
    public KeyCode[] advanceKeys;

    // Optional: after pressing the key, let gameplay run for a bit (real time),
    // then freeze again and show next step.
    public float playSecondsAfterPress = 0f;
}