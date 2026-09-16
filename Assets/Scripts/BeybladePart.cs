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

    [Header("Stat Katkıları")]
    public float attackBonus = 0f;
    public float defenseBonus = 0f;
    public float staminaBonus = 0f;
    public float weightBonus = 0f;

    [Header("Görsel (Opsiyonel)")]
    public GameObject partPrefab; // Özelleştirme ekranında veya arenada göstermek için 3D model
}
