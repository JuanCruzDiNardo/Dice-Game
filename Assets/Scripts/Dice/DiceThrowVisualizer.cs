using UnityEngine;

public class DiceThrowVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform endMarker;

    [Header("Line")]
    [SerializeField] private float minimumWidth = 0.03f;
    [SerializeField] private float maximumWidth = 0.12f;
    [SerializeField] private float heightOffset = 0.05f;

    [Header("End Marker")]
    [SerializeField] private float minimumMarkerScale = 0.1f;
    [SerializeField] private float maximumMarkerScale = 0.3f;

    [Header("Maximum Power Effect")]
    [SerializeField] private bool pulseAtMaximum = true;
    [SerializeField] private float pulseSpeed = 8f;
    [SerializeField] private float pulseAmount = 0.15f;

    private Vector3 originalMarkerScale = Vector3.one;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;
        }

        if (endMarker != null)
        {
            originalMarkerScale = endMarker.localScale;
            endMarker.gameObject.SetActive(false);
        }
    }

    public void Show()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = true;

        if (endMarker != null)
            endMarker.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;

        if (endMarker != null)
            endMarker.gameObject.SetActive(false);
    }

    public void UpdateVisual(Vector3 dicePosition, Vector3 targetPosition, float normalizedPower)
    {
        normalizedPower = Mathf.Clamp01(normalizedPower);

        dicePosition.y += heightOffset;
        targetPosition.y = dicePosition.y;

        UpdateLine(dicePosition, targetPosition, normalizedPower);
        UpdateMarker(targetPosition, normalizedPower);
    }

    private void UpdateLine(Vector3 startPosition, Vector3 endPosition, float normalizedPower)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.SetPosition(0, startPosition);
        lineRenderer.SetPosition(1, endPosition);

        float width = Mathf.Lerp(minimumWidth, maximumWidth, normalizedPower);

        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
    }

    private void UpdateMarker(Vector3 targetPosition, float normalizedPower)
    {
        if (endMarker == null)
            return;

        endMarker.position = targetPosition;

        float scale = Mathf.Lerp(minimumMarkerScale, maximumMarkerScale, normalizedPower);

        if (pulseAtMaximum && normalizedPower >= 0.99f)
            scale *= 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        endMarker.localScale = originalMarkerScale * scale;
    }
}