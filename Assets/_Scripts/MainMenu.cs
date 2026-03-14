using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string mainLevel;
    public string experimentZone;   // NEW

    public void OpenStartGame()
    {
        SceneManager.LoadScene(mainLevel);
    }

    public void OpenExperimentZone()   // NEW
    {
        SceneManager.LoadScene(experimentZone);
    }

    public void OpenLoadGame()
    {

    }

    public void OpenTutorial()
    {

    }

    public void OpenSettings()
    {

    }

    public void CloseSettings()
    {

    }

    public void QuitGame()
    {
        Application.Quit();
    }
}