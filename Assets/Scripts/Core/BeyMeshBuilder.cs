using System;
using System.Collections.Generic;

namespace BeybladeCore
{
    /// <summary>Bir parçanın mesh'i ve malzeme yuvalarının renkleri.</summary>
    public struct PartMesh
    {
        public MeshData Mesh;
        public BeyPalette Palette;
    }

    /// <summary>
    /// Özgün bey parçalarını koddan üretir (model dosyası gerekmez).
    /// Birim: layer dış yarıçapı 1. Layer altı y=0'da; disk altında, uç en altta.
    /// Unity tarafı ölçekleyip yerleştirir (ProceduralBey).
    /// </summary>
    public static class BeyMeshBuilder
    {
        const float TwoPi = (float)(Math.PI * 2.0);
        public const int OutlineSamples = 240;

        // =====================================================================
        // Layer
        // =====================================================================

        /// <summary>Layer'ın gövde (kanat dışı) yarıçapı.</summary>
        public static float LayerBody(LayerShape s)
        {
            switch (s)
            {
                case LayerShape.Claw: return 0.56f;
                case LayerShape.Shield: return 0.8f;
                case LayerShape.Wing: return 0.52f;
                case LayerShape.Bolt: return 0.58f;
                default: return 0.66f;
            }
        }

        /// <summary>Açıya göre layer kenar yarıçapı (0..1). Hücum kenarı sert, arka kenar yumuşak.</summary>
        public static float LayerOutline(LayerShape shape, int blades, float theta)
        {
            float u = Frac(theta / TwoPi * Math.Max(1, blades));
            float body = LayerBody(shape);
            switch (shape)
            {
                case LayerShape.Claw:
                {
                    float blade = Smooth(0f, 0.045f, u) * (1f - Smooth(0.07f, 0.5f, u));
                    float hook = Smooth(0f, 0.02f, u) * (1f - Smooth(0.02f, 0.09f, u));
                    return body + 0.38f * blade + 0.06f * hook;
                }
                case LayerShape.Shield:
                {
                    // Kalkan dilimleri: düz üst kenar, dilimler arasında belirgin oyuk
                    float lobe = Smooth(0.06f, 0.2f, u) * (1f - Smooth(0.8f, 0.94f, u));
                    return body - 0.06f + 0.2f * lobe;
                }
                case LayerShape.Wing:
                {
                    // Türbin kanadı: geniş kök, kıvrık ve incelen uç
                    float feather = Smooth(0f, 0.12f, u) * (1f - Smooth(0.24f, 0.7f, u));
                    float tip = Smooth(0f, 0.05f, u) * (1f - Smooth(0.05f, 0.2f, u));
                    return body + 0.4f * feather + 0.08f * tip;
                }
                case LayerShape.Bolt:
                {
                    float t1 = Smooth(0f, 0.035f, u) * (1f - Smooth(0.17f, 0.25f, u));
                    float t2 = Smooth(0.27f, 0.31f, u) * (1f - Smooth(0.4f, 0.5f, u));
                    return body + 0.4f * Math.Max(t1, 0.68f * t2);
                }
                default:
                {
                    float blade = Smooth(0f, 0.09f, u) * (1f - Smooth(0.3f, 0.6f, u));
                    return body + 0.32f * blade;
                }
            }
        }

        struct LayerStyle
        {
            public float CoreR, CoreAmp, CorePhase;   // gövde yarıçapı ve kanat yönündeki çıkıntı
            public float WingRoot, WingTip, Tilt;     // kanat plakası kalınlığı (kökte/uçta), hücum kenarı eğimi
            public float CoreTop, Bevel;              // gövde üst yüksekliği, pah genişliği
        }

        static LayerStyle StyleFor(LayerShape s)
        {
            switch (s)
            {
                case LayerShape.Claw: return new LayerStyle { CoreR = 0.5f, CoreAmp = 0.07f, CorePhase = 0.1f, WingRoot = 0.11f, WingTip = 0.035f, Tilt = 0.06f, CoreTop = 0.27f, Bevel = 0.07f };
                case LayerShape.Shield: return new LayerStyle { CoreR = 0.6f, CoreAmp = 0.05f, CorePhase = 0.5f, WingRoot = 0.16f, WingTip = 0.12f, Tilt = 0.0f, CoreTop = 0.31f, Bevel = 0.08f };
                case LayerShape.Wing: return new LayerStyle { CoreR = 0.44f, CoreAmp = 0.03f, CorePhase = 0.15f, WingRoot = 0.08f, WingTip = 0.025f, Tilt = 0.05f, CoreTop = 0.23f, Bevel = 0.05f };
                case LayerShape.Bolt: return new LayerStyle { CoreR = 0.5f, CoreAmp = 0.06f, CorePhase = 0.12f, WingRoot = 0.1f, WingTip = 0.04f, Tilt = 0.05f, CoreTop = 0.26f, Bevel = 0.06f };
                default: return new LayerStyle { CoreR = 0.54f, CoreAmp = 0.04f, CorePhase = 0.2f, WingRoot = 0.1f, WingTip = 0.045f, Tilt = 0.04f, CoreTop = 0.25f, Bevel = 0.06f };
            }
        }

        /// <summary>
        /// Katmanlı layer: altta kanat plakası (ikincil renk), ortada pahlı gövde (ana renk),
        /// üstte metal göbek + vurgu kapağı + amblem. Kanat kenarlarında vurgu şeridi.
        /// </summary>
        public static PartMesh BuildLayer(LayerVisual v)
        {
            var m = new MeshData();
            int n = OutlineSamples;
            int blades = Math.Max(1, v.Blades);
            float body = LayerBody(v.Shape);
            LayerStyle st = StyleFor(v.Shape);

            float[] rOut = new float[n], core = new float[n], uSeg = new float[n];
            float rMax = 0f;
            for (int i = 0; i < n; i++)
            {
                float a = i * TwoPi / n;
                rOut[i] = LayerOutline(v.Shape, blades, a);
                uSeg[i] = Frac(a / TwoPi * blades);
                float bump = 0.5f + 0.5f * (float)Math.Cos(TwoPi * (uSeg[i] - st.CorePhase));
                core[i] = Math.Min(st.CoreR + st.CoreAmp * bump, rOut[i] - 0.02f);
                rMax = Math.Max(rMax, rOut[i]);
            }

            // ---------- Kanat plakası ----------
            // Halkalar: gövdenin altından başlar, tam gövde kenarında bir halka (temiz renk sınırı),
            // sonra kanat, en dışta ince vurgu şeridi
            const float rim = 0.035f;
            float[] fr = { -1f, 0f, 0.33f, 0.66f, 0.999f, 1f }; // -1: gövde altı, 0: gövde kenarı, 1: dış kenar
            int K = fr.Length;
            var P = new Vec3[n, K];
            for (int i = 0; i < n; i++)
            {
                float a = i * TwoPi / n;
                float span = Math.Max(0f, rOut[i] - rim - core[i]);
                bool isBlade = rOut[i] > body + 0.02f;
                float lead = 1f - Smooth(0f, 0.5f, uSeg[i]);
                for (int k = 0; k < K; k++)
                {
                    float rr;
                    if (fr[k] < 0f) rr = core[i] * 0.85f;
                    else if (fr[k] >= 1f) rr = rOut[i];
                    else if (fr[k] >= 0.999f) rr = core[i] + span;
                    else rr = core[i] + span * fr[k];
                    float t = Clamp01((rr - core[i]) / Math.Max(0.05f, rMax - core[i]));
                    float y = st.WingTip + (st.WingRoot - st.WingTip) * (1f - t);
                    if (isBlade) y += st.Tilt * lead * t;
                    P[i, k] = Polar(rr, a, y);
                }
            }
            AddGridSurface(m, P, (i, k) =>
            {
                bool blade = Math.Max(rOut[i], rOut[(i + 1) % n]) > body + 0.02f;
                if (k == K - 2) return blade ? MatSlot.Accent : MatSlot.Secondary;
                return MatSlot.Secondary;
            }, Vec3.Up);
            AddSideWall(m, rOut, i => P[i, K - 1].Y, i => 0f, MatSlot.Dark);
            AddFlatAnnulus(m, 0.2f, rOut, 0f, false, MatSlot.Dark);

            // ---------- Gövde (pahlı) ----------
            float[] coreTopR = new float[n];
            for (int i = 0; i < n; i++) coreTopR[i] = core[i] - st.Bevel;
            float yBevel = st.CoreTop - st.Bevel * 0.7f;
            // Dikey gövde duvarı: kanat plakasından pah başlangıcına
            AddSideWall(m, core, i => yBevel, i => P[i, 1].Y - 0.002f, MatSlot.Primary);
            // Pah (eğimli halka)
            var B = new Vec3[n, 2];
            for (int i = 0; i < n; i++)
            {
                float a = i * TwoPi / n;
                B[i, 0] = Polar(core[i], a, yBevel);
                B[i, 1] = Polar(coreTopR[i], a, st.CoreTop);
            }
            AddGridSurface(m, B, (i, k) => MatSlot.Primary, Vec3.Up);
            // Üst yüz: hafif kubbe, göbeğe kadar
            const float hubR = 0.2f;
            var T = new Vec3[n, 3];
            for (int i = 0; i < n; i++)
            {
                float a = i * TwoPi / n;
                T[i, 0] = Polar(coreTopR[i], a, st.CoreTop);
                T[i, 1] = Polar((coreTopR[i] + hubR) * 0.5f, a, st.CoreTop + 0.02f);
                T[i, 2] = Polar(hubR, a, st.CoreTop + 0.03f);
            }
            AddGridSurface(m, T, (i, k) => k == 0 && uSeg[i] < 0.12f ? MatSlot.Accent : MatSlot.Primary, Vec3.Up);

            // ---------- Göbek + amblem ----------
            float hubTop = st.CoreTop + 0.09f;
            AddCylinder(m, hubR, st.CoreTop, hubTop, 48, MatSlot.Metal);
            AddDisc(m, hubR, hubTop, 48, true, hubR * 0.74f, MatSlot.Metal, MatSlot.Accent);
            AddStar(m, Math.Max(3, blades), 0.125f, 0.058f, hubTop + 0.002f, hubTop + 0.02f, MatSlot.Primary);

            return new PartMesh { Mesh = m, Palette = LayerPalette(v) };
        }

        public static BeyPalette LayerPalette(LayerVisual v)
            => new BeyPalette { Primary = v.Primary, Secondary = v.Secondary, Accent = v.Accent, Metal = RGBA.Hex("#C9D2DC"), Dark = BeyDesignCatalog.DarkPlastic };

        public static BeyPalette DiskPalette(DiskVisual v)
            => new BeyPalette { Metal = v.Metal, Dark = RGBA.Hex("#2B313C"), Primary = v.Metal, Secondary = v.Metal, Accent = v.Metal };

        public static BeyPalette TipPalette(TipVisual v)
            => new BeyPalette { Primary = v.Body, Secondary = v.Contact, Dark = BeyDesignCatalog.DarkPlastic, Metal = v.Contact, Accent = v.Body };

        /// <summary>
        /// [açı, halka] ızgarasından yüzey (açı yönünde kapalı). Normaller sayısal türevle hesaplanır
        /// ve 'outward' ile aynı yarıküreye çevrilir.
        /// </summary>
        static void AddGridSurface(MeshData m, Vec3[,] P, Func<int, int, MatSlot> slotOf, Vec3 outward)
        {
            int n = P.GetLength(0), K = P.GetLength(1);
            int[,] idx = new int[n, K];
            for (int i = 0; i < n; i++)
            {
                int ip = (i - 1 + n) % n, inx = (i + 1) % n;
                for (int k = 0; k < K; k++)
                {
                    int kp = Math.Max(0, k - 1), kn = Math.Min(K - 1, k + 1);
                    Vec3 du = P[inx, k] - P[ip, k];
                    Vec3 dv = P[i, kn] - P[i, kp];
                    Vec3 nrm = Vec3.Cross(du, dv);
                    if (nrm.Length < 1e-7f) nrm = outward;
                    if (Vec3.Dot(nrm, outward) < 0f) nrm = -nrm;
                    idx[i, k] = m.Vertex(P[i, k], nrm);
                }
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                for (int k = 0; k < K - 1; k++)
                    m.Quad(slotOf(i, k), idx[i, k], idx[j, k], idx[j, k + 1], idx[i, k + 1], outward);
            }
        }

        static float Clamp01(float x) => x < 0f ? 0f : (x > 1f ? 1f : x);

        // =====================================================================
        // Disk
        // =====================================================================

        public static float DiskOutline(DiskShape shape, float theta)
        {
            switch (shape)
            {
                case DiskShape.Heavy:
                {
                    float u = Frac(theta / TwoPi * 3f);
                    float lobe = 0.5f + 0.5f * (float)Math.Cos(TwoPi * (u - 0.5f));
                    float u2 = Frac(theta / TwoPi * 12f);
                    float notch = Smooth(0.4f, 0.46f, u2) * (1f - Smooth(0.54f, 0.6f, u2));
                    return 0.8f + 0.06f * (float)Math.Pow(lobe, 0.7f) - 0.035f * notch;
                }
                case DiskShape.Light:
                {
                    float u = Frac(theta / TwoPi * 8f);
                    float notch = Smooth(0.35f, 0.42f, u) * (1f - Smooth(0.58f, 0.65f, u));
                    return 0.75f - 0.12f * notch;
                }
                default:
                {
                    float u = Frac(theta / TwoPi * 6f);
                    float notch = Smooth(0.42f, 0.45f, u) * (1f - Smooth(0.55f, 0.58f, u));
                    return 0.8f - 0.06f * notch;
                }
            }
        }

        public static void DiskHeights(DiskShape s, out float topY, out float bottomY)
        {
            topY = -0.008f;
            switch (s)
            {
                case DiskShape.Heavy: bottomY = -0.125f; break;
                case DiskShape.Light: bottomY = -0.06f; break;
                default: bottomY = -0.09f; break;
            }
        }

        public static PartMesh BuildDisk(DiskVisual v)
        {
            var m = new MeshData();
            int n = OutlineSamples;
            const float inner = 0.27f;
            DiskHeights(v.Shape, out float yTop, out float yBot);

            float[] rOut = new float[n];
            for (int i = 0; i < n; i++) rOut[i] = DiskOutline(v.Shape, i * TwoPi / n);

            // Üst yüz: iç bant metal, ince koyu oyuk çizgi, dış bant metal
            float[] bands = { inner, 0.47f, 0.51f, -1f };
            int[,] grid = new int[n, bands.Length];
            for (int i = 0; i < n; i++)
            {
                float a = i * TwoPi / n;
                float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
                for (int k = 0; k < bands.Length; k++)
                {
                    float rr = bands[k] < 0f ? rOut[i] : Math.Min(bands[k], rOut[i] - 0.01f);
                    grid[i, k] = m.Vertex(new Vec3(rr * c, yTop, rr * s), Vec3.Up);
                }
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                for (int k = 0; k < bands.Length - 1; k++)
                    m.Quad(k == 1 ? MatSlot.Dark : MatSlot.Metal, grid[i, k], grid[j, k], grid[j, k + 1], grid[i, k + 1], Vec3.Up);
            }

            AddSideWall(m, rOut, i => yTop, i => yBot, MatSlot.Metal);
            AddFlatAnnulus(m, inner, rOut, yBot, false, MatSlot.Metal);
            AddCylinder(m, inner, yBot, yTop, 48, MatSlot.Dark, inward: true);

            return new PartMesh { Mesh = m, Palette = DiskPalette(v) };
        }

        // =====================================================================
        // Uç (performance tip) — torna profili
        // =====================================================================

        struct ProfilePoint
        {
            public float R, Y;
            public MatSlot Slot;
            public ProfilePoint(float r, float y, MatSlot s) { R = r; Y = y; Slot = s; }
        }

        static List<ProfilePoint> TipProfile(TipShape shape)
        {
            var p = new List<ProfilePoint>
            {
                new ProfilePoint(0.30f, -0.085f, MatSlot.Primary),
                new ProfilePoint(0.315f, -0.12f, MatSlot.Primary),
                new ProfilePoint(0.285f, -0.17f, MatSlot.Primary),
                new ProfilePoint(0.22f, -0.235f, MatSlot.Primary),
                new ProfilePoint(0.175f, -0.285f, MatSlot.Primary),
            };
            switch (shape)
            {
                case TipShape.Flat:
                    p.Add(new ProfilePoint(0.2f, -0.315f, MatSlot.Secondary));
                    p.Add(new ProfilePoint(0.2f, -0.37f, MatSlot.Secondary));
                    p.Add(new ProfilePoint(0.17f, -0.4f, MatSlot.Secondary));
                    p.Add(new ProfilePoint(0f, -0.4f, MatSlot.Secondary));
                    break;
                case TipShape.Ball:
                    for (int i = 0; i <= 8; i++)
                    {
                        float ang = i / 8f * (float)Math.PI * 0.5f;
                        p.Add(new ProfilePoint(0.13f * (float)Math.Cos(ang), -0.3f - 0.13f * (float)Math.Sin(ang), MatSlot.Secondary));
                    }
                    break;
                case TipShape.Needle:
                    p.Add(new ProfilePoint(0.11f, -0.33f, MatSlot.Secondary));
                    p.Add(new ProfilePoint(0.05f, -0.42f, MatSlot.Secondary));
                    p.Add(new ProfilePoint(0.012f, -0.47f, MatSlot.Secondary));
                    p.Add(new ProfilePoint(0f, -0.48f, MatSlot.Secondary));
                    break;
                default: // yarı düz
                    p.Add(new ProfilePoint(0.145f, -0.33f, MatSlot.Secondary));
                    p.Add(new ProfilePoint(0.115f, -0.385f, MatSlot.Secondary));
                    p.Add(new ProfilePoint(0.075f, -0.415f, MatSlot.Secondary));
                    p.Add(new ProfilePoint(0f, -0.425f, MatSlot.Secondary));
                    break;
            }
            return p;
        }

        /// <summary>Ucun en alt noktası (zemine değen yer), layer birimiyle.</summary>
        public static float TipContactY(TipShape shape)
        {
            List<ProfilePoint> p = TipProfile(shape);
            return p[p.Count - 1].Y;
        }

        public static PartMesh BuildTip(TipVisual v)
        {
            var m = new MeshData();
            List<ProfilePoint> prof = TipProfile(v.Shape);
            // Üst kapak (disk deliğinden görünmesin)
            AddDisc(m, prof[0].R, prof[0].Y, 48, true, 0f, MatSlot.Dark, MatSlot.Dark);
            Lathe(m, prof, 48);
            return new PartMesh { Mesh = m, Palette = TipPalette(v) };
        }

        // =====================================================================
        // Stadyum (menü arenası)
        // =====================================================================

        /// <summary>Stadyum zemininin yüksekliği (r: merkezden uzaklık, dünya birimi).</summary>
        public static float StadiumFloorY(float r) => 0.016f * r * r;

        public const float StadiumFloorRadius = 5.35f;

        public static PartMesh BuildStadium()
        {
            var m = new MeshData();
            var prof = new List<ProfilePoint>();
            // Zemin (merkezden kenara)
            prof.Add(new ProfilePoint(0f, StadiumFloorY(0f), MatSlot.Dark));
            for (int i = 1; i <= 24; i++)
            {
                float r = StadiumFloorRadius * i / 24f;
                prof.Add(new ProfilePoint(r, StadiumFloorY(r), MatSlot.Dark));
            }
            float rimBase = StadiumFloorY(StadiumFloorRadius);
            prof.Add(new ProfilePoint(5.45f, rimBase + 0.45f, MatSlot.Secondary));
            prof.Add(new ProfilePoint(5.52f, rimBase + 0.58f, MatSlot.Accent));
            prof.Add(new ProfilePoint(6.1f, rimBase + 0.64f, MatSlot.Secondary));
            prof.Add(new ProfilePoint(6.45f, rimBase + 0.45f, MatSlot.Secondary));
            prof.Add(new ProfilePoint(6.5f, -0.6f, MatSlot.Primary));
            // Profil merkezden dışa: zemin yukarı, iç duvar merkeze, dış etek dışa bakar
            Lathe(m, prof, 96);

            // Neon halkalar (zeminden biraz yukarıda)
            foreach (float r in new[] { 0.95f, 2.45f, 3.95f, 5.05f })
                AddFloorRing(m, r, r < 1f ? 0.045f : 0.035f, 128, MatSlot.Accent);
            // Merkez işareti
            AddFloorRing(m, 0.32f, 0.06f, 64, MatSlot.Metal);

            return new PartMesh
            {
                Mesh = m,
                Palette = new BeyPalette
                {
                    Dark = RGBA.Hex("#0B1324"),
                    Secondary = RGBA.Hex("#1C2B47"),
                    Accent = RGBA.Hex("#2FD3FF"),
                    Metal = RGBA.Hex("#FF4D6D"),
                    Primary = RGBA.Hex("#070B14")
                }
            };
        }

        static void AddFloorRing(MeshData m, float radius, float width, int segs, MatSlot slot)
        {
            int[] inner = new int[segs], outer = new int[segs];
            for (int i = 0; i < segs; i++)
            {
                float a = i * TwoPi / segs;
                float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
                float r0 = radius - width * 0.5f, r1 = radius + width * 0.5f;
                inner[i] = m.Vertex(new Vec3(r0 * c, StadiumFloorY(r0) + 0.012f, r0 * s), Vec3.Up);
                outer[i] = m.Vertex(new Vec3(r1 * c, StadiumFloorY(r1) + 0.012f, r1 * s), Vec3.Up);
            }
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % segs;
                m.Quad(slot, inner[i], inner[j], outer[j], outer[i], Vec3.Up);
            }
        }

        // =====================================================================
        // Ortak geometri yardımcıları
        // =====================================================================

        /// <summary>Kutupsal kenar boyunca dikey duvar; normaller kenar eğrisine dik (dışa).</summary>
        static void AddSideWall(MeshData m, float[] rOut, Func<int, float> topAt, Func<int, float> bottomAt, MatSlot slot)
        {
            int n = rOut.Length;
            int[] t = new int[n], b = new int[n];
            Vec3[] normals = new Vec3[n];
            for (int i = 0; i < n; i++)
            {
                int ip = (i - 1 + n) % n, inx = (i + 1) % n;
                Vec3 pPrev = Polar(rOut[ip], ip * TwoPi / n, 0f);
                Vec3 pNext = Polar(rOut[inx], inx * TwoPi / n, 0f);
                Vec3 tan = pNext - pPrev;
                normals[i] = new Vec3(tan.Z, 0f, -tan.X).Normalized;
                Vec3 radial = Polar(1f, i * TwoPi / n, 0f);
                // Kenar yönü θ artarken matematik yönünde: dış normal (tz, -tx). Emniyet için radyal ile kontrol.
                if (Vec3.Dot(normals[i], radial) < -0.2f) normals[i] = -normals[i];
            }
            for (int i = 0; i < n; i++)
            {
                Vec3 p = Polar(rOut[i], i * TwoPi / n, 0f);
                t[i] = m.Vertex(new Vec3(p.X, topAt(i), p.Z), normals[i]);
                b[i] = m.Vertex(new Vec3(p.X, bottomAt(i), p.Z), normals[i]);
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vec3 outward = (normals[i] + normals[j]).Normalized;
                m.Quad(slot, t[i], t[j], b[j], b[i], outward);
            }
        }

        /// <summary>Düz halka yüzey (iç yarıçap sabit, dış kenar kutupsal).</summary>
        static void AddFlatAnnulus(MeshData m, float inner, float[] rOut, float y, bool up, MatSlot slot)
        {
            int n = rOut.Length;
            Vec3 nrm = up ? Vec3.Up : Vec3.Down;
            int[] a = new int[n], b = new int[n];
            for (int i = 0; i < n; i++)
            {
                float ang = i * TwoPi / n;
                a[i] = m.Vertex(Polar(inner, ang, y), nrm);
                b[i] = m.Vertex(Polar(rOut[i], ang, y), nrm);
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                m.Quad(slot, a[i], a[j], b[j], b[i], nrm);
            }
        }

        static void AddCylinder(MeshData m, float r, float y0, float y1, int segs, MatSlot slot, bool inward = false)
        {
            int[] lo = new int[segs], hi = new int[segs];
            for (int i = 0; i < segs; i++)
            {
                float a = i * TwoPi / segs;
                Vec3 nrm = Polar(1f, a, 0f);
                if (inward) nrm = -nrm;
                lo[i] = m.Vertex(Polar(r, a, y0), nrm);
                hi[i] = m.Vertex(Polar(r, a, y1), nrm);
            }
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % segs;
                Vec3 outward = Polar(1f, (i + 0.5f) * TwoPi / segs, 0f);
                if (inward) outward = -outward;
                m.Quad(slot, lo[i], lo[j], hi[j], hi[i], outward);
            }
        }

        /// <summary>Daire kapak; innerR > 0 ise iç kısım farklı renkte (halka + merkez).</summary>
        static void AddDisc(MeshData m, float r, float y, int segs, bool up, float innerR, MatSlot ringSlot, MatSlot centerSlot)
        {
            Vec3 nrm = up ? Vec3.Up : Vec3.Down;
            int center = m.Vertex(new Vec3(0f, y, 0f), nrm);
            int[] ring = new int[segs], mid = new int[segs];
            for (int i = 0; i < segs; i++)
            {
                float a = i * TwoPi / segs;
                ring[i] = m.Vertex(Polar(r, a, y), nrm);
                if (innerR > 0f) mid[i] = m.Vertex(Polar(innerR, a, y), nrm);
            }
            for (int i = 0; i < segs; i++)
            {
                int j = (i + 1) % segs;
                if (innerR > 0f)
                {
                    m.Quad(ringSlot, mid[i], mid[j], ring[j], ring[i], nrm);
                    m.Tri(centerSlot, center, mid[i], mid[j], nrm);
                }
                else m.Tri(ringSlot, center, ring[i], ring[j], nrm);
            }
        }

        /// <summary>Kabartma yıldız amblem.</summary>
        static void AddStar(MeshData m, int points, float rOuter, float rInner, float y0, float y1, MatSlot slot)
        {
            int n = points * 2;
            float[] rr = new float[n];
            for (int i = 0; i < n; i++) rr[i] = i % 2 == 0 ? rOuter : rInner;
            int center = m.Vertex(new Vec3(0f, y1, 0f), Vec3.Up);
            int[] topRing = new int[n];
            for (int i = 0; i < n; i++)
                topRing[i] = m.Vertex(Polar(rr[i], i * TwoPi / n + 0.3f, y1), Vec3.Up);
            for (int i = 0; i < n; i++)
                m.Tri(slot, center, topRing[i], topRing[(i + 1) % n], Vec3.Up);
            // Yan kenarlar
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vec3 a = Polar(rr[i], i * TwoPi / n + 0.3f, 0f), b = Polar(rr[j], j * TwoPi / n + 0.3f, 0f);
                Vec3 edge = b - a;
                Vec3 outward = new Vec3(edge.Z, 0f, -edge.X).Normalized;
                Vec3 mid = (a + b) * 0.5f;
                if (Vec3.Dot(outward, mid) < 0f) outward = -outward;
                int t0 = m.Vertex(new Vec3(a.X, y1, a.Z), outward), t1 = m.Vertex(new Vec3(b.X, y1, b.Z), outward);
                int b0 = m.Vertex(new Vec3(a.X, y0, a.Z), outward), b1 = m.Vertex(new Vec3(b.X, y0, b.Z), outward);
                m.Quad(MatSlot.Dark, t0, t1, b1, b0, outward);
            }
        }

        /// <summary>Profili y ekseni etrafında döndürür. Profil noktaları sırasıyla birleşir; normaller dışa bakar.</summary>
        static void Lathe(MeshData m, List<ProfilePoint> prof, int segs)
        {
            int pc = prof.Count;
            if (pc < 2) return;
            // Segment normalleri (r-y düzleminde): d=(dr,dy) → n=(-dy, dr) dışa/aşağı
            var segN = new float[pc - 1, 2];
            for (int j = 0; j < pc - 1; j++)
            {
                float dr = prof[j + 1].R - prof[j].R, dy = prof[j + 1].Y - prof[j].Y;
                float nr = -dy, ny = dr;
                float l = (float)Math.Sqrt(nr * nr + ny * ny);
                if (l < 1e-6f) { nr = 1f; ny = 0f; l = 1f; }
                segN[j, 0] = nr / l;
                segN[j, 1] = ny / l;
            }

            for (int j = 0; j < pc - 1; j++)
            {
                // Uçlarda komşu segmentle yumuşat (açı küçükse)
                float n0r = segN[j, 0], n0y = segN[j, 1], n1r = n0r, n1y = n0y;
                if (j > 0 && segN[j - 1, 0] * segN[j, 0] + segN[j - 1, 1] * segN[j, 1] > 0.75f)
                { n0r = segN[j - 1, 0] + segN[j, 0]; n0y = segN[j - 1, 1] + segN[j, 1]; }
                if (j < pc - 2 && segN[j + 1, 0] * segN[j, 0] + segN[j + 1, 1] * segN[j, 1] > 0.75f)
                { n1r = segN[j + 1, 0] + segN[j, 0]; n1y = segN[j + 1, 1] + segN[j, 1]; }

                int[] ra = new int[segs], rb = new int[segs];
                for (int i = 0; i < segs; i++)
                {
                    float a = i * TwoPi / segs;
                    float c = (float)Math.Cos(a), s = (float)Math.Sin(a);
                    ra[i] = m.Vertex(new Vec3(prof[j].R * c, prof[j].Y, prof[j].R * s), new Vec3(n0r * c, n0y, n0r * s));
                    rb[i] = m.Vertex(new Vec3(prof[j + 1].R * c, prof[j + 1].Y, prof[j + 1].R * s), new Vec3(n1r * c, n1y, n1r * s));
                }
                MatSlot slot = prof[j + 1].Slot;
                for (int i = 0; i < segs; i++)
                {
                    int k = (i + 1) % segs;
                    float am = (i + 0.5f) * TwoPi / segs;
                    Vec3 outward = new Vec3(segN[j, 0] * (float)Math.Cos(am), segN[j, 1], segN[j, 0] * (float)Math.Sin(am));
                    m.Quad(slot, ra[i], ra[k], rb[k], rb[i], outward);
                }
            }
        }

        static Vec3 Polar(float r, float a, float y) => new Vec3(r * (float)Math.Cos(a), y, r * (float)Math.Sin(a));
        static float Frac(float x) => x - (float)Math.Floor(x);
        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        static float Smooth(float a, float b, float x)
        {
            if (b <= a) return x >= b ? 1f : 0f;
            float t = (x - a) / (b - a);
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return t * t * (3f - 2f * t);
        }
    }
}
