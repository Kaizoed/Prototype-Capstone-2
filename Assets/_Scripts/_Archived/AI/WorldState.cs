using UnityEngine;

public class WorldState : MonoBehaviour
{
    public static WorldState Instance;

    public bool earthquakeActive;
    public bool npcIsUnderCover;
    public bool npcIsCrouching;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        earthquakeActive = EarthquakeController.Instance != null &&
                           EarthquakeController.Instance.IsQuaking;
    }
}
