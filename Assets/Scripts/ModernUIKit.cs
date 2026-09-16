using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Runtime UI: yuvarlatılmış paneller, TMP, cam butonlar.
/// </summary>
public static class ModernUIKit
{
    public static readonly Color Bg = new Color(0.035f, 0.04f, 0.07f, 1f);
    public static readonly Color Panel = new Color(0.07f, 0.09f, 0.14f, 0.92f);
    public static readonly Color Glass = new Color(1f, 1f, 1f, 0.06f);
    public static readonly Color Line = new Color(1f, 1f, 1f, 0.12f);
    public static readonly Color Text = new Color(0.95f, 0.97f, 1f, 1f);
    public static readonly Color Dim = new Color(0.62f, 0.68f, 0.78f, 1f);
    public static readonly Color Cyan = new Color(0.25f, 0.85f, 1f, 1f);
    public static readonly Color Magenta = new Color(1f, 0.28f, 0.55f, 1f);
    public static readonly Color Gold = new Color(1f, 0.82f, 0.28f, 1f);
    public static readonly Color Danger = new Color(1f, 0.32f, 0.38f, 1f);
    public static readonly Color Mint = new Color(0.35f, 1f, 0.72f, 1f);

    static Sprite roundSprite;
    static Sprite circleSprite;

    public static Sprite RoundSprite
    {
        get
        {
            if (roundSprite == null) roundSprite = BuildRoundRect(64, 64, 18);
            return roundSprite;
        }
    }

    public static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null) circleSprite = BuildCircle(64);
            return circleSprite;
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
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    public static Button MakeButton(Transform parent, string name, string label, Color accent, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        Image img = MakeImage(parent, name, new Color(accent.r, accent.g, accent.b, 0.18f));
        RectTransform rt = img.rectTransform;
        rt.sizeDelta = size;

        Image stroke = MakeImage(img.transform, "Stroke", new Color(accent.r, accent.g, accent.b, 0.55f));
        Stretch(stroke.rectTransform);
        stroke.raycastTarget = false;
        var strokeOutline = stroke.gameObject.AddComponent<Outline>();
        strokeOutline.effectColor = new Color(accent.r, accent.g, accent.b, 0.0f);

        TextMeshProUGUI txt = Label(img.transform, "Label", label, 28, Text);
        txt.fontStyle = FontStyles.Bold;
        txt.characterSpacing = 6f;
        Stretch(txt.rectTransform);

        Button btn = img.gameObject.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.fadeDuration = 0.12f;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);
        return btn;
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
        {
            for (int x = 0; x < w; x++)
            {
                float ax = Mathf.Min(x, w - 1 - x);
                float ay = Mathf.Min(y, h - 1 - y);
                float a = 1f;
                if (ax < r && ay < r)
                {
                    float dx = r - ax;
                    float dy = r - ay;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    a = Mathf.Clamp01(r - d + 0.5f);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    static Sprite BuildCircle(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Pow(a, 1.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
