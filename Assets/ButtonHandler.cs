using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private bool _isPressed;
    private bool _longPressTriggered;
    private float _pressTime;
    private const float LongPressThreshold = 0.5f;

    public event System.Action OnShortClick;
    public event System.Action OnLongPressInitiated;
    public event System.Action OnLongPressReleased;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isPressed && !_longPressTriggered)
        {
            OnShortClick?.Invoke();
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
            OnLongPressReleased?.Invoke();
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
            OnLongPressInitiated?.Invoke();
        }
    }
}