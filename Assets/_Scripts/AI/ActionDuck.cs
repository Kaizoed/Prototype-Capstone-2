using UnityEngine;

public class ActionDuck : GOAPAction
{
    public override bool CanRun()
    {
        return WorldState.Instance.npcIsUnderCover &&
               !WorldState.Instance.npcIsCrouching;
    }

    public override void Run()
    {
        WorldState.Instance.npcIsCrouching = true;
        // animator.SetBool("Crouch", true);
    }

    public override bool IsComplete() => true;
    public override void Stop() { }
}
