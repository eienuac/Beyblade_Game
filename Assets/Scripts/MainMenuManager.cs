using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// QuadStrike referans layout: Title → Main (ARA/SAVAŞ/BEYLOCKER) → Shop.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public GameObject loginPanel;
    public GameObject mainMenuPanel;
    public GameObject shopPanel;
    public GameObject customizationPanel;
    public GameObject tasksPanel;
    public GameObject pathingPanel;
    public PartDatabase partDatabase;

    TextMeshProUGUI touchPulse;
    TextMeshProUGUI coinHud;
    TextMeshProUGUI profileName;
    TextMeshProUGUI profileRank;
    RectTransform battlePulse;
    ShopUI shopUI;
    Image arenaRing;

    void Start()
    {
        ModernUIKit.EnsureEventSystem();
        ModernUIKit.EnsureFonts();
        EnsurePlayerData();
        ResolvePartDatabase();
        RebuildUI();
        ShowLogin();
    }

    void EnsurePlayerData()
    {
        if (PlayerDataManager.Instance == null)
            new GameObject("PlayerDataManager").AddComponent<PlayerDataManager>();
    }

    void ResolvePartDatabase()
    {
        if (partDatabase != null) return;
        PartDatabase[] found = Resources.FindObjectsOfTypeAll<PartDatabase>();
        if (found != null && found.Length > 0) partDatabase = found[0];
#if UNITY_EDITOR
        if (partDatabase == null)
            partDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<PartDatabase>("Assets/GlobalPartDatabase.asset");
#endif
    }

    void RebuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = ModernUIKit.CreateCanvas("MainCanvas", 10);
        Transform root = canvas.transform;
        if (root != transform && GetComponent<Canvas>() == null)
            transform.SetParent(root, false);

        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyImmediate(root.GetChild(i).gameObject);

        Image bg = ModernUIKit.MakeImage(root, "BG", ModernUIKit.BgDeep, null, false);
        ModernUIKit.Stretch(bg.rectTransform);
        bg.sprite = null;
        bg.raycastTarget = false;

        Image gradTop = ModernUIKit.MakeImage(root, "GradTop", new Color(0.04f, 0.10f, 0.22f, 0.55f), ModernUIKit.VerticalGradientSprite, false);
        ModernUIKit.Stretch(gradTop.rectTransform);
        gradTop.raycastTarget = false;
        gradTop.rectTransform.localScale = new Vector3(1f, -1f, 1f);

        ModernUIKit.MakeArenaBackdrop(root);
        arenaRing = root.Find("ArenaBlueRing") != null ? root.Find("ArenaBlueRing").GetComponent<Image>() : null;

        BuildLogin(root);
        BuildMainPanel(root);
        BuildShop(root);
        BuildCustomization(root);
        BuildTasks(root);
        BuildPathing(root);
    }

    // ========== TITLE (referans 1) ==========
    void BuildLogin(Transform root)
    {
        loginPanel = ModernUIKit.MakeImage(root, "LoginPanel", Color.clear, null, false).gameObject;
        ModernUIKit.Stretch(loginPanel.GetComponent<RectTransform>());
        loginPanel.GetComponent<Image>().raycastTarget = false;

        RectTransform logo = ModernUIKit.MakeQuadStrikeLogo(loginPanel.transform, 1.05f);
        ModernUIKit.Anchor(logo, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960, 300));

        touchPulse = ModernUIKit.Label(loginPanel.transform, "Touch", "BAŞLAMAK İÇİN DOKUNUN", 22, new Color(1f, 1f, 1f, 0.55f), ModernUIKit.FontRole.Title);
        touchPulse.characterSpacing = 4f;
        ModernUIKit.Anchor(touchPulse.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 56), new Vector2(700, 36));

        Image tap = ModernUIKit.MakeImage(loginPanel.transform, "TapCatcher", Color.clear, null, false);
        ModernUIKit.Stretch(tap.rectTransform);
        tap.sprite = null;
        Button tapBtn = tap.gameObject.AddComponent<Button>();
        tapBtn.transition = Selectable.Transition.None;
        tapBtn.onClick.AddListener(EnterAsGuest);
        tap.transform.SetAsFirstSibling();
    }

    void EnterAsGuest()
    {
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.data.playerName == "Blader")
            PlayerDataManager.Instance.data.playerName = "PensiveDevil";
        ShowMainMenu();
    }

    // ========== MAIN (referans 2) ==========
    void BuildMainPanel(Transform root)
    {
        mainMenuPanel = ModernUIKit.MakeImage(root, "MainMenuPanel", Color.clear, null, false).gameObject;
        ModernUIKit.Stretch(mainMenuPanel.GetComponent<RectTransform>());
        mainMenuPanel.GetComponent<Image>().raycastTarget = false;

        // Settings hex — sol üst
        Button settings = ModernUIKit.MakeHexButton(mainMenuPanel.transform, "Settings", "", "SET", ModernUIKit.Cyan, new Vector2(72, 72), () => { });
        ModernUIKit.Anchor(settings.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28, -24), new Vector2(72, 72));

        BuildTopHud(mainMenuPanel.transform);

        // Üst logo (küçük)
        RectTransform logo = ModernUIKit.MakeQuadStrikeLogo(mainMenuPanel.transform, 0.55f);
        ModernUIKit.Anchor(logo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(520, 160));

        // ARA | SAVAŞ | BEYLOCKER
        Button ara = ModernUIKit.MakeSideNavButton(mainMenuPanel.transform, "BtnAra", "ARA", "QR", true, ShowTasks);
        ModernUIKit.Anchor(ara.GetComponent<RectTransform>(), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f), new Vector2(-300, 0), new Vector2(220, 170));

        Button battle = ModernUIKit.MakeHexButton(mainMenuPanel.transform, "BtnBattle", "SAVAŞ", "ATK", ModernUIKit.Battle, new Vector2(280, 280), StartBattle, true);
        ModernUIKit.Anchor(battle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 280));
        battlePulse = battle.GetComponent<RectTransform>();

        Button locker = ModernUIKit.MakeSideNavButton(mainMenuPanel.transform, "BtnLocker", "BEYLOCKER", "BEY", false, ShowCustomization);
        ModernUIKit.Anchor(locker.GetComponent<RectTransform>(), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f), new Vector2(300, 0), new Vector2(220, 170));

        // Alt bar: BAŞARILAR | PROFİLİM
        Image bottom = ModernUIKit.MakeImage(mainMenuPanel.transform, "BottomBar", new Color(0.06f, 0.14f, 0.28f, 0.72f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(bottom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 22), new Vector2(620, 70));
        Outline bol = bottom.gameObject.AddComponent<Outline>();
        bol.effectColor = new Color(ModernUIKit.Cyan.r, ModernUIKit.Cyan.g, ModernUIKit.Cyan.b, 0.4f);
        bol.effectDistance = new Vector2(1f, -1f);

        Button ach = ModernUIKit.MakeButton(bottom.transform, "Ach", "BAŞARILAR", ModernUIKit.Cyan, new Vector2(250, 50), ShowTasks);
        ModernUIKit.Anchor(ach.GetComponent<RectTransform>(), new Vector2(0.28f, 0.5f), new Vector2(0.28f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250, 50));

        Button profile = ModernUIKit.MakeButton(bottom.transform, "Prof", "PROFİLİM", ModernUIKit.Profile, new Vector2(250, 50), ShowCustomization);
        ModernUIKit.Anchor(profile.GetComponent<RectTransform>(), new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250, 50));
    }

    void BuildTopHud(Transform parent)
    {
        // Shop hex (yeşil sepet)
        Button shopHex = ModernUIKit.MakeHexButton(parent, "HudShop", "", "C", ModernUIKit.Battle, new Vector2(64, 64), ShowShop);
        ModernUIKit.Anchor(shopHex.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-390, -26), new Vector2(64, 64));

        // Coin
        Image coinPill = ModernUIKit.MakeImage(parent, "CoinPill", new Color(0.08f, 0.14f, 0.26f, 0.9f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(coinPill.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-250, -32), new Vector2(120, 44));
        TextMeshProUGUI coinIcon = ModernUIKit.Label(coinPill.transform, "I", "C", 18, ModernUIKit.Cyan, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        ModernUIKit.Anchor(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10, 0), new Vector2(24, 24));
        coinHud = ModernUIKit.Label(coinPill.transform, "V", "50", 22, Color.white, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        coinHud.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(coinHud.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36, 0), new Vector2(-10, 28));

        // Profile chip — sağ
        Image profileChip = ModernUIKit.MakeImage(parent, "ProfileChip", new Color(0.08f, 0.08f, 0.12f, 0.92f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(profileChip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28, -24), new Vector2(210, 58));
        Outline pol = profileChip.gameObject.AddComponent<Outline>();
        pol.effectColor = new Color(ModernUIKit.Profile.r, ModernUIKit.Profile.g, ModernUIKit.Profile.b, 0.65f);
        pol.effectDistance = new Vector2(1.5f, -1.5f);

        Image avatar = ModernUIKit.MakeImage(profileChip.transform, "Avatar", new Color(ModernUIKit.Profile.r, ModernUIKit.Profile.g, ModernUIKit.Profile.b, 0.7f), ModernUIKit.HexSprite, false);
        ModernUIKit.Anchor(avatar.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6, 0), new Vector2(48, 48));

        Image badge = ModernUIKit.MakeImage(avatar.transform, "Badge", ModernUIKit.Danger, ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(badge.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-4, 4), new Vector2(22, 22));
        TextMeshProUGUI badgeTxt = ModernUIKit.Label(badge.transform, "N", "1", 12, Color.white);
        ModernUIKit.Stretch(badgeTxt.rectTransform);

        profileName = ModernUIKit.Label(profileChip.transform, "Name", "PensiveDevil", 16, Color.white, ModernUIKit.FontRole.Title, TextAlignmentOptions.Right);
        profileName.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(profileName.rectTransform, new Vector2(0f, 0.62f), new Vector2(1f, 0.62f), new Vector2(1f, 0.5f), new Vector2(-58, 0), new Vector2(-12, 24));

        profileRank = ModernUIKit.Label(profileChip.transform, "Rank", "Newbie", 13, ModernUIKit.Dim, ModernUIKit.FontRole.Caption, TextAlignmentOptions.Right);
        ModernUIKit.Anchor(profileRank.rectTransform, new Vector2(0f, 0.28f), new Vector2(1f, 0.28f), new Vector2(1f, 0.5f), new Vector2(-58, 0), new Vector2(-12, 18));
    }

    // ========== SHOP (referans 3) ==========
    void BuildShop(Transform root)
    {
        shopPanel = ModernUIKit.MakeImage(root, "ShopPanel", new Color(0.02f, 0.04f, 0.1f, 0.94f), null, false).gameObject;
        ModernUIKit.Stretch(shopPanel.GetComponent<RectTransform>());
        shopPanel.GetComponent<Image>().sprite = null;

        Button back = ModernUIKit.MakeHexButton(shopPanel.transform, "Back", "", "<", ModernUIKit.Cyan, new Vector2(64, 64), ShowMainMenu);
        ModernUIKit.Anchor(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28, -24), new Vector2(64, 64));

        // BEYCOINS tab
        Image tab = ModernUIKit.MakeImage(shopPanel.transform, "BeycoinsTab", ModernUIKit.ShopBlue, ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(tab.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(120, -28), new Vector2(200, 48));
        TextMeshProUGUI tabLbl = ModernUIKit.Label(tab.transform, "T", "C  BEYCOINS", 18, Color.white, ModernUIKit.FontRole.Title);
        tabLbl.fontStyle = FontStyles.Bold;
        ModernUIKit.Stretch(tabLbl.rectTransform);

        TextMeshProUGUI timer = ModernUIKit.Label(shopPanel.transform, "Timer", "06 GÜN  12 SAAT  06 DAKİKA", 18, Color.white, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        ModernUIKit.Anchor(timer.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(340, -36), new Vector2(420, 32));

        // Sağ üst profil + coin
        Image goldBox = ModernUIKit.MakeImage(shopPanel.transform, "GoldBox", new Color(0.08f, 0.14f, 0.26f, 0.9f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(goldBox.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-250, -32), new Vector2(120, 44));
        TextMeshProUGUI goldLbl = ModernUIKit.Label(goldBox.transform, "G", "C  50", 20, Color.white, ModernUIKit.FontRole.Title);
        goldLbl.fontStyle = FontStyles.Bold;
        ModernUIKit.Stretch(goldLbl.rectTransform);

        Image gemBox = ModernUIKit.MakeImage(shopPanel.transform, "GemBox", Color.clear, null, false);
        gemBox.gameObject.SetActive(false);
        TextMeshProUGUI gemsLbl = ModernUIKit.Label(shopPanel.transform, "GemsHidden", "", 1, Color.clear);

        Image shopProfile = ModernUIKit.MakeImage(shopPanel.transform, "ShopProfile", new Color(0.08f, 0.08f, 0.12f, 0.92f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(shopProfile.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28, -24), new Vector2(210, 58));
        Outline spol = shopProfile.gameObject.AddComponent<Outline>();
        spol.effectColor = new Color(ModernUIKit.Profile.r, ModernUIKit.Profile.g, ModernUIKit.Profile.b, 0.65f);
        spol.effectDistance = new Vector2(1.5f, -1.5f);
        Image av = ModernUIKit.MakeImage(shopProfile.transform, "Av", new Color(ModernUIKit.Profile.r, ModernUIKit.Profile.g, ModernUIKit.Profile.b, 0.7f), ModernUIKit.HexSprite, false);
        ModernUIKit.Anchor(av.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6, 0), new Vector2(48, 48));
        TextMeshProUGUI sn = ModernUIKit.Label(shopProfile.transform, "N", "PensiveDevil", 15, Color.white, ModernUIKit.FontRole.Title, TextAlignmentOptions.Right);
        sn.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(sn.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 0.55f), new Vector2(1f, 0.5f), new Vector2(-58, 0), new Vector2(-10, 22));

        TextMeshProUGUI status = ModernUIKit.Label(shopPanel.transform, "Status", "", 16, ModernUIKit.Dim);
        ModernUIKit.Anchor(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 20), new Vector2(700, 28));

        // Panel çerçevesi
        Image frame = ModernUIKit.MakeImage(shopPanel.transform, "ShopFrame", new Color(0.04f, 0.08f, 0.16f, 0.55f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Stretch(frame.rectTransform, 48, 48, 100, 48);
        Outline fol = frame.gameObject.AddComponent<Outline>();
        fol.effectColor = new Color(0.2f, 0.7f, 1f, 0.45f);
        fol.effectDistance = new Vector2(1.5f, -1.5f);
        frame.raycastTarget = false;

        GameObject grid = new GameObject("ShopGrid");
        grid.transform.SetParent(shopPanel.transform, false);
        RectTransform gr = grid.AddComponent<RectTransform>();
        ModernUIKit.Stretch(gr, 70, 70, 120, 70);

        shopUI = shopPanel.AddComponent<ShopUI>();
        shopUI.gridRoot = grid.transform;
        shopUI.goldLabel = goldLbl;
        shopUI.gemsLabel = gemsLbl;
        shopUI.timerLabel = timer;
        shopUI.statusLabel = status;
    }

    void BuildCustomization(Transform root)
    {
        customizationPanel = MakeSubPanel(root, "CustomizationPanel", "BEYLOCKER", "Parçalarını seç, dengenı kur.");
        GameObject statCard = ModernUIKit.MakeGlassCard(customizationPanel.transform, "StatCard", new Vector2(520, 560)).gameObject;
        ModernUIKit.Anchor(statCard.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(80, -20), new Vector2(520, 560));
        TextMeshProUGUI st = ModernUIKit.Label(statCard.transform, "StatTitle", "STATS", 26, Color.white, ModernUIKit.FontRole.Display, TextAlignmentOptions.Left);
        st.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(st.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(28, -20), new Vector2(-40, 40));

        CustomizationUI customUI = customizationPanel.AddComponent<CustomizationUI>();
        customUI.partDatabase = partDatabase;
        customUI.attackBar = MakeStat(statCard.transform, "ATTACK", ModernUIKit.Magenta, 0);
        customUI.defenseBar = MakeStat(statCard.transform, "DEFENSE", ModernUIKit.Cyan, 1);
        customUI.staminaBar = MakeStat(statCard.transform, "STAMINA", ModernUIKit.Mint, 2);
        customUI.weightBar = MakeStat(statCard.transform, "WEIGHT", ModernUIKit.Gold, 3);

        GameObject partCard = ModernUIKit.MakeGlassCard(customizationPanel.transform, "PartCard", new Vector2(560, 720)).gameObject;
        ModernUIKit.Anchor(partCard.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-80, -20), new Vector2(560, 720));
        customUI.layerListContainer = MakeSlot(partCard.transform, "LAYER", 0);
        customUI.diskListContainer = MakeSlot(partCard.transform, "DISK", 1);
        customUI.tipListContainer = MakeSlot(partCard.transform, "TIP", 2);
        GameObject prefab = ModernUIKit.MakeButton(customizationPanel.transform, "PartBtnPrefab", "Parca", ModernUIKit.Glass, new Vector2(480, 52), () => { }).gameObject;
        prefab.SetActive(false);
        customUI.partButtonPrefab = prefab;
    }

    Image MakeStat(Transform parent, string label, Color col, int index)
    {
        float y = -80 - index * 110;
        TextMeshProUGUI lbl = ModernUIKit.Label(parent, "Lbl_" + label, label, 18, ModernUIKit.Dim, ModernUIKit.FontRole.Caption, TextAlignmentOptions.Left);
        ModernUIKit.Anchor(lbl.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(28, y), new Vector2(-56, 28));
        Image bg = ModernUIKit.MakeImage(parent, "Bar_" + label, new Color(1f, 1f, 1f, 0.06f));
        ModernUIKit.Anchor(bg.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, y - 34), new Vector2(-56, 18));
        Image fill = ModernUIKit.MakeImage(bg.transform, "Fill", col);
        ModernUIKit.Stretch(fill.rectTransform);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillAmount = 0.45f;
        return fill;
    }

    Transform MakeSlot(Transform parent, string title, int index)
    {
        float y = -24 - index * 220;
        TextMeshProUGUI lbl = ModernUIKit.Label(parent, "Lbl_" + title, title, 22, ModernUIKit.Cyan, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        lbl.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(lbl.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(24, y), new Vector2(-48, 32));
        GameObject box = new GameObject(title + "_Container");
        box.transform.SetParent(parent, false);
        RectTransform rt = box.AddComponent<RectTransform>();
        ModernUIKit.Anchor(rt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, y - 40), new Vector2(-48, 150));
        VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        return box.transform;
    }

    void BuildTasks(Transform root)
    {
        tasksPanel = MakeSubPanel(root, "TasksPanel", "BAŞARILAR", "Tamamla, parça kazan.");
        MakeInfoCard(tasksPanel.transform, 0, "3 rakip yen", "Ödül · Layer", ModernUIKit.Gold);
        MakeInfoCard(tasksPanel.transform, 1, "1000 hasar ver", "Ödül · Disk", ModernUIKit.Cyan);
        MakeInfoCard(tasksPanel.transform, 2, "5 maç kazan", "Ödül · Tip", ModernUIKit.Mint);
    }

    void BuildPathing(Transform root)
    {
        pathingPanel = MakeSubPanel(root, "PathingPanel", "KARİYER", "Seviye atla, botları geç.");
        int level = PlayerDataManager.Instance != null ? PlayerDataManager.Instance.data.currentPathingLevel : 1;
        for (int i = 0; i < 5; i++)
        {
            bool open = (i + 1) <= level;
            Color col = open ? ModernUIKit.Mint : ModernUIKit.Dim;
            string status = open ? (i + 1 == level ? "ŞİMDİ" : "AÇIK") : "KİLİT";
            Image card = ModernUIKit.MakeGlassCard(pathingPanel.transform, "Path_" + i, new Vector2(210, 280));
            ModernUIKit.Anchor(card.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), new Vector2(-480 + i * 240, -20), new Vector2(210, 280));
            TextMeshProUGUI t = ModernUIKit.Label(card.transform, "T", "LV " + (i + 1), 32, Color.white, ModernUIKit.FontRole.Display);
            t.fontStyle = FontStyles.Bold;
            ModernUIKit.Anchor(t.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 40));
            TextMeshProUGUI s = ModernUIKit.Label(card.transform, "S", status, 18, col, ModernUIKit.FontRole.Title);
            ModernUIKit.Anchor(s.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 30));
            if (open)
            {
                Button btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = card;
                btn.onClick.AddListener(StartPathingMatch);
            }
        }
    }

    void MakeInfoCard(Transform parent, int index, string title, string reward, Color accent)
    {
        Image card = ModernUIKit.MakeGlassCard(parent, "Task_" + index, new Vector2(760, 112));
        ModernUIKit.Anchor(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -200 - index * 140), new Vector2(760, 112));
        Image bar = ModernUIKit.MakeImage(card.transform, "Bar", accent, null, false);
        ModernUIKit.Anchor(bar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6, 0));
        bar.sprite = null;
        TextMeshProUGUI t = ModernUIKit.Label(card.transform, "T", title, 28, Color.white, ModernUIKit.FontRole.Title, TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(t.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(28, 0), new Vector2(-40, -10));
        TextMeshProUGUI r = ModernUIKit.Label(card.transform, "R", reward, 18, accent, ModernUIKit.FontRole.Caption, TextAlignmentOptions.Left);
        ModernUIKit.Anchor(r.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28, 8), new Vector2(-40, -8));
    }

    GameObject MakeSubPanel(Transform root, string name, string title, string sub)
    {
        GameObject panel = ModernUIKit.MakeImage(root, name, new Color(0.015f, 0.025f, 0.05f, 0.97f), null, false).gameObject;
        ModernUIKit.Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().sprite = null;
        TextMeshProUGUI t = ModernUIKit.DisplayLabel(panel.transform, "Title", title, 48, Color.white);
        ModernUIKit.Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(900, 70));
        TextMeshProUGUI s = ModernUIKit.Label(panel.transform, "Sub", sub, 20, ModernUIKit.Dim, ModernUIKit.FontRole.Body);
        ModernUIKit.Anchor(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(800, 36));
        Button back = ModernUIKit.MakeButton(panel.transform, "Btn_Back", "GERİ", ModernUIKit.Dim, new Vector2(140, 52), ShowMainMenu);
        ModernUIKit.Anchor(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36, -28), new Vector2(140, 52));
        return panel;
    }

    void Update()
    {
        float t = Time.unscaledTime;
        if (arenaRing != null)
            arenaRing.rectTransform.localEulerAngles = new Vector3(0f, 0f, t * 12f);
        if (touchPulse != null)
            touchPulse.color = new Color(1f, 1f, 1f, 0.3f + 0.35f * (0.5f + 0.5f * Mathf.Sin(t * 2.2f)));
        if (battlePulse != null && mainMenuPanel != null && mainMenuPanel.activeSelf)
            battlePulse.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(t * 2.5f));
    }

    void RefreshHudLabels()
    {
        if (PlayerDataManager.Instance == null) return;
        var d = PlayerDataManager.Instance.data;
        if (coinHud != null) coinHud.text = d.gold.ToString("N0");
        if (profileName != null) profileName.text = d.playerName;
        if (profileRank != null) profileRank.text = d.rankTitle;
    }

    public void ShowLogin() { HideAllPanels(); if (loginPanel != null) loginPanel.SetActive(true); }
    public void ShowMainMenu() { HideAllPanels(); if (mainMenuPanel != null) mainMenuPanel.SetActive(true); RefreshHudLabels(); }
    public void ShowShop()
    {
        HideAllPanels();
        if (shopPanel != null) shopPanel.SetActive(true);
        if (shopUI != null) { shopUI.BuildCards(); shopUI.RefreshCurrency(); }
    }
    public void ShowCustomization() { HideAllPanels(); if (customizationPanel != null) customizationPanel.SetActive(true); }
    public void ShowTasks() { HideAllPanels(); if (tasksPanel != null) tasksPanel.SetActive(true); }
    public void ShowPathing() { HideAllPanels(); if (pathingPanel != null) pathingPanel.SetActive(true); }

    void HideAllPanels()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (customizationPanel != null) customizationPanel.SetActive(false);
        if (tasksPanel != null) tasksPanel.SetActive(false);
        if (pathingPanel != null) pathingPanel.SetActive(false);
    }

    public void StartBattle()
    {
        PlayerPrefs.SetInt("IsPathingMatch", 0);
        SceneManager.LoadScene("SampleScene");
    }

    public void StartPathingMatch()
    {
        if (GetComponent<PathingManager>() == null) gameObject.AddComponent<PathingManager>();
        GetComponent<PathingManager>().StartNextMatch();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
