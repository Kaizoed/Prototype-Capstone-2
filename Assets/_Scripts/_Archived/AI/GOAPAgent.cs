using UnityEngine;

public class GOAPAgent : MonoBehaviour
{
    private GOAPAction[] actions;
    private GOAPAction currentAction;

    private void Awake()
    {
        actions = GetComponents<GOAPAction>();
    }

    private void Update()
    {
        if (currentAction != null)
        {
            if (currentAction.IsComplete())
            {
                currentAction.Stop();
                currentAction = null;
            }
            return;
        }

        foreach (var action in actions)
        {
            if (action.CanRun())
            {
                currentAction = action;
                action.Run();
                break;
            }
        }
    }
}
