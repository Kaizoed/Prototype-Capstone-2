using UnityEngine;

public abstract class GOAPAction : MonoBehaviour
{
    public abstract bool CanRun();
    public abstract bool IsComplete();
    public abstract void Run();
    public abstract void Stop();
}
