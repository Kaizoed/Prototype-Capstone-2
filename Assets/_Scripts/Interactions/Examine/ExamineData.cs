using UnityEngine;

namespace ShakySurvival.Interactions.Examine
{
    [CreateAssetMenu(
        fileName = "New ExamineData",
        menuName = "ShakySurvival/ExamineData",
        order = 0)]
    public class ExamineData : ScriptableObject
    {
        [Header("Display Info")]
        [Tooltip("Name shown in the examine UI (e.g. \"Old Photo\").")]
        public string objectName = "Unknown Object";

        [TextArea(2, 5)]
        [Tooltip("Description shown below the name while examining.")]
        public string description = "";
    }
}
