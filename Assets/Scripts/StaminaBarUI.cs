using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Beyin üstünde küçük işaretçi (SEN / RAKİP) ve ince dönüş çubuğu.
/// Asıl göstergeler üstteki BattleHUD'dadır; bu bileşen ayrıca beyi BattleHUD'a kaydeder.
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("Referans")]
    public BeybladeController beyblade;

    [Header("Ayarlar")]
    public Vector3 offset = new Vector3(0, 1.0f, 0);

    Canvas canvas;
    RectTransform marker;
    Image chevron;
    Image miniFill;
    TextMeshProUGUI tagLabel;
    CanvasGroup group;

    void Start()
    {
        if (beyblade == null) beyblade = GetComponent<BeybladeController>();
        if (beyblade == null) return;

        BattleHUD.Register(beyblade);
        Build();
    }

    void Build()
    {
        ModernUIKit.EnsureFonts();
        bool mine = beyblade.isPlayer && !MatchContext.IsBalanceTest;
        Color c = mine ? ModernUIKit.Cyan : ModernUIKit.Danger;

        canvas = ModernUIKit.CreateCanvas("BeyMarker_" + gameObject.name, 90);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;

        GameObject root = new GameObject("Marker");
        root.transform.SetParent(canvas.transform, false);
        marker = root.AddComponent<RectTransform>();
        marker.sizeDelta = new Vector2(110f, 54f);
        group = root.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        tagLabel = ModernUIKit.Label(marker, "Tag", mine ? "SEN" : "RAKİP", 15, c, ModernUIKit.FontRole.Title);
        tagLabel.fontStyle = FontStyles.Bold;
        tagLabel.characterSpacing = 3f;
        ModernUIKit.Anchor(tagLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(110f, 20f));

        Image track = ModernUIKit.MakeImage(marker, "Track", new Color(0f, 0f, 0f, 0.55f), null, false);
        track.sprite = null;
        track.raycastTarget = false;
        ModernUIKit.Anchor(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(64f, 5f));
        miniFill = ModernUIKit.MakeHorizontalFill(track.transform, "Fill", c);
        ModernUIKit.Stretch(miniFill.rectTransform, 1, 1, 1, 1);

        chevron = ModernUIKit.MakeImage(marker, "Chevron", c, ModernUIKit.ChevronSprite, false);
        chevron.raycastTarget = false;
        ModernUIKit.Anchor(chevron.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(18f, 14f));
    }

    void LateUpdate()
    {
        if (beyblade == null || marker == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(beyblade.transform.position + offset);
        bool visible = screenPos.z > 0f && !(beyblade.hasToppled || (!beyblade.isSpinning && beyblade.isLaunched));
        if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
        if (!visible) return;

        marker.position = screenPos;
        float ratio = beyblade.StaminaRatio;
        miniFill.fillAmount = ratio;
        bool low = ratio < 0.25f && beyblade.isLaunched;
        float pulse = low ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 10f) : 1f;
        chevron.rectTransform.anchoredPosition = new Vector2(0f, -32f + Mathf.Sin(Time.unscaledTime * 4f) * 2f);
        group.alpha = pulse;
    }

    void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }
}
