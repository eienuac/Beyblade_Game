using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Paneller")]
    public GameObject mainMenuPanel;
    public GameObject customizationPanel;
    public GameObject tasksPanel;
    public GameObject pathingPanel;
    public PartDatabase partDatabase;

    private Image orbA;
    private Image orbB;
    private TextMeshProUGUI titlePulse;

    private void Start()
    {
        ModernUIKit.EnsureEventSystem();
        EnsurePlayerData();
        ResolvePartDatabase();
        RebuildUI();
        ShowMainMenu();
    }

    private void EnsurePlayerData()
    {
        if (PlayerDataManager.Instance == null)
        {
            GameObject go = new GameObject("PlayerDataManager");
            go.AddComponent<PlayerDataManager>();
        }
    }

    private void ResolvePartDatabase()
    {
        if (partDatabase != null) return;
        PartDatabase[] found = Resources.FindObjectsOfTypeAll<PartDatabase>();
        if (found != null && found.Length > 0) partDatabase = found[0];
#if UNITY_EDITOR
        if (partDatabase == null)
            partDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<PartDatabase>("Assets/GlobalPartDatabase.asset");
#endif
    }

    private void RebuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = ModernUIKit.CreateCanvas("MainCanvas", 10);
        Transform root = canvas.transform;
        if (root != transform && GetComponent<Canvas>() == null)
            transform.SetParent(root, false);

        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyImmediate(root.GetChild(i).gameObject);

        Image bg = ModernUIKit.MakeImage(root, "BG", ModernUIKit.Bg, null, false);
        ModernUIKit.Stretch(bg.rectTransform);
        bg.sprite = null;
        bg.raycastTarget = false;

        orbA = ModernUIKit.MakeImage(root, "OrbA", new Color(0.15f, 0.75f, 1f, 0.16f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(orbA.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(280, -180), new Vector2(720, 720));
        orbA.raycastTarget = false;

        orbB = ModernUIKit.MakeImage(root, "OrbB", new Color(1f, 0.2f, 0.55f, 0.12f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(orbB.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-220, 160), new Vector2(640, 640));
        orbB.raycastTarget = false;

        BuildMainPanel(root);
        BuildCustomization(root);
        BuildTasks(root);
        BuildPathing(root);
    }

    private void BuildMainPanel(Transform root)
    {
        mainMenuPanel = ModernUIKit.MakeImage(root, "MainMenuPanel", Color.clear, null, false).gameObject;
        ModernUIKit.Stretch(mainMenuPanel.GetComponent<RectTransform>());
        mainMenuPanel.GetComponent<Image>().raycastTarget = false;

        TextMeshProUGUI kicker = ModernUIKit.Label(mainMenuPanel.transform, "Kicker", "BEYBLADE X", 20, ModernUIKit.Cyan, TextAlignmentOptions.Left);
        kicker.characterSpacing = 16f;
        kicker.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(kicker.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110, -70), new Vector2(600, 36));

        titlePulse = ModernUIKit.Label(mainMenuPanel.transform, "Title", "CLASH", 118, Color.white, TextAlignmentOptions.Left);
        titlePulse.fontStyle = FontStyles.Bold;
        titlePulse.characterSpacing = 8f;
        ModernUIKit.Anchor(titlePulse.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(100, -100), new Vector2(900, 150));

        TextMeshProUGUI sub = ModernUIKit.Label(mainMenuPanel.transform, "Sub", "Modern arena. Keskin dönüş. Let it rip.", 24, ModernUIKit.Dim, TextAlignmentOptions.Left);
        ModernUIKit.Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110, -250), new Vector2(700, 40));

        Image line = ModernUIKit.MakeImage(mainMenuPanel.transform, "Line", ModernUIKit.Cyan, null, false);
        ModernUIKit.Anchor(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110, -300), new Vector2(88, 3));
        line.sprite = null;

        CreateMenuRow(mainMenuPanel.transform, "Savaş", "Hemen arenaya gir", ModernUIKit.Cyan, -360, StartBattle);
        CreateMenuRow(mainMenuPanel.transform, "Özelleştir", "Layer / disk / tip", new Color(0.55f, 0.45f, 1f), -470, ShowCustomization);
        CreateMenuRow(mainMenuPanel.transform, "Görevler", "Parça ve ödül kovala", ModernUIKit.Gold, -580, ShowTasks);
        CreateMenuRow(mainMenuPanel.transform, "Kariyer", "Botlara karşı yüksel", ModernUIKit.Mint, -690, ShowPathing);
        CreateMenuRow(mainMenuPanel.transform, "Çıkış", "Oyunu kapat", ModernUIKit.Dim, -800, QuitGame);

        TextMeshProUGUI hint = ModernUIKit.Label(mainMenuPanel.transform, "Hint", "LET IT RIP", 16, new Color(1f, 1f, 1f, 0.28f), TextAlignmentOptions.Right);
        hint.characterSpacing = 12f;
        ModernUIKit.Anchor(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-70, 40), new Vector2(400, 30));
    }

    private void CreateMenuRow(Transform parent, string title, string desc, Color accent, float y, UnityEngine.Events.UnityAction action)
    {
        Image row = ModernUIKit.MakeImage(parent, "Row_" + title, new Color(1f, 1f, 1f, 0.045f));
        ModernUIKit.Anchor(row.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(100, y), new Vector2(560, 92));

        Image accentBar = ModernUIKit.MakeImage(row.transform, "Accent", accent, null, false);
        ModernUIKit.Anchor(accentBar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6, 0));
        accentBar.sprite = null;

        TextMeshProUGUI t = ModernUIKit.Label(row.transform, "Title", title.ToUpper(), 30, Color.white, TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 3f;
        ModernUIKit.Anchor(t.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(28, -8), new Vector2(-80, -12));

        TextMeshProUGUI d = ModernUIKit.Label(row.transform, "Desc", desc, 18, ModernUIKit.Dim, TextAlignmentOptions.Left);
        ModernUIKit.Anchor(d.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(28, 10), new Vector2(-80, -8));

        TextMeshProUGUI arrow = ModernUIKit.Label(row.transform, "Arrow", "›", 48, accent, TextAlignmentOptions.Right);
        ModernUIKit.Anchor(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-22, 0), new Vector2(50, 0));

        Button btn = row.gameObject.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        cb.fadeDuration = 0.1f;
        btn.colors = cb;
        btn.targetGraphic = row;
        btn.onClick.AddListener(action);
    }

    private void BuildCustomization(Transform root)
    {
        customizationPanel = MakeSubPanel(root, "CustomizationPanel", "ÖZELLEŞTİR", "Parçalarını seç, dengenı kur.");

        GameObject statCard = ModernUIKit.MakeImage(customizationPanel.transform, "StatCard", ModernUIKit.Glass).gameObject;
        ModernUIKit.Anchor(statCard.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(80, -20), new Vector2(520, 560));

        TextMeshProUGUI st = ModernUIKit.Label(statCard.transform, "StatTitle", "STATS", 26, Color.white, TextAlignmentOptions.Left);
        st.fontStyle = FontStyles.Bold;
        st.characterSpacing = 8f;
        ModernUIKit.Anchor(st.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(28, -20), new Vector2(-40, 40));

        CustomizationUI customUI = customizationPanel.AddComponent<CustomizationUI>();
        customUI.partDatabase = partDatabase;
        customUI.attackBar = MakeStat(statCard.transform, "ATTACK", ModernUIKit.Magenta, 0);
        customUI.defenseBar = MakeStat(statCard.transform, "DEFENSE", ModernUIKit.Cyan, 1);
        customUI.staminaBar = MakeStat(statCard.transform, "STAMINA", ModernUIKit.Mint, 2);
        customUI.weightBar = MakeStat(statCard.transform, "WEIGHT", ModernUIKit.Gold, 3);

        GameObject partCard = ModernUIKit.MakeImage(customizationPanel.transform, "PartCard", ModernUIKit.Glass).gameObject;
        ModernUIKit.Anchor(partCard.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-80, -20), new Vector2(560, 720));

        customUI.layerListContainer = MakeSlot(partCard.transform, "LAYER", 0);
        customUI.diskListContainer = MakeSlot(partCard.transform, "DISK", 1);
        customUI.tipListContainer = MakeSlot(partCard.transform, "TIP", 2);

        GameObject prefab = ModernUIKit.MakeButton(customizationPanel.transform, "PartBtnPrefab", "Parça", ModernUIKit.Glass, new Vector2(480, 52), () => { }).gameObject;
        prefab.SetActive(false);
        customUI.partButtonPrefab = prefab;
    }

    private Image MakeStat(Transform parent, string label, Color col, int index)
    {
        float y = -80 - index * 110;
        TextMeshProUGUI lbl = ModernUIKit.Label(parent, "Lbl_" + label, label, 18, ModernUIKit.Dim, TextAlignmentOptions.Left);
        lbl.characterSpacing = 4f;
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

    private Transform MakeSlot(Transform parent, string title, int index)
    {
        float y = -24 - index * 220;
        TextMeshProUGUI lbl = ModernUIKit.Label(parent, "Lbl_" + title, title, 22, ModernUIKit.Cyan, TextAlignmentOptions.Left);
        lbl.fontStyle = FontStyles.Bold;
        lbl.characterSpacing = 6f;
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

    private void BuildTasks(Transform root)
    {
        tasksPanel = MakeSubPanel(root, "TasksPanel", "GÖREVLER", "Tamamla, parça kazan.");
        MakeInfoCard(tasksPanel.transform, 0, "3 rakip yen", "Ödül · Layer", ModernUIKit.Gold);
        MakeInfoCard(tasksPanel.transform, 1, "1000 hasar ver", "Ödül · Disk", ModernUIKit.Cyan);
        MakeInfoCard(tasksPanel.transform, 2, "5 maç kazan", "Ödül · Tip", ModernUIKit.Mint);
    }

    private void BuildPathing(Transform root)
    {
        pathingPanel = MakeSubPanel(root, "PathingPanel", "KARİYER", "Seviye atla, botları geç.");
        int level = PlayerDataManager.Instance != null ? PlayerDataManager.Instance.data.currentPathingLevel : 1;
        for (int i = 0; i < 5; i++)
        {
            bool open = (i + 1) <= level;
            Color col = open ? ModernUIKit.Mint : ModernUIKit.Dim;
            string status = open ? (i + 1 == level ? "ŞİMDİ" : "AÇIK") : "KİLİT";
            Image card = ModernUIKit.MakeImage(pathingPanel.transform, "Path_" + i, ModernUIKit.Glass);
            ModernUIKit.Anchor(card.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), new Vector2(-480 + i * 240, -20), new Vector2(210, 280));

            TextMeshProUGUI t = ModernUIKit.Label(card.transform, "T", "LV " + (i + 1), 28, Color.white);
            t.fontStyle = FontStyles.Bold;
            ModernUIKit.Anchor(t.rectTransform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 40));

            TextMeshProUGUI s = ModernUIKit.Label(card.transform, "S", status, 18, col);
            s.characterSpacing = 4f;
            ModernUIKit.Anchor(s.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 30));

            if (open)
            {
                Button btn = card.gameObject.AddComponent<Button>();
                btn.targetGraphic = card;
                btn.onClick.AddListener(StartPathingMatch);
            }
        }
    }

    private void MakeInfoCard(Transform parent, int index, string title, string reward, Color accent)
    {
        Image card = ModernUIKit.MakeImage(parent, "Task_" + index, ModernUIKit.Glass);
        ModernUIKit.Anchor(card.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -200 - index * 140), new Vector2(760, 112));

        Image bar = ModernUIKit.MakeImage(card.transform, "Bar", accent, null, false);
        ModernUIKit.Anchor(bar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6, 0));
        bar.sprite = null;

        TextMeshProUGUI t = ModernUIKit.Label(card.transform, "T", title, 28, Color.white, TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(t.rectTransform, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(28, 0), new Vector2(-40, -10));

        TextMeshProUGUI r = ModernUIKit.Label(card.transform, "R", reward, 18, accent, TextAlignmentOptions.Left);
        ModernUIKit.Anchor(r.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28, 8), new Vector2(-40, -8));
    }

    private GameObject MakeSubPanel(Transform root, string name, string title, string sub)
    {
        GameObject panel = ModernUIKit.MakeImage(root, name, new Color(0.03f, 0.035f, 0.06f, 0.97f), null, false).gameObject;
        ModernUIKit.Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().sprite = null;

        TextMeshProUGUI t = ModernUIKit.Label(panel.transform, "Title", title, 48, Color.white, TextAlignmentOptions.Center);
        t.fontStyle = FontStyles.Bold;
        t.characterSpacing = 8f;
        ModernUIKit.Anchor(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(800, 70));

        TextMeshProUGUI s = ModernUIKit.Label(panel.transform, "Sub", sub, 20, ModernUIKit.Dim, TextAlignmentOptions.Center);
        ModernUIKit.Anchor(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(800, 36));

        Button back = ModernUIKit.MakeButton(panel.transform, "Btn_Back", "‹  GERİ", ModernUIKit.Dim, new Vector2(180, 56), ShowMainMenu);
        ModernUIKit.Anchor(back.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40, -28), new Vector2(180, 56));
        return panel;
    }

    private void Update()
    {
        if (orbA != null)
        {
            float t = Time.unscaledTime;
            orbA.rectTransform.anchoredPosition = new Vector2(280 + Mathf.Sin(t * 0.35f) * 40f, -180 + Mathf.Cos(t * 0.28f) * 30f);
            orbB.rectTransform.anchoredPosition = new Vector2(-220 + Mathf.Cos(t * 0.22f) * 50f, 160 + Mathf.Sin(t * 0.3f) * 36f);
            Color ca = orbA.color; ca.a = 0.12f + 0.05f * Mathf.Sin(t * 0.8f); orbA.color = ca;
        }
        if (titlePulse != null)
        {
            float g = 0.92f + 0.08f * Mathf.Sin(Time.unscaledTime * 1.4f);
            titlePulse.color = new Color(g, g, 1f, 1f);
        }
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void ShowCustomization()
    {
        HideAllPanels();
        if (customizationPanel != null) customizationPanel.SetActive(true);
    }

    public void ShowTasks()
    {
        HideAllPanels();
        if (tasksPanel != null) tasksPanel.SetActive(true);
    }

    public void ShowPathing()
    {
        HideAllPanels();
        if (pathingPanel != null) pathingPanel.SetActive(true);
    }

    private void HideAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
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
