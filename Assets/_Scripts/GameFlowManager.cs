using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    public enum GameStep
    {
        Intro,                  // Cutscene + Dialogue Part 1
        GoBag,                  // Go Bag tutorial
        DialoguePart2,          // Resume teacher dialogue
        EarthquakeResponse,     // Earthquake starts, player must crouch/hide
        GuardEvacuationCutscene,// Guard enters and tells everyone to evacuate
        FallInLine,             // Optional: player lines up first
        Evacuate,               // Player can finally move with WASD
        End
    }

    public GameStep currentStep = GameStep.Intro;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SetStep(GameStep newStep)
    {
        currentStep = newStep;
        Debug.Log("Current Step: " + currentStep);
    }
}