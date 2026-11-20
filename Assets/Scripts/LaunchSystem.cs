using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Fırlatma: güç göstergesi gidip gelir, ipi aşağı çekerek o anki güçle fırlatırsın.
/// Altın bölge (%90+) = mükemmel fırlatış. Güç başlangıç dönüşünü ve fırlatma hızını belirler
/// (BeybladeCore.Combat.LaunchSpinFraction: önemli ama tek başına maçı kazandırmaz).
/// </summary>
public class LaunchSystem : MonoBehaviour
{
    [Header("Referanslar")]
    public BeybladeController targetBeyblade;

    [Header("Güç Göstergesi")]
    public float minFillDuration = 1.6f;
    public float maxFillDuration = 2.3f;
    [Tooltip("Bu güç ve üstü 'mükemmel'")]
    public float perfectThreshold = 0.9f;

    bool launched;
    float fillDuration;
    float fillTimer;
    float currentPower;

    bool isDragging;
    float dragStartY;
    float pullThreshold = 200f;
    float lastPull;

    Canvas uiCanvas;
    CanvasGroup group;
    Image flashOverlay;

    // Üst başlık
    TextMeshProUGUI instructionText;
    TextMeshProUGUI instructionSub;

    // Güç
    Image powerFill, powerCap, powerCapGlow;
    TextMeshProUGUI powerPct, powerRank;

    // İp
    RectTransform ripcordHandle;
    Image ripcordImage, ropeFill;
    TextMeshProUGUI handleLabel;
    RectTransform[] arrows;
    float ripcordInitialY;

    // Sonuç
    TextMeshProUGUI resultText, resultSub;
    float resultPulse;

    void Start()
    {
        ModernUIKit.EnsureEventSystem();
        ModernUIKit.EnsureFonts();

        if (targetBeyblade == null)
        {
            GameObject player = GameObject.Find("PlayerBeyblade");
            if (player != null) targetBeyblade = player.GetComponent<BeybladeController>();
        }

        if (targetBeyblade != null)
        {
            targetBeyblade.isLaunched = false;
            targetBeyblade.isSpinning = true;
            Rigidbody rb = targetBeyblade.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

        // Denge testi: iki taraf da bot, fırlatma otomatik
        if (MatchContext.IsBalanceTest)
        {
            launched = true;
            Invoke(nameof(AutoLaunch), 0.05f);
            return;
        }

        CreateUI();
        fillDuration = Random.Range(minFillDuration, maxFillDuration);
        fillTimer = 0f;
        currentPower = 0f;
    }

    void AutoLaunch()
    {
        if (targetBeyblade != null) AIBeyblade.Launch(targetBeyblade, Random.Range(0.7f, 0.95f));
        enabled = false;
    }

    static Color PowerColor(float p)
    {
        if (p < 0.45f) return Color.Lerp(ModernUIKit.Cyan, ModernUIKit.Mint, p / 0.45f);
        if (p < 0.9f) return Color.Lerp(ModernUIKit.Mint, ModernUIKit.Heat, (p - 0.45f) / 0.45f);
        return ModernUIKit.Gold;
    }

    static string PowerRank(float p, out Color c)
    {
        if (p >= 0.9f) { c = ModernUIKit.Gold; return "MÜKEMMEL"; }
        if (p >= 0.75f) { c = ModernUIKit.Battle; return "HARİKA"; }
        if (p >= 0.55f) { c = ModernUIKit.Cyan; return "İYİ"; }
        if (p >= 0.35f) { c = ModernUIKit.Heat; return "ORTA"; }
        c = ModernUIKit.Danger; return "ZAYIF";
    }

    void Update()
    {
        if (uiCanvas == null) return;
        float udt = Time.unscaledDeltaTime;

        if (launched)
        {
            if (resultPulse > 0f)
            {
                resultPulse -= udt;
                float s = Mathf.Lerp(1f, 1.18f, Mathf.Clamp01(resultPulse - 1.2f) * 2.5f);
                resultText.transform.localScale = Vector3.one * s;
                flashOverlay.color = new Color(1f, 1f, 1f, Mathf.Clamp01(resultPulse - 1.3f) * 0.35f);
                group.alpha = Mathf.Clamp01(resultPulse / 0.4f);
            }
            return;
        }

        // Güç gidip gelir (yavaşlayan uçlar: tepe noktasını yakalamak zamanlama ister)
        fillTimer += Time.deltaTime;
        float raw = Mathf.PingPong(fillTimer / fillDuration, 1f);
        currentPower = Mathf.SmoothStep(0f, 1f, raw);

        Color barColor = PowerColor(currentPower);
        powerFill.fillAmount = currentPower;
        powerFill.color = barColor;
        PlaceOnBar(powerCap.rectTransform, currentPower);
        PlaceOnBar(powerCapGlow.rectTransform, currentPower);
        powerCap.color = barColor;
        powerCapGlow.color = ModernUIKit.WithAlpha(barColor, 0.55f);

        int pct = Mathf.RoundToInt(currentPower * 100f);
        powerPct.text = pct + "<size=55%>%</size>";
        powerPct.color = currentPower >= perfectThreshold ? ModernUIKit.Gold : Color.white;
        powerRank.text = PowerRank(currentPower, out Color rc);
        powerRank.color = rc;

        // Klavye: SPACE basılı tut = ipi çek
        bool keyPull = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        if (keyPull)
        {
            lastPull = Mathf.MoveTowards(lastPull, 1f, udt * 4f);
            UpdateRipcordVisual(lastPull);
            if (lastPull >= 0.98f) LaunchBeyblade();
            AnimateArrows();
            return;
        }

        // Fare / dokunmatik: ekranın herhangi bir yerinde basılı tutup aşağı çek
        bool isPressed = false;
        Vector2 inputPos = Vector2.zero;
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPressed = true;
            inputPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isPressed = true;
            inputPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }

        if (isPressed)
        {
            if (!isDragging)
            {
                isDragging = true;
                dragStartY = inputPos.y;
            }
            else
            {
                float pullRatio = Mathf.Clamp01((dragStartY - inputPos.y) / pullThreshold);
                lastPull = pullRatio;
                UpdateRipcordVisual(pullRatio);
                if (pullRatio >= 0.95f)
                    LaunchBeyblade();
            }
        }
        else
        {
            isDragging = false;
            lastPull = Mathf.MoveTowards(lastPull, 0f, udt * 5f);
            UpdateRipcordVisual(lastPull);
        }

        AnimateArrows();
    }

    static void PlaceOnBar(RectTransform rt, float x)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(x, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    void AnimateArrows()
    {
        for (int i = 0; i < arrows.Length; i++)
        {
            float t = (Time.unscaledTime * 1.6f + i * 0.33f) % 1f;
            arrows[i].anchoredPosition = new Vector2(0f, -150f - t * 70f - lastPull * 120f);
            arrows[i].GetComponent<Image>().color = new Color(1f, 0.6f, 0.2f, Mathf.Sin(t * Mathf.PI) * (1f - lastPull) * 0.8f);
        }
    }

    void UpdateRipcordVisual(float pull)
    {
        ripcordHandle.anchoredPosition = new Vector2(0f, ripcordInitialY - pull * 300f);
        ripcordImage.color = Color.Lerp(ModernUIKit.Heat, ModernUIKit.Gold, pull);
        ropeFill.fillAmount = pull;
        ropeFill.color = Color.Lerp(ModernUIKit.WithAlpha(ModernUIKit.Heat, 0.6f), ModernUIKit.Gold, pull);
        handleLabel.text = pull < 0.05f ? "ÇEK" : Mathf.RoundToInt(pull * 100f) + "%";

        if (pull > 0.08f)
        {
            instructionText.text = "ÇEK!";
            instructionSub.text = "GÜÇ " + Mathf.RoundToInt(currentPower * 100f) + "%  ·  SONUNA KADAR ÇEK";
        }
        else
        {
            instructionText.text = "FIRLATMAYA HAZIRLAN";
            bool desktop = Touchscreen.current == null && !Application.isMobilePlatform;
            instructionSub.text = desktop
                ? "GÜÇ ALTIN BÖLGEDEYKEN İPİ AŞAĞI ÇEK  ·  veya SPACE BASILI TUT"
                : "GÜÇ ALTIN BÖLGEDEYKEN İPİ AŞAĞI ÇEK";
        }
    }

    void LaunchBeyblade()
    {
        if (launched) return;
        launched = true;
        isDragging = false;
        float launchPower = currentPower;
        resultPulse = 1.9f;

        string rank = PowerRank(launchPower, out Color qColor);
        resultText.gameObject.SetActive(true);
        resultText.text = rank;
        resultText.color = qColor;
        resultSub.gameObject.SetActive(true);
        resultSub.text = "FIRLATIŞ  ·  " + Mathf.RoundToInt(launchPower * 100f) + "% GÜÇ";

        instructionText.text = "FIRLAT!";
        instructionText.color = qColor;
        instructionSub.text = "";

        if (targetBeyblade != null)
            AIBeyblade.Launch(targetBeyblade, Mathf.Max(launchPower, 0.08f));
        if (launchPower >= perfectThreshold && PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.RecordPerfectLaunch();

        Invoke(nameof(HideUI), 1.9f);
    }

    void HideUI()
    {
        if (uiCanvas != null) uiCanvas.gameObject.SetActive(false);
        enabled = false;
    }

    void CreateUI()
    {
        uiCanvas = ModernUIKit.CreateCanvas("LaunchUI", 150);
        group = uiCanvas.gameObject.AddComponent<CanvasGroup>();

        flashOverlay = ModernUIKit.MakeImage(uiCanvas.transform, "Flash", new Color(1f, 1f, 1f, 0f), null, false);
        flashOverlay.sprite = null;
        ModernUIKit.Stretch(flashOverlay.rectTransform);
        flashOverlay.raycastTarget = false;

        // ---- Üst başlık ----
        Image banner = ModernUIKit.MakeSkewPanel(uiCanvas.transform, "Banner", new Color(0.02f, 0.03f, 0.07f, 0.85f));
        ModernUIKit.Anchor(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(900f, 96f));
        Image bannerLine = ModernUIKit.MakeImage(banner.transform, "Line", ModernUIKit.Heat, null, false);
        bannerLine.sprite = null;
        bannerLine.raycastTarget = false;
        ModernUIKit.Anchor(bannerLine.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(140f, 3f));

        instructionText = ModernUIKit.Label(banner.transform, "Instruction", "FIRLATMAYA HAZIRLAN", 38, Color.white, ModernUIKit.FontRole.Display);
        instructionText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        instructionText.characterSpacing = 6f;
        ModernUIKit.Anchor(instructionText.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 46f));

        instructionSub = ModernUIKit.Label(banner.transform, "Sub", "", 15, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        instructionSub.characterSpacing = 3f;
        ModernUIKit.Anchor(instructionSub.rectTransform, new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 22f));

        // ---- Güç göstergesi (alt orta) ----
        GameObject powerRoot = new GameObject("Power");
        powerRoot.transform.SetParent(uiCanvas.transform, false);
        RectTransform pr = powerRoot.AddComponent<RectTransform>();
        ModernUIKit.Anchor(pr, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(820f, 120f));

        TextMeshProUGUI cap = ModernUIKit.Label(pr, "Caption", "GÜÇ", 16, ModernUIKit.Dim, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        cap.fontStyle = FontStyles.Bold;
        cap.characterSpacing = 8f;
        ModernUIKit.Anchor(cap.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 46f), new Vector2(200f, 22f));

        powerRank = ModernUIKit.Label(pr, "Rank", "", 18, Color.white, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        powerRank.fontStyle = FontStyles.Bold | FontStyles.Italic;
        powerRank.characterSpacing = 4f;
        ModernUIKit.Anchor(powerRank.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(70f, 46f), new Vector2(300f, 22f));

        powerPct = ModernUIKit.Label(pr, "Pct", "0%", 56, Color.white, ModernUIKit.FontRole.Display, TextAlignmentOptions.Right);
        powerPct.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(powerPct.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 36f), new Vector2(240f, 64f));

        Image trackBg = ModernUIKit.MakeImage(pr, "Track", new Color(0.02f, 0.03f, 0.07f, 0.9f), null, false);
        trackBg.sprite = null;
        trackBg.raycastTarget = false;
        ModernUIKit.Anchor(trackBg.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(0f, 26f));
        Outline tol = trackBg.gameObject.AddComponent<Outline>();
        tol.effectColor = new Color(1f, 1f, 1f, 0.14f);
        tol.effectDistance = new Vector2(1f, -1f);

        // Altın bölge
        Image zone = ModernUIKit.MakeImage(trackBg.transform, "PerfectZone", ModernUIKit.WithAlpha(ModernUIKit.Gold, 0.18f), null, false);
        zone.sprite = null;
        zone.raycastTarget = false;
        ModernUIKit.Anchor(zone.rectTransform, new Vector2(perfectThreshold, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image zoneEdge = ModernUIKit.MakeImage(trackBg.transform, "ZoneEdge", ModernUIKit.Gold, null, false);
        zoneEdge.sprite = null;
        zoneEdge.raycastTarget = false;
        ModernUIKit.Anchor(zoneEdge.rectTransform, new Vector2(perfectThreshold, 0f), new Vector2(perfectThreshold, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 10f));
        float zoneMid = (perfectThreshold + 1f) * 0.5f;
        TextMeshProUGUI zoneLbl = ModernUIKit.Label(trackBg.transform, "ZoneLbl", "MAX", 12, ModernUIKit.Gold, ModernUIKit.FontRole.Title);
        zoneLbl.fontStyle = FontStyles.Bold;
        zoneLbl.characterSpacing = 3f;
        ModernUIKit.Anchor(zoneLbl.rectTransform, new Vector2(zoneMid, 0f), new Vector2(zoneMid, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(80f, 16f));

        powerFill = ModernUIKit.MakeHorizontalFill(trackBg.transform, "Fill", ModernUIKit.Cyan);
        ModernUIKit.Stretch(powerFill.rectTransform, 2, 2, 2, 2);

        for (int i = 1; i < 10; i++)
        {
            Image tick = ModernUIKit.MakeImage(trackBg.transform, "Tick" + i, new Color(0f, 0f, 0f, 0.5f), null, false);
            tick.sprite = null;
            tick.raycastTarget = false;
            float x = i / 10f;
            ModernUIKit.Anchor(tick.rectTransform, new Vector2(x, 0f), new Vector2(x, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 0f));
        }

        powerCapGlow = ModernUIKit.MakeImage(trackBg.transform, "CapGlow", Color.white, ModernUIKit.GlowSprite, false);
        powerCapGlow.raycastTarget = false;
        powerCapGlow.rectTransform.sizeDelta = new Vector2(70f, 70f);
        powerCap = ModernUIKit.MakeImage(trackBg.transform, "Cap", Color.white, null, false);
        powerCap.sprite = null;
        powerCap.raycastTarget = false;
        powerCap.rectTransform.sizeDelta = new Vector2(6f, 40f);

        // ---- İp (sağ) ----
        GameObject ropeRoot = new GameObject("Ripcord");
        ropeRoot.transform.SetParent(uiCanvas.transform, false);
        RectTransform rr = ropeRoot.AddComponent<RectTransform>();
        ModernUIKit.Anchor(rr, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-90f, 60f), new Vector2(170f, 520f));

        Image ropeTrack = ModernUIKit.MakeImage(rr, "Track", new Color(1f, 1f, 1f, 0.08f), ModernUIKit.RoundSprite);
        ropeTrack.raycastTarget = false;
        ModernUIKit.Anchor(ropeTrack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(10f, 400f));
        ropeFill = ModernUIKit.MakeImage(ropeTrack.transform, "Fill", ModernUIKit.Heat, ModernUIKit.RoundSprite);
        ModernUIKit.Stretch(ropeFill.rectTransform);
        ropeFill.type = Image.Type.Filled;
        ropeFill.fillMethod = Image.FillMethod.Vertical;
        ropeFill.fillOrigin = (int)Image.OriginVertical.Top;
        ropeFill.fillAmount = 0f;
        ropeFill.raycastTarget = false;

        Image handle = ModernUIKit.MakeSkewPanel(rr, "Handle", ModernUIKit.Heat);
        ripcordHandle = handle.rectTransform;
        ModernUIKit.Anchor(ripcordHandle, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(150f, 66f));
        ripcordInitialY = ripcordHandle.anchoredPosition.y;
        ripcordImage = handle;
        handleLabel = ModernUIKit.Label(handle.transform, "Lbl", "ÇEK", 28, Color.white, ModernUIKit.FontRole.Display);
        handleLabel.fontStyle = FontStyles.Bold | FontStyles.Italic;
        handleLabel.characterSpacing = 4f;
        ModernUIKit.Stretch(handleLabel.rectTransform);

        arrows = new RectTransform[3];
        for (int i = 0; i < arrows.Length; i++)
        {
            Image a = ModernUIKit.MakeImage(rr, "Arrow" + i, ModernUIKit.Heat, ModernUIKit.ChevronSprite, false);
            a.raycastTarget = false;
            arrows[i] = a.rectTransform;
            ModernUIKit.Anchor(arrows[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(34f, 22f));
        }

        // ---- Sonuç ----
        resultText = ModernUIKit.Label(uiCanvas.transform, "Result", "", 92, Color.white, ModernUIKit.FontRole.Display);
        resultText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        resultText.characterSpacing = 8f;
        resultText.outlineWidth = 0.15f;
        resultText.outlineColor = new Color32(0, 0, 0, 200);
        ModernUIKit.Anchor(resultText.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 120f));
        resultText.gameObject.SetActive(false);

        resultSub = ModernUIKit.Label(uiCanvas.transform, "ResultSub", "", 22, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        resultSub.characterSpacing = 8f;
        ModernUIKit.Anchor(resultSub.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(900f, 30f));
        resultSub.gameObject.SetActive(false);

        UpdateRipcordVisual(0f);
    }
}
