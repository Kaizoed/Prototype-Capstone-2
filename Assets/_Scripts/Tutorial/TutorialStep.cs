using UnityEngine;
using System;

[System.Serializable]
public class TutorialStep
{
    [TextArea] public string instruction;

    [Tooltip("Type keys like: W,A,S,D or Space or LeftShift")]
    public string advanceKeysText = "E";

    public float playSecondsAfterPress = 0f;

    public KeyCode[] GetKeys()
    {
        if (string.IsNullOrWhiteSpace(advanceKeysText))
            return Array.Empty<KeyCode>();

        string[] parts = advanceKeysText.Split(',', StringSplitOptions.RemoveEmptyEntries);

        KeyCode[] keys = new KeyCode[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            string s = parts[i].Trim();

            // Try parse KeyCode from the typed text
            if (Enum.TryParse(s, true, out KeyCode key))
                keys[i] = key;
            else
                keys[i] = KeyCode.None; // invalid key typed
        }

        return keys;
    }
}