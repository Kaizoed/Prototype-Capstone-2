using UnityEngine;

public class CoverManager : MonoBehaviour
{
    public static CoverManager Instance;

    [SerializeField] private Transform[] coverPoints;

    private void Awake()
    {
        Instance = this;
    }

    public Transform GetNearestCover(Vector3 pos)
    {
        Transform best = null;
        float bestDist = float.MaxValue;

        foreach (var c in coverPoints)
        {
            float d = Vector3.Distance(pos, c.position);
            if (d < bestDist)
            {
                best = c;
                bestDist = d;
            }
        }

        return best;
    }
}
