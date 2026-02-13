using UnityEngine;

public class ActionHold : GOAPAction
{
    public override bool CanRun()
    {
        return WorldState.Instance.earthquakeActive &&
               WorldState.Instance.npcIsUnderCover &&
               WorldState.Instance.npcIsCrouching;
    }

    public override void Run()
    {
        // Do nothing, just hold
    }

    public override bool IsComplete()
    {
        return !WorldState.Instance.earthquakeActive;
    }

    public override void Stop()
    {
        WorldState.Instance.npcIsCrouching = false;
        WorldState.Instance.npcIsUnderCover = false;
    }
}
