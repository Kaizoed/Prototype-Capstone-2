using UnityEngine;

public class TutorialObjectiveTrigger : MonoBehaviour
{
    public static TutorialObjectiveTrigger Instance;

    [SerializeField] private TutorialObjectiveUI objectiveUI;

    private void Awake()
    {
        Instance = this;
    }

    public void CompleteObjective(string id)
    {
        if (objectiveUI != null)
            objectiveUI.CompleteObjective(id);
    }
}