using UnityEngine;
using UnityEngine.EventSystems;

public class InvectorMobileSprint : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler
{
    public Invector.vCharacterController.vThirdPersonInput playerInput;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (playerInput != null)
            playerInput.MobileSprintDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (playerInput != null)
            playerInput.MobileSprintUp();
    }
}