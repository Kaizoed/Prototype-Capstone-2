using UnityEngine;
using ShakySurvival.Earthquake;

public class ClassroomNPCManager : MonoBehaviour
{
    [SerializeField] private NPCEarthquakeReaction[] classroomNPCs;
    [SerializeField] private DoorInteractable classroomDoor;
    [SerializeField] private GameObject doorOpenerReference;

    private void OnEnable()
    {
        EarthquakeEvents.OnEarthquakeStop += HandleEarthquakeStop;
    }

    private void OnDisable()
    {
        EarthquakeEvents.OnEarthquakeStop -= HandleEarthquakeStop;
    }

    public void EndIntroForAllNPCs()
    {
        foreach (NPCEarthquakeReaction npc in classroomNPCs)
        {
            if (npc != null)
                npc.EndIntro();
        }
    }

    private void HandleEarthquakeStop()
    {
        OpenClassroomDoor();
    }

    public void OpenClassroomDoor()
    {
        if (classroomDoor != null)
        {
            if (doorOpenerReference != null)
                classroomDoor.OpenDoorFrom(doorOpenerReference);
            else
                classroomDoor.OpenDoorForward();
        }
    }
}