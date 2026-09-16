using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIBuilder : EditorWindow
{
    // RENKLER
    static readonly Color BG_DARK = new Color(0.06f, 0.06f, 0.12f, 1f);
    static readonly Color PANEL_BG = new Color(0.08f, 0.10f, 0.18f, 0.95f);
    static readonly Color CARD_BG = new Color(0.12f, 0.14f, 0.22f, 0.9f);
    static readonly Color ACCENT_BLUE = new Color(0.20f, 0.50f, 1.0f);
    static readonly Color ACCENT_ORANGE = new Color(1.0f, 0.60f, 0.15f);
    static readonly Color ACCENT_GREEN = new Color(0.15f, 0.85f, 0.45f);
    static readonly Color ACCENT_RED = new Color(1.0f, 0.25f, 0.30f);
    static readonly Color ACCENT_PURPLE = new Color(0.6f, 0.3f, 1.0f);
    static readonly Color TEXT_WHITE = new Color(0.95f, 0.95f, 0.98f);
    static readonly Color TEXT_DIM = new Color(0.55f, 0.55f, 0.65f);
    static readonly Color GLASS = new Color(1f, 1f, 1f, 0.04f);
    static readonly Color GLASS_BORDER = new Color(1f, 1f, 1f, 0.12f);

    [MenuItem("Beyblade/Otomatik Arayüz (UI) Kur")]
    public static void BuildUI()
    {
        // --- TEMİZLİK ---
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            DestroyImmediate(c.gameObject);
        foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            DestroyImmediate(es.gameObject);
        foreach (var mm in Object.FindObjectsByType<MainMenuManager>(FindObjectsSortMode.None))
            if (mm.GetComponent<Canvas>() == null) DestroyImmediate(mm.gameObject);

        // --- EVENT SYSTEM ---
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        System.Type inputModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModule != null) esObj.AddComponent(inputModule);
        else esObj.AddComponent<StandaloneInputModule>();

        // --- CANVAS ---
        GameObject canvasObj = new GameObject("MainCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // --- MENU MANAGER ---
        MainMenuManager mgr = canvasObj.AddComponent<MainMenuManager>();

        // --- ARKA PLAN ---
        MakeFullPanel("BG", canvasObj.transform, BG_DARK);

        // ===========================
        //  ANA MENÜ PANELİ
        // ===========================
        GameObject mainPanel = MakeFullPanel("MainMenuPanel", canvasObj.transform, Color.clear);
        mgr.mainMenuPanel = mainPanel;

        // Başlık
        var titleObj = MakeTMP("Title", mainPanel.transform, "BEYBLADE\nCLASH", 110, TEXT_WHITE, TextAlignmentOptions.BottomLeft);
        titleObj.fontStyle = FontStyles.Bold;
        AnchorTopLeft(titleObj.rectTransform, 120, -80, 700, 280);

        // Alt yazı
        var subObj = MakeTMP("Sub", mainPanel.transform, "CHOOSE YOUR DESTINY", 26, TEXT_DIM, TextAlignmentOptions.TopLeft);
        subObj.fontStyle = FontStyles.Italic;
        AnchorTopLeft(subObj.rectTransform, 125, -360, 500, 50);

        // Butonlar (sol tarafta dikey sıralama)
        float btnY = -440;
        float btnSpacing = 120;

        var b1 = MakeMenuButton("Btn_Customization", mainPanel.transform, "CUSTOMIZATION", "Beyblade'ini Özelleştir", ACCENT_BLUE, btnY);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(b1.onClick, mgr.ShowCustomization);

        var b2 = MakeMenuButton("Btn_Tasking", mainPanel.transform, "TASKING", "Görevleri Tamamla", ACCENT_ORANGE, btnY - btnSpacing);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(b2.onClick, mgr.ShowTasks);

        var b3 = MakeMenuButton("Btn_Pathing", mainPanel.transform, "PATHING", "Kariyer Moduna Gir", ACCENT_GREEN, btnY - btnSpacing * 2);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(b3.onClick, mgr.ShowPathing);

        var b4 = MakeMenuButton("Btn_Battle", mainPanel.transform, "BATTLE", "Hemen Savaş!", ACCENT_RED, btnY - btnSpacing * 3);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(b4.onClick, mgr.StartBattle);

        // ===========================
        //  CUSTOMIZATION PANELİ
        // ===========================
        GameObject custPanel = MakeFullPanel("CustomizationPanel", canvasObj.transform, PANEL_BG);
        mgr.customizationPanel = custPanel;

        // Başlık
        var custTitle = MakeTMP("Title", custPanel.transform, "CUSTOMIZATION", 60, TEXT_WHITE, TextAlignmentOptions.Center);
        custTitle.fontStyle = FontStyles.Bold;
        AnchorTopCenter(custTitle.rectTransform, 0, -40, 600, 80);

        var custSub = MakeTMP("SubTitle", custPanel.transform, "Parçalarını seç, gücünü hisset.", 22, TEXT_DIM, TextAlignmentOptions.Center);
        AnchorTopCenter(custSub.rectTransform, 0, -110, 600, 40);

        // Sol Kart: Stat Barları
        GameObject statCard = MakeGlassCard("StatCard", custPanel.transform);
        AnchorLeft(statCard.GetComponent<RectTransform>(), 60, 0, 460, 550);
        
        var statTitle = MakeTMP("StatTitle", statCard.transform, "STATS", 32, TEXT_WHITE, TextAlignmentOptions.TopLeft);
        statTitle.fontStyle = FontStyles.Bold;
        AnchorTopLeft(statTitle.rectTransform, 30, -20, 200, 50);

        CustomizationUI customUI = custPanel.AddComponent<CustomizationUI>();
        customUI.attackBar = MakeStatBar(statCard.transform, "ATTACK", ACCENT_RED, 0);
        customUI.defenseBar = MakeStatBar(statCard.transform, "DEFENSE", ACCENT_BLUE, 1);
        customUI.staminaBar = MakeStatBar(statCard.transform, "STAMINA", ACCENT_GREEN, 2);
        customUI.weightBar = MakeStatBar(statCard.transform, "WEIGHT", TEXT_DIM, 3);

        // Sağ Kart: Parça Listeleri
        GameObject partCard = MakeGlassCard("PartCard", custPanel.transform);
        AnchorRight(partCard.GetComponent<RectTransform>(), -60, 0, 500, 750);

        customUI.layerListContainer = MakePartSlot(partCard.transform, "LAYER", "(Saldırı Gücü)", ACCENT_RED, 0);
        customUI.diskListContainer = MakePartSlot(partCard.transform, "DISK", "(Savunma & Ağırlık)", ACCENT_BLUE, 1);
        customUI.tipListContainer = MakePartSlot(partCard.transform, "TIP", "(Dayanıklılık)", ACCENT_GREEN, 2);

        // Şablon buton (gizli)
        var prefabBtn = MakeSmallButton("PartBtnPrefab", custPanel.transform, "Parça", CARD_BG);
        prefabBtn.gameObject.SetActive(false);
        customUI.partButtonPrefab = prefabBtn.gameObject;

        // BACK butonu
        MakeBackButton(custPanel.transform, mgr);

        // ===========================
        //  TASKS PANELİ
        // ===========================
        GameObject taskPanel = MakeFullPanel("TasksPanel", canvasObj.transform, PANEL_BG);
        mgr.tasksPanel = taskPanel;

        var taskTitle = MakeTMP("Title", taskPanel.transform, "TASKING", 60, TEXT_WHITE, TextAlignmentOptions.Center);
        taskTitle.fontStyle = FontStyles.Bold;
        AnchorTopCenter(taskTitle.rectTransform, 0, -30, 600, 80);

        var taskSub = MakeTMP("Sub", taskPanel.transform, "Görevleri tamamla, parça kazan!", 22, TEXT_DIM, TextAlignmentOptions.Center);
        AnchorTopCenter(taskSub.rectTransform, 0, -100, 600, 40);

        // Örnek görev kartları
        MakeTaskCard(taskPanel.transform, "3 Rakip Yen", "Ödül: Layer Tier 1", ACCENT_ORANGE, 0);
        MakeTaskCard(taskPanel.transform, "1000 Hasar Ver", "Ödül: Disk Tier 1", ACCENT_BLUE, 1);
        MakeTaskCard(taskPanel.transform, "5 Maç Kazan", "Ödül: Tip Tier 1", ACCENT_GREEN, 2);

        MakeBackButton(taskPanel.transform, mgr);

        // ===========================
        //  PATHING PANELİ
        // ===========================
        GameObject pathPanel = MakeFullPanel("PathingPanel", canvasObj.transform, PANEL_BG);
        mgr.pathingPanel = pathPanel;

        var pathTitle = MakeTMP("Title", pathPanel.transform, "PATHING", 60, TEXT_WHITE, TextAlignmentOptions.Center);
        pathTitle.fontStyle = FontStyles.Bold;
        AnchorTopCenter(pathTitle.rectTransform, 0, -30, 600, 80);

        var pathSub = MakeTMP("Sub", pathPanel.transform, "Botlarla savaş, seviye atla!", 22, TEXT_DIM, TextAlignmentOptions.Center);
        AnchorTopCenter(pathSub.rectTransform, 0, -100, 600, 40);

        // Seviye kartları
        for (int i = 0; i < 5; i++)
        {
            Color col = (i == 0) ? ACCENT_GREEN : new Color(0.3f, 0.3f, 0.4f);
            string status = (i == 0) ? "MEVCUT" : "KİLİTLİ";
            MakePathCard(pathPanel.transform, "Seviye " + (i + 1), status, col, i);
        }

        MakeBackButton(pathPanel.transform, mgr);

        Debug.Log("Premium UI başarıyla kuruldu!");
    }

    // ==========================
    //  YARDIMCI FONKSİYONLAR
    // ==========================

    static GameObject MakeFullPanel(string name, Transform parent, Color col)
    {
        GameObject p = MakePanel(name, parent, col);
        RectTransform r = p.GetComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        return p;
    }

    static GameObject MakePanel(string name, Transform parent, Color col)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = col;
        return obj;
    }

    static GameObject MakeGlassCard(string name, Transform parent)
    {
        GameObject card = MakePanel(name, parent, GLASS);
        Outline ol = card.AddComponent<Outline>();
        ol.effectColor = GLASS_BORDER;
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        return card;
    }

    static TextMeshProUGUI MakeTMP(string name, Transform parent, string text, int size, Color col, TextAlignmentOptions align)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = col;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    // --- ANA MENÜ BUTONU ---
    static Button MakeMenuButton(string name, Transform parent, string title, string desc, Color accent, float yPos)
    {
        GameObject obj = MakePanel(name, parent, new Color(accent.r, accent.g, accent.b, 0.12f));
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(100, yPos);
        r.sizeDelta = new Vector2(480, 95);

        // Sol kenar renk çizgisi
        GameObject line = MakePanel("Line", obj.transform, accent);
        RectTransform lr = line.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(0, 1);
        lr.pivot = new Vector2(0, 0.5f);
        lr.offsetMin = Vector2.zero; lr.offsetMax = new Vector2(5, 0);

        // Başlık
        var t = MakeTMP("Title", obj.transform, title, 32, TEXT_WHITE, TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold;
        AnchorTopLeft(t.rectTransform, 25, -15, 300, 40);

        // Alt açıklama
        var d = MakeTMP("Desc", obj.transform, desc, 18, TEXT_DIM, TextAlignmentOptions.Left);
        AnchorTopLeft(d.rectTransform, 25, -55, 300, 30);

        // Ok ikonu
        var arrow = MakeTMP("Arrow", obj.transform, "›", 50, accent, TextAlignmentOptions.Right);
        AnchorFill(arrow.rectTransform, 0, 10, 0, 0);

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
        cb.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        cb.fadeDuration = 0.15f;
        btn.colors = cb;

        Outline ol = obj.AddComponent<Outline>();
        ol.effectColor = new Color(accent.r, accent.g, accent.b, 0.3f);
        ol.effectDistance = new Vector2(1, -1);

        return btn;
    }

    // --- STAT BAR ---
    static Image MakeStatBar(Transform parent, string label, Color barCol, int index)
    {
        float yStart = -90;
        float spacing = 110;
        float y = yStart - (index * spacing);

        var lbl = MakeTMP("Lbl_" + label, parent, label, 22, TEXT_DIM, TextAlignmentOptions.Left);
        lbl.fontStyle = FontStyles.Bold;
        AnchorTopLeft(lbl.rectTransform, 30, y, 200, 30);

        // Arka plan bar
        GameObject bg = MakePanel("Bar_" + label + "_BG", parent, new Color(1, 1, 1, 0.06f));
        RectTransform bgR = bg.GetComponent<RectTransform>();
        bgR.anchorMin = new Vector2(0, 1); bgR.anchorMax = new Vector2(1, 1);
        bgR.pivot = new Vector2(0.5f, 1);
        bgR.anchoredPosition = new Vector2(0, y - 35);
        bgR.offsetMin = new Vector2(30, bgR.offsetMin.y);
        bgR.offsetMax = new Vector2(-30, bgR.offsetMax.y);
        bgR.sizeDelta = new Vector2(bgR.sizeDelta.x, 28);

        // Dolu kısım
        GameObject fill = MakePanel("Fill", bg.transform, barCol);
        Image fillImg = fill.GetComponent<Image>();
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0.5f;
        RectTransform fr = fill.GetComponent<RectTransform>();
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
        fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;

        return fillImg;
    }

    // --- PARÇA SLOTU ---
    static Transform MakePartSlot(Transform parent, string title, string desc, Color col, int index)
    {
        float yStart = -30;
        float spacing = 240;
        float y = yStart - (index * spacing);

        var lbl = MakeTMP("Lbl_" + title, parent, title, 28, col, TextAlignmentOptions.Left);
        lbl.fontStyle = FontStyles.Bold;
        AnchorTopLeft(lbl.rectTransform, 25, y, 250, 40);

        var sub = MakeTMP("Sub_" + title, parent, desc, 18, TEXT_DIM, TextAlignmentOptions.Left);
        AnchorTopLeft(sub.rectTransform, 25, y - 35, 300, 30);

        GameObject container = new GameObject(title + "_Container");
        container.transform.SetParent(parent, false);
        RectTransform cr = container.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1); cr.anchorMax = new Vector2(1, 1);
        cr.pivot = new Vector2(0.5f, 1);
        cr.anchoredPosition = new Vector2(0, y - 70);
        cr.sizeDelta = new Vector2(-50, 140);

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 8;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        return container.transform;
    }

    // --- KÜÇÜK BUTON ---
    static Button MakeSmallButton(string name, Transform parent, string text, Color bg)
    {
        GameObject obj = MakePanel(name, parent, bg);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 50);
        Button btn = obj.AddComponent<Button>();

        var t = MakeTMP("T", obj.transform, text, 22, TEXT_WHITE, TextAlignmentOptions.Center);
        AnchorFill(t.rectTransform, 5, 5, 5, 5);

        return btn;
    }

    // --- GERİ BUTONU ---
    static void MakeBackButton(Transform parent, MainMenuManager mgr)
    {
        GameObject obj = MakePanel("Btn_Back", parent, new Color(1, 1, 1, 0.08f));
        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(30, -20);
        r.sizeDelta = new Vector2(180, 60);

        var t = MakeTMP("T", obj.transform, "‹  BACK", 26, TEXT_WHITE, TextAlignmentOptions.Center);
        t.fontStyle = FontStyles.Bold;
        AnchorFill(t.rectTransform, 0, 0, 0, 0);

        Button btn = obj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.4f, 1.4f, 1.4f);
        cb.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        btn.colors = cb;

        Outline ol = obj.AddComponent<Outline>();
        ol.effectColor = GLASS_BORDER;
        ol.effectDistance = new Vector2(1, -1);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, mgr.ShowMainMenu);
    }

    // --- GÖREV KARTI ---
    static void MakeTaskCard(Transform parent, string title, string reward, Color accent, int index)
    {
        float y = -180 - (index * 140);

        GameObject card = MakeGlassCard("Task_" + index, parent);
        RectTransform cr = card.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 1); cr.anchorMax = new Vector2(0.5f, 1);
        cr.pivot = new Vector2(0.5f, 1);
        cr.anchoredPosition = new Vector2(0, y);
        cr.sizeDelta = new Vector2(700, 110);

        // Sol çizgi
        GameObject line = MakePanel("Line", card.transform, accent);
        RectTransform lr = line.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 0); lr.anchorMax = new Vector2(0, 1);
        lr.pivot = new Vector2(0, 0.5f);
        lr.offsetMin = Vector2.zero; lr.offsetMax = new Vector2(5, 0);

        var t = MakeTMP("Title", card.transform, title, 28, TEXT_WHITE, TextAlignmentOptions.Left);
        t.fontStyle = FontStyles.Bold;
        AnchorFill(t.rectTransform, 20, 0, -10, -50);

        var r2 = MakeTMP("Reward", card.transform, reward, 20, accent, TextAlignmentOptions.Left);
        AnchorFill(r2.rectTransform, 20, 0, -55, -10);
    }

    // --- KARİYER KARTI ---
    static void MakePathCard(Transform parent, string title, string status, Color col, int index)
    {
        float startX = -500;
        float spacing = 250;

        GameObject card = MakeGlassCard("Path_" + index, parent);
        RectTransform cr = card.GetComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0.5f); cr.anchorMax = new Vector2(0.5f, 0.5f);
        cr.anchoredPosition = new Vector2(startX + (index * spacing), -50);
        cr.sizeDelta = new Vector2(200, 260);

        var t = MakeTMP("Title", card.transform, title, 28, TEXT_WHITE, TextAlignmentOptions.Center);
        AnchorCenter(t.rectTransform, 0, 30, 180, 50);
        t.fontStyle = FontStyles.Bold;

        var s = MakeTMP("Status", card.transform, status, 20, col, TextAlignmentOptions.Center);
        AnchorCenter(s.rectTransform, 0, -30, 180, 40);

        // Daire ikonu
        GameObject icon = MakePanel("Icon", card.transform, new Color(col.r, col.g, col.b, 0.2f));
        AnchorCenter(icon.GetComponent<RectTransform>(), 0, 70, 80, 80);
    }

    // ==========================
    //  ANCHOR YARDIMCILARI
    // ==========================

    static void AnchorTopLeft(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(0, 1);
        r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    static void AnchorTopCenter(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = new Vector2(0.5f, 1); r.anchorMax = new Vector2(0.5f, 1);
        r.pivot = new Vector2(0.5f, 1);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    static void AnchorCenter(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = new Vector2(0.5f, 0.5f); r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    static void AnchorLeft(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = new Vector2(0, 0.5f); r.anchorMax = new Vector2(0, 0.5f);
        r.pivot = new Vector2(0, 0.5f);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    static void AnchorRight(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = new Vector2(1, 0.5f); r.anchorMax = new Vector2(1, 0.5f);
        r.pivot = new Vector2(1, 0.5f);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);
    }

    static void AnchorFill(RectTransform r, float left, float right, float top, float bottom)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(-right, top);
    }

    static void AddGlow(GameObject obj, Color col, float dist)
    {
        Shadow s = obj.AddComponent<Shadow>();
        s.effectColor = new Color(col.r, col.g, col.b, 0.7f);
        s.effectDistance = new Vector2(dist, -dist);
    }
}
