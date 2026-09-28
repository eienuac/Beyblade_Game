using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Odak barı + atılma butonu + (dokunmatikte) sanal joystick.
/// </summary>
public class ControlUI : MonoBehaviour
{
    BeyControl control;
    Canvas canvas;
    CanvasGroup group;

    Image focusFill;
    Image focusGlow;
    Image focusDashMark;
    TextMeshProUGUI focusLabel;

    Image dashBtn;
    Image dashCooldown;
    Image dashRing;
    TextMeshProUGUI dashLabel;
    TextMeshProUGUI dashHint;
    DashButton dashInput;
    Image aimKnob;
    float dashFlash;

    VirtualJoystick joystick;
    bool touchMode;

    public Vector2 JoystickValue => joystick != null ? joystick.Value : Vector2.zero;
    public bool DashAiming => dashInput != null && dashInput.IsAiming;
    public Vector2 DashDrag => dashInput != null ? dashInput.Drag : Vector2.zero;

    public void Bind(BeyControl c)
    {
        control = c;
        Build();
    }

    void Build()
    {
        ModernUIKit.EnsureEventSystem();
        ModernUIKit.EnsureFonts();
        touchMode = Touchscreen.current != null || Application.isMobilePlatform;

        canvas = ModernUIKit.CreateCanvas("ControlUI", 190);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        if (touchMode)
            joystick = VirtualJoystick.Create(canvas.transform);

        // ---- Odak barı (alt orta) ----
        Image frame = ModernUIKit.MakeImage(canvas.transform, "FocusFrame", new Color(0.04f, 0.07f, 0.14f, 0.9f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(frame.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(440f, 22f));
        frame.raycastTarget = false;
        Outline fol = frame.gameObject.AddComponent<Outline>();
        fol.effectColor = new Color(ModernUIKit.Cyan.r, ModernUIKit.Cyan.g, ModernUIKit.Cyan.b, 0.45f);
        fol.effectDistance = new Vector2(1.2f, -1.2f);

        focusGlow = ModernUIKit.MakeImage(frame.transform, "Glow", new Color(0.2f, 0.8f, 1f, 0.15f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Stretch(focusGlow.rectTransform, -8, -8, -8, -8);
        focusGlow.raycastTarget = false;

        focusFill = ModernUIKit.MakeImage(frame.transform, "Fill", ModernUIKit.Cyan, ModernUIKit.SharpCardSprite);
        ModernUIKit.Stretch(focusFill.rectTransform, 3, 3, 3, 3);
        focusFill.type = Image.Type.Filled;
        focusFill.fillMethod = Image.FillMethod.Horizontal;
        focusFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        focusFill.raycastTarget = false;

        // Atılma maliyeti çizgisi
        focusDashMark = ModernUIKit.MakeImage(frame.transform, "DashMark", new Color(1f, 1f, 1f, 0.6f), null, false);
        focusDashMark.sprite = null;
        float markX = control.maxFocus > 0f ? control.dashCost / control.maxFocus : 0.35f;
        ModernUIKit.Anchor(focusDashMark.rectTransform, new Vector2(markX, 0f), new Vector2(markX, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 0f));
        focusDashMark.raycastTarget = false;

        focusLabel = ModernUIKit.Label(canvas.transform, "FocusLabel", "ODAK", 16, ModernUIKit.Dim, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        focusLabel.fontStyle = FontStyles.Bold;
        focusLabel.characterSpacing = 4f;
        ModernUIKit.Anchor(focusLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(-220f, 62f), new Vector2(200f, 22f));

        if (!touchMode)
        {
            TextMeshProUGUI keys = ModernUIKit.Label(canvas.transform, "KeyHint", "WASD YÖNLENDİR  ·  SHIFT ATIL  ·  SAĞ TIK FAREYE ATIL", 14, new Color(1f, 1f, 1f, 0.45f), ModernUIKit.FontRole.Caption, TextAlignmentOptions.Right);
            keys.characterSpacing = 1.5f;
            keys.raycastTarget = false;
            ModernUIKit.Anchor(keys.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(220f, 62f), new Vector2(460f, 22f));
        }

        // ---- Atılma butonu (skill halkasının solu) ----
        dashBtn = ModernUIKit.MakeImage(canvas.transform, "DashButton", new Color(0.05f, 0.08f, 0.15f, 0.94f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(dashBtn.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-236f, 48f), new Vector2(118f, 118f));
        Outline dol = dashBtn.gameObject.AddComponent<Outline>();
        dol.effectColor = new Color(ModernUIKit.Heat.r, ModernUIKit.Heat.g, ModernUIKit.Heat.b, 0.5f);
        dol.effectDistance = new Vector2(1.5f, -1.5f);

        dashRing = ModernUIKit.MakeImage(dashBtn.transform, "Ring", ModernUIKit.Heat, ModernUIKit.RingSprite, false);
        ModernUIKit.Stretch(dashRing.rectTransform, 4, 4, 4, 4);
        dashRing.raycastTarget = false;

        dashCooldown = ModernUIKit.MakeImage(dashBtn.transform, "Cooldown", new Color(0f, 0f, 0f, 0.55f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Stretch(dashCooldown.rectTransform, 10, 10, 10, 10);
        dashCooldown.type = Image.Type.Filled;
        dashCooldown.fillMethod = Image.FillMethod.Radial360;
        dashCooldown.fillOrigin = (int)Image.Origin360.Top;
        dashCooldown.fillClockwise = false;
        dashCooldown.raycastTarget = false;

        dashLabel = ModernUIKit.Label(dashBtn.transform, "Label", "ATIL", 24, Color.white, ModernUIKit.FontRole.Display);
        dashLabel.fontStyle = FontStyles.Bold;
        dashLabel.characterSpacing = 3f;
        ModernUIKit.Anchor(dashLabel.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 32f));

        dashLabel.raycastTarget = false;

        dashHint = ModernUIKit.Label(dashBtn.transform, "Hint", "SÜRÜKLE: YÖN", 11, ModernUIKit.Dim, ModernUIKit.FontRole.Caption);
        dashHint.characterSpacing = 1f;
        dashHint.raycastTarget = false;
        ModernUIKit.Anchor(dashHint.rectTransform, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 20f));

        aimKnob = ModernUIKit.MakeImage(dashBtn.transform, "AimKnob", ModernUIKit.Heat, ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(aimKnob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 34f));
        aimKnob.raycastTarget = false;
        aimKnob.gameObject.SetActive(false);

        dashInput = dashBtn.gameObject.AddComponent<DashButton>();
        dashInput.Released = drag => control.RequestDash(drag);
    }

    public void OnDash()
    {
        dashFlash = 1f;
    }

    void Update()
    {
        if (control == null || canvas == null) return;

        bool active = control.Active;
        group.alpha = Mathf.MoveTowards(group.alpha, active ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        group.blocksRaycasts = active;
        group.interactable = active;

        float ratio = control.FocusRatio;
        focusFill.fillAmount = ratio;
        Color fc = control.IsExhausted ? ModernUIKit.Danger
            : control.IsSteering ? ModernUIKit.Gold
            : ModernUIKit.Cyan;
        focusFill.color = fc;
        focusGlow.color = new Color(fc.r, fc.g, fc.b, control.IsSteering ? 0.3f : 0.1f);
        focusLabel.text = control.IsExhausted ? "ODAK  ·  DOLUYOR" : "ODAK";
        focusLabel.color = control.IsExhausted ? ModernUIKit.Danger : ModernUIKit.Dim;

        bool ready = control.CanDash;
        bool enoughFocus = control.Focus >= control.dashCost;
        dashCooldown.fillAmount = 1f - control.DashCooldownRatio;
        dashRing.color = ready ? ModernUIKit.Heat : new Color(0.5f, 0.55f, 0.65f, 0.5f);
        dashLabel.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        if (!enoughFocus) dashHint.text = "ODAK YOK";
        else if (DashAiming) dashHint.text = "BIRAK: ATIL";
        else dashHint.text = "SÜRÜKLE: YÖN";

        bool aiming = DashAiming;
        aimKnob.gameObject.SetActive(aiming);
        if (aiming)
        {
            aimKnob.rectTransform.anchoredPosition = DashDrag.normalized * 42f;
            aimKnob.color = ready ? ModernUIKit.Heat : new Color(0.6f, 0.65f, 0.75f, 0.8f);
        }

        if (dashFlash > 0f)
        {
            dashFlash = Mathf.Max(0f, dashFlash - Time.unscaledDeltaTime * 3f);
            dashBtn.transform.localScale = Vector3.one * (1f + 0.12f * dashFlash);
        }
        else if (ready)
        {
            dashBtn.transform.localScale = Vector3.one * (1f + 0.025f * Mathf.Sin(Time.unscaledTime * 5f));
        }
        else
        {
            dashBtn.transform.localScale = Vector3.one;
        }
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
