using UnityEngine;
using ShakySurvival.Earthquake;

public class WallShatter : MonoBehaviour
{
    [SerializeField] private GameObject DestroyOriginal; // broken wall prefab (child, inactive)
    private bool shattered = false;

    void Update()
    {
        if (shattered) return;

        // Break the first frame the earthquake is active
        if (EarthquakeManager.Instance != null && EarthquakeManager.Instance.IsActive)
        {
            ShatterNow();
        }
    }

    private void ShatterNow()
    {
        shattered = true;

        gameObject.SetActive(false);                 // hide intact wall object
        DestroyOriginal.transform.SetParent(null);    // detach broken prefab
        DestroyOriginal.SetActive(true);              // show broken wall
    }
}