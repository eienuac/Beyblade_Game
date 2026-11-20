using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Özel güç HUD'u: sağ altta büyük skill butonu (enerji halkası, tip rengi), zamanlama çubuğu, sonuç yazısı.
/// Aktivasyon: butona dokun / E / gamepad Y. Zamanlama: SPACE / E / dokun / gamepad A.
/// </summary>
public class SkillUI : MonoBehaviour
{
    SkillSystem skill;
    BeybladeController bey;

    Canvas canvas;

    // Buton
    RectTransform btnRoot;
    Image glow, baseDisc, track, energyRing, activeRing, inner, readyPulse;
    TextMeshProUGUI nameLabel, stateLabel;
    Button activateBtn;
    float pulseT;

    // Zamanlama
    GameObject timingRoot;
    RectTransform barRect;
    Image needle, needleGlow, perfectZone, goodZone;
    TextMeshProUGUI timingTitle;
    float needlePos, needleDir = 1f, needleSpeed = 1.35f;
    float greenCenter = 0.55f, greenHalf = 0.085f;
    bool timingOpen;
    float timingOpenedAt;

    // Sonuç
    TextMeshProUGUI feedbackLabel, feedbackSub;
    Image feedbackLine;
    float feedbackTimer;

    public void Bind(SkillSystem s)
    {
        skill = s;
        bey = s.GetComponent<BeybladeController>();
        BuildUI();
    }

    Color TypeCol => bey != null ? ModernUIKit.TypeColor(bey.beybladeType) : ModernUIKit.Cyan;

    void BuildUI()
    {
        ModernUIKit.EnsureEventSystem();
        ModernUIKit.EnsureFonts();
        canvas = ModernUIKit.CreateCanvas("SkillUI", 200);

        // ---------- Skill butonu (sağ alt) ----------
        const float size = 200f;
        GameObject rootObj = new GameObject("SkillButton");
        rootObj.transform.SetParent(canvas.transform, false);
        btnRoot = rootObj.AddComponent<RectTransform>();
        ModernUIKit.Anchor(btnRoot, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-64f, 46f), new Vector2(size, size));

        glow = ModernUIKit.MakeImage(btnRoot, "Glow", ModernUIKit.WithAlpha(TypeCol, 0f), ModernUIKit.GlowSprite, false);
        glow.raycastTarget = false;
        ModernUIKit.Stretch(glow.rectTransform, -70, -70, -70, -70);

        baseDisc = ModernUIKit.MakeImage(btnRoot, "Base", new Color(0.03f, 0.05f, 0.1f, 0.92f), ModernUIKit.DiscSprite, false);
        ModernUIKit.Stretch(baseDisc.rectTransform);

        track = ModernUIKit.MakeImage(btnRoot, "Track", new Color(1f, 1f, 1f, 0.08f), ModernUIKit.ThickRingSprite, false);
        track.raycastTarget = false;
        ModernUIKit.Stretch(track.rectTransform);

        energyRing = ModernUIKit.MakeRadialRing(btnRoot, "Energy", TypeCol);
        ModernUIKit.Stretch(energyRing.rectTransform);

        activeRing = ModernUIKit.MakeRadialRing(btnRoot, "Active", Color.white, false);
        ModernUIKit.Stretch(activeRing.rectTransform, -10, -10, -10, -10);
        activeRing.fillAmount = 0f;

        inner = ModernUIKit.MakeImage(btnRoot, "Inner", ModernUIKit.WithAlpha(TypeCol, 0.1f), ModernUIKit.DiscSprite, false);
        inner.raycastTarget = false;
        ModernUIKit.Stretch(inner.rectTransform, 30, 30, 30, 30);

        readyPulse = ModernUIKit.MakeImage(btnRoot, "Pulse", ModernUIKit.WithAlpha(TypeCol, 0f), ModernUIKit.ThinRingSprite, false);
        readyPulse.raycastTarget = false;
        ModernUIKit.Stretch(readyPulse.rectTransform);

        nameLabel = ModernUIKit.Label(btnRoot, "Name", "ÖZEL", 30, Color.white, ModernUIKit.FontRole.Display);
        nameLabel.fontStyle = FontStyles.Bold | FontStyles.Italic;
        nameLabel.characterSpacing = 2f;
        ModernUIKit.Anchor(nameLabel.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 38f));

        stateLabel = ModernUIKit.Label(btnRoot, "State", "0%", 16, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        stateLabel.fontStyle = FontStyles.Bold;
        stateLabel.characterSpacing = 3f;
        ModernUIKit.Anchor(stateLabel.rectTransform, new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.34f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 22f));

        bool touch = Touchscreen.current != null || Application.isMobilePlatform;
        if (!touch) ModernUIKit.MakeKeyHint(btnRoot, "Key", "E", new Vector2(0.5f, 0f), new Vector2(0f, -2f));

        activateBtn = baseDisc.gameObject.AddComponent<Button>();
        activateBtn.targetGraphic = baseDisc;
        activateBtn.transition = Selectable.Transition.None;
        activateBtn.onClick.AddListener(OnSkillPressed);

        // ---------- Zamanlama ----------
        timingRoot = ModernUIKit.MakeImage(canvas.transform, "TimingRoot", new Color(0f, 0.01f, 0.03f, 0.35f), null, false).gameObject;
        ModernUIKit.Stretch(timingRoot.GetComponent<RectTransform>());
        timingRoot.GetComponent<Image>().sprite = null;
        timingRoot.SetActive(false);

        timingTitle = ModernUIKit.Label(timingRoot.transform, "Title", "ZAMANLA!", 40, Color.white, ModernUIKit.FontRole.Display);
        timingTitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
        timingTitle.characterSpacing = 10f;
        ModernUIKit.Anchor(timingTitle.rectTransform, new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.5f), new Vector2(0f, 46f), new Vector2(800f, 50f));

        TextMeshProUGUI hint = ModernUIKit.Label(timingRoot.transform, "Hint", touch ? "ALTIN BÖLGEDE DOKUN" : "ALTIN BÖLGEDE  SPACE / E", 15, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        hint.characterSpacing = 4f;
        ModernUIKit.Anchor(hint.rectTransform, new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(800f, 24f));

        Image bar = ModernUIKit.MakeImage(timingRoot.transform, "Bar", new Color(0.02f, 0.03f, 0.07f, 0.95f), null, false);
        bar.sprite = null;
        barRect = bar.rectTransform;
        ModernUIKit.Anchor(barRect, new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 22f));
        Outline bol = bar.gameObject.AddComponent<Outline>();
        bol.effectColor = new Color(1f, 1f, 1f, 0.15f);
        bol.effectDistance = new Vector2(1f, -1f);

        goodZone = ModernUIKit.MakeImage(bar.transform, "Good", ModernUIKit.WithAlpha(ModernUIKit.Cyan, 0.3f), null, false);
        goodZone.sprite = null;
        goodZone.raycastTarget = false;
        perfectZone = ModernUIKit.MakeImage(bar.transform, "Perfect", ModernUIKit.WithAlpha(ModernUIKit.Gold, 0.9f), null, false);
        perfectZone.sprite = null;
        perfectZone.raycastTarget = false;

        needleGlow = ModernUIKit.MakeImage(bar.transform, "NeedleGlow", new Color(1f, 1f, 1f, 0.35f), ModernUIKit.GlowSprite, false);
        needleGlow.raycastTarget = false;
        needle = ModernUIKit.MakeImage(bar.transform, "Needle", Color.white, null, false);
        needle.sprite = null;
        needle.raycastTarget = false;

        Button catcher = timingRoot.AddComponent<Button>();
        catcher.transition = Selectable.Transition.None;
        catcher.onClick.AddListener(LockTiming);

        // ---------- Sonuç yazısı ----------
        feedbackLabel = ModernUIKit.Label(canvas.transform, "Feedback", "", 64, Color.white, ModernUIKit.FontRole.Display);
        feedbackLabel.fontStyle = FontStyles.Bold | FontStyles.Italic;
        feedbackLabel.characterSpacing = 6f;
        feedbackLabel.outlineWidth = 0.15f;
        feedbackLabel.outlineColor = new Color32(0, 0, 0, 200);
        ModernUIKit.Anchor(feedbackLabel.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 80f));
        feedbackLabel.gameObject.SetActive(false);

        feedbackLine = ModernUIKit.MakeImage(canvas.transform, "FeedbackLine", Color.white, null, false);
        feedbackLine.sprite = null;
        feedbackLine.raycastTarget = false;
        ModernUIKit.Anchor(feedbackLine.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), new Vector2(0f, -44f), new Vector2(0f, 4f));
        feedbackLine.gameObject.SetActive(false);

        feedbackSub = ModernUIKit.Label(canvas.transform, "FeedbackSub", "", 20, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        feedbackSub.characterSpacing = 8f;
        ModernUIKit.Anchor(feedbackSub.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), new Vector2(0f, -66f), new Vector2(800f, 28f));
        feedbackSub.gameObject.SetActive(false);
    }

    void OnSkillPressed()
    {
        if (skill != null && skill.CanActivate) skill.PlayerRequestSkill();
    }

    public void OpenTimingBar()
    {
        timingOpen = true;
        timingOpenedAt = Time.unscaledTime;
        timingRoot.SetActive(true);
        needlePos = Random.Range(0.03f, 0.15f);
        needleDir = 1f;
        greenCenter = Random.Range(0.45f, 0.75f);
        greenHalf = 0.06f;
        needleSpeed = Random.Range(1.2f, 1.55f);

        float good = greenHalf * 2.2f;
        ModernUIKit.Anchor(goodZone.rectTransform, new Vector2(greenCenter - good, 0f), new Vector2(greenCenter + good, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ModernUIKit.Anchor(perfectZone.rectTransform, new Vector2(greenCenter - greenHalf, 0f), new Vector2(greenCenter + greenHalf, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        timingTitle.color = Color.Lerp(TypeCol, Color.white, 0.3f);
    }

    void LockTiming()
    {
        if (!timingOpen || skill == null) return;
        // Açılış dokunuşu kilitlemesin
        if (Time.unscaledTime - timingOpenedAt < 0.12f) return;
        timingOpen = false;
        timingRoot.SetActive(false);

        float dist = Mathf.Abs(needlePos - greenCenter);
        float quality;
        if (dist <= greenHalf * 0.5f) quality = 1f;
        else if (dist <= greenHalf) quality = 0.82f;
        else if (dist <= greenHalf * 2.2f) quality = 0.55f;
        else if (dist <= 0.35f) quality = 0.32f;
        else quality = 0.18f;

        skill.CompleteTiming(quality);
    }

    public void ShowSkillFeedback(float quality, BeybladeType type)
    {
        if (feedbackLabel == null) return;
        string rank;
        Color col;
        if (quality >= 0.95f) { rank = "MÜKEMMEL"; col = ModernUIKit.Gold; }
        else if (quality >= 0.75f) { rank = "HARİKA"; col = ModernUIKit.Battle; }
        else if (quality >= 0.5f) { rank = "İYİ"; col = ModernUIKit.Cyan; }
        else if (quality >= 0.3f) { rank = "ZAYIF"; col = ModernUIKit.Heat; }
        else { rank = "KAÇTI"; col = ModernUIKit.Danger; }

        feedbackLabel.text = rank;
        feedbackLabel.color = col;
        feedbackLabel.gameObject.SetActive(true);
        feedbackLabel.transform.localScale = Vector3.one * 1.25f;

        feedbackSub.text = ModernUIKit.SkillName(type) + "  ·  " + Mathf.RoundToInt(quality * 100f) + "%";
        feedbackSub.gameObject.SetActive(true);

        feedbackLine.color = col;
        feedbackLine.gameObject.SetActive(true);
        feedbackLine.rectTransform.sizeDelta = new Vector2(0f, 4f);

        feedbackTimer = 1.4f;
    }

    void Update()
    {
        if (skill == null || energyRing == null) return;
        float udt = Time.unscaledDeltaTime;
        Color tc = TypeCol;

        // ---------- Klavye / gamepad ----------
        Keyboard kb = Keyboard.current;
        Gamepad gp = Gamepad.current;
        if (!timingOpen)
        {
            bool press = (kb != null && (kb.eKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame))
                         || (gp != null && gp.buttonNorth.wasPressedThisFrame);
            if (press) OnSkillPressed();
        }
        else
        {
            bool lockPress = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                             || (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame));
            if (lockPress) LockTiming();
        }

        // ---------- Buton ----------
        float ratio = skill.EnergyRatio;
        bool ready = skill.phase == SkillSystem.Phase.Ready;
        bool active = skill.phase == SkillSystem.Phase.Active;
        bool timing = skill.phase == SkillSystem.Phase.Timing;
        bool launched = bey != null && bey.isLaunched && bey.isSpinning;

        nameLabel.text = bey != null ? ModernUIKit.SkillName(bey.beybladeType) : "ÖZEL";
        energyRing.fillAmount = active ? 1f : ratio;
        energyRing.color = active ? Color.white : tc;
        inner.color = ModernUIKit.WithAlpha(tc, ready ? 0.22f : 0.08f);
        activeRing.fillAmount = active ? 1f - skill.ActiveRatio : 0f;
        activeRing.color = ModernUIKit.WithAlpha(tc, 0.9f);

        if (ready)
        {
            pulseT = (pulseT + udt * 1.2f) % 1f;
            float s = 1f + pulseT * 0.22f;
            readyPulse.rectTransform.localScale = new Vector3(s, s, 1f);
            readyPulse.color = ModernUIKit.WithAlpha(tc, (1f - pulseT) * 0.8f);
            glow.color = ModernUIKit.WithAlpha(tc, 0.35f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f));
            stateLabel.text = "HAZIR";
            stateLabel.color = tc;
            nameLabel.color = Color.white;
            btnRoot.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(Time.unscaledTime * 5f));
        }
        else
        {
            readyPulse.color = ModernUIKit.WithAlpha(tc, 0f);
            glow.color = ModernUIKit.WithAlpha(tc, active ? 0.3f : 0.05f + ratio * 0.12f);
            btnRoot.localScale = Vector3.one;
            nameLabel.color = active ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            if (active) { stateLabel.text = "AKTİF"; stateLabel.color = tc; }
            else if (timing) { stateLabel.text = "ZAMANLA"; stateLabel.color = ModernUIKit.Gold; }
            else if (skill.phase == SkillSystem.Phase.Cooldown) { stateLabel.text = "BEKLE"; stateLabel.color = ModernUIKit.Dim; }
            else { stateLabel.text = Mathf.RoundToInt(ratio * 100f) + "%"; stateLabel.color = ModernUIKit.Dim; }
        }
        btnRoot.gameObject.SetActive(launched || active);

        // ---------- Zamanlama iğnesi ----------
        if (timingOpen)
        {
            needlePos += needleDir * needleSpeed * udt;
            if (needlePos >= 1f) { needlePos = 1f; needleDir = -1f; }
            if (needlePos <= 0f) { needlePos = 0f; needleDir = 1f; }
            ModernUIKit.Anchor(needle.rectTransform, new Vector2(needlePos, 0f), new Vector2(needlePos, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 14f));
            ModernUIKit.Anchor(needleGlow.rectTransform, new Vector2(needlePos, 0.5f), new Vector2(needlePos, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f));
            bool inPerfect = Mathf.Abs(needlePos - greenCenter) <= greenHalf;
            needle.color = inPerfect ? ModernUIKit.Gold : Color.white;
            // Zamanlama açıkken skill fazı değiştiyse (devrilme vb.) kapat
            if (skill.phase != SkillSystem.Phase.Timing)
            {
                timingOpen = false;
                timingRoot.SetActive(false);
            }
        }

        // ---------- Sonuç ----------
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= udt;
            feedbackLabel.transform.localScale = Vector3.Lerp(feedbackLabel.transform.localScale, Vector3.one, udt * 10f);
            float lineW = Mathf.Lerp(feedbackLine.rectTransform.sizeDelta.x, 520f, udt * 8f);
            feedbackLine.rectTransform.sizeDelta = new Vector2(lineW, 4f);
            float a = Mathf.Clamp01(feedbackTimer / 0.35f);
            SetAlpha(feedbackLabel, a);
            SetAlpha(feedbackSub, a);
            feedbackLine.color = ModernUIKit.WithAlpha(feedbackLine.color, a);
            if (feedbackTimer <= 0f)
            {
                feedbackLabel.gameObject.SetActive(false);
                feedbackSub.gameObject.SetActive(false);
                feedbackLine.gameObject.SetActive(false);
            }
        }
    }

    static void SetAlpha(TextMeshProUGUI t, float a)
    {
        Color c = t.color;
        t.color = new Color(c.r, c.g, c.b, a);
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
