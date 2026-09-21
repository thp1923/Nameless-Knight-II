using UnityEngine;
using UnityEngine.EventSystems;

public class InvectorMobileJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    public Invector.vCharacterController.vThirdPersonInput playerInput;

    public RectTransform handle;
    public float radius = 50f;

    private RectTransform joystickRect;

    private void Awake()
    {
        joystickRect = transform as RectTransform;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (playerInput == null)
            return;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        Vector2 value = localPoint / radius;
        value = Vector2.ClampMagnitude(value, 1f);

        if (handle != null)
            handle.anchoredPosition = value * radius;

        playerInput.SetMobileMove(value);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        if (playerInput != null)
            playerInput.ReleaseMobileMove();
    }
}