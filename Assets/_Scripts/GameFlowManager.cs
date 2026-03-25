using UnityEngine;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    public enum GameStep
    {
        GoBag,
        LectureDuckCoverHold,
        DuckCoverHold,
        FallInLine,
        DownedClassmate,
        CallResponders,
        Evacuate,
        End
    }

    public GameStep currentStep = GameStep.GoBag;

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