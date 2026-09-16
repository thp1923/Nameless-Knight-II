using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    public MobileInput mobileInput;

    [Header("Joystick")]
    public RectTransform handle;
    public float radius = 50f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        Vector2 value = localPoint / radius;
        value = Vector2.ClampMagnitude(value, 1f);

        if (handle != null)
            handle.anchoredPosition = value * radius;

        if (mobileInput != null)
            mobileInput.SetMove(value);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (mobileInput != null)
            mobileInput.StopMove();
    }
}