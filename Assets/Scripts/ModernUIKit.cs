using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using TMPro;

/// <summary>
/// QuadStrike tarzı runtime UI: Rajdhani tipografi, hex butonlar, arena backdrop.
/// </summary>
public static class ModernUIKit
{
    public static readonly Color Bg = new Color(0.02f, 0.03f, 0.06f, 1f);
    public static readonly Color BgDeep = new Color(0.01f, 0.015f, 0.035f, 1f);
    public static readonly Color Panel = new Color(0.07f, 0.10f, 0.16f, 0.92f);
    public static readonly Color Glass = new Color(0.14f, 0.22f, 0.36f, 0.28f);
    public static readonly Color GlassStrong = new Color(0.05f, 0.10f, 0.20f, 0.88f);
    public static readonly Color Line = new Color(0.35f, 0.82f, 1f, 0.5f);
    public static readonly Color Text = new Color(0.97f, 0.98f, 1f, 1f);
    public static readonly Color Dim = new Color(0.58f, 0.68f, 0.80f, 1f);
    public static readonly Color Cyan = new Color(0.12f, 0.82f, 1f, 1f);
    public static readonly Color Electric = new Color(0.35f, 0.92f, 1f, 1f);
    public static readonly Color Magenta = new Color(1f, 0.28f, 0.55f, 1f);
    public static readonly Color Gold = new Color(1f, 0.82f, 0.28f, 1f);
    public static readonly Color Heat = new Color(1f, 0.42f, 0.12f, 1f);
    public static readonly Color Danger = new Color(1f, 0.25f, 0.32f, 1f);
    public static readonly Color Mint = new Color(0.2f, 1f, 0.55f, 1f);
    public static readonly Color Battle = new Color(0.25f, 0.95f, 0.35f, 1f);
    public static readonly Color Profile = new Color(0.95f, 0.18f, 0.28f, 1f);
    public static readonly Color Crimson = new Color(0.85f, 0.1f, 0.18f, 1f);
    public static readonly Color ShopBlue = new Color(0.05f, 0.55f, 0.95f, 1f);

    public enum FontRole { Display, Title, Body, Caption }

    static Sprite roundSprite, softCardSprite, sharpCardSprite, circleSprite, hexSprite, ringSprite, gradientSprite;
    static Sprite trapLeftSprite, trapRightSprite, honeycombSprite, hexGridSprite;
    static TMP_FontAsset fontDisplay, fontTitle, fontBody;
    static bool fontsReady;

    public static Sprite RoundSprite { get { if (roundSprite == null) roundSprite = BuildRoundRect(96, 96, 18); return roundSprite; } }
    public static Sprite SoftCardSprite { get { if (softCardSprite == null) softCardSprite = BuildRoundRect(128, 128, 28); return softCardSprite; } }
    public static Sprite SharpCardSprite { get { if (sharpCardSprite == null) sharpCardSprite = BuildRoundRect(96, 96, 6); return sharpCardSprite; } }
    public static Sprite CircleSprite { get { if (circleSprite == null) circleSprite = BuildCircle(128); return circleSprite; } }
    public static Sprite HexSprite { get { if (hexSprite == null) hexSprite = BuildHex(128); return hexSprite; } }
    public static Sprite RingSprite { get { if (ringSprite == null) ringSprite = BuildRing(256, 0.72f, 0.92f); return ringSprite; } }
    public static Sprite VerticalGradientSprite { get { if (gradientSprite == null) gradientSprite = BuildVerticalGradient(8, 256); return gradientSprite; } }
    public static Sprite TrapLeftSprite { get { if (trapLeftSprite == null) trapLeftSprite = BuildTrapezoid(128, 96, true); return trapLeftSprite; } }
    public static Sprite TrapRightSprite { get { if (trapRightSprite == null) trapRightSprite = BuildTrapezoid(128, 96, false); return trapRightSprite; } }
    public static Sprite HoneycombSprite { get { if (honeycombSprite == null) honeycombSprite = BuildHoneycomb(128); return honeycombSprite; } }
    public static Sprite HexGridSprite { get { if (hexGridSprite == null) hexGridSprite = BuildHexGrid(256); return hexGridSprite; } }

    public static void EnsureFonts()
    {
        if (fontsReady && fontBody != null) return;
        fontDisplay = LoadDynamicFont("Fonts/Rajdhani-Bold", 110);
        fontTitle = LoadDynamicFont("Fonts/Rajdhani-SemiBold", 90);
        fontBody = LoadDynamicFont("Fonts/Rajdhani-Medium", 72);
        if (fontDisplay == null) fontDisplay = fontTitle ?? fontBody;
        if (fontTitle == null) fontTitle = fontDisplay ?? fontBody;
        if (fontBody == null) fontBody = fontTitle ?? fontDisplay;
        fontsReady = fontBody != null;
    }

    static TMP_FontAsset LoadDynamicFont(string resourcesPath, int sampling)
    {
        Font unityFont = Resources.Load<Font>(resourcesPath);
        if (unityFont == null)
        {
            Debug.LogWarning("Font bulunamadı: Resources/" + resourcesPath);
            return null;
        }
        try
        {
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(unityFont, sampling, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            if (asset != null)
            {
                asset.name = "Runtime_" + unityFont.name;
                asset.TryAddCharacters("ABCÇDEFGĞHIİJKLMNOÖPQRSŞTUÜVWXYZabcçdefgğhıijklmnoöpqrsştuüvwxyz0123456789%.,·:-/!?+'\"()[]");
            }
            return asset;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("TMP font create failed: " + e.Message);
            return null;
        }
    }

    public static TMP_FontAsset GetFont(FontRole role)
    {
        EnsureFonts();
        switch (role)
        {
            case FontRole.Display: return fontDisplay ?? fontTitle ?? fontBody;
            case FontRole.Title: return fontTitle ?? fontDisplay ?? fontBody;
            case FontRole.Caption: return fontBody ?? fontTitle;
            default: return fontBody ?? fontTitle ?? fontDisplay;
        }
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        System.Type inputModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModule != null) es.AddComponent(inputModule);
        else es.AddComponent<StandaloneInputModule>();
    }

    public static Canvas CreateCanvas(string name, int sort)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sort;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static Image MakeImage(Transform parent, string name, Color color, Sprite sprite = null, bool sliced = true)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite != null ? sprite : RoundSprite;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.raycastTarget = true;
        return img;
    }

    public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        return Label(parent, name, text, size, color, FontRole.Body, align);
    }

    public static TextMeshProUGUI Label(Transform parent, string name, string text, float size, Color color, FontRole role, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        EnsureFonts();
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset f = GetFont(role);
        if (f != null) tmp.font = f;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableKerning = true;
        return tmp;
    }

    public static TextMeshProUGUI DisplayLabel(Transform parent, string name, string text, float size, Color color)
    {
        TextMeshProUGUI tmp = Label(parent, name, text, size, color, FontRole.Display);
        tmp.fontStyle = FontStyles.Bold;
        tmp.characterSpacing = 6f;
        tmp.enableVertexGradient = true;
        tmp.colorGradient = new VertexGradient(Color.white, Color.white, new Color(0.75f, 0.9f, 1f), new Color(0.55f, 0.8f, 1f));
        tmp.outlineWidth = 0.18f;
        tmp.outlineColor = new Color32(20, 80, 160, 160);
        return tmp;
    }

    public static RectTransform MakeQuadStrikeLogo(Transform parent, float scale = 1f)
    {
        GameObject root = new GameObject("QSLogo");
        root.transform.SetParent(parent, false);
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(900f * scale, 280f * scale);

        Image swoosh = MakeImage(rt, "Swoosh", new Color(0.15f, 0.55f, 1f, 0.45f), CircleSprite, false);
        Anchor(swoosh.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), new Vector2(-40f * scale, 10f * scale), new Vector2(520f * scale, 200f * scale));
        swoosh.raycastTarget = false;
        swoosh.rectTransform.localEulerAngles = new Vector3(0, 0, -18f);

        TextMeshProUGUI bey = Label(rt, "Beyblade", "BEYBLADE", 78f * scale, Color.white, FontRole.Display);
        bey.fontStyle = FontStyles.Bold | FontStyles.Italic;
        bey.characterSpacing = 2f;
        bey.enableVertexGradient = true;
        bey.colorGradient = new VertexGradient(Color.white, Color.white, new Color(0.7f, 0.85f, 1f), new Color(0.55f, 0.7f, 0.9f));
        bey.outlineWidth = 0.28f;
        bey.outlineColor = new Color32(0, 0, 0, 220);
        Anchor(bey.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f * scale, 100f * scale));

        TextMeshProUGUI burst = Label(rt, "Burst", "BURST", 34f * scale, Gold, FontRole.Display);
        burst.fontStyle = FontStyles.Bold | FontStyles.Italic;
        burst.characterSpacing = 8f;
        burst.outlineWidth = 0.15f;
        burst.outlineColor = new Color32(80, 40, 0, 180);
        Anchor(burst.rectTransform, new Vector2(0.62f, 0.48f), new Vector2(0.62f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f * scale, 48f * scale));

        TextMeshProUGUI quad = Label(rt, "Quad", "QUAD", 48f * scale, new Color(1f, 0.28f, 0.18f), FontRole.Display);
        quad.fontStyle = FontStyles.Bold | FontStyles.Italic;
        quad.outlineWidth = 0.2f;
        quad.outlineColor = new Color32(40, 0, 0, 200);
        Anchor(quad.rectTransform, new Vector2(0.32f, 0.22f), new Vector2(0.32f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f * scale, 60f * scale));

        TextMeshProUGUI strike = Label(rt, "Strike", "STRIKE", 48f * scale, Electric, FontRole.Display);
        strike.fontStyle = FontStyles.Bold | FontStyles.Italic;
        strike.outlineWidth = 0.2f;
        strike.outlineColor = new Color32(0, 40, 80, 200);
        Anchor(strike.rectTransform, new Vector2(0.68f, 0.22f), new Vector2(0.68f, 0.22f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f * scale, 60f * scale));

        // Sarı elmas (font glyph değil — Image, uyarı yok)
        Image diamond = MakeImage(rt, "Dia", Gold, null, false);
        diamond.sprite = null;
        Anchor(diamond.rectTransform, new Vector2(0.58f, 0.38f), new Vector2(0.58f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12f * scale, 12f * scale));
        diamond.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        diamond.raycastTarget = false;
        return rt;
    }

    public static void MakeArenaBackdrop(Transform parent)
    {
        Image floor = MakeImage(parent, "HexFloor", new Color(0.12f, 0.14f, 0.18f, 0.55f), HexGridSprite, false);
        Anchor(floor.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 900));
        floor.raycastTarget = false;

        Image ring = MakeImage(parent, "ArenaBlueRing", new Color(0.15f, 0.75f, 1f, 0.55f), RingSprite, false);
        Anchor(ring.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720, 720));
        ring.raycastTarget = false;

        Image ringOuter = MakeImage(parent, "ArenaOrangeRing", new Color(1f, 0.55f, 0.15f, 0.35f), RingSprite, false);
        Anchor(ringOuter.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 820));
        ringOuter.raycastTarget = false;

        for (int i = 0; i < 2; i++)
        {
            Image cross = MakeImage(parent, "RedCross_" + i, new Color(1f, 0.15f, 0.2f, 0.45f), null, false);
            cross.sprite = null;
            Anchor(cross.rectTransform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 700));
            cross.rectTransform.localEulerAngles = new Vector3(0, 0, i == 0 ? 28f : -28f);
            cross.raycastTarget = false;
        }
    }

    public static Button MakeButton(Transform parent, string name, string label, Color accent, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        Image img = MakeImage(parent, name, new Color(accent.r, accent.g, accent.b, 0.16f), SharpCardSprite);
        img.rectTransform.sizeDelta = size;
        Outline ol = img.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(accent.r, accent.g, accent.b, 0.65f);
        ol.effectDistance = new Vector2(1.8f, -1.8f);
        TextMeshProUGUI txt = Label(img.transform, "Label", label, Mathf.Clamp(size.y * 0.38f, 18f, 28f), Text, FontRole.Title);
        txt.fontStyle = FontStyles.Bold;
        txt.characterSpacing = 3f;
        Stretch(txt.rectTransform);
        Button btn = img.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        return btn;
    }

    public static Button MakePrimaryCTA(Transform parent, string name, string label, string sub, Color accent, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        Image img = MakeImage(parent, name, new Color(accent.r, accent.g, accent.b, 0.92f), SharpCardSprite);
        img.rectTransform.sizeDelta = size;
        Shadow sh = img.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(accent.r, accent.g, accent.b, 0.55f);
        sh.effectDistance = new Vector2(0f, -10f);
        TextMeshProUGUI txt = Label(img.transform, "Label", label, 42, Color.white, FontRole.Display);
        txt.fontStyle = FontStyles.Bold;
        txt.characterSpacing = 8f;
        if (string.IsNullOrEmpty(sub)) Stretch(txt.rectTransform);
        else Anchor(txt.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 20f, 52f));
        if (!string.IsNullOrEmpty(sub))
        {
            TextMeshProUGUI s = Label(img.transform, "Sub", sub, 16, new Color(1f, 1f, 1f, 0.8f), FontRole.Caption);
            Anchor(s.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x - 30f, 28f));
        }
        Button btn = img.gameObject.AddComponent<Button>();
        btn.onClick.AddListener(onClick);
        return btn;
    }

    public static Button MakeNavPill(Transform parent, string name, string label, Color accent, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        Image img = MakeImage(parent, name, new Color(0.08f, 0.12f, 0.2f, 0.9f), SharpCardSprite);
        img.rectTransform.sizeDelta = size;
        TextMeshProUGUI txt = Label(img.transform, "Label", label, 20, Text, FontRole.Title);
        txt.fontStyle = FontStyles.Bold;
        Stretch(txt.rectTransform);
        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        return btn;
    }

    public static Button MakeHexButton(Transform parent, string name, string label, string iconGlyph, Color accent, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        return MakeHexButton(parent, name, label, iconGlyph, accent, size, onClick, false);
    }

    public static Button MakeHexButton(Transform parent, string name, string label, string iconGlyph, Color accent, Vector2 size, UnityEngine.Events.UnityAction onClick, bool filled)
    {
        float fillA = filled ? 0.88f : 0.22f;
        Image hex = MakeImage(parent, name, new Color(accent.r, accent.g, accent.b, fillA), HexSprite, false);
        hex.rectTransform.sizeDelta = size;
        if (filled)
        {
            Image honey = MakeImage(hex.transform, "Honey", new Color(1f, 1f, 1f, 0.12f), HoneycombSprite, false);
            Stretch(honey.rectTransform, 18, 18, 18, 18);
            honey.raycastTarget = false;
        }
        Outline glow = hex.gameObject.AddComponent<Outline>();
        glow.effectColor = new Color(accent.r, accent.g, accent.b, filled ? 0.95f : 0.75f);
        glow.effectDistance = new Vector2(filled ? 4f : 2.5f, filled ? -4f : -2.5f);
        Shadow soft = hex.gameObject.AddComponent<Shadow>();
        soft.effectColor = new Color(accent.r, accent.g, accent.b, filled ? 0.55f : 0.3f);
        soft.effectDistance = new Vector2(0f, filled ? -12f : -6f);

        if (!string.IsNullOrEmpty(iconGlyph))
        {
            TextMeshProUGUI icon = Label(hex.transform, "Icon", iconGlyph, size.y * (filled ? 0.26f : 0.22f), Color.white, FontRole.Display);
            Anchor(icon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x * 0.7f, size.y * 0.35f));
        }
        if (!string.IsNullOrEmpty(label))
        {
            TextMeshProUGUI txt = Label(hex.transform, "Label", label, Mathf.Clamp(size.y * 0.11f, 16f, 30f), Color.white, FontRole.Title);
            txt.fontStyle = FontStyles.Bold;
            txt.characterSpacing = 3f;
            Anchor(txt.rectTransform, new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x * 0.85f, 36f));
        }
        Button btn = hex.gameObject.AddComponent<Button>();
        btn.targetGraphic = hex;
        btn.onClick.AddListener(onClick);
        return btn;
    }

    public static Button MakeSideNavButton(Transform parent, string name, string label, string iconGlyph, bool leftSide, UnityEngine.Events.UnityAction onClick)
    {
        Sprite sp = leftSide ? TrapLeftSprite : TrapRightSprite;
        Image img = MakeImage(parent, name, new Color(0.12f, 0.35f, 0.65f, 0.55f), sp, false);
        img.rectTransform.sizeDelta = new Vector2(220, 170);
        Outline ol = img.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0.3f, 0.85f, 1f, 0.7f);
        ol.effectDistance = new Vector2(2f, -2f);
        TextMeshProUGUI icon = Label(img.transform, "Icon", iconGlyph, 36, Color.white, FontRole.Display);
        Anchor(icon.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 50));
        TextMeshProUGUI txt = Label(img.transform, "Label", label, 20, Color.white, FontRole.Title);
        txt.fontStyle = FontStyles.Bold;
        Anchor(txt.rectTransform, new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 32));
        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);
        return btn;
    }

    public static Image MakeGlassCard(Transform parent, string name, Vector2 size)
    {
        Image card = MakeImage(parent, name, GlassStrong, SharpCardSprite);
        card.rectTransform.sizeDelta = size;
        Outline ol = card.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.35f);
        ol.effectDistance = new Vector2(1.2f, -1.2f);
        return card;
    }

    public static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b);
        rt.offsetMax = new Vector2(-r, -t);
    }

    public static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static Sprite BuildRoundRect(int w, int h, int radius)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float r = radius;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float ax = Mathf.Min(x, w - 1 - x);
                float ay = Mathf.Min(y, h - 1 - y);
                float a = 1f;
                if (ax < r && ay < r)
                {
                    float d = Mathf.Sqrt((r - ax) * (r - ax) + (r - ay) * (r - ay));
                    a = Mathf.Clamp01(r - d + 0.5f);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    static Sprite BuildCircle(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 1.55f)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildRing(int size, float inner, float outer)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = 0f;
                if (d >= inner && d <= outer)
                    a = Mathf.SmoothStep(0f, 1f, Mathf.Min(d - inner, outer - d) * 18f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildVerticalGradient(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float a = Mathf.SmoothStep(0f, 1f, (float)y / (h - 1));
            for (int x = 0; x < w; x++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildHex(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = (size - 1) * 0.5f, cy = (size - 1) * 0.5f, r = size * 0.48f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = (x - cx) / r, py = (y - cy) / r;
                float d = Mathf.Max(Mathf.Abs(px) * 0.8660254f + Mathf.Abs(py) * 0.5f, Mathf.Abs(py));
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - d) * size * 0.55f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildTrapezoid(int w, int h, bool leftHeavy)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
        {
            float t = (float)y / (h - 1);
            float insetA = leftHeavy ? 0.08f : 0.28f;
            float insetB = leftHeavy ? 0.28f : 0.08f;
            float left = Mathf.Lerp(insetA, insetB, t) * w;
            float right = w - Mathf.Lerp(insetB, insetA, t) * w;
            for (int x = 0; x < w; x++)
            {
                float a = 0f;
                if (x >= left && x <= right)
                    a = Mathf.Clamp01(Mathf.Min(x - left, right - x) * 0.35f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildHoneycomb(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cell = size / 4.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x / cell, py = y / cell;
                int row = Mathf.FloorToInt(py);
                float ox = (row % 2) * 0.5f;
                float fx = Mathf.Abs((px + ox) % 1f - 0.5f);
                float fy = Mathf.Abs(py % 1f - 0.5f);
                float d = Mathf.Max(fx * 0.866f + fy * 0.5f, fy);
                float a = (d > 0.42f && d < 0.48f) ? 0.9f : 0f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static Sprite BuildHexGrid(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cell = size / 8f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px = x / cell, py = y / cell;
                int row = Mathf.FloorToInt(py);
                float ox = (row % 2) * 0.5f;
                float fx = Mathf.Abs((px + ox) % 1f - 0.5f);
                float fy = Mathf.Abs(py % 1f - 0.5f);
                float d = Mathf.Max(fx * 0.866f + fy * 0.5f, fy);
                float a = (d > 0.44f && d < 0.49f) ? 0.55f : 0.08f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
