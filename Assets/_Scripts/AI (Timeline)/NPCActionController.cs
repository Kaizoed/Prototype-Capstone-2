using UnityEngine;

public class NPCActionController : MonoBehaviour
{
    public Animator anim;

    public void WalkOn()
    {
        anim.SetBool("IsWalking", true);
        anim.SetBool("IsRunning", false);
    }

    public void RunOn()
    {
        anim.SetBool("IsRunning", true);
        anim.SetBool("IsWalking", false);
    }

    public void StopMove()
    {
        anim.SetBool("IsRunning", false);
        anim.SetBool("IsWalking", false);
    }

    public void DuckOn()
    {
        anim.SetBool("IsDucking", true);
        StopMove(); // optional: duck means stop moving
    }

    public void DuckOff()
    {
        anim.SetBool("IsDucking", false);
    }
}