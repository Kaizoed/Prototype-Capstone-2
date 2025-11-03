using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField]
    private float shakeDuration = 1.0f;
    [SerializeField]
    private float shakeAmount = 0.2f;
    private bool isShaking = false;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            ShakeIt();
        }
    }

    private IEnumerator Shake()
    {
        if (isShaking)
        {
            yield return null;
        }
        isShaking = true;
        Vector3 originalPos = transform.localPosition;

        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeAmount;
            float y = Random.Range(-1f, 1f) / shakeAmount;

            transform.localPosition = new Vector3(originalPos.x + x,
                originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPos;
        isShaking = false;
    }
    
    public void ShakeIt()
    {
        StartCoroutine(Shake());
    }
}
