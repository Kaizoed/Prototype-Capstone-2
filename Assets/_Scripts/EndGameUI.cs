using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour
{
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToExperimentZone()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("ExperimentZone"); // must match your scene name
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("Quit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}