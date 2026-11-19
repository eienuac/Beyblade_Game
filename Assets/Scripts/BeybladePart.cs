using UnityEngine;

public enum PartType
{
    Layer,       // Üst Çarpışma Yüzeyi (Attack)
    WeightDisk,  // Orta Ağırlık Diski (Defense & Weight)
    PerformanceTip // Alt Uç (Stamina & RPM)
}

[CreateAssetMenu(fileName = "New Beyblade Part", menuName = "Beyblade/Part")]
public class BeybladePart : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string partID; // Örn: "layer_dragon", "tip_flat"
    public string partName;
    public PartType type;
    public Sprite partIcon;
    public int tier = 1; // Parçanın seviyesi (1, 2, 3)
    [Tooltip("Mağaza fiyatı (beycoin). 0 = mağazada satılmaz / başlangıç parçası")]
    public int price = 100;

    [Header("Stat Katkıları")]
    [Tooltip("Saldırı: toplam + 2 → 2..10")]
    public float attackBonus = 0f;
    [Tooltip("Savunma: toplam + 0.1 → 0.05..0.9")]
    public float defenseBonus = 0f;
    [Tooltip("Dayanıklılık: her 1 puan dönüş kaybını 0.1 azaltır")]
    public float staminaBonus = 0f;
    [Tooltip("Ağırlık: toplam + 0.8 → 1.4..6")]
    public float weightBonus = 0f;

    public BeybladeCore.PartStats ToStats() => new BeybladeCore.PartStats(attackBonus, defenseBonus, staminaBonus, weightBonus);

    [Header("Görsel (Opsiyonel)")]
    public GameObject partPrefab; // Özelleştirme ekranında veya arenada göstermek için 3D model
}
