using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public event System.Action ShortClick;
    public event System.Action LongPressStart;
    public event System.Action LongPressStop;

    private bool _isPressed;
    private bool _longPressTriggered;
    private float _pressTime;

    private const float LongPressThreshold = 0.5f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isPressed && !_longPressTriggered)
        {
            ShortClick?.Invoke();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;
        _longPressTriggered = false;
        _pressTime = Time.time;

        Invoke("CheckIfLongPress", LongPressThreshold);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
        CancelInvoke("CheckIfLongPress");

        if (Time.time - _pressTime >= LongPressThreshold)
        {
            LongPressStop?.Invoke();
        }
        else
        {
            _longPressTriggered = false;
        }
    }

    private void CheckIfLongPress()
    {
        if (_isPressed)
        {
            _longPressTriggered = true;
            LongPressStart?.Invoke();
        }
    }
}