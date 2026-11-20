using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Dokunmatik joystick: dokunulan yerde belirir, Value -1..1 döner.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public Vector2 Value { get; private set; }

    RectTransform area;
    RectTransform baseRt;
    RectTransform knobRt;
    Image baseImg;
    Image knobImg;
    float radius = 90f;
    Vector2 origin;

    public static VirtualJoystick Create(Transform parent)
    {
        Image zone = ModernUIKit.MakeImage(parent, "JoystickZone", new Color(0f, 0f, 0f, 0f), null, false);
        zone.sprite = null;
        ModernUIKit.Anchor(zone.rectTransform, new Vector2(0f, 0f), new Vector2(0.45f, 0.6f), new Vector2(0f, 0f), Vector2.zero, Vector2.zero);

        VirtualJoystick js = zone.gameObject.AddComponent<VirtualJoystick>();
        js.area = zone.rectTransform;

        Image baseFill = ModernUIKit.MakeImage(zone.transform, "BaseFill", new Color(0.02f, 0.04f, 0.09f, 0.45f), ModernUIKit.DiscSprite, false);
        baseFill.raycastTarget = false;
        js.baseImg = ModernUIKit.MakeImage(baseFill.transform, "Base", new Color(0.3f, 0.85f, 1f, 0.35f), ModernUIKit.ThinRingSprite, false);
        js.baseImg.raycastTarget = false;
        ModernUIKit.Stretch(js.baseImg.rectTransform);
        js.baseRt = baseFill.rectTransform;
        ModernUIKit.Anchor(js.baseRt, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(200f, 180f), new Vector2(200f, 200f));

        js.knobImg = ModernUIKit.MakeImage(js.baseRt, "Knob", new Color(0.3f, 0.85f, 1f, 0.75f), ModernUIKit.DiscSprite, false);
        js.knobImg.raycastTarget = false;
        js.knobRt = js.knobImg.rectTransform;
        ModernUIKit.Anchor(js.knobRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f));

        js.origin = js.baseRt.anchoredPosition;
        return js;
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out Vector2 local))
        {
            Vector2 fromCorner = local + Vector2.Scale(area.rect.size, area.pivot);
            baseRt.anchoredPosition = fromCorner;
        }
        baseImg.color = new Color(0.3f, 0.85f, 1f, 0.8f);
        OnDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRt, e.position, e.pressEventCamera, out Vector2 local))
            return;
        Vector2 clamped = Vector2.ClampMagnitude(local, radius);
        knobRt.anchoredPosition = clamped;
        Vector2 v = clamped / radius;
        Value = v.magnitude < 0.15f ? Vector2.zero : v;
    }

    public void OnPointerUp(PointerEventData e)
    {
        Value = Vector2.zero;
        knobRt.anchoredPosition = Vector2.zero;
        baseRt.anchoredPosition = origin;
        baseImg.color = new Color(0.3f, 0.85f, 1f, 0.35f);
    }

    void OnDisable()
    {
        Value = Vector2.zero;
    }
}
