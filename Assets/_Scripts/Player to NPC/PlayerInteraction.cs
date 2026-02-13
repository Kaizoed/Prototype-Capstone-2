using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Camera cam;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactMask;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable current;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void Update()
    {
        FindInteractable();

        if (current != null && Input.GetKeyDown(interactKey))
        {
            current.Interact(this);
        }
    }

    private void FindInteractable()
    {
        current = null;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
        {
            current = hit.collider.GetComponentInParent<IInteractable>();
        }

        // Optional: show prompt when current != null
    }
}
