using UnityEngine;
using UnityEngine. SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string mainLevel;
    //public string loadGame;
    //public string tutorial;
    //public string settings;
    //public string closeSettings;

    /*Paano gamitin?
    1. iattach ang name ng scene(dapat same ang name sa folder) sa script(nakalagay sa canvas)
    2. ayusin sa functions
    
    kung may kulang or revision pakilagay  na lng*/

    public void OpenStartGame()
    {
         SceneManager.LoadScene(mainLevel);
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
