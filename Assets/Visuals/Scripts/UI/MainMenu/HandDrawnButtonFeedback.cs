using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class HandDrawnButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField, Range(1f, 1.1f)]
    private float focusedScale = 1.035f;

    [SerializeField, Min(1f)]
    private float transitionSpeed = 14f;

    private RectTransform rectTransform;
    private bool pointerFocused;
    private bool navigationFocused;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        bool focused = pointerFocused || navigationFocused;
        Vector3 targetScale = Vector3.one * (focused ? focusedScale : 1f);
        float blend = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, blend);
    }

    private void OnDisable()
    {
        pointerFocused = false;
        navigationFocused = false;

        if (rectTransform != null)
            rectTransform.localScale = Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerFocused = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerFocused = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        navigationFocused = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        navigationFocused = false;
    }
}
