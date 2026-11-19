using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Oyunun temel parça seti. Asset oluşturmadan da oyun çalışsın diye kodda tanımlı.
/// Ayarlamak için: Beyblade > Varsayılan Parça Asset'lerini Oluştur menüsüyle asset'e çevirip Inspector'dan düzenle.
///
/// Stat ölçeği (BeyStatCalculator):
///   Attack  → 2 + toplam (2..10)
///   Defense → 0.1 + toplam (0.05..0.9)
///   Stamina → decay = 1.25 - 0.1 * toplam (0.45..1.3)
///   Weight  → 0.8 + toplam (1.4..6)
/// </summary>
public static class DefaultPartCatalog
{
    public struct Def
    {
        public string id, name;
        public PartType type;
        public int tier, price;
        public float atk, def, stm, wgt;

        public Def(string id, string name, PartType type, int tier, int price, float atk, float def, float stm, float wgt)
        {
            this.id = id; this.name = name; this.type = type; this.tier = tier; this.price = price;
            this.atk = atk; this.def = def; this.stm = stm; this.wgt = wgt;
        }
    }

    public const string StarterLayer = "layer_basic";
    public const string StarterDisk = "disk_basic";
    public const string StarterTip = "tip_basic";

    public static readonly Def[] Definitions =
    {
        //            id               isim             tip                      tier fiyat  atk   def   stm  wgt
        // LAYER: çarpışma yüzeyi, karakteri belirler
        new Def("layer_basic",   "Çelik Kanat",  PartType.Layer,          1,   0,  2.5f, 0.15f, 1f,   0.6f),
        new Def("layer_ejderha", "Ejderha",      PartType.Layer,          1, 150,  5f,   0.05f, 0f,   0.5f),
        new Def("layer_kale",    "Kale",         PartType.Layer,          1, 150,  1f,   0.40f, 0.5f, 1.2f),
        new Def("layer_ruzgar",  "Rüzgar",       PartType.Layer,          1, 150,  1f,   0.10f, 3f,   0.3f),
        new Def("layer_yildirim","Yıldırım",     PartType.Layer,          2, 400,  6f,   0.10f, 0.5f, 0.8f),
        // DISK: ağırlık ve denge
        new Def("disk_basic",    "Standart Disk",PartType.WeightDisk,     1,   0,  0f,   0.10f, 1f,   1.0f),
        new Def("disk_agir",     "Ağır Disk",    PartType.WeightDisk,     1, 120,  0.5f, 0.20f, 0.5f, 2.5f),
        new Def("disk_hafif",    "Hafif Disk",   PartType.WeightDisk,     1, 120,  0.5f, 0.00f, 2f,   0.4f),
        // TIP: hareket ve dayanıklılık
        new Def("tip_basic",     "Denge Ucu",    PartType.PerformanceTip, 1,   0,  0.5f, 0.10f, 1.5f, 0f),
        new Def("tip_duz",       "Düz Uç",       PartType.PerformanceTip, 1, 100,  2f,   0.00f, 0f,   0f),
        new Def("tip_top",       "Top Uç",       PartType.PerformanceTip, 1, 100,  0f,   0.25f, 1f,   0.1f),
        new Def("tip_igne",      "İğne Uç",      PartType.PerformanceTip, 1, 100,  0f,   0.05f, 4f,   0f),
    };

    public static List<BeybladePart> CreateAll()
    {
        var list = new List<BeybladePart>(Definitions.Length);
        foreach (Def d in Definitions) list.Add(Create(d));
        return list;
    }

    public static BeybladePart Create(Def d)
    {
        BeybladePart p = ScriptableObject.CreateInstance<BeybladePart>();
        Apply(d, p);
        p.hideFlags = HideFlags.DontSave;
        return p;
    }

    public static void Apply(Def d, BeybladePart p)
    {
        p.name = d.name;
        p.partID = d.id;
        p.partName = d.name;
        p.type = d.type;
        p.tier = d.tier;
        p.price = d.price;
        p.attackBonus = d.atk;
        p.defenseBonus = d.def;
        p.staminaBonus = d.stm;
        p.weightBonus = d.wgt;
    }
}
