using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using BeybladeCore;

/// <summary>
/// Ana menü (tamamen koddan kurulur):
///  Giriş → (ilk açılışta isim) → Ana menü
///  Ana menü: üst bar (profil, beycoin, ayarlar) · Hızlı Savaş / Kariyer kartları · bey vitrini ·
///            alt gezinme (Beylocker, Mağaza, Görevler, Profil)
///  Alt ekranlar: Mağaza, Beylocker, Kariyer, Görevler, Profil, Ayarlar.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public GameObject loginPanel;
    public GameObject mainMenuPanel;
    public GameObject shopPanel;
    public GameObject customizationPanel;
    public GameObject tasksPanel;
    public GameObject pathingPanel;
    public GameObject profilePanel;
    public GameObject settingsPanel;
    public PartDatabase partDatabase;

    Transform root;

    // Arka plan animasyonu
    RectTransform[] bgRings;
    RectTransform bgStreak;

    // Giriş
    GameObject nameEntry, welcomeGroup;
    TMP_InputField nameField;
    TextMeshProUGUI welcomeLabel, touchPulse;

    // Ana menü dinamik
    TextMeshProUGUI profileName, profileRank, profileInitial, coinHud;
    Image profileAvatar;
    TextMeshProUGUI careerSub, careerLevel, battleSub, lockerSub, lockerType, tasksBadgeLabel;
    Image tasksBadge, battleGlow, lockerTypeChip;
    RectTransform battleCard;
    RawImage lockerIcon;
    string lockerIconKey;

    // Alt ekranlar
    ShopUI shopUI;
    Transform tasksList, profileContent, pathingContent;
    TextMeshProUGUI[] subCoinLabels = new TextMeshProUGUI[0];
    TextMeshProUGUI settingsVolumeValue, resetLabel;
    Button[] qualityButtons;
    float resetArmedUntil;

    static readonly Color Panel = new Color(0.03f, 0.05f, 0.1f, 0.88f);
    static readonly Color PanelSoft = new Color(0.05f, 0.08f, 0.15f, 0.7f);

    void Start()
    {
        ModernUIKit.EnsureEventSystem();
        ModernUIKit.EnsureFonts();
        EnsurePlayerData();
        partDatabase = PartDatabase.Resolve(partDatabase);
        Time.timeScale = 1f;
        // Arkada canlı savaş
        MenuArena.Ensure();
        RebuildUI();

        // Maçtan dönülüyorsa girişi atla
        if (cameFromBattle) ShowMainMenu();
        else ShowLogin();
        cameFromBattle = true;
    }

    static bool cameFromBattle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => cameFromBattle = false;

    void EnsurePlayerData()
    {
        if (PlayerDataManager.Instance == null)
            new GameObject("PlayerDataManager").AddComponent<PlayerDataManager>();
    }

    PlayerSaveData Data => PlayerDataManager.Instance != null ? PlayerDataManager.Instance.data : null;

    void RebuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = ModernUIKit.CreateCanvas("MainCanvas", 10);
        root = canvas.transform;
        if (root != transform && GetComponent<Canvas>() == null)
            transform.SetParent(root, false);

        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyImmediate(root.GetChild(i).gameObject);

        BuildBackground();
        BuildLogin();
        BuildMainPanel();
        BuildShop();
        BuildCustomization();
        BuildTasks();
        BuildPathing();
        BuildProfile();
        BuildSettings();
    }

    // =====================================================================
    // Arka plan
    // =====================================================================

    void BuildBackground()
    {
        // Arka planda MenuArena (3B canlı savaş) var: burada sadece okunabilirlik için yumuşak karartmalar
        bgRings = new RectTransform[0];

        Image tint = ModernUIKit.MakeImage(root, "Tint", new Color(0.01f, 0.02f, 0.05f, 0.18f), null, false);
        tint.sprite = null;
        tint.raycastTarget = false;
        ModernUIKit.Stretch(tint.rectTransform);

        Image topShade = ModernUIKit.MakeImage(root, "TopShade", new Color(0.01f, 0.02f, 0.05f, 0.85f), ModernUIKit.VerticalGradientSprite, false);
        topShade.raycastTarget = false;
        ModernUIKit.Anchor(topShade.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 260f));

        Image bottomShade = ModernUIKit.MakeImage(root, "BottomShade", new Color(0.01f, 0.02f, 0.05f, 0.92f), ModernUIKit.VerticalGradientSprite, false);
        bottomShade.raycastTarget = false;
        ModernUIKit.Anchor(bottomShade.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 460f));
        bottomShade.rectTransform.localScale = new Vector3(1f, -1f, 1f);

        Image streak = ModernUIKit.MakeSkewPanel(root, "Streak", new Color(0.3f, 0.85f, 1f, 0.03f));
        ModernUIKit.Anchor(streak.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200f, 0f), new Vector2(420f, 1500f));
        streak.rectTransform.localEulerAngles = new Vector3(0f, 0f, -28f);
        bgStreak = streak.rectTransform;
    }

    // =====================================================================
    // Giriş ekranı
    // =====================================================================

    void BuildLogin()
    {
        loginPanel = NewLayer("LoginPanel");

        RectTransform logo = BuildLogo(loginPanel.transform, 1f);
        ModernUIKit.Anchor(logo, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, logo.sizeDelta);

        // --- İlk açılış: isim ---
        nameEntry = new GameObject("NameEntry");
        nameEntry.transform.SetParent(loginPanel.transform, false);
        RectTransform ne = nameEntry.AddComponent<RectTransform>();
        ModernUIKit.Anchor(ne, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 220f));

        TextMeshProUGUI ask = ModernUIKit.Label(ne, "Ask", "BLADER ADIN", 18, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        ask.fontStyle = FontStyles.Bold;
        ask.characterSpacing = 8f;
        ModernUIKit.Anchor(ask.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(600f, 26f));

        nameField = MakeInputField(ne, "Name", "İsmini yaz...", new Vector2(0f, -34f), new Vector2(520f, 66f));

        Button start = MakePrimaryButton(ne, "Start", "BAŞLA", ModernUIKit.Cyan, new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(320f, 74f), ConfirmName);

        // --- Dönen oyuncu: hoş geldin ---
        welcomeGroup = new GameObject("Welcome");
        welcomeGroup.transform.SetParent(loginPanel.transform, false);
        RectTransform wg = welcomeGroup.AddComponent<RectTransform>();
        ModernUIKit.Stretch(wg);

        Image tap = ModernUIKit.MakeImage(wg, "TapCatcher", Color.clear, null, false);
        tap.sprite = null;
        ModernUIKit.Stretch(tap.rectTransform);
        Button tapBtn = tap.gameObject.AddComponent<Button>();
        tapBtn.transition = Selectable.Transition.None;
        tapBtn.onClick.AddListener(ShowMainMenu);

        welcomeLabel = ModernUIKit.Label(wg, "Welcome", "", 24, Color.white, ModernUIKit.FontRole.Title);
        welcomeLabel.characterSpacing = 6f;
        ModernUIKit.Anchor(welcomeLabel.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 34f));

        touchPulse = ModernUIKit.Label(wg, "Touch", "BAŞLAMAK İÇİN DOKUN", 22, Color.white, ModernUIKit.FontRole.Title);
        touchPulse.fontStyle = FontStyles.Bold;
        touchPulse.characterSpacing = 10f;
        ModernUIKit.Anchor(touchPulse.rectTransform, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 34f));

        Button change = MakeTextButton(wg, "ChangeName", "PROFİLİ DEĞİŞTİR", new Vector2(0.5f, 0f), new Vector2(0f, 60f), () =>
        {
            nameEntry.SetActive(true);
            welcomeGroup.SetActive(false);
            if (Data != null) nameField.text = Data.playerName;
        });

        TextMeshProUGUI ver = ModernUIKit.Label(loginPanel.transform, "Version", GameBrand.Version, 14, new Color(1f, 1f, 1f, 0.3f), ModernUIKit.FontRole.Caption, TextAlignmentOptions.Left);
        ModernUIKit.Anchor(ver.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(28f, 20f), new Vector2(200f, 20f));
    }

    RectTransform BuildLogo(Transform parent, float scale)
    {
        GameObject go = new GameObject("Logo");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1000f * scale, 360f * scale);

        Image ring = ModernUIKit.MakeImage(rt, "LogoRing", new Color(0.3f, 0.85f, 1f, 0.5f), ModernUIKit.ThickRingSprite, false);
        ring.raycastTarget = false;
        ModernUIKit.Anchor(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f * scale), new Vector2(330f * scale, 330f * scale));
        ring.type = Image.Type.Filled;
        ring.fillMethod = Image.FillMethod.Radial360;
        ring.fillAmount = 0.78f;

        TextMeshProUGUI top = ModernUIKit.Label(rt, "Top", GameBrand.TitleTop, 150f * scale, Color.white, ModernUIKit.FontRole.Display);
        top.fontStyle = FontStyles.Bold | FontStyles.Italic;
        top.characterSpacing = 16f;
        top.enableVertexGradient = true;
        top.colorGradient = new VertexGradient(Color.white, Color.white, new Color(0.7f, 0.88f, 1f), new Color(0.55f, 0.78f, 1f));
        top.outlineWidth = 0.12f;
        top.outlineColor = new Color32(0, 20, 50, 220);
        ModernUIKit.Anchor(top.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), new Vector2(-70f * scale, 0f), new Vector2(900f * scale, 150f * scale));

        TextMeshProUGUI bottom = ModernUIKit.Label(rt, "Bottom", GameBrand.TitleBottom, 120f * scale, ModernUIKit.Cyan, ModernUIKit.FontRole.Display);
        bottom.fontStyle = FontStyles.Bold | FontStyles.Italic;
        bottom.characterSpacing = 28f;
        bottom.outlineWidth = 0.12f;
        bottom.outlineColor = new Color32(0, 30, 60, 220);
        ModernUIKit.Anchor(bottom.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), new Vector2(80f * scale, 0f), new Vector2(900f * scale, 130f * scale));

        Image bar = ModernUIKit.MakeSkewPanel(rt, "Bar", ModernUIKit.Heat);
        ModernUIKit.Anchor(bar.rectTransform, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.5f), new Vector2(60f * scale, 0f), new Vector2(360f * scale, 10f * scale));

        TextMeshProUGUI tag = ModernUIKit.Label(rt, "Tagline", GameBrand.Tagline, Mathf.Max(13f, 20f * scale), ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        tag.characterSpacing = scale < 0.6f ? 6f : 14f;
        ModernUIKit.Anchor(tag.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f * scale), new Vector2(900f * scale, 30f * scale));
        return rt;
    }

    void ConfirmName()
    {
        string n = nameField != null ? nameField.text : "";
        if (string.IsNullOrWhiteSpace(n))
        {
            if (nameField != null) nameField.placeholder.color = ModernUIKit.Danger;
            return;
        }
        if (PlayerDataManager.Instance != null) PlayerDataManager.Instance.SetPlayerName(n);
        ShowMainMenu();
    }

    // =====================================================================
    // Ana menü
    // =====================================================================

    void BuildMainPanel()
    {
        mainMenuPanel = NewLayer("MainMenuPanel");
        Transform p = mainMenuPanel.transform;

        BuildTopBar(p);

        // Küçük logo (üst orta)
        RectTransform logo = BuildLogo(p, 0.42f);
        ModernUIKit.Anchor(logo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), logo.sizeDelta);

        // ---- Alt: üç büyük kart (arena ortada görünür kalır) ----
        const float y = 150f;
        RectTransform career = MakeActionCard(p, "Career", "KARİYER", ModernUIKit.Gold, new Vector2(-560f, y), new Vector2(430f, 180f), 42, ShowPathing, out careerSub);
        careerLevel = MakeCardBadge(career, ModernUIKit.Gold);

        battleCard = MakeActionCard(p, "Battle", "SAVAŞ", ModernUIKit.Battle, new Vector2(0f, y + 14f), new Vector2(560f, 220f), 78, StartBattle, out battleSub, true);
        battleGlow = battleCard.Find("Glow").GetComponent<Image>();
        MakeBattleIcon(battleCard);

        RectTransform locker = MakeActionCard(p, "Locker", "BEYLOCKER", ModernUIKit.Cyan, new Vector2(560f, y), new Vector2(430f, 180f), 38, ShowCustomization, out lockerSub);
        // Senin beyin: canlı 3B ikon
        lockerIcon = new GameObject("BeyIcon").AddComponent<RawImage>();
        lockerIcon.transform.SetParent(locker, false);
        lockerIcon.raycastTarget = false;
        // Kartın üst kenarından taşan "pop-out" görsel
        ModernUIKit.Anchor(lockerIcon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-6f, 6f), new Vector2(190f, 190f));
        lockerTypeChip = ModernUIKit.MakeSkewPanel(locker, "TypeChip", ModernUIKit.Cyan);
        ModernUIKit.Anchor(lockerTypeChip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(170f, 30f));
        lockerType = ModernUIKit.Label(lockerTypeChip.transform, "T", "", 14, new Color(0.02f, 0.04f, 0.08f), ModernUIKit.FontRole.Display);
        lockerType.fontStyle = FontStyles.Bold;
        lockerType.characterSpacing = 3f;
        ModernUIKit.Stretch(lockerType.rectTransform);

        // ---- En alt: küçük bağlantılar ----
        GameObject nav = new GameObject("BottomLinks");
        nav.transform.SetParent(p, false);
        RectTransform navRt = nav.AddComponent<RectTransform>();
        ModernUIKit.Anchor(navRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(780f, 62f));
        Image navBg = ModernUIKit.MakeSkewPanel(navRt, "Bg", new Color(0.02f, 0.04f, 0.09f, 0.78f));
        ModernUIKit.Stretch(navBg.rectTransform);

        string[] labels = { "GÖREVLER", "MAĞAZA", "PROFİL" };
        string[] icons = { "G", "M", "P" };
        Color[] accents = { ModernUIKit.Mint, ModernUIKit.Gold, ModernUIKit.Magenta };
        UnityAction[] actions = { ShowTasks, ShowShop, ShowProfile };
        for (int i = 0; i < labels.Length; i++)
        {
            Button b = MakeNavButton(navRt, labels[i], icons[i], accents[i], i, labels.Length, actions[i]);
            if (i == 0)
            {
                tasksBadge = ModernUIKit.MakeImage(b.transform, "Badge", ModernUIKit.Danger, ModernUIKit.DiscSprite, false);
                tasksBadge.raycastTarget = false;
                ModernUIKit.Anchor(tasksBadge.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-34f, -12f), new Vector2(24f, 24f));
                tasksBadgeLabel = ModernUIKit.Label(tasksBadge.transform, "N", "", 13, Color.white, ModernUIKit.FontRole.Title);
                tasksBadgeLabel.fontStyle = FontStyles.Bold;
                ModernUIKit.Stretch(tasksBadgeLabel.rectTransform);
            }
        }
    }

    void BuildTopBar(Transform p)
    {
        // Profil çipi (sol üst)
        Image chip = ModernUIKit.MakeSkewPanel(p, "ProfileChip", Panel);
        chip.raycastTarget = true;
        ModernUIKit.Anchor(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -26f), new Vector2(400f, 80f));
        Button chipBtn = chip.gameObject.AddComponent<Button>();
        chipBtn.targetGraphic = chip;
        chipBtn.onClick.AddListener(ShowProfile);
        ModernUIKit.AddPressFeedback(chipBtn);

        profileAvatar = ModernUIKit.MakeImage(chip.transform, "Avatar", ModernUIKit.Cyan, ModernUIKit.DiscSprite, false);
        profileAvatar.raycastTarget = false;
        ModernUIKit.Anchor(profileAvatar.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(58f, 58f));
        profileInitial = ModernUIKit.Label(profileAvatar.transform, "I", "B", 30, new Color(0.02f, 0.04f, 0.08f), ModernUIKit.FontRole.Display);
        profileInitial.fontStyle = FontStyles.Bold;
        ModernUIKit.Stretch(profileInitial.rectTransform);

        profileName = ModernUIKit.Label(chip.transform, "Name", "", 24, Color.white, ModernUIKit.FontRole.Display, TextAlignmentOptions.Left);
        profileName.fontStyle = FontStyles.Bold;
        profileName.textWrappingMode = TextWrappingModes.NoWrap;
        profileName.overflowMode = TextOverflowModes.Ellipsis;
        ModernUIKit.Anchor(profileName.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(102f, -2f), new Vector2(-140f, 30f));

        profileRank = ModernUIKit.Label(chip.transform, "Rank", "", 14, ModernUIKit.Dim, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        profileRank.characterSpacing = 3f;
        ModernUIKit.Anchor(profileRank.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 1f), new Vector2(102f, -2f), new Vector2(-140f, 22f));

        // Ayarlar (sağ üst)
        Button settings = MakeIconButton(p, "Settings", new Vector2(1f, 1f), new Vector2(-28f, -30f), ShowSettings);

        // Beycoin
        coinHud = MakeCoinPill(p, "Coins", new Vector2(1f, 1f), new Vector2(-112f, -30f));
    }

    /// <summary>Alt sıradaki büyük eylem kartı (eğik panel, vurgu şeridi, parıltı, başlık + alt yazı).</summary>
    RectTransform MakeActionCard(Transform parent, string name, string title, Color accent, Vector2 pos, Vector2 size, float titleSize,
                                 UnityAction onClick, out TextMeshProUGUI sub, bool primary = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        ModernUIKit.Anchor(rt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, size);

        Image glow = ModernUIKit.MakeImage(rt, "Glow", ModernUIKit.WithAlpha(accent, primary ? 0.25f : 0.1f), ModernUIKit.GlowSprite, false);
        glow.raycastTarget = false;
        ModernUIKit.Stretch(glow.rectTransform, -140, -140, -110, -110);

        Image bg = ModernUIKit.MakeSkewPanel(rt, "Bg", primary ? ModernUIKit.WithAlpha(accent, 0.92f) : new Color(0.03f, 0.06f, 0.12f, 0.9f));
        bg.raycastTarget = true;
        ModernUIKit.Stretch(bg.rectTransform);
        Outline ol = bg.gameObject.AddComponent<Outline>();
        ol.effectColor = ModernUIKit.WithAlpha(primary ? Color.white : accent, primary ? 0.35f : 0.6f);
        ol.effectDistance = new Vector2(2f, -2f);

        if (primary)
        {
            // Üst parlama şeridi
            Image sheen = ModernUIKit.MakeImage(rt, "Sheen", new Color(1f, 1f, 1f, 0.16f), ModernUIKit.VerticalGradientSprite, false);
            sheen.raycastTarget = false;
            ModernUIKit.Anchor(sheen.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(-30f, size.y * 0.5f));
        }
        else
        {
            Image stripe = ModernUIKit.MakeSkewPanel(rt, "Stripe", accent);
            ModernUIKit.Anchor(stripe.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(-30f, 6f));
        }

        Color titleCol = primary ? new Color(0.02f, 0.06f, 0.03f) : Color.white;
        TextMeshProUGUI t = ModernUIKit.Label(rt, "Title", title, titleSize, titleCol, ModernUIKit.FontRole.Display, primary ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold | FontStyles.Italic;
        t.characterSpacing = primary ? 14f : 6f;
        if (primary)
            ModernUIKit.Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(56f, 14f), new Vector2(size.x - 170f, titleSize + 12f));
        else
            ModernUIKit.Anchor(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(36f, -4f), new Vector2(-60f, titleSize + 10f));

        sub = ModernUIKit.Label(rt, "Sub", "", primary ? 17 : 15, primary ? new Color(0.02f, 0.08f, 0.04f, 0.85f) : ModernUIKit.Dim, ModernUIKit.FontRole.Title,
            primary ? TextAlignmentOptions.Center : TextAlignmentOptions.Left);
        sub.characterSpacing = 1.5f;
        sub.richText = true;
        if (primary)
            ModernUIKit.Anchor(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(56f, -26f), new Vector2(size.x - 170f, 50f));
        else
            ModernUIKit.Anchor(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 1f), new Vector2(38f, -10f), new Vector2(-60f, 50f));

        Button btn = bg.gameObject.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(onClick);
        ModernUIKit.AddPressFeedback(btn);
        return rt;
    }

    /// <summary>Kart köşesinde küçük rozet (ör. kariyer seviyesi).</summary>
    TextMeshProUGUI MakeCardBadge(RectTransform card, Color accent)
    {
        Image disc = ModernUIKit.MakeImage(card, "Badge", new Color(0.02f, 0.04f, 0.08f, 0.95f), ModernUIKit.DiscSprite, false);
        disc.raycastTarget = false;
        ModernUIKit.Anchor(disc.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 4f), new Vector2(112f, 112f));
        Image ring = ModernUIKit.MakeImage(disc.transform, "Ring", accent, ModernUIKit.ThickRingSprite, false);
        ring.raycastTarget = false;
        ModernUIKit.Stretch(ring.rectTransform);
        TextMeshProUGUI cap = ModernUIKit.Label(disc.transform, "Cap", "SEVİYE", 12, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        cap.characterSpacing = 3f;
        ModernUIKit.Anchor(cap.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 18f));
        TextMeshProUGUI v = ModernUIKit.Label(disc.transform, "V", "1", 46, accent, ModernUIKit.FontRole.Display);
        v.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(v.rectTransform, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 54f));
        return v;
    }

    /// <summary>SAVAŞ kartındaki çapraz kanat simgesi (özgün, basit şekiller).</summary>
    void MakeBattleIcon(RectTransform card)
    {
        for (int i = 0; i < 2; i++)
        {
            Image blade = ModernUIKit.MakeSkewPanel(card, "Blade" + i, new Color(0.02f, 0.08f, 0.04f, 0.85f));
            ModernUIKit.Anchor(blade.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(88f, 4f), new Vector2(20f, 96f));
            blade.rectTransform.localEulerAngles = new Vector3(0f, 0f, i == 0 ? 40f : -40f);
        }
        Image hub = ModernUIKit.MakeImage(card, "Hub", new Color(0.02f, 0.08f, 0.04f, 0.9f), ModernUIKit.DiscSprite, false);
        hub.raycastTarget = false;
        ModernUIKit.Anchor(hub.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(88f, 4f), new Vector2(28f, 28f));
    }

    void RefreshMain()
    {
        PlayerSaveData d = Data;
        if (d == null) return;

        profileName.text = d.playerName.ToUpper();
        profileInitial.text = d.playerName.Length > 0 ? d.playerName.Substring(0, 1).ToUpper() : "B";
        profileRank.text = (string.IsNullOrEmpty(d.rankTitle) ? PlayerDataManager.RankFor(d.totalWins) : d.rankTitle).ToUpper()
                           + "  ·  " + d.totalWins + " GALİBİYET";
        RefreshCoins();

        BeyStatBlock stats = PlayerLoadout.ComputeStats(partDatabase);
        BeybladeType type = (BeybladeType)(int)stats.Archetype;
        Color tc = ModernUIKit.TypeColor(type);
        profileAvatar.color = tc;

        battleSub.text = "Eşit güçte <b>SAVUNMA</b> rakibe karşı  ·  40+ beycoin";
        BeyArchetype rival = BotDifficulty.ArchetypeForLevel(d.currentPathingLevel);
        careerSub.text = "Sıradaki rakip\n<b><color=#FFFFFF>" + PlayerLoadout.ArchetypeName(rival) + "</color></b>";
        careerLevel.text = d.currentPathingLevel.ToString();

        BeybladePart l = partDatabase.GetPartByID(d.equippedLayerID);
        lockerSub.text = "Senin beyin\n<b><color=#FFFFFF>" + (l != null ? l.partName.ToUpper() : "-") + "</color></b>";
        lockerType.text = ModernUIKit.TypeName(type);
        lockerTypeChip.color = tc;

        // Beyin canlı 3B ikonu (parça değişince yeni ikon)
        string key = "bey_" + d.equippedLayerID + "_" + d.equippedDiskID + "_" + d.equippedTipID;
        if (lockerIconKey != key)
        {
            lockerIconKey = key;
            string li = d.equippedLayerID, di = d.equippedDiskID, ti = d.equippedTipID;
            PartStudio studio = PartStudio.Get();
            lockerIcon.texture = studio.Atlas;
            lockerIcon.uvRect = studio.IconRect(key, parent =>
            {
                GameObject bey = ProceduralBeyFactory.BuildBey(BeyDesignCatalog.Assemble(li, di, ti), parent, 1f, out float contact);
                bey.transform.localPosition = new Vector3(0f, -contact * 0.35f, 0f);
                return bey;
            });
        }

        int claimable = GameTasks.ClaimableCount(d);
        tasksBadge.gameObject.SetActive(claimable > 0);
        tasksBadgeLabel.text = claimable.ToString();
    }

    void RefreshCoins()
    {
        PlayerSaveData d = Data;
        if (d == null) return;
        string g = d.gold.ToString("N0");
        if (coinHud != null) coinHud.text = g;
        foreach (TextMeshProUGUI l in subCoinLabels) if (l != null) l.text = g;
    }

    // =====================================================================
    // Mağaza
    // =====================================================================

    void BuildShop()
    {
        shopPanel = MakeSubPanel("ShopPanel", "MAĞAZA", "Parça al, beyini güçlendir. Beycoin maçlardan ve görevlerden gelir.", ModernUIKit.Gold, out TextMeshProUGUI shopCoins);

        GameObject grid = new GameObject("ShopGrid");
        grid.transform.SetParent(shopPanel.transform, false);
        RectTransform gr = grid.AddComponent<RectTransform>();
        ModernUIKit.Stretch(gr, 70, 70, 170, 70);

        TextMeshProUGUI status = ModernUIKit.Label(shopPanel.transform, "Status", "", 18, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        status.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(900f, 28f));

        shopUI = shopPanel.AddComponent<ShopUI>();
        shopUI.gridRoot = grid.transform;
        shopUI.statusLabel = status;
        shopUI.goldLabel = shopCoins;
        shopUI.partDatabase = partDatabase;
    }

    // =====================================================================
    // Beylocker
    // =====================================================================

    void BuildCustomization()
    {
        customizationPanel = MakeSubPanel("CustomizationPanel", "BEYLOCKER", "Parçalarını seç, tipini belirle. Statlar savaşta aynen geçerli.", ModernUIKit.Cyan, out _);

        Image statCard = ModernUIKit.MakeImage(customizationPanel.transform, "StatCard", PanelSoft, ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(statCard.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, -50f), new Vector2(560f, 600f));
        TextMeshProUGUI st = ModernUIKit.Label(statCard.transform, "StatTitle", "STATLAR", 24, Color.white, ModernUIKit.FontRole.Display, TextAlignmentOptions.Left);
        st.fontStyle = FontStyles.Bold;
        st.characterSpacing = 6f;
        ModernUIKit.Anchor(st.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(32f, -24f), new Vector2(-64f, 34f));

        CustomizationUI customUI = customizationPanel.AddComponent<CustomizationUI>();
        customUI.partDatabase = partDatabase;
        customUI.attackBar = MakeStat(statCard.transform, "SALDIRI", ModernUIKit.Heat, 0);
        customUI.defenseBar = MakeStat(statCard.transform, "SAVUNMA", ModernUIKit.Cyan, 1);
        customUI.staminaBar = MakeStat(statCard.transform, "DAYANIKLILIK", ModernUIKit.Mint, 2);
        customUI.weightBar = MakeStat(statCard.transform, "AĞIRLIK", ModernUIKit.Gold, 3);

        TextMeshProUGUI typeLbl = ModernUIKit.Label(statCard.transform, "TypeLabel", "TİP · DENGE", 26, ModernUIKit.Cyan, ModernUIKit.FontRole.Display, TextAlignmentOptions.Left);
        typeLbl.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(typeLbl.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(32f, -500f), new Vector2(-64f, 34f));
        customUI.typeLabel = typeLbl;

        TextMeshProUGUI detailLbl = ModernUIKit.Label(statCard.transform, "DetailLabel", "", 15, ModernUIKit.Dim, ModernUIKit.FontRole.Caption, TextAlignmentOptions.Left);
        ModernUIKit.Anchor(detailLbl.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(32f, -540f), new Vector2(-64f, 26f));
        customUI.detailLabel = detailLbl;

        Image partCard = ModernUIKit.MakeImage(customizationPanel.transform, "PartCard", PanelSoft, ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(partCard.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-110f, -50f), new Vector2(600f, 720f));
        customUI.layerListContainer = MakeSlot(partCard.transform, "LAYER", 0);
        customUI.diskListContainer = MakeSlot(partCard.transform, "DISK", 1);
        customUI.tipListContainer = MakeSlot(partCard.transform, "TIP", 2);

        GameObject prefab = ModernUIKit.MakeButton(customizationPanel.transform, "PartBtnPrefab", "Parça", ModernUIKit.Cyan, new Vector2(170f, 60f), () => { }).gameObject;
        prefab.SetActive(false);
        customUI.partButtonPrefab = prefab;

        TextMeshProUGUI tip = ModernUIKit.Label(customizationPanel.transform, "Tip", "Yeni parçalar MAĞAZA'da.  Tip, statların en yükseğine göre belirlenir: Saldırı > Dayanıklılık > Savunma > Saldırı.", 15, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
        ModernUIKit.Anchor(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1500f, 24f));
    }

    Image MakeStat(Transform parent, string label, Color col, int index)
    {
        float y = -90f - index * 100f;
        TextMeshProUGUI lbl = ModernUIKit.Label(parent, "Lbl_" + label, label, 16, ModernUIKit.Dim, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        lbl.fontStyle = FontStyles.Bold;
        lbl.characterSpacing = 5f;
        ModernUIKit.Anchor(lbl.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(32f, y), new Vector2(-64f, 24f));
        Image bg = ModernUIKit.MakeImage(parent, "Bar_" + label, new Color(1f, 1f, 1f, 0.07f), null, false);
        bg.sprite = null;
        bg.raycastTarget = false;
        ModernUIKit.Anchor(bg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y - 34f), new Vector2(-64f, 14f));
        Image fill = ModernUIKit.MakeHorizontalFill(bg.transform, "Fill", col);
        ModernUIKit.Stretch(fill.rectTransform);
        fill.fillAmount = 0.45f;
        return fill;
    }

    Transform MakeSlot(Transform parent, string title, int index)
    {
        float y = -26f - index * 230f;
        TextMeshProUGUI lbl = ModernUIKit.Label(parent, "Lbl_" + title, title, 22, ModernUIKit.Cyan, ModernUIKit.FontRole.Display, TextAlignmentOptions.Left);
        lbl.fontStyle = FontStyles.Bold;
        lbl.characterSpacing = 6f;
        ModernUIKit.Anchor(lbl.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(30f, y), new Vector2(-60f, 32f));
        GameObject box = new GameObject(title + "_Container");
        box.transform.SetParent(parent, false);
        RectTransform rt = box.AddComponent<RectTransform>();
        ModernUIKit.Anchor(rt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y - 44f), new Vector2(-60f, 150f));
        GridLayoutGroup grid = box.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(170f, 60f);
        grid.spacing = new Vector2(10f, 10f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.childAlignment = TextAnchor.UpperLeft;
        return box.transform;
    }

    // =====================================================================
    // Kariyer
    // =====================================================================

    void BuildPathing()
    {
        pathingPanel = MakeSubPanel("PathingPanel", "KARİYER", "Her seviyede bot biraz daha akıllı ve güçlü. Kazan, ilerle, daha çok beycoin topla.", ModernUIKit.Gold, out _);
        GameObject c = new GameObject("Content");
        c.transform.SetParent(pathingPanel.transform, false);
        RectTransform rt = c.AddComponent<RectTransform>();
        ModernUIKit.Stretch(rt, 0, 0, 170, 0);
        pathingContent = rt;
    }

    void RebuildPathing()
    {
        Clear(pathingContent);
        PlayerSaveData d = Data;
        int level = d != null ? d.currentPathingLevel : 1;
        int first = Mathf.Max(1, level - 2);

        for (int i = 0; i < 5; i++)
        {
            int lv = first + i;
            bool done = lv < level;
            bool current = lv == level;
            bool locked = lv > level;
            Color accent = current ? ModernUIKit.Gold : (done ? ModernUIKit.Mint : ModernUIKit.Dim);

            Image card = ModernUIKit.MakeImage(pathingContent, "Level" + lv, current ? Panel : PanelSoft, ModernUIKit.SharpCardSprite);
            ModernUIKit.Anchor(card.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(-560f + i * 280f, 0f), new Vector2(250f, current ? 360f : 320f));
            Outline ol = card.gameObject.AddComponent<Outline>();
            ol.effectColor = ModernUIKit.WithAlpha(accent, current ? 0.9f : 0.3f);
            ol.effectDistance = new Vector2(current ? 2f : 1f, current ? -2f : -1f);

            Image top = ModernUIKit.MakeImage(card.transform, "Top", accent, null, false);
            top.sprite = null;
            top.raycastTarget = false;
            ModernUIKit.Anchor(top.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 6f));

            TextMeshProUGUI lvl = ModernUIKit.Label(card.transform, "Lv", "LV " + lv, 54, locked ? ModernUIKit.Dim : Color.white, ModernUIKit.FontRole.Display);
            lvl.fontStyle = FontStyles.Bold | FontStyles.Italic;
            ModernUIKit.Anchor(lvl.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 64f));

            BeyArchetype rival = BotDifficulty.ArchetypeForLevel(lv);
            TextMeshProUGUI r = ModernUIKit.Label(card.transform, "Rival", "RAKİP\n<size=130%><b>" + PlayerLoadout.ArchetypeName(rival) + "</b></size>", 14,
                locked ? ModernUIKit.Dim : ModernUIKit.TypeColor((BeybladeType)(int)rival), ModernUIKit.FontRole.Title);
            r.characterSpacing = 3f;
            r.richText = true;
            ModernUIKit.Anchor(r.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 56f));

            int reward = MatchRewards.Calculate(MatchOutcome.Win, true, lv, false);
            TextMeshProUGUI rw = ModernUIKit.Label(card.transform, "Reward", done ? "TAMAMLANDI" : (locked ? "KİLİTLİ" : "ÖDÜL  " + reward + " C"), 15,
                done ? ModernUIKit.Mint : (locked ? ModernUIKit.Dim : ModernUIKit.Gold), ModernUIKit.FontRole.Title);
            rw.fontStyle = FontStyles.Bold;
            rw.characterSpacing = 3f;
            ModernUIKit.Anchor(rw.rectTransform, new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 22f));

            if (!locked)
            {
                Button b = card.gameObject.AddComponent<Button>();
                b.targetGraphic = card;
                int captured = lv;
                b.onClick.AddListener(() => StartPathingMatch(captured));
                ModernUIKit.AddPressFeedback(b);
            }
        }

        MakePrimaryButton(pathingContent, "Play", "LV " + level + " · SAVAŞA BAŞLA", ModernUIKit.Gold, new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(520f, 84f),
            () => StartPathingMatch(level));
    }

    // =====================================================================
    // Görevler
    // =====================================================================

    void BuildTasks()
    {
        tasksPanel = MakeSubPanel("TasksPanel", "GÖREVLER", "Oynadıkça ilerler. Tamamlananın ödülünü AL.", ModernUIKit.Mint, out _);
        GameObject c = new GameObject("List");
        c.transform.SetParent(tasksPanel.transform, false);
        RectTransform rt = c.AddComponent<RectTransform>();
        ModernUIKit.Anchor(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(1100f, 840f));
        tasksList = rt;
    }

    void RebuildTasks()
    {
        Clear(tasksList);
        PlayerSaveData d = Data;
        for (int i = 0; i < GameTasks.All.Count; i++)
        {
            GameTasks.Def t = GameTasks.All[i];
            int prog = GameTasks.Progress(t, d);
            bool complete = GameTasks.IsComplete(t, d);
            bool claimed = GameTasks.IsClaimed(t, d);
            Color accent = claimed ? ModernUIKit.Dim : (complete ? ModernUIKit.Mint : ModernUIKit.Cyan);

            Image card = ModernUIKit.MakeImage(tasksList, "Task" + i, claimed ? new Color(0.03f, 0.05f, 0.1f, 0.5f) : PanelSoft, ModernUIKit.SharpCardSprite);
            card.raycastTarget = false;
            ModernUIKit.Anchor(card.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -i * 112f), new Vector2(0f, 98f));

            Image bar = ModernUIKit.MakeImage(card.transform, "Bar", accent, null, false);
            bar.sprite = null;
            bar.raycastTarget = false;
            ModernUIKit.Anchor(bar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6f, 0f));

            TextMeshProUGUI title = ModernUIKit.Label(card.transform, "Title", t.title, 26, claimed ? ModernUIKit.Dim : Color.white, ModernUIKit.FontRole.Display, TextAlignmentOptions.Left);
            title.fontStyle = FontStyles.Bold | FontStyles.Italic;
            title.characterSpacing = 3f;
            ModernUIKit.Anchor(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(32f, 2f), new Vector2(520f, 34f));

            TextMeshProUGUI desc = ModernUIKit.Label(card.transform, "Desc", t.desc, 16, ModernUIKit.Dim, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
            ModernUIKit.Anchor(desc.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 1f), new Vector2(32f, -4f), new Vector2(420f, 22f));

            // İlerleme
            Image track = ModernUIKit.MakeImage(card.transform, "Track", new Color(1f, 1f, 1f, 0.07f), null, false);
            track.sprite = null;
            track.raycastTarget = false;
            ModernUIKit.Anchor(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(480f, -10f), new Vector2(260f, 8f));
            Image fill = ModernUIKit.MakeHorizontalFill(track.transform, "Fill", accent);
            ModernUIKit.Stretch(fill.rectTransform);
            fill.fillAmount = t.goal > 0 ? (float)prog / t.goal : 1f;
            TextMeshProUGUI pr = ModernUIKit.Label(card.transform, "Prog", prog + " / " + t.goal, 15, Color.white, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
            pr.fontStyle = FontStyles.Bold;
            ModernUIKit.Anchor(pr.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(480f, 14f), new Vector2(260f, 22f));

            TextMeshProUGUI reward = ModernUIKit.Label(card.transform, "Reward", "+" + t.reward + " C", 20, claimed ? ModernUIKit.Dim : ModernUIKit.Gold, ModernUIKit.FontRole.Display, TextAlignmentOptions.Right);
            reward.fontStyle = FontStyles.Bold;
            ModernUIKit.Anchor(reward.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-220f, 0f), new Vector2(140f, 30f));

            if (complete && !claimed)
            {
                GameTasks.Def captured = t;
                MakePrimaryButton(card.transform, "Claim", "AL", ModernUIKit.Mint, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(170f, 60f), () =>
                {
                    if (GameTasks.Claim(captured))
                    {
                        RefreshCoins();
                        RebuildTasks();
                    }
                }, new Vector2(1f, 0.5f));
            }
            else
            {
                TextMeshProUGUI st = ModernUIKit.Label(card.transform, "State", claimed ? "ALINDI" : "DEVAM", 16, claimed ? ModernUIKit.Mint : ModernUIKit.Dim, ModernUIKit.FontRole.Title, TextAlignmentOptions.Center);
                st.fontStyle = FontStyles.Bold;
                st.characterSpacing = 4f;
                ModernUIKit.Anchor(st.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(170f, 30f));
            }
        }
    }

    // =====================================================================
    // Profil
    // =====================================================================

    TMP_InputField profileNameField;

    void BuildProfile()
    {
        profilePanel = MakeSubPanel("ProfilePanel", "PROFİL", "İstatistiklerin ve blader adın.", ModernUIKit.Magenta, out _);
        GameObject c = new GameObject("Content");
        c.transform.SetParent(profilePanel.transform, false);
        RectTransform rt = c.AddComponent<RectTransform>();
        ModernUIKit.Stretch(rt, 0, 0, 170, 0);
        profileContent = rt;

        // İsim değiştir
        TextMeshProUGUI cap = ModernUIKit.Label(profilePanel.transform, "NameCap", "BLADER ADI", 16, ModernUIKit.Dim, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        cap.fontStyle = FontStyles.Bold;
        cap.characterSpacing = 8f;
        ModernUIKit.Anchor(cap.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(-480f, 200f), new Vector2(400f, 22f));
        profileNameField = MakeInputField(profilePanel.transform, "NameField", "İsmini yaz...", Vector2.zero, new Vector2(560f, 64f));
        ModernUIKit.Anchor((RectTransform)profileNameField.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(-480f, 120f), new Vector2(560f, 64f));
        MakePrimaryButton(profilePanel.transform, "SaveName", "KAYDET", ModernUIKit.Magenta, new Vector2(0.5f, 0f), new Vector2(110f, 120f), new Vector2(260f, 64f), () =>
        {
            if (PlayerDataManager.Instance != null && !string.IsNullOrWhiteSpace(profileNameField.text))
            {
                PlayerDataManager.Instance.SetPlayerName(profileNameField.text);
                RebuildProfile();
            }
        }, new Vector2(0f, 0f));
    }

    void RebuildProfile()
    {
        Clear(profileContent);
        PlayerSaveData d = Data;
        if (d == null) return;
        if (profileNameField != null) profileNameField.text = d.playerName;

        int decided = d.totalWins + d.totalLosses;
        string winRate = decided > 0 ? Mathf.RoundToInt(100f * d.totalWins / decided) + "%" : "-";
        string[] titles = { "MAÇ", "GALİBİYET", "MAĞLUBİYET", "KAZANMA ORANI", "ARENA DIŞI", "MÜKEMMEL FIRLATIŞ", "KARİYER SEVİYESİ", "RÜTBE" };
        string[] values = { d.totalMatches.ToString(), d.totalWins.ToString(), d.totalLosses.ToString(), winRate,
                            d.ringOutWins.ToString(), d.perfectLaunches.ToString(), d.currentPathingLevel.ToString(),
                            PlayerDataManager.RankFor(d.totalWins).ToUpper() };
        Color[] cols = { Color.white, ModernUIKit.Mint, ModernUIKit.Danger, ModernUIKit.Cyan, ModernUIKit.Heat, ModernUIKit.Gold, ModernUIKit.Gold, ModernUIKit.Magenta };

        for (int i = 0; i < titles.Length; i++)
        {
            int col = i % 4, row = i / 4;
            Image tile = ModernUIKit.MakeImage(profileContent, "Stat" + i, PanelSoft, ModernUIKit.SharpCardSprite);
            tile.raycastTarget = false;
            ModernUIKit.Anchor(tile.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-480f + col * 320f, -40f - row * 200f), new Vector2(290f, 170f));
            Image bar = ModernUIKit.MakeImage(tile.transform, "Bar", cols[i], null, false);
            bar.sprite = null;
            bar.raycastTarget = false;
            ModernUIKit.Anchor(bar.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 4f));
            TextMeshProUGUI v = ModernUIKit.Label(tile.transform, "V", values[i], 54, cols[i], ModernUIKit.FontRole.Display);
            v.fontStyle = FontStyles.Bold;
            ModernUIKit.Anchor(v.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270f, 64f));
            TextMeshProUGUI t = ModernUIKit.Label(tile.transform, "T", titles[i], 14, ModernUIKit.Dim, ModernUIKit.FontRole.Title);
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 4f;
            ModernUIKit.Anchor(t.rectTransform, new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.2f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270f, 22f));
        }
    }

    // =====================================================================
    // Ayarlar
    // =====================================================================

    void BuildSettings()
    {
        settingsPanel = MakeSubPanel("SettingsPanel", "AYARLAR", "Ses, grafik ve kayıt.", ModernUIKit.Dim, out _);
        Transform p = settingsPanel.transform;

        GameObject box = new GameObject("Box");
        box.transform.SetParent(p, false);
        RectTransform b = box.AddComponent<RectTransform>();
        ModernUIKit.Anchor(b, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(900f, 560f));
        Image bg = ModernUIKit.MakeImage(b, "Bg", PanelSoft, ModernUIKit.SharpCardSprite);
        bg.raycastTarget = false;
        ModernUIKit.Stretch(bg.rectTransform);

        // Ses
        SettingRowLabel(b, "SES", 0);
        Slider vol = MakeSlider(b, new Vector2(300f, -60f), new Vector2(440f, 30f));
        vol.value = Data != null ? Data.masterVolume : 1f;
        settingsVolumeValue = ModernUIKit.Label(b, "VolVal", "", 20, Color.white, ModernUIKit.FontRole.Title, TextAlignmentOptions.Right);
        settingsVolumeValue.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(settingsVolumeValue.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-40f, -60f), new Vector2(100f, 30f));
        settingsVolumeValue.text = Mathf.RoundToInt(vol.value * 100f) + "%";
        vol.onValueChanged.AddListener(v =>
        {
            if (Data == null) return;
            Data.masterVolume = v;
            PlayerDataManager.Instance.ApplySettings();
            PlayerDataManager.Instance.SaveData();
            settingsVolumeValue.text = Mathf.RoundToInt(v * 100f) + "%";
        });

        // Grafik
        SettingRowLabel(b, "GRAFİK", 1);
        string[] q = { "DÜŞÜK", "ORTA", "YÜKSEK" };
        qualityButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            qualityButtons[i] = ModernUIKit.MakeButton(b, "Q" + i, q[i], ModernUIKit.Cyan, new Vector2(140f, 52f), () => SetQuality(idx));
            ModernUIKit.Anchor(qualityButtons[i].GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(300f + i * 150f, -170f), new Vector2(140f, 52f));
        }
        RefreshQualityButtons();

        // Kayıt sıfırla
        SettingRowLabel(b, "KAYIT", 2);
        Button reset = ModernUIKit.MakeButton(b, "Reset", "KAYDI SIFIRLA", ModernUIKit.Danger, new Vector2(300f, 56f), ResetSave);
        ModernUIKit.Anchor(reset.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(300f, -280f), new Vector2(300f, 56f));
        resetLabel = reset.GetComponentInChildren<TextMeshProUGUI>();

        // Çıkış
        SettingRowLabel(b, "OYUN", 3);
        Button quit = ModernUIKit.MakeButton(b, "Quit", "OYUNDAN ÇIK", ModernUIKit.Dim, new Vector2(300f, 56f), QuitGame);
        ModernUIKit.Anchor(quit.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(300f, -390f), new Vector2(300f, 56f));

        TextMeshProUGUI ver = ModernUIKit.Label(b, "Ver", GameBrand.TitleTop + " " + GameBrand.TitleBottom + "  " + GameBrand.Version, 14, ModernUIKit.Dim, ModernUIKit.FontRole.Caption, TextAlignmentOptions.Right);
        ModernUIKit.Anchor(ver.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 20f), new Vector2(400f, 20f));
    }

    void SettingRowLabel(Transform parent, string text, int row)
    {
        TextMeshProUGUI l = ModernUIKit.Label(parent, "Row" + row, text, 22, Color.white, ModernUIKit.FontRole.Display, TextAlignmentOptions.Left);
        l.fontStyle = FontStyles.Bold;
        l.characterSpacing = 6f;
        ModernUIKit.Anchor(l.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(50f, -60f - row * 110f), new Vector2(220f, 30f));
    }

    void SetQuality(int idx)
    {
        int count = QualitySettings.names.Length;
        if (count == 0 || Data == null) return;
        int level = idx == 0 ? 0 : (idx == 1 ? count / 2 : count - 1);
        Data.qualityLevel = level;
        PlayerDataManager.Instance.ApplySettings();
        PlayerDataManager.Instance.SaveData();
        RefreshQualityButtons();
    }

    void RefreshQualityButtons()
    {
        if (qualityButtons == null) return;
        int count = Mathf.Max(1, QualitySettings.names.Length);
        int cur = QualitySettings.GetQualityLevel();
        int sel = cur <= 0 ? 0 : (cur >= count - 1 ? 2 : 1);
        for (int i = 0; i < qualityButtons.Length; i++)
        {
            Image img = qualityButtons[i].GetComponent<Image>();
            img.color = ModernUIKit.WithAlpha(ModernUIKit.Cyan, i == sel ? 0.55f : 0.12f);
        }
    }

    void ResetSave()
    {
        // İki adımlı onay
        if (Time.unscaledTime > resetArmedUntil)
        {
            resetArmedUntil = Time.unscaledTime + 3f;
            if (resetLabel != null) resetLabel.text = "EMİN MİSİN? TEKRAR BAS";
            return;
        }
        resetArmedUntil = 0f;
        if (PlayerDataManager.Instance != null) PlayerDataManager.Instance.ResetAll();
        MatchContext.Clear();
        cameFromBattle = false;
        RebuildUI();
        ShowLogin();
    }

    // =====================================================================
    // Panel geçişleri
    // =====================================================================

    public void ShowLogin()
    {
        HideAllPanels();
        loginPanel.SetActive(true);
        bool hasProfile = Data != null && Data.hasProfile;
        nameEntry.SetActive(!hasProfile);
        welcomeGroup.SetActive(hasProfile);
        if (hasProfile) welcomeLabel.text = "HOŞ GELDİN, <b>" + Data.playerName.ToUpper() + "</b>";
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        mainMenuPanel.SetActive(true);
        RefreshMain();
    }

    public void ShowShop()
    {
        HideAllPanels();
        shopPanel.SetActive(true);
        RefreshCoins();
        if (shopUI != null) shopUI.BuildCards();
    }

    public void ShowCustomization() { HideAllPanels(); customizationPanel.SetActive(true); RefreshCoins(); }
    public void ShowTasks() { HideAllPanels(); tasksPanel.SetActive(true); RefreshCoins(); RebuildTasks(); }
    public void ShowPathing() { HideAllPanels(); pathingPanel.SetActive(true); RefreshCoins(); RebuildPathing(); }
    public void ShowProfile() { HideAllPanels(); profilePanel.SetActive(true); RefreshCoins(); RebuildProfile(); }
    public void ShowSettings() { HideAllPanels(); settingsPanel.SetActive(true); RefreshCoins(); RefreshQualityButtons(); if (resetLabel != null) resetLabel.text = "KAYDI SIFIRLA"; }

    void HideAllPanels()
    {
        GameObject[] all = { loginPanel, mainMenuPanel, shopPanel, customizationPanel, tasksPanel, pathingPanel, profilePanel, settingsPanel };
        foreach (GameObject g in all) if (g != null) g.SetActive(false);
    }

    public void StartBattle()
    {
        MatchContext.SetupQuickMatch();
        SceneManager.LoadScene("SampleScene");
    }

    public void StartPathingMatch()
    {
        int level = Data != null ? Data.currentPathingLevel : 1;
        StartPathingMatch(level);
    }

    public void StartPathingMatch(int level)
    {
        if (GetComponent<PathingManager>() == null) gameObject.AddComponent<PathingManager>();
        GetComponent<PathingManager>().StartMatchAtLevel(level);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Update()
    {
        float t = Time.unscaledTime;
        if (bgRings != null)
        {
            for (int i = 0; i < bgRings.Length; i++)
                if (bgRings[i] != null) bgRings[i].localEulerAngles = new Vector3(0f, 0f, t * (i % 2 == 0 ? 6f : -9f) * (i + 1));
        }
        if (bgStreak != null)
            bgStreak.anchoredPosition = new Vector2(-200f + Mathf.Sin(t * 0.15f) * 260f, 0f);

        if (touchPulse != null && touchPulse.gameObject.activeInHierarchy)
            touchPulse.color = new Color(1f, 1f, 1f, 0.35f + 0.45f * (0.5f + 0.5f * Mathf.Sin(t * 2.6f)));

        if (mainMenuPanel != null && mainMenuPanel.activeSelf)
        {
            if (battleGlow != null)
                battleGlow.color = ModernUIKit.WithAlpha(ModernUIKit.Battle, 0.22f + 0.14f * Mathf.Sin(t * 2.6f));
            if (battleCard != null)
                battleCard.localScale = Vector3.one * (1f + 0.018f * Mathf.Sin(t * 2.6f));
        }

        if (resetLabel != null && resetArmedUntil > 0f && t > resetArmedUntil)
        {
            resetArmedUntil = 0f;
            resetLabel.text = "KAYDI SIFIRLA";
        }

        // Giriş ekranında Enter = devam
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && loginPanel != null && loginPanel.activeSelf && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
        {
            if (nameEntry.activeSelf) ConfirmName();
            else ShowMainMenu();
        }
    }

    // =====================================================================
    // Yardımcılar
    // =====================================================================

    GameObject NewLayer(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        ModernUIKit.Stretch(rt);
        return go;
    }

    static void Clear(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }

    /// <summary>Alt ekran: koyu zemin + geri butonu + başlık + alt başlık + beycoin.</summary>
    GameObject MakeSubPanel(string name, string title, string sub, Color accent, out TextMeshProUGUI coins)
    {
        GameObject panel = NewLayer(name);
        Image dim = panel.AddComponent<Image>();
        dim.color = new Color(0.01f, 0.015f, 0.035f, 0.86f);

        Button back = MakeBackButton(panel.transform);

        TextMeshProUGUI t = ModernUIKit.Label(panel.transform, "Title", title, 52, Color.white, ModernUIKit.FontRole.Display, TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold | FontStyles.Italic;
        t.characterSpacing = 8f;
        ModernUIKit.Anchor(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(200f, -28f), new Vector2(900f, 64f));

        Image line = ModernUIKit.MakeSkewPanel(panel.transform, "Accent", accent);
        ModernUIKit.Anchor(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(204f, -96f), new Vector2(120f, 6f));

        TextMeshProUGUI s = ModernUIKit.Label(panel.transform, "Sub", sub, 18, ModernUIKit.Dim, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        s.characterSpacing = 1.5f;
        ModernUIKit.Anchor(s.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(204f, -112f), new Vector2(1200f, 26f));

        coins = MakeCoinPill(panel.transform, "Coins", new Vector2(1f, 1f), new Vector2(-28f, -30f));
        var list = new System.Collections.Generic.List<TextMeshProUGUI>(subCoinLabels) { coins };
        subCoinLabels = list.ToArray();

        panel.SetActive(false);
        return panel;
    }

    Button MakeBackButton(Transform parent)
    {
        Image img = ModernUIKit.MakeSkewPanel(parent, "Back", Panel);
        img.raycastTarget = true;
        ModernUIKit.Anchor(img.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -34f), new Vector2(150f, 60f));
        Outline ol = img.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 1f, 1f, 0.15f);
        ol.effectDistance = new Vector2(1f, -1f);

        Image arrow = ModernUIKit.MakeImage(img.transform, "Arrow", Color.white, ModernUIKit.ChevronSprite, false);
        arrow.raycastTarget = false;
        ModernUIKit.Anchor(arrow.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(36f, 0f), new Vector2(22f, 18f));
        arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -90f);

        TextMeshProUGUI l = ModernUIKit.Label(img.transform, "L", "GERİ", 20, Color.white, ModernUIKit.FontRole.Display);
        l.fontStyle = FontStyles.Bold;
        l.characterSpacing = 4f;
        ModernUIKit.Anchor(l.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(14f, 0f), Vector2.zero);

        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(ShowMainMenu);
        ModernUIKit.AddPressFeedback(b);
        return b;
    }

    TextMeshProUGUI MakeCoinPill(Transform parent, string name, Vector2 anchor, Vector2 pos)
    {
        Image pill = ModernUIKit.MakeSkewPanel(parent, name, Panel);
        ModernUIKit.Anchor(pill.rectTransform, anchor, anchor, new Vector2(1f, 1f), pos, new Vector2(220f, 56f));
        Image coin = ModernUIKit.MakeImage(pill.transform, "Coin", ModernUIKit.Gold, ModernUIKit.DiscSprite, false);
        coin.raycastTarget = false;
        ModernUIKit.Anchor(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(32f, 32f));
        TextMeshProUGUI c = ModernUIKit.Label(coin.transform, "C", "C", 18, new Color(0.25f, 0.15f, 0f), ModernUIKit.FontRole.Display);
        c.fontStyle = FontStyles.Bold;
        ModernUIKit.Stretch(c.rectTransform);
        TextMeshProUGUI v = ModernUIKit.Label(pill.transform, "V", "0", 24, Color.white, ModernUIKit.FontRole.Display, TextAlignmentOptions.Right);
        v.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(v.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, 0f), new Vector2(-90f, 0f));
        return v;
    }

    Button MakeIconButton(Transform parent, string name, Vector2 anchor, Vector2 pos, UnityAction onClick)
    {
        Image img = ModernUIKit.MakeSkewPanel(parent, name, Panel);
        img.raycastTarget = true;
        ModernUIKit.Anchor(img.rectTransform, anchor, anchor, new Vector2(1f, 1f), pos, new Vector2(72f, 56f));
        // "Menü" simgesi: üç çizgi
        for (int i = 0; i < 3; i++)
        {
            Image line = ModernUIKit.MakeImage(img.transform, "L" + i, Color.white, null, false);
            line.sprite = null;
            line.raycastTarget = false;
            ModernUIKit.Anchor(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f - i * 10f), new Vector2(26f, 3f));
        }
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        ModernUIKit.AddPressFeedback(b);
        return b;
    }

    Button MakeNavButton(RectTransform nav, string label, string icon, Color accent, int index, int count, UnityAction onClick)
    {
        float w = nav.sizeDelta.x / count;
        Image img = ModernUIKit.MakeImage(nav, "Nav_" + label, new Color(1f, 1f, 1f, 0f), null, false);
        img.sprite = null;
        ModernUIKit.Anchor(img.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(index * w, 0f), new Vector2(w, 0f));

        Image dot = ModernUIKit.MakeImage(img.transform, "Icon", ModernUIKit.WithAlpha(accent, 0.18f), ModernUIKit.DiscSprite, false);
        dot.raycastTarget = false;
        ModernUIKit.Anchor(dot.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(44f, 44f));
        Image ring = ModernUIKit.MakeImage(dot.transform, "Ring", accent, ModernUIKit.ThinRingSprite, false);
        ring.raycastTarget = false;
        ModernUIKit.Stretch(ring.rectTransform);
        TextMeshProUGUI ic = ModernUIKit.Label(dot.transform, "I", icon, 20, accent, ModernUIKit.FontRole.Display);
        ic.fontStyle = FontStyles.Bold;
        ModernUIKit.Stretch(ic.rectTransform);

        TextMeshProUGUI l = ModernUIKit.Label(img.transform, "L", label, 18, Color.white, ModernUIKit.FontRole.Display, TextAlignmentOptions.Left);
        l.fontStyle = FontStyles.Bold;
        l.characterSpacing = 3f;
        ModernUIKit.Anchor(l.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(84f, 0f), new Vector2(-90f, 0f));

        if (index > 0)
        {
            Image sep = ModernUIKit.MakeImage(img.transform, "Sep", new Color(1f, 1f, 1f, 0.08f), null, false);
            sep.sprite = null;
            sep.raycastTarget = false;
            ModernUIKit.Anchor(sep.rectTransform, new Vector2(0f, 0.2f), new Vector2(0f, 0.8f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1f, 0f));
        }

        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        ColorBlock cb = b.colors;
        cb.normalColor = new Color(1f, 1f, 1f, 0f);
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.06f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.12f);
        cb.selectedColor = new Color(1f, 1f, 1f, 0f);
        b.colors = cb;
        img.color = Color.white;
        return b;
    }

    Button MakePrimaryButton(Transform parent, string name, string label, Color accent, Vector2 anchor, Vector2 pos, Vector2 size, UnityAction onClick, Vector2? pivot = null)
    {
        Image img = ModernUIKit.MakeSkewPanel(parent, name, accent);
        img.raycastTarget = true;
        ModernUIKit.Anchor(img.rectTransform, anchor, anchor, pivot ?? new Vector2(0.5f, 0f), pos, size);
        Shadow sh = img.gameObject.AddComponent<Shadow>();
        sh.effectColor = ModernUIKit.WithAlpha(accent, 0.45f);
        sh.effectDistance = new Vector2(0f, -6f);
        TextMeshProUGUI l = ModernUIKit.Label(img.transform, "L", label, Mathf.Clamp(size.y * 0.4f, 18f, 32f), new Color(0.02f, 0.04f, 0.08f), ModernUIKit.FontRole.Display);
        l.fontStyle = FontStyles.Bold | FontStyles.Italic;
        l.characterSpacing = 5f;
        ModernUIKit.Stretch(l.rectTransform);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);
        ModernUIKit.AddPressFeedback(b);
        return b;
    }

    Button MakeTextButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, UnityAction onClick)
    {
        TextMeshProUGUI l = ModernUIKit.Label(parent, name, label, 15, new Color(1f, 1f, 1f, 0.5f), ModernUIKit.FontRole.Title);
        l.characterSpacing = 6f;
        l.raycastTarget = true;
        ModernUIKit.Anchor(l.rectTransform, anchor, anchor, new Vector2(0.5f, 0f), pos, new Vector2(400f, 30f));
        Button b = l.gameObject.AddComponent<Button>();
        b.targetGraphic = l;
        b.onClick.AddListener(onClick);
        return b;
    }

    TMP_InputField MakeInputField(Transform parent, string name, string placeholder, Vector2 pos, Vector2 size)
    {
        GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        ModernUIKit.Anchor(rt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, size);

        Image bg = go.GetComponent<Image>();
        bg.sprite = ModernUIKit.SharpCardSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.04f, 0.07f, 0.14f, 0.95f);
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = ModernUIKit.WithAlpha(ModernUIKit.Cyan, 0.5f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        TMP_InputField field = go.GetComponent<TMP_InputField>();
        field.characterLimit = 16;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.caretColor = ModernUIKit.Cyan;
        field.selectionColor = ModernUIKit.WithAlpha(ModernUIKit.Cyan, 0.35f);

        TMP_FontAsset font = ModernUIKit.GetFont(ModernUIKit.FontRole.Title);
        if (field.textComponent != null)
        {
            if (font != null) field.textComponent.font = font;
            field.textComponent.fontSize = 30;
            field.textComponent.color = Color.white;
            field.textComponent.alignment = TextAlignmentOptions.Center;
        }
        TextMeshProUGUI ph = field.placeholder as TextMeshProUGUI;
        if (ph != null)
        {
            if (font != null) ph.font = font;
            ph.text = placeholder;
            ph.fontSize = 26;
            ph.color = new Color(1f, 1f, 1f, 0.3f);
            ph.alignment = TextAlignmentOptions.Center;
            ph.fontStyle = FontStyles.Italic;
        }
        return field;
    }

    Slider MakeSlider(Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        ModernUIKit.Anchor(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), pos, size);

        Transform bgT = go.transform.Find("Background");
        if (bgT != null)
        {
            Image bg = bgT.GetComponent<Image>();
            bg.sprite = null;
            bg.color = new Color(1f, 1f, 1f, 0.08f);
        }
        Transform fillT = go.transform.Find("Fill Area/Fill");
        if (fillT != null)
        {
            Image fill = fillT.GetComponent<Image>();
            fill.sprite = null;
            fill.color = ModernUIKit.Cyan;
        }
        Transform handleT = go.transform.Find("Handle Slide Area/Handle");
        if (handleT != null)
        {
            Image h = handleT.GetComponent<Image>();
            h.sprite = ModernUIKit.DiscSprite;
            h.color = Color.white;
            ((RectTransform)handleT).sizeDelta = new Vector2(30f, 0f);
        }
        return go.GetComponent<Slider>();
    }
}
