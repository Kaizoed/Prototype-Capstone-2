using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using ShakySurvival.Earthquake;

public class ExperimentationZoneUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject experimentPanel;
    [SerializeField] private Slider magnitudeSlider;
    [SerializeField] private TMP_Text magnitudeText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Scene Settings")]
    [SerializeField] private string mainSceneName = "MainScene";

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    [SerializeField] private GameObject instructionText;

    private bool isUIOpen = false;

    private void Start()
    {
        if (magnitudeSlider != null)
        {
            magnitudeSlider.minValue = 3f;
            magnitudeSlider.maxValue = 9f;
            magnitudeSlider.wholeNumbers = false;
            magnitudeSlider.value = 5f;

            magnitudeSlider.onValueChanged.AddListener(UpdateMagnitudeUI);
            UpdateMagnitudeUI(magnitudeSlider.value);
        }

        CloseUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleUI();
        }
    }

    private void OnDestroy()
    {
        if (magnitudeSlider != null)
        {
            magnitudeSlider.onValueChanged.RemoveListener(UpdateMagnitudeUI);
        }
    }

    private void UpdateMagnitudeUI(float value)
    {
        if (magnitudeText != null)
        {
            magnitudeText.text = $"Magnitude: {value:F1}";
        }

        if (descriptionText != null)
        {
            descriptionText.text = GetMagnitudeDescription(value);
        }
    }

    private string GetMagnitudeDescription(float value)
    {
        if (value < 4f) return "Minor shaking";
        if (value < 5f) return "Light shaking";
        if (value < 6f) return "Moderate shaking";
        if (value < 7f) return "Strong shaking";
        if (value < 8f) return "Major shaking";
        return "Severe shaking";
    }

    public void ToggleUI()
    {
        if (instructionText != null)
        {
            instructionText.SetActive(false);
        }

        if (isUIOpen)
        {
            CloseUI();
        }
        else
        {
            OpenUI();
        }
    }

    public void OpenUI()
    {
        isUIOpen = true;

        if (experimentPanel != null)
            experimentPanel.SetActive(true);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseUI()
    {
        isUIOpen = false;

        if (experimentPanel != null)
            experimentPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void StartEarthquakeFromSlider()
    {
        if (EarthquakeManager.Instance == null)
        {
            Debug.LogWarning("No EarthquakeManager found in this scene.");
            return;
        }

        float selectedMagnitude = magnitudeSlider.value;

        EarthquakeManager.Instance.StopEarthquake();
        CloseUI();
        EarthquakeManager.Instance.StartEarthquakeFromExperiment(selectedMagnitude);
    }

    public void StopEarthquake()
    {
        if (EarthquakeManager.Instance == null) return;

        EarthquakeManager.Instance.StopEarthquake();
    }

    public void ResetScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMainScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName);
    }
}