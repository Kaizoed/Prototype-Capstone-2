using System.Collections;
using UnityEngine;

public class WallShatter : MonoBehaviour
{
    /* How to
    1. Attach to wall game object
    2. make prefab a child game object and set inactive in inspector
    3. adjust timer*/

    [SerializeField] GameObject DestroyOriginal;
    [SerializeField] private float ToShatter = 2f; //time before shattering

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Shatter());
    }

   IEnumerator Shatter()
    {
        yield return new WaitForSeconds(ToShatter);

        gameObject.SetActive(false); //set original game object to inactive when shattering
        DestroyOriginal.transform.SetParent(null); //detach child game object
        DestroyOriginal.SetActive(true); //set active the wall prefab
    }  
}
