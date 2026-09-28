using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// QuadStrike Beycoins shop: 2x4 grid, mavi fiyat barı.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Serializable]
    public class ShopOffer
    {
        public string id;
        public string title;
        public string category;
        public int price;
        public bool useGems;
        public Color rarity;
        public string partIdToUnlock;

        public ShopOffer(string id, string title, string category, int price, bool useGems, Color rarity, string partId = "")
        {
            this.id = id;
            this.title = title;
            this.category = category;
            this.price = price;
            this.useGems = useGems;
            this.rarity = rarity;
            this.partIdToUnlock = partId;
        }
    }

    public Transform gridRoot;
    public TextMeshProUGUI goldLabel;
    public TextMeshProUGUI gemsLabel;
    public TextMeshProUGUI timerLabel;
    public TextMeshProUGUI statusLabel;

    static readonly ShopOffer[] Catalog =
    {
        new ShopOffer("s1", "KATANA VALTRYEK V7", "DİJİTAL BEYBLADE", 100000, false, new Color(0.55f, 0.35f, 1f), "layer_basic"),
        new ShopOffer("s2", "HYPERSPHERE", "DİJİTAL BEYSTADIUM", 225000, false, new Color(1f, 0.55f, 0.15f)),
        new ShopOffer("s3", "HELLS SCYTHE", "DİJİTAL BEYBLADE", 85000, false, new Color(1f, 0.35f, 0.45f), "layer_basic"),
        new ShopOffer("s4", "WIZARD ARROW", "DİJİTAL BEYBLADE", 72000, false, new Color(0.45f, 0.55f, 1f), "layer_basic"),
        new ShopOffer("s5", "ROKTAVOR", "DIŞ GÖRÜNÜM", 2500, false, new Color(0.95f, 0.55f, 0.2f)),
        new ShopOffer("s6", "D06", "DIŞ GÖRÜNÜM", 5500, false, new Color(0.25f, 0.85f, 0.45f), "disk_basic"),
        new ShopOffer("s7", "TS01", "DIŞ GÖRÜNÜM", 3500, false, new Color(1f, 0.45f, 0.75f), "tip_basic"),
        new ShopOffer("s8", "D05", "DIŞ GÖRÜNÜM", 5500, false, new Color(0.85f, 0.55f, 0.25f), "disk_basic"),
    };

    float refreshSeconds = 6f * 24f * 3600f + 12f * 3600f + 6f * 60f;

    public void BuildCards()
    {
        if (gridRoot == null) return;
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
            Destroy(gridRoot.GetChild(i).gameObject);
        for (int i = 0; i < Catalog.Length; i++)
            CreateCard(Catalog[i], i);
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
        if (goldLabel != null) goldLabel.text = "C  " + PlayerDataManager.Instance.data.gold.ToString("N0");
        if (gemsLabel != null) gemsLabel.text = "GEM  " + PlayerDataManager.Instance.data.gems.ToString("N0");
    }

    void CreateCard(ShopOffer offer, int index)
    {
        int col = index % 4;
        int row = index / 4;
        float cardW = 260f;
        float cardH = 320f;
        float gapX = 18f;
        float gapY = 22f;
        float startX = -((3 * (cardW + gapX)) * 0.5f);
        float startY = 40f;

        Image card = ModernUIKit.MakeImage(gridRoot, "Offer_" + offer.id, new Color(0.04f, 0.08f, 0.16f, 0.75f), ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(card.rectTransform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(startX + col * (cardW + gapX), startY - row * (cardH + gapY)),
            new Vector2(cardW, cardH));

        Outline border = card.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.25f, 0.75f, 1f, 0.4f);
        border.effectDistance = new Vector2(1f, -1f);

        // Preview orb
        Image preview = ModernUIKit.MakeImage(card.transform, "Preview", new Color(offer.rarity.r, offer.rarity.g, offer.rarity.b, 0.35f), ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(preview.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));
        preview.raycastTarget = false;

        TextMeshProUGUI glyph = ModernUIKit.Label(preview.transform, "G", offer.title.Substring(0, 1), 40, Color.white, ModernUIKit.FontRole.Display);
        ModernUIKit.Stretch(glyph.rectTransform);

        TextMeshProUGUI title = ModernUIKit.Label(card.transform, "Title", offer.title, 17, Color.white, ModernUIKit.FontRole.Title);
        title.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(title.rectTransform, new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 36));

        TextMeshProUGUI cat = ModernUIKit.Label(card.transform, "Cat", offer.category, 13, ModernUIKit.Cyan, ModernUIKit.FontRole.Caption);
        ModernUIKit.Anchor(cat.rectTransform, new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.20f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 22));

        // Solid mavi fiyat barı (referans)
        Image priceBar = ModernUIKit.MakeImage(card.transform, "PriceBar", ModernUIKit.ShopBlue, ModernUIKit.SharpCardSprite);
        ModernUIKit.Anchor(priceBar.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(0, 48));

        string priceText = offer.useGems ? $"GEM  {offer.price:N0}" : $"C  {offer.price:N0}";
        TextMeshProUGUI priceLbl = ModernUIKit.Label(priceBar.transform, "Price", priceText, 20, Color.white, ModernUIKit.FontRole.Title);
        priceLbl.fontStyle = FontStyles.Bold;
        ModernUIKit.Stretch(priceLbl.rectTransform);

        Button buy = priceBar.gameObject.AddComponent<Button>();
        buy.targetGraphic = priceBar;
        buy.onClick.AddListener(() => TryBuy(offer));
    }

    void TryBuy(ShopOffer offer)
    {
        if (PlayerDataManager.Instance == null) return;
        bool ok = offer.useGems
            ? PlayerDataManager.Instance.SpendGems(offer.price)
            : PlayerDataManager.Instance.SpendGold(offer.price);

        if (!ok)
        {
            if (statusLabel != null)
            {
                statusLabel.text = offer.useGems ? "Yetersiz gem!" : "Yetersiz beycoin!";
                statusLabel.color = ModernUIKit.Danger;
            }
            return;
        }

        if (!string.IsNullOrEmpty(offer.partIdToUnlock))
            PlayerDataManager.Instance.UnlockPart(offer.partIdToUnlock);

        if (statusLabel != null)
        {
            statusLabel.text = offer.title + " alındı!";
            statusLabel.color = ModernUIKit.Mint;
        }
        RefreshCurrency();
    }
}
