using UnityEngine;

public class IntroCutscene : MonoBehaviour
{
    [SerializeField] private AudioSource schoolBell;

    void Start()
    {
        schoolBell.Play();
    }
}