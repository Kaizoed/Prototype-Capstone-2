using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    [Header("Optional: NPC Drill")]
    [SerializeField] private NPCDrillController drill; // your NPC drill script (go to cover/duck/hold)

    [Header("Cover Point")]
    [SerializeField] private Transform coverPoint;

    [Header("Debug")]
    [SerializeField] private string npcName = "Student";

    private void Awake()
    {
        if (drill == null) drill = GetComponent<NPCDrillController>();
    }

    public void Interact(PlayerInteractor interactor)
    {
        Debug.Log($"Interacted with {npcName}");

        // Simple behavior options (pick one or keep all):
        // 1) Just talk (debug for now)
        // 2) Tell NPC to go to cover
        // 3) Tell NPC to duck/hold

        if (drill != null)
        {
            // Set cover point if needed
            drill.SetCoverPoint(coverPoint);

            // Example: Press E once -> go to cover
            drill.GoToCover();

            // You can later chain this or open a small UI menu
        }
    }
}
