using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Savaş üst göstergesi: iki tarafın isim/tip/dönüş çubuğu karşılıklı, ortada süre.
/// Hasar alınca çubukta beyaz "gecikmeli iz" kalır (dövüş oyunu tarzı).
/// </summary>
public class BattleHUD : MonoBehaviour
{
    static BattleHUD instance;

    BeybladeController left, right;   // sol: oyuncu, sağ: rakip
    Side leftSide, rightSide;
    Canvas canvas;
    TextMeshProUGUI timerLabel;
    TextMeshProUGUI timerCaption;
    float matchTime;
    bool built;

    class Side
    {
        public RectTransform root;
        public Image stripe, fill, ghost, flash;
        public TextMeshProUGUI name, type, pct, rpm;
        public float ghostValue = 1f, ghostHold, lastValue = 1f, flashTimer;
        public Color color;
        public bool mirrored;
    }

    public static void Register(BeybladeController bey)
    {
        if (bey == null) return;
        if (instance == null)
        {
            GameObject go = new GameObject("BattleHUD");
            instance = go.AddComponent<BattleHUD>();
        }
        bool leftSlot = bey.isPlayer || bey.gameObject.name.Contains("Player");
        if (leftSlot) instance.left = bey; else instance.right = bey;
    }

    void LateUpdate()
    {
        if (!built && left != null && right != null) Build();
        if (!built) return;

        bool running = left.isLaunched && right.isLaunched && !left.hasToppled && !right.hasToppled;
        if (running) matchTime += Time.deltaTime;
        int secs = Mathf.FloorToInt(matchTime);
        timerLabel.text = (secs / 60) + ":" + (secs % 60).ToString("00");

        UpdateSide(leftSide, left);
        UpdateSide(rightSide, right);
    }

    void Build()
    {
        built = true;
        ModernUIKit.EnsureFonts();
        canvas = ModernUIKit.CreateCanvas("BattleHUD", 95);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;

        // Orta: süre
        Image timerBox = ModernUIKit.MakeImage(canvas.transform, "TimerBox", new Color(0.02f, 0.03f, 0.07f, 0.88f), ModernUIKit.SharpCardSprite);
        timerBox.raycastTarget = false;
        ModernUIKit.Anchor(timerBox.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(124f, 70f));
        Outline tol = timerBox.gameObject.AddComponent<Outline>();
        tol.effectColor = new Color(1f, 1f, 1f, 0.12f);
        tol.effectDistance = new Vector2(1f, -1f);

        timerLabel = ModernUIKit.Label(timerBox.transform, "Time", "0:00", 34, Color.white, ModernUIKit.FontRole.Display);
        timerLabel.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(timerLabel.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 40f));
        timerCaption = ModernUIKit.Label(timerBox.transform, "Cap", MatchContext.IsCareer ? "KARİYER LV " + MatchContext.CareerLevel : "SÜRE", 12, ModernUIKit.Dim, ModernUIKit.FontRole.Caption);
        timerCaption.characterSpacing = 3f;
        ModernUIKit.Anchor(timerCaption.rectTransform, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 18f));

        string playerName = PlayerDataManager.Instance != null ? PlayerDataManager.Instance.data.playerName : "SEN";
        if (MatchContext.IsBalanceTest) playerName = "BOT A";
        string enemyName = MatchContext.IsBalanceTest ? "BOT B" : (MatchContext.IsCareer ? "BOT · LV " + MatchContext.CareerLevel : "RAKİP");

        leftSide = BuildSide(false, playerName.ToUpper(), left);
        rightSide = BuildSide(true, enemyName, right);
    }

    Side BuildSide(bool mirrored, string displayName, BeybladeController bey)
    {
        var s = new Side { mirrored = mirrored, color = ModernUIKit.TypeColor(bey.beybladeType) };
        float sign = mirrored ? 1f : -1f;
        Vector2 pivot = new Vector2(mirrored ? 0f : 1f, 1f);

        GameObject rootObj = new GameObject(mirrored ? "RightSide" : "LeftSide");
        rootObj.transform.SetParent(canvas.transform, false);
        s.root = rootObj.AddComponent<RectTransform>();
        ModernUIKit.Anchor(s.root, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pivot, new Vector2(sign * 74f, -18f), new Vector2(560f, 70f));

        Image bg = ModernUIKit.MakeSkewPanel(s.root, "Bg", new Color(0.02f, 0.03f, 0.07f, 0.86f));
        ModernUIKit.Stretch(bg.rectTransform);
        if (mirrored) bg.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

        s.stripe = ModernUIKit.MakeImage(s.root, "Stripe", s.color, null, false);
        s.stripe.sprite = null;
        s.stripe.raycastTarget = false;
        ModernUIKit.Anchor(s.stripe.rectTransform, new Vector2(mirrored ? 1f : 0f, 0f), new Vector2(mirrored ? 1f : 0f, 1f),
            new Vector2(mirrored ? 1f : 0f, 0.5f), new Vector2(mirrored ? -26f : 26f, 0f), new Vector2(5f, -18f));

        TextAlignmentOptions outerAlign = mirrored ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
        TextAlignmentOptions innerAlign = mirrored ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;

        s.name = ModernUIKit.Label(s.root, "Name", displayName, 22, Color.white, ModernUIKit.FontRole.Display, outerAlign);
        s.name.fontStyle = FontStyles.Bold;
        s.name.characterSpacing = 2f;
        s.name.textWrappingMode = TextWrappingModes.NoWrap;
        s.name.overflowMode = TextOverflowModes.Ellipsis;
        ModernUIKit.Anchor(s.name.rectTransform, new Vector2(mirrored ? 1f : 0f, 1f), new Vector2(mirrored ? 1f : 0f, 1f),
            new Vector2(mirrored ? 1f : 0f, 1f), new Vector2(mirrored ? -42f : 42f, -6f), new Vector2(260f, 28f));

        s.type = ModernUIKit.Label(s.root, "Type", ModernUIKit.TypeName(bey.beybladeType), 13, s.color, ModernUIKit.FontRole.Title, outerAlign);
        s.type.fontStyle = FontStyles.Bold;
        s.type.characterSpacing = 4f;
        ModernUIKit.Anchor(s.type.rectTransform, new Vector2(mirrored ? 1f : 0f, 1f), new Vector2(mirrored ? 1f : 0f, 1f),
            new Vector2(mirrored ? 1f : 0f, 1f), new Vector2(mirrored ? -260f : 260f, -12f), new Vector2(160f, 20f));
        s.type.alignment = innerAlign;

        // Dönüş çubuğu
        Image track = ModernUIKit.MakeImage(s.root, "Track", new Color(1f, 1f, 1f, 0.07f), null, false);
        track.sprite = null;
        track.raycastTarget = false;
        ModernUIKit.Anchor(track.rectTransform, new Vector2(mirrored ? 1f : 0f, 0f), new Vector2(mirrored ? 1f : 0f, 0f),
            new Vector2(mirrored ? 1f : 0f, 0f), new Vector2(mirrored ? -42f : 42f, 12f), new Vector2(400f, 16f));

        s.ghost = ModernUIKit.MakeHorizontalFill(track.transform, "Ghost", new Color(1f, 1f, 1f, 0.6f), mirrored);
        ModernUIKit.Stretch(s.ghost.rectTransform);
        s.fill = ModernUIKit.MakeHorizontalFill(track.transform, "Fill", s.color, mirrored);
        ModernUIKit.Stretch(s.fill.rectTransform);
        s.flash = ModernUIKit.MakeImage(track.transform, "Flash", new Color(1f, 1f, 1f, 0f), null, false);
        s.flash.sprite = null;
        s.flash.raycastTarget = false;
        ModernUIKit.Stretch(s.flash.rectTransform, -2, -2, -2, -2);

        // Çubuk üzerinde 4 bölme çizgisi
        for (int i = 1; i < 4; i++)
        {
            Image tick = ModernUIKit.MakeImage(track.transform, "Tick" + i, new Color(0f, 0f, 0f, 0.45f), null, false);
            tick.sprite = null;
            tick.raycastTarget = false;
            float x = i / 4f;
            ModernUIKit.Anchor(tick.rectTransform, new Vector2(x, 0f), new Vector2(x, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 0f));
        }

        s.pct = ModernUIKit.Label(s.root, "Pct", "100", 30, Color.white, ModernUIKit.FontRole.Display, innerAlign);
        s.pct.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(s.pct.rectTransform, new Vector2(mirrored ? 0f : 1f, 0f), new Vector2(mirrored ? 0f : 1f, 0f),
            new Vector2(mirrored ? 0f : 1f, 0f), new Vector2(mirrored ? 30f : -30f, 4f), new Vector2(80f, 36f));

        s.rpm = ModernUIKit.Label(s.root, "Rpm", "", 11, ModernUIKit.Dim, ModernUIKit.FontRole.Caption, innerAlign);
        s.rpm.characterSpacing = 2f;
        ModernUIKit.Anchor(s.rpm.rectTransform, new Vector2(mirrored ? 0f : 1f, 0f), new Vector2(mirrored ? 0f : 1f, 0f),
            new Vector2(mirrored ? 0f : 1f, 0f), new Vector2(mirrored ? 30f : -30f, 38f), new Vector2(110f, 16f));
        return s;
    }

    void UpdateSide(Side s, BeybladeController bey)
    {
        if (s == null || bey == null) return;
        float v = bey.isLaunched ? bey.StaminaRatio : 1f;

        // Tip, fırlatmadan sonra parçalarla değişebilir: rengi takip et
        Color typeCol = ModernUIKit.TypeColor(bey.beybladeType);
        if (typeCol != s.color)
        {
            s.color = typeCol;
            s.stripe.color = typeCol;
            s.type.color = typeCol;
            s.type.text = ModernUIKit.TypeName(bey.beybladeType);
        }

        // Ani düşüşte beyaz iz bir süre kalır, sonra kayar
        if (v < s.lastValue - 0.004f)
        {
            s.ghostHold = 0.45f;
            s.flashTimer = 0.18f;
        }
        s.lastValue = v;
        if (s.ghostHold > 0f) s.ghostHold -= Time.unscaledDeltaTime;
        else s.ghostValue = Mathf.MoveTowards(s.ghostValue, v, Time.unscaledDeltaTime * 0.6f);
        if (s.ghostValue < v) s.ghostValue = v;

        s.fill.fillAmount = v;
        s.ghost.fillAmount = s.ghostValue;

        bool low = v < 0.25f && bey.isLaunched;
        Color fc = low ? Color.Lerp(ModernUIKit.Danger, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.unscaledTime * 12f)) : s.color;
        s.fill.color = fc;

        s.flashTimer = Mathf.Max(0f, s.flashTimer - Time.unscaledDeltaTime);
        s.flash.color = new Color(1f, 1f, 1f, s.flashTimer * 2.2f);

        s.pct.text = Mathf.CeilToInt(v * 100f).ToString();
        s.pct.color = low ? ModernUIKit.Danger : Color.white;
        s.rpm.text = bey.isLaunched ? Mathf.RoundToInt(bey.currentSpinRPM) + " RPM" : "HAZIR";
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
