using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Savaş kontrolleri: atılma butonu (bekleme süresi halkası + sürükle-nişan), odak çubuğu,
/// dokunmatikte sanal joystick, masaüstünde tuş ipuçları. Sağ altta skill butonuyla bir küme oluşturur.
/// </summary>
public class ControlUI : MonoBehaviour
{
    BeyControl control;
    Canvas canvas;
    CanvasGroup group;

    // Odak
    Image focusFill, focusGlow;
    TextMeshProUGUI focusLabel, focusValue;

    // Atılma
    Image dashBtn, dashGlow, dashRingTrack, dashRing, dashInner, aimKnob;
    TextMeshProUGUI dashLabel, dashState;
    DashButton dashInput;
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

        BuildDashButton();
        BuildFocusBar();
        if (!touchMode) BuildKeyHints();
    }

    void BuildDashButton()
    {
        // Skill butonunun solunda (SkillUI: sağ alt, 200 px)
        const float size = 136f;
        Vector2 anchor = new Vector2(1f, 0f);
        Vector2 pos = new Vector2(-300f, 52f);

        dashGlow = ModernUIKit.MakeImage(canvas.transform, "DashGlow", ModernUIKit.WithAlpha(ModernUIKit.Heat, 0f), ModernUIKit.GlowSprite, false);
        dashGlow.raycastTarget = false;
        ModernUIKit.Anchor(dashGlow.rectTransform, anchor, anchor, new Vector2(1f, 0f), pos + new Vector2(40f, -40f), new Vector2(size + 80f, size + 80f));

        dashBtn = ModernUIKit.MakeImage(canvas.transform, "DashButton", new Color(0.03f, 0.05f, 0.1f, 0.9f), ModernUIKit.DiscSprite, false);
        ModernUIKit.Anchor(dashBtn.rectTransform, anchor, anchor, new Vector2(1f, 0f), pos, new Vector2(size, size));

        dashRingTrack = ModernUIKit.MakeImage(dashBtn.transform, "Track", new Color(1f, 1f, 1f, 0.08f), ModernUIKit.ThickRingSprite, false);
        dashRingTrack.raycastTarget = false;
        ModernUIKit.Stretch(dashRingTrack.rectTransform);

        dashRing = ModernUIKit.MakeRadialRing(dashBtn.transform, "Cooldown", ModernUIKit.Heat);
        ModernUIKit.Stretch(dashRing.rectTransform);

        dashInner = ModernUIKit.MakeImage(dashBtn.transform, "Inner", ModernUIKit.WithAlpha(ModernUIKit.Heat, 0.1f), ModernUIKit.DiscSprite, false);
        dashInner.raycastTarget = false;
        ModernUIKit.Stretch(dashInner.rectTransform, 22, 22, 22, 22);

        dashLabel = ModernUIKit.Label(dashBtn.transform, "Label", "ATIL", 26, Color.white, ModernUIKit.FontRole.Display);
        dashLabel.fontStyle = FontStyles.Bold | FontStyles.Italic;
        dashLabel.characterSpacing = 3f;
        ModernUIKit.Anchor(dashLabel.rectTransform, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 32f));

        dashState = ModernUIKit.Label(dashBtn.transform, "State", "", 11, ModernUIKit.Dim, ModernUIKit.FontRole.Caption);
        dashState.characterSpacing = 2f;
        ModernUIKit.Anchor(dashState.rectTransform, new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 16f));

        aimKnob = ModernUIKit.MakeImage(dashBtn.transform, "AimKnob", ModernUIKit.Heat, ModernUIKit.DiscSprite, false);
        aimKnob.raycastTarget = false;
        ModernUIKit.Anchor(aimKnob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        aimKnob.gameObject.SetActive(false);

        if (!touchMode)
            ModernUIKit.MakeKeyHint(dashBtn.transform, "Key", "SHIFT", new Vector2(0.5f, 0f), new Vector2(0f, -4f));

        dashInput = dashBtn.gameObject.AddComponent<DashButton>();
        dashInput.Released = drag => control.RequestDash(drag);
    }

    void BuildFocusBar()
    {
        // Butonların üstünde ince odak çubuğu
        Vector2 anchor = new Vector2(1f, 0f);
        Image frame = ModernUIKit.MakeImage(canvas.transform, "FocusFrame", new Color(0f, 0f, 0f, 0.55f), null, false);
        frame.sprite = null;
        frame.raycastTarget = false;
        ModernUIKit.Anchor(frame.rectTransform, anchor, anchor, new Vector2(1f, 0f), new Vector2(-64f, 286f), new Vector2(372f, 10f));

        focusGlow = ModernUIKit.MakeImage(frame.transform, "Glow", ModernUIKit.WithAlpha(ModernUIKit.Cyan, 0.15f), ModernUIKit.GlowSprite, false);
        focusGlow.raycastTarget = false;
        ModernUIKit.Stretch(focusGlow.rectTransform, -10, -10, -14, -14);

        focusFill = ModernUIKit.MakeHorizontalFill(frame.transform, "Fill", ModernUIKit.Cyan);
        ModernUIKit.Stretch(focusFill.rectTransform, 1, 1, 1, 1);

        // 10'luk bölmeler
        for (int i = 1; i < 10; i++)
        {
            Image tick = ModernUIKit.MakeImage(frame.transform, "Tick" + i, new Color(0f, 0f, 0f, 0.6f), null, false);
            tick.sprite = null;
            tick.raycastTarget = false;
            float x = i / 10f;
            ModernUIKit.Anchor(tick.rectTransform, new Vector2(x, 0f), new Vector2(x, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 0f));
        }

        // Atılma maliyeti işareti
        float markX = control.maxFocus > 0f ? control.dashCost / control.maxFocus : 0.35f;
        Image mark = ModernUIKit.MakeImage(frame.transform, "DashMark", ModernUIKit.Heat, null, false);
        mark.sprite = null;
        mark.raycastTarget = false;
        ModernUIKit.Anchor(mark.rectTransform, new Vector2(markX, 0f), new Vector2(markX, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3f, 10f));

        focusLabel = ModernUIKit.Label(canvas.transform, "FocusLabel", "ODAK", 14, ModernUIKit.Dim, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        focusLabel.fontStyle = FontStyles.Bold;
        focusLabel.characterSpacing = 4f;
        ModernUIKit.Anchor(focusLabel.rectTransform, anchor, anchor, new Vector2(1f, 0f), new Vector2(-236f, 300f), new Vector2(200f, 20f));

        focusValue = ModernUIKit.Label(canvas.transform, "FocusValue", "100", 14, Color.white, ModernUIKit.FontRole.Title, TextAlignmentOptions.Right);
        focusValue.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(focusValue.rectTransform, anchor, anchor, new Vector2(1f, 0f), new Vector2(-64f, 300f), new Vector2(80f, 20f));
    }

    void BuildKeyHints()
    {
        string[] keys = { "WASD", "SHIFT", "E", "SAĞ TIK" };
        string[] texts = { "YÖNLENDİR", "ATIL", "ÖZEL GÜÇ", "İMLECE ATIL" };
        float x = 40f;
        for (int i = 0; i < keys.Length; i++)
        {
            TextMeshProUGUI k = ModernUIKit.MakeKeyHint(canvas.transform, "Hint" + i, keys[i], Vector2.zero, Vector2.zero);
            RectTransform chip = (RectTransform)k.transform.parent;
            float w = chip.sizeDelta.x;
            chip.anchorMin = chip.anchorMax = Vector2.zero;
            chip.pivot = new Vector2(0f, 0f);
            chip.anchoredPosition = new Vector2(x, 34f);
            x += w + 8f;

            TextMeshProUGUI t = ModernUIKit.Label(canvas.transform, "HintText" + i, texts[i], 13, new Color(1f, 1f, 1f, 0.55f), ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
            t.characterSpacing = 2f;
            float tw = 12f + texts[i].Length * 9f;
            ModernUIKit.Anchor(t.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0f, 0f), new Vector2(x, 34f), new Vector2(tw, 26f));
            x += tw + 18f;
        }
    }

    public void OnDash()
    {
        dashFlash = 1f;
    }

    void Update()
    {
        if (control == null || canvas == null) return;
        float udt = Time.unscaledDeltaTime;

        bool active = control.Active;
        group.alpha = Mathf.MoveTowards(group.alpha, active ? 1f : 0f, udt * 4f);
        group.blocksRaycasts = active;
        group.interactable = active;

        // Odak
        float ratio = control.FocusRatio;
        focusFill.fillAmount = ratio;
        Color fc = control.IsExhausted ? ModernUIKit.Danger
            : control.IsSteering ? ModernUIKit.Gold
            : ModernUIKit.Cyan;
        focusFill.color = fc;
        focusGlow.color = ModernUIKit.WithAlpha(fc, control.IsSteering ? 0.35f : 0.12f);
        focusLabel.text = control.IsExhausted ? "ODAK · DOLUYOR" : "ODAK";
        focusLabel.color = control.IsExhausted ? ModernUIKit.Danger : ModernUIKit.Dim;
        focusValue.text = Mathf.RoundToInt(control.Focus).ToString();

        // Atılma
        bool ready = control.CanDash;
        bool enoughFocus = control.Focus >= control.dashCost;
        float cd = control.DashCooldownRatio;
        dashRing.fillAmount = enoughFocus ? cd : Mathf.Min(cd, control.Focus / Mathf.Max(1f, control.dashCost));
        dashRing.color = ready ? ModernUIKit.Heat : new Color(0.55f, 0.6f, 0.7f, 0.7f);
        dashInner.color = ModernUIKit.WithAlpha(ModernUIKit.Heat, ready ? 0.16f : 0.04f);
        dashLabel.color = ready ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        dashState.text = !enoughFocus ? "ODAK YOK" : (DashAiming ? "BIRAK" : (ready ? "HAZIR" : ""));
        dashState.color = !enoughFocus ? ModernUIKit.Danger : ModernUIKit.Dim;

        bool aiming = DashAiming;
        aimKnob.gameObject.SetActive(aiming);
        if (aiming)
        {
            aimKnob.rectTransform.anchoredPosition = DashDrag.normalized * 44f;
            aimKnob.color = ready ? ModernUIKit.Heat : new Color(0.6f, 0.65f, 0.75f, 0.8f);
        }

        float glowTarget = ready ? 0.18f + 0.1f * Mathf.Sin(Time.unscaledTime * 4f) : 0f;
        if (dashFlash > 0f)
        {
            dashFlash = Mathf.Max(0f, dashFlash - udt * 3f);
            glowTarget = Mathf.Max(glowTarget, dashFlash * 0.7f);
        }
        dashGlow.color = ModernUIKit.WithAlpha(ModernUIKit.Heat, glowTarget);
        dashBtn.transform.localScale = Vector3.one * (1f + 0.1f * dashFlash);
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
