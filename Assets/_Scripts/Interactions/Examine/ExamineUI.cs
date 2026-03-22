using TMPro;
using UnityEngine;

namespace ShakySurvival.Interactions.Examine
{
    public class ExamineUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The ExamineController on the Player.")]
        [SerializeField] private ExamineController examineController;

        [Header("UI Elements")]
        [SerializeField] private CanvasGroup panelGroup;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("Fade (optional)")]
        [SerializeField] private float fadeDuration = 0.2f;

        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            SetPanelVisible(false);
        }

        private void OnEnable()
        {
            if (examineController != null)
            {
                examineController.OnExamineStarted += HandleExamineStarted;
                examineController.OnExamineStopped += HandleExamineStopped;
            }
            SetPanelVisible(false);
        }

        private void OnDisable()
        {
            if (examineController != null)
            {
                examineController.OnExamineStarted -= HandleExamineStarted;
                examineController.OnExamineStopped -= HandleExamineStopped;
            }
        }

        private void HandleExamineStarted(ExamineInteractable interactable)
        {
            ExamineData data = interactable.Data;

            if (data != null)
            {
                if (nameText != null) nameText.text = data.objectName;
                if (descriptionText != null) descriptionText.text = data.description;
            }
            else
            {
                if (nameText != null) nameText.text = interactable.gameObject.name;
                if (descriptionText != null) descriptionText.text = "";
            }

            FadeTo(1f);
        }

        private void HandleExamineStopped()
        {
            FadeTo(0f);
        }

        // ── Helpers ─────────────────────────────────────────────

        private void SetPanelVisible(bool visible)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.interactable = visible;
            panelGroup.blocksRaycasts = visible;
        }

        private void FadeTo(float target)
        {
            if (panelGroup == null) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

            if (fadeDuration <= 0f)
            {
                SetPanelVisible(target > 0.5f);
                return;
            }

            _fadeCoroutine = StartCoroutine(FadeRoutine(target));
        }

        private System.Collections.IEnumerator FadeRoutine(float target)
        {
            float start = panelGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                panelGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
                yield return null;
            }

            panelGroup.alpha = target;
            panelGroup.interactable = target > 0.5f;
            panelGroup.blocksRaycasts = target > 0.5f;
            _fadeCoroutine = null;
        }
    }
}
