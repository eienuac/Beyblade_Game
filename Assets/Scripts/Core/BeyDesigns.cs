namespace BeybladeCore
{
    /// <summary>Layer (çarpışma halkası) siluetleri — hepsi bu oyuna özgü tasarımlar.</summary>
    public enum LayerShape
    {
        /// <summary>Dengeli 4 kanat: orta boy, düzgün.</summary>
        Balanced,
        /// <summary>3 kıvrık pençe: uzun, keskin hücum kenarı.</summary>
        Claw,
        /// <summary>6 yuvarlak kalkan dilimi: kalın, dairesel, savrulmaz.</summary>
        Shield,
        /// <summary>8 ince tüy-kanat: hafif, aerodinamik.</summary>
        Wing,
        /// <summary>5 zikzak şimşek dişi.</summary>
        Bolt
    }

    public enum DiskShape { Standard, Heavy, Light }
    public enum TipShape { SemiFlat, Flat, Ball, Needle }

    public struct BeyPalette
    {
        public RGBA Primary, Secondary, Metal, Accent, Dark;

        public RGBA this[MatSlot s]
        {
            get
            {
                switch (s)
                {
                    case MatSlot.Primary: return Primary;
                    case MatSlot.Secondary: return Secondary;
                    case MatSlot.Metal: return Metal;
                    case MatSlot.Accent: return Accent;
                    default: return Dark;
                }
            }
        }
    }

    public struct LayerVisual
    {
        public LayerShape Shape;
        public int Blades;
        public RGBA Primary, Secondary, Accent;
    }

    public struct DiskVisual
    {
        public DiskShape Shape;
        public RGBA Metal;
    }

    public struct TipVisual
    {
        public TipShape Shape;
        public RGBA Body, Contact;
    }

    /// <summary>Tam bir beyin görünümü (layer + disk + uç).</summary>
    public struct BeyVisual
    {
        public LayerVisual Layer;
        public DiskVisual Disk;
        public TipVisual Tip;
    }

    /// <summary>
    /// Parça kimliği → görünüm. Yeni parça eklersen buraya bir satır ekle;
    /// eşleşme yoksa parçanın türüne göre makul bir varsayılan döner.
    /// </summary>
    public static class BeyDesignCatalog
    {
        public static readonly RGBA DarkPlastic = RGBA.Hex("#161B26");

        public static LayerVisual Layer(string partId)
        {
            switch (partId)
            {
                case "layer_ejderha": return L(LayerShape.Claw, 3, "#D62839", "#F2B134", "#FF6B1A");
                case "layer_kale": return L(LayerShape.Shield, 6, "#2EC4F1", "#1B3A66", "#9BF6FF");
                case "layer_ruzgar": return L(LayerShape.Wing, 6, "#22C55E", "#E8FFF0", "#B6FF3B");
                case "layer_yildirim": return L(LayerShape.Bolt, 5, "#7B2FF7", "#FFD60A", "#FFF275");
                default: return L(LayerShape.Balanced, 4, "#2F6FDB", "#D0D8E0", "#3FD8FF"); // layer_basic: Çelik Kanat
            }
        }

        public static DiskVisual Disk(string partId)
        {
            switch (partId)
            {
                case "disk_agir": return new DiskVisual { Shape = DiskShape.Heavy, Metal = RGBA.Hex("#9AA4B2") };
                case "disk_hafif": return new DiskVisual { Shape = DiskShape.Light, Metal = RGBA.Hex("#E3E9F0") };
                default: return new DiskVisual { Shape = DiskShape.Standard, Metal = RGBA.Hex("#AEB8C4") };
            }
        }

        public static TipVisual Tip(string partId)
        {
            switch (partId)
            {
                case "tip_duz": return T(TipShape.Flat, "#FF7A1A", "#2A2F3A");
                case "tip_top": return T(TipShape.Ball, "#14B8A6", "#E8F1F2");
                case "tip_igne": return T(TipShape.Needle, "#A855F7", "#E8E8F0");
                default: return T(TipShape.SemiFlat, "#64748B", "#E2E8F0"); // tip_basic: Denge Ucu
            }
        }

        public static BeyVisual Assemble(string layerId, string diskId, string tipId)
            => new BeyVisual { Layer = Layer(layerId), Disk = Disk(diskId), Tip = Tip(tipId) };

        /// <summary>Parçasız (bot) beyler için tipe uygun parça seti.</summary>
        public static void DefaultLoadout(BeyArchetype type, out string layer, out string disk, out string tip)
        {
            switch (type)
            {
                case BeyArchetype.Attack: layer = "layer_ejderha"; disk = "disk_hafif"; tip = "tip_duz"; break;
                case BeyArchetype.Defense: layer = "layer_kale"; disk = "disk_agir"; tip = "tip_top"; break;
                case BeyArchetype.Stamina: layer = "layer_ruzgar"; disk = "disk_hafif"; tip = "tip_igne"; break;
                default: layer = "layer_basic"; disk = "disk_basic"; tip = "tip_basic"; break;
            }
        }

        public static BeyVisual ForArchetype(BeyArchetype type)
        {
            DefaultLoadout(type, out string l, out string d, out string t);
            return Assemble(l, d, t);
        }

        static LayerVisual L(LayerShape s, int blades, string p, string sec, string acc)
            => new LayerVisual { Shape = s, Blades = blades, Primary = RGBA.Hex(p), Secondary = RGBA.Hex(sec), Accent = RGBA.Hex(acc) };

        static TipVisual T(TipShape s, string body, string contact)
            => new TipVisual { Shape = s, Body = RGBA.Hex(body), Contact = RGBA.Hex(contact) };
    }
}
