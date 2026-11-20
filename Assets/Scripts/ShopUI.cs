using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Beycoin mağazası: parça veritabanındaki fiyatı olan (price > 0) parçaları satar.
/// Satın alınan parça Beylocker'da takılabilir hale gelir.
/// </summary>
public class ShopUI : MonoBehaviour
{
    public Transform gridRoot;
    public TextMeshProUGUI goldLabel;
    public TextMeshProUGUI gemsLabel;
    public TextMeshProUGUI timerLabel;
    public TextMeshProUGUI statusLabel;
    public PartDatabase partDatabase;

    const int Columns = 5;
    const float CardW = 250f;
    const float CardH = 300f;
    const float GapX = 18f;
    const float GapY = 22f;

    float refreshSeconds = 6f * 24f * 3600f + 12f * 3600f + 6f * 60f;

    public void BuildCards()
    {
        if (gridRoot == null) return;
        partDatabase = PartDatabase.Resolve(partDatabase);

        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);

        List<BeybladePart> offers = new List<BeybladePart>();
        foreach (BeybladePart p in partDatabase.allParts)
            if (p != null && p.price > 0) offers.Add(p);
        offers.Sort((a, b) => a.type != b.type ? a.type.CompareTo(b.type) : a.price.CompareTo(b.price));

        int rows = Mathf.Max(1, Mathf.CeilToInt(offers.Count / (float)Columns));
        for (int i = 0; i < offers.Count; i++)
            CreateCard(offers[i], i, Mathf.Min(Columns, offers.Count), rows);
        RefreshCurrency();
    }

    void Update()
    {
        if (timerLabel == null) return;
        refreshSeconds -= Time.unscaledDeltaTime;
        if (refreshSeconds < 0f) refreshSeconds = 6f * 24f * 3600f;
        int total = Mathf.Max(0, Mathf.FloorToInt(refreshSeconds));
        int days = total / 86400;
        int hours = (total % 86400) / 3600;
        int mins = (total % 3600) / 60;
        timerLabel.text = $"{days:00} GÜN  {hours:00} SAAT  {mins:00} DAKİKA";
    }

    public void RefreshCurrency()
    {
        if (PlayerDataManager.Instance == null) return;
        if (goldLabel != null) goldLabel.text = PlayerDataManager.Instance.data.gold.ToString("N0");
        if (gemsLabel != null) gemsLabel.text = "GEM  " + PlayerDataManager.Instance.data.gems.ToString("N0");
    }

    static Color TypeColor(PartType t)
    {
        switch (t)
        {
            case PartType.Layer: return ModernUIKit.Magenta;
            case PartType.WeightDisk: return ModernUIKit.Gold;
            default: return ModernUIKit.Mint;
        }
    }

    static string TypeName(PartType t)
    {
        switch (t)
        {
            case PartType.Layer: return "LAYER";
            case PartType.WeightDisk: return "DISK";
            default: return "TIP";
        }
    }

    public static string StatLine(BeybladePart p)
    {
        var parts = new List<string>();
        if (p.attackBonus != 0f) parts.Add("ATK " + p.attackBonus.ToString("+0.#;-0.#"));
        if (p.defenseBonus != 0f) parts.Add("DEF " + (p.defenseBonus * 10f).ToString("+0.#;-0.#"));
        if (p.staminaBonus != 0f) parts.Add("STM " + p.staminaBonus.ToString("+0.#;-0.#"));
        if (p.weightBonus != 0f) parts.Add("KG " + p.weightBonus.ToString("+0.#;-0.#"));
        return string.Join("  ", parts);
    }

    void CreateCard(BeybladePart part, int index, int columns, int rows)
    {
        int col = index % Columns;
        int row = index / Columns;
        float startX = -((columns - 1) * (CardW + GapX)) * 0.5f;
        float startY = ((rows - 1) * (CardH + GapY)) * 0.5f;

        bool owned = PlayerDataManager.Instance != null && PlayerDataManager.Instance.data.unlockedParts.Contains(part.partID);
        Color accent = TypeColor(part.type);

        Image card = ModernUIKit.MakeImage(gridRoot, "Offer_" + part.partID, new Color(0.04f, 0.08f, 0.16f, 0.75f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(card.rectTransform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(startX + col * (CardW + GapX), startY - row * (CardH + GapY)),
            new Vector2(CardW, CardH));

        Outline border = card.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(accent.r, accent.g, accent.b, 0.45f);
        border.effectDistance = new Vector2(1f, -1f);

        Image preview = ModernUIKit.MakeImage(card.transform, "Preview", new Color(accent.r, accent.g, accent.b, 0.3f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(preview.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100, 100));
        preview.raycastTarget = false;
        if (part.partIcon != null)
        {
            preview.sprite = part.partIcon;
            preview.color = Color.white;
        }
        else
        {
            TextMeshProUGUI glyph = ModernUIKit.Label(preview.transform, "G", part.partName.Substring(0, 1), 40, Color.white, ModernUIKit.FontRole.Display);
            ModernUIKit.Stretch(glyph.rectTransform);
        }

        TextMeshProUGUI title = ModernUIKit.Label(card.transform, "Title", part.partName.ToUpper(), 19, Color.white, ModernUIKit.FontRole.Title);
        title.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(title.rectTransform, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardW - 16, 32));

        TextMeshProUGUI cat = ModernUIKit.Label(card.transform, "Cat", TypeName(part.type) + "  ·  T" + part.tier, 13, accent, ModernUIKit.FontRole.Caption);
        ModernUIKit.Anchor(cat.rectTransform, new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardW - 16, 22));

        TextMeshProUGUI stats = ModernUIKit.Label(card.transform, "Stats", StatLine(part), 13, ModernUIKit.Dim, ModernUIKit.FontRole.Caption);
        ModernUIKit.Anchor(stats.rectTransform, new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CardW - 12, 22));

        Image priceBar = ModernUIKit.MakeImage(card.transform, "PriceBar", owned ? new Color(1f, 1f, 1f, 0.08f) : ModernUIKit.ShopBlue, ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(priceBar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(0, 48));

        TextMeshProUGUI priceLbl = ModernUIKit.Label(priceBar.transform, "Price", owned ? "SAHİPSİN" : $"C  {part.price:N0}", 20, owned ? ModernUIKit.Mint : Color.white, ModernUIKit.FontRole.Title);
        priceLbl.fontStyle = FontStyles.Bold;
        ModernUIKit.Stretch(priceLbl.rectTransform);

        if (!owned)
        {
            Button buy = priceBar.gameObject.AddComponent<Button>();
            buy.targetGraphic = priceBar;
            buy.onClick.AddListener(() => TryBuy(part));
        }
    }

    void TryBuy(BeybladePart part)
    {
        if (PlayerDataManager.Instance == null) return;
        if (PlayerDataManager.Instance.data.unlockedParts.Contains(part.partID)) return;

        if (!PlayerDataManager.Instance.SpendGold(part.price))
        {
            SetStatus("Yetersiz beycoin! Maç kazanarak beycoin topla.", ModernUIKit.Danger);
            return;
        }

        PlayerDataManager.Instance.UnlockPart(part.partID);
        SetStatus(part.partName + " alındı! Beylocker'dan takabilirsin.", ModernUIKit.Mint);
        BuildCards();
    }

    void SetStatus(string msg, Color c)
    {
        if (statusLabel == null) return;
        statusLabel.text = msg;
        statusLabel.color = c;
    }
}
