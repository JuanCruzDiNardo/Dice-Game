using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Shake")]
    [SerializeField, Min(0f)] private float defaultDuration = 0.15f;
    [SerializeField, Min(0f)] private float defaultIntensity = 0.08f;
    [SerializeField, Min(0f)] private float frequency = 30f;

    [Header("Settings")]
    [SerializeField] private bool shakeRotation;
    [SerializeField, Min(0f)] private float rotationIntensity = 1f;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        Instance = this;

        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultIntensity);
    }

    public void Shake(float duration, float intensity)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, intensity));
    }

    private IEnumerator ShakeCoroutine(float duration, float intensity)
    {
        float elapsed = 0f;
        float seedX = Random.Range(0f, 100f);
        float seedY = Random.Range(0f, 100f);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float currentIntensity = intensity * (1f - progress);

            float time = Time.unscaledTime * frequency;

            float x = (Mathf.PerlinNoise(seedX, time) * 2f - 1f) * currentIntensity;
            float y = (Mathf.PerlinNoise(seedY, time) * 2f - 1f) * currentIntensity;

            transform.localPosition = originalLocalPosition + new Vector3(x, y, 0f);

            if (shakeRotation)
            {
                float rotation = (Mathf.PerlinNoise(seedX + 50f, time) * 2f - 1f) * rotationIntensity * (1f - progress);
                transform.localRotation = originalLocalRotation * Quaternion.Euler(0f, 0f, rotation);
            }

            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        shakeCoroutine = null;
    }
}