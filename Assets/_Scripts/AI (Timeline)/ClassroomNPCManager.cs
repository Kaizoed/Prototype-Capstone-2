using UnityEngine;
using UnityEngine.AI;

public class ClassroomNPCManager : MonoBehaviour
{
    [Header("Teacher / Earthquake NPCs")]
    [SerializeField] private NPCEarthquakeReaction[] classroomNPCs;

    [Header("Door")]
    [SerializeField] private DoorInteractable classroomDoor;
    [SerializeField] private GameObject doorOpenerReference;

    [Header("Student Behavior Graph NPCs")]
    [SerializeField] private MonoBehaviour[] behaviorAgents;
    [SerializeField] private NavMeshAgent[] navMeshAgents;

    private void Awake()
    {
        DisableNPCBehaviors();
    }

    public void EndIntroForAllNPCs()
    {
        foreach (NPCEarthquakeReaction npc in classroomNPCs)
        {
            if (npc != null)
                npc.EndIntro();
        }
    }

    public void DisableNPCBehaviors()
    {
        foreach (var agent in behaviorAgents)
        {
            if (agent != null)
                agent.enabled = false;
        }

        foreach (var nav in navMeshAgents)
        {
            if (nav != null)
            {
                nav.isStopped = true;
                nav.velocity = Vector3.zero;
                nav.ResetPath();
            }
        }
    }

    public void EnableNPCBehaviors()
    {
        foreach (var agent in behaviorAgents)
        {
            if (agent != null)
                agent.enabled = true;
        }

        foreach (var nav in navMeshAgents)
        {
            if (nav != null)
                nav.isStopped = false;
        }
    }

    public void FreezeEarthquakeNPCsForCutscene()
    {
        foreach (NPCEarthquakeReaction npc in classroomNPCs)
        {
            if (npc != null)
                npc.FreezeForCutscene();
        }
    }

    public void ResumeEarthquakeNPCsAfterCutscene()
    {
        foreach (NPCEarthquakeReaction npc in classroomNPCs)
        {
            if (npc != null)
                npc.ResumeAfterCutscene();
        }
    }

    public void StartTeacherEvacuation()
    {
        foreach (NPCEarthquakeReaction npc in classroomNPCs)
        {
            if (npc != null)
                npc.StartManualEvacuation();
        }
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