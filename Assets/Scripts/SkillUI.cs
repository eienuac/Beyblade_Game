using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Modern skill HUD: enerji halkası + timing bar + feedback.
/// </summary>
public class SkillUI : MonoBehaviour
{
    SkillSystem skill;

    Canvas canvas;
    Image energyFill;
    Image energyGlow;
    Image readyPulse;
    Image energyFrame;
    Button activateBtn;
    TextMeshProUGUI energyLabel;
    TextMeshProUGUI hintLabel;
    TextMeshProUGUI skillNameLabel;

    GameObject timingRoot;
    Image timingNeedle;
    Image greenZone;
    TextMeshProUGUI timingHint;
    TextMeshProUGUI feedbackLabel;
    Image feedbackGlow;

    float needlePos;
    float needleDir = 1f;
    float needleSpeed = 1.35f;
    float greenCenter = 0.55f;
    float greenHalf = 0.09f;
    bool timingOpen;
    float feedbackTimer;

    public void Bind(SkillSystem s)
    {
        skill = s;
        BuildUI();
    }

    void BuildUI()
    {
        ModernUIKit.EnsureEventSystem();
        ModernUIKit.EnsureFonts();
        canvas = ModernUIKit.CreateCanvas("SkillUI", 200);

        // Enerji kartı — sağ alt
        energyFrame = ModernUIKit.MakeImage(canvas.transform, "EnergyFrame", new Color(0.04f, 0.07f, 0.14f, 0.94f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(energyFrame.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 42f), new Vector2(168f, 168f));
        Outline frameOl = energyFrame.gameObject.AddComponent<Outline>();
        frameOl.effectColor = new Color(ModernUIKit.Cyan.r, ModernUIKit.Cyan.g, ModernUIKit.Cyan.b, 0.55f);
        frameOl.effectDistance = new Vector2(2f, -2f);
        Shadow frameSh = energyFrame.gameObject.AddComponent<Shadow>();
        frameSh.effectColor = new Color(0f, 0f, 0f, 0.55f);
        frameSh.effectDistance = new Vector2(0f, -10f);

        energyGlow = ModernUIKit.MakeImage(energyFrame.transform, "Glow", new Color(0.2f, 0.9f, 1f, 0.12f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Stretch(energyGlow.rectTransform, -22, -22, -22, -22);
        energyGlow.raycastTarget = false;

        Image track = ModernUIKit.MakeImage(energyFrame.transform, "Track", new Color(1f, 1f, 1f, 0.07f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Stretch(track.rectTransform, 16, 16, 16, 16);
        track.raycastTarget = false;

        energyFill = ModernUIKit.MakeImage(energyFrame.transform, "Fill", ModernUIKit.Cyan, ModernUIKit.CircleSprite, false);
        ModernUIKit.Stretch(energyFill.rectTransform, 16, 16, 16, 16);
        energyFill.type = Image.Type.Filled;
        energyFill.fillMethod = Image.FillMethod.Radial360;
        energyFill.fillOrigin = (int)Image.Origin360.Top;
        energyFill.fillClockwise = true;
        energyFill.fillAmount = 0f;
        energyFill.raycastTarget = false;

        Image inner = ModernUIKit.MakeImage(energyFrame.transform, "Inner", new Color(0.03f, 0.05f, 0.1f, 0.92f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Stretch(inner.rectTransform, 34, 34, 34, 34);
        inner.raycastTarget = false;

        readyPulse = ModernUIKit.MakeImage(energyFrame.transform, "Ready", new Color(0.3f, 1f, 0.5f, 0f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Stretch(readyPulse.rectTransform, -10, -10, -10, -10);
        readyPulse.raycastTarget = false;

        energyLabel = ModernUIKit.Label(energyFrame.transform, "Pct", "0%", 30, Color.white);
        energyLabel.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(energyLabel.rectTransform, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 40f));

        hintLabel = ModernUIKit.Label(energyFrame.transform, "Hint", "SKILL", 13, ModernUIKit.Dim);
        hintLabel.characterSpacing = 3f;
        hintLabel.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(hintLabel.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 22f));

        skillNameLabel = ModernUIKit.Label(canvas.transform, "SkillName", "OZEL SALDIRI", 14, ModernUIKit.Dim);
        skillNameLabel.fontStyle = FontStyles.Bold;
        skillNameLabel.characterSpacing = 2f;
        ModernUIKit.Anchor(skillNameLabel.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-42f, 220f), new Vector2(180f, 28f));

        activateBtn = energyFrame.gameObject.AddComponent<Button>();
        activateBtn.targetGraphic = energyFrame;
        activateBtn.transition = Selectable.Transition.None;
        activateBtn.onClick.AddListener(OnEnergyClicked);

        // Timing overlay
        timingRoot = ModernUIKit.MakeImage(canvas.transform, "TimingRoot", new Color(0.01f, 0.02f, 0.05f, 0.62f), null, false).gameObject;
        ModernUIKit.Stretch(timingRoot.GetComponent<RectTransform>());
        timingRoot.GetComponent<Image>().sprite = null;
        timingRoot.SetActive(false);

        Image panel = ModernUIKit.MakeGlassCard(timingRoot.transform, "TimingPanel", new Vector2(780f, 160f));
        ModernUIKit.Anchor(panel.rectTransform, new Vector2(0.5f, 0.26f), new Vector2(0.5f, 0.26f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 160f));

        timingHint = ModernUIKit.Label(panel.transform, "THint", "YESILDE DOKUN", 26, Color.white);
        timingHint.fontStyle = FontStyles.Bold;
        timingHint.characterSpacing = 4f;
        ModernUIKit.Anchor(timingHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(700f, 36f));

        Image barBg = ModernUIKit.MakeImage(panel.transform, "BarBg", new Color(0.05f, 0.07f, 0.12f, 0.98f), ModernUIKit.RoundSprite);
        ModernUIKit.Anchor(barBg.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 56f));

        Image goodZone = ModernUIKit.MakeImage(barBg.transform, "GoodZone", new Color(0.95f, 0.85f, 0.2f, 0.2f), null, false);
        goodZone.sprite = null;

        greenZone = ModernUIKit.MakeImage(barBg.transform, "Green", new Color(0.25f, 0.95f, 0.45f, 0.55f), null, false);
        greenZone.sprite = null;

        timingNeedle = ModernUIKit.MakeImage(barBg.transform, "Needle", Color.white, null, false);
        timingNeedle.sprite = null;
        ModernUIKit.Anchor(timingNeedle.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 0f));
        Outline nOl = timingNeedle.gameObject.AddComponent<Outline>();
        nOl.effectColor = new Color(1f, 1f, 1f, 0.8f);
        nOl.effectDistance = new Vector2(1.5f, 0f);

        Button catcher = timingRoot.GetComponent<Button>();
        if (catcher == null) catcher = timingRoot.AddComponent<Button>();
        catcher.transition = Selectable.Transition.None;
        catcher.onClick.AddListener(LockTiming);

        feedbackGlow = ModernUIKit.MakeImage(canvas.transform, "FeedbackGlow", new Color(1f, 1f, 1f, 0f), ModernUIKit.SoftCardSprite);
        ModernUIKit.Anchor(feedbackGlow.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 72f));
        feedbackGlow.raycastTarget = false;
        feedbackGlow.gameObject.SetActive(false);

        feedbackLabel = ModernUIKit.Label(canvas.transform, "Feedback", "", 40, Color.white);
        feedbackLabel.fontStyle = FontStyles.Bold;
        feedbackLabel.characterSpacing = 3f;
        ModernUIKit.Anchor(feedbackLabel.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 64f));
        feedbackLabel.gameObject.SetActive(false);
    }

    void OnEnergyClicked()
    {
        if (skill == null) return;
        if (skill.CanActivate) skill.PlayerRequestSkill();
    }

    public void OpenTimingBar()
    {
        timingOpen = true;
        timingRoot.SetActive(true);
        needlePos = Random.Range(0.05f, 0.2f);
        needleDir = 1f;
        greenCenter = Random.Range(0.42f, 0.62f);
        greenHalf = 0.085f;
        needleSpeed = Random.Range(1.15f, 1.55f);

        RectTransform bar = timingRoot.transform.Find("TimingPanel/BarBg") as RectTransform;
        if (bar == null)
        {
            Transform p = timingRoot.transform.Find("TimingPanel");
            if (p != null) bar = p.Find("BarBg") as RectTransform;
        }
        if (bar == null) return;

        ModernUIKit.Anchor(greenZone.rectTransform, new Vector2(greenCenter - greenHalf, 0.12f), new Vector2(greenCenter + greenHalf, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        Transform goodT = bar.Find("GoodZone");
        if (goodT != null)
        {
            float half = greenHalf * 2.1f;
            ModernUIKit.Anchor(goodT.GetComponent<RectTransform>(), new Vector2(greenCenter - half, 0.12f), new Vector2(greenCenter + half, 0.88f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }
    }

    void LockTiming()
    {
        if (!timingOpen || skill == null) return;
        timingOpen = false;
        timingRoot.SetActive(false);

        float dist = Mathf.Abs(needlePos - greenCenter);
        float quality;
        if (dist <= greenHalf * 0.45f) quality = 1f;
        else if (dist <= greenHalf) quality = 0.82f;
        else if (dist <= greenHalf * 2.1f) quality = 0.55f;
        else if (dist <= 0.35f) quality = 0.32f;
        else quality = 0.18f;

        skill.CompleteTiming(quality);
    }

    public void ShowSkillFeedback(float quality, BeybladeType type)
    {
        if (feedbackLabel == null) return;
        string rank;
        Color col;
        if (quality >= 0.95f) { rank = "MUKEMMEL"; col = ModernUIKit.Gold; }
        else if (quality >= 0.75f) { rank = "HARIKA"; col = ModernUIKit.Battle; }
        else if (quality >= 0.5f) { rank = "IYI"; col = ModernUIKit.Cyan; }
        else if (quality >= 0.3f) { rank = "ZAYIF"; col = ModernUIKit.Heat; }
        else { rank = "KOTU"; col = ModernUIKit.Danger; }

        string skillName = type == BeybladeType.Saldiri ? "HUCUM"
            : type == BeybladeType.Savunma ? "KALKAN"
            : type == BeybladeType.Dayaniklilik ? "REGEN"
            : "BURST";

        feedbackLabel.text = rank + "  ·  " + skillName;
        feedbackLabel.color = col;
        feedbackLabel.gameObject.SetActive(true);
        feedbackLabel.transform.localScale = Vector3.one * 1.08f;

        if (feedbackGlow != null)
        {
            feedbackGlow.gameObject.SetActive(true);
            feedbackGlow.color = new Color(col.r, col.g, col.b, 0.22f);
        }

        feedbackTimer = 1.5f;
    }

    void Update()
    {
        if (skill == null || energyFill == null) return;

        float ratio = skill.EnergyRatio;
        energyFill.fillAmount = ratio;
        energyLabel.text = Mathf.RoundToInt(ratio * 100f) + "%";

        BeybladeController bey = skill.GetComponent<BeybladeController>();
        if (skillNameLabel != null && bey != null)
        {
            skillNameLabel.text = bey.beybladeType == BeybladeType.Saldiri ? "HUCUM RUSH"
                : bey.beybladeType == BeybladeType.Savunma ? "KALKAN"
                : bey.beybladeType == BeybladeType.Dayaniklilik ? "REGEN"
                : "BURST";
        }

        bool ready = skill.phase == SkillSystem.Phase.Ready;
        bool active = skill.phase == SkillSystem.Phase.Active;
        Color fillCol = active ? ModernUIKit.Heat : (ready ? ModernUIKit.Battle : ModernUIKit.Cyan);
        energyFill.color = fillCol;

        if (ready)
        {
            float pulse = 0.35f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f));
            readyPulse.color = new Color(0.25f, 1f, 0.5f, pulse);
            hintLabel.text = "DOKUN";
            hintLabel.color = ModernUIKit.Battle;
            energyGlow.color = new Color(0.3f, 1f, 0.5f, 0.28f + pulse * 0.35f);
            energyFrame.transform.localScale = Vector3.one * (1f + pulse * 0.04f);
        }
        else if (active)
        {
            readyPulse.color = new Color(1f, 0.4f, 0.1f, 0.35f);
            hintLabel.text = "AKTIF";
            hintLabel.color = ModernUIKit.Heat;
            energyFrame.transform.localScale = Vector3.one;
        }
        else if (skill.phase == SkillSystem.Phase.Timing)
        {
            hintLabel.text = "TIMING";
            hintLabel.color = ModernUIKit.Gold;
            readyPulse.color = new Color(1f, 0.85f, 0.2f, 0.3f);
            energyFrame.transform.localScale = Vector3.one;
        }
        else
        {
            readyPulse.color = new Color(1f, 1f, 1f, 0f);
            hintLabel.text = "SKILL";
            hintLabel.color = ModernUIKit.Dim;
            energyGlow.color = new Color(0.2f, 0.8f, 1f, 0.08f + ratio * 0.22f);
            energyFrame.transform.localScale = Vector3.one;
        }

        if (timingOpen)
        {
            needlePos += needleDir * needleSpeed * Time.unscaledDeltaTime;
            if (needlePos >= 1f) { needlePos = 1f; needleDir = -1f; }
            if (needlePos <= 0f) { needlePos = 0f; needleDir = 1f; }
            if (timingNeedle != null)
            {
                RectTransform nrt = timingNeedle.rectTransform;
                nrt.anchorMin = new Vector2(needlePos, 0f);
                nrt.anchorMax = new Vector2(needlePos, 1f);
                nrt.anchoredPosition = Vector2.zero;
                nrt.sizeDelta = new Vector2(5f, 0f);
            }

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                LockTiming();
        }

        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.unscaledDeltaTime;
            if (feedbackLabel != null)
                feedbackLabel.transform.localScale = Vector3.Lerp(feedbackLabel.transform.localScale, Vector3.one, Time.unscaledDeltaTime * 8f);
            if (feedbackTimer <= 0f)
            {
                if (feedbackLabel != null) feedbackLabel.gameObject.SetActive(false);
                if (feedbackGlow != null) feedbackGlow.gameObject.SetActive(false);
            }
        }
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
