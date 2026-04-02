using UnityEngine;
using ShakySurvival.Earthquake;

public class NPCHelmetManager : MonoBehaviour
{
    [Header("Helmets on NPC heads")]
    [SerializeField] private GameObject[] headHelmets;

    [Header("Helmets on tables")]
    [SerializeField] private GameObject[] tableHelmets;

    private void Start()
    {
        // hide head helmets at start
        foreach (var h in headHelmets)
        {
            if (h != null)
                h.SetActive(false);
        }

        // show table helmets at start
        foreach (var t in tableHelmets)
        {
            if (t != null)
                t.SetActive(true);
        }
    }

    private void OnEnable()
    {
        EarthquakeEvents.OnEarthquakeStart += OnEarthquakeStart;
    }

    private void OnDisable()
    {
        EarthquakeEvents.OnEarthquakeStart -= OnEarthquakeStart;
    }

    private void OnEarthquakeStart()
    {
        // show head helmets
        foreach (var h in headHelmets)
        {
            if (h != null)
                h.SetActive(true);
        }

        // hide table helmets
        foreach (var t in tableHelmets)
        {
            if (t != null)
                t.SetActive(false);
        }
    }
}