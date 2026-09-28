using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Atılma butonu: dokun = otomatik yön, basılı tut + sürükle = yön seç, bırak = atıl.
/// </summary>
public class DashButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public const float AimThreshold = 28f;

    public bool Pressed { get; private set; }
    public Vector2 Drag { get; private set; }
    public bool IsAiming => Pressed && Drag.sqrMagnitude > AimThreshold * AimThreshold;

    public System.Action<Vector2> Released;

    Vector2 start;

    public void OnPointerDown(PointerEventData e)
    {
        Pressed = true;
        start = e.position;
        Drag = Vector2.zero;
    }

    public void OnDrag(PointerEventData e)
    {
        if (Pressed) Drag = e.position - start;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!Pressed) return;
        Pressed = false;
        Vector2 d = Drag;
        Drag = Vector2.zero;
        Released?.Invoke(d.sqrMagnitude > AimThreshold * AimThreshold ? d : Vector2.zero);
    }

    void OnDisable()
    {
        Pressed = false;
        Drag = Vector2.zero;
    }
}
