using UnityEngine;

public class NPCHelmetSwap : MonoBehaviour
{
    [Header("Helmet References")]
    [SerializeField] private GameObject tableHelmet;
    [SerializeField] private GameObject headHelmet;

    private bool equipped = false;

    private void Start()
    {
        if (tableHelmet != null)
            tableHelmet.SetActive(true);

        if (headHelmet != null)
            headHelmet.SetActive(false);
    }

    public void EquipHelmet()
    {
        if (equipped) return;
        equipped = true;

        if (tableHelmet != null)
            tableHelmet.SetActive(false);

        if (headHelmet != null)
            headHelmet.SetActive(true);
    }
}