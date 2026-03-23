using UnityEngine;

namespace ShakySurvival.Interactions.Examine
{
    [RequireComponent(typeof(Collider))]
    public class ExamineInteractable : MonoBehaviour, IInteractable
    {
        [Header("Examine Data")]
        [Tooltip("ScriptableObject holding name + description for this object.")]
        [SerializeField] private ExamineData examineData;

        [Header("Prompt")]
        [SerializeField] private string prompt = "Examine";

        public ExamineData Data => examineData;

        // ── IInteractable ───────────────────────────────────────

        public string InteractionPrompt
        {
            get
            {
                if (ExamineController.Instance != null && ExamineController.Instance.IsExamining)
                    return string.Empty;

                string label = examineData != null ? examineData.objectName : gameObject.name;
                return $"{prompt} {label}";
            }
        }

        public bool CanInteract(GameObject interactor)
        {
            if (!enabled) return false;

            if (ExamineController.Instance != null && ExamineController.Instance.IsExamining)
                return false;

            return true;
        }

        public void Interact(GameObject interactor)
        {
            if (ExamineController.Instance == null)
            {
                Debug.LogError("[ExamineInteractable] No ExamineController found! " +
                               "Add one to the Player GameObject.", this);
                return;
            }

            ExamineController.Instance.StartExamine(this);
        }
    }
}
