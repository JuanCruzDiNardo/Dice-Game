using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class FloatingUIElement : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float amplitude = 12f;

    [SerializeField, Min(0.01f)]
    private float cyclesPerSecond = 0.55f;

    [SerializeField]
    private float phaseOffset;

    private RectTransform rectTransform;
    private Vector2 restingPosition;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        restingPosition = rectTransform.anchoredPosition;
    }

    private void LateUpdate()
    {
        float phase = (Time.unscaledTime * cyclesPerSecond * Mathf.PI * 2f) + phaseOffset;
        rectTransform.anchoredPosition = restingPosition + Vector2.up * (Mathf.Sin(phase) * amplitude);
    }

    private void OnDisable()
    {
        if (rectTransform != null)
            rectTransform.anchoredPosition = restingPosition;
    }

    public void Configure(float movementAmplitude, float movementCyclesPerSecond, float phase)
    {
        amplitude = Mathf.Max(0f, movementAmplitude);
        cyclesPerSecond = Mathf.Max(0.01f, movementCyclesPerSecond);
        phaseOffset = phase;
    }

    private void CacheReferences()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
    }
}
