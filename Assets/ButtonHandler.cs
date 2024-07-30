using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public event System.Action Click;
    public event System.Action PressStart;
    public event System.Action PressStop;

    public void OnPointerClick(PointerEventData eventData)
    {
        Click?.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        PressStart?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        PressStop?.Invoke();
    }
}