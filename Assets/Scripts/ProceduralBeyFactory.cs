using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using BeybladeCore;

/// <summary>
/// Koddan üretilen özgün bey/stadyum modellerini Unity nesnelerine çevirir.
/// Mesh'ler parça kimliğine göre, malzemeler renk+yuvaya göre önbelleklenir.
/// </summary>
public static class ProceduralBeyFactory
{
    static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
    static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();
    static Shader litShader;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        meshCache.Clear();
        matCache.Clear();
        litShader = null;
    }

    // ---------------------------------------------------------------- Mesh

    public static Mesh ToMesh(MeshData d, string name)
    {
        var mesh = new Mesh { name = name };
        if (d.VertexCount > 65000) mesh.indexFormat = IndexFormat.UInt32;
        var verts = new List<Vector3>(d.VertexCount);
        var norms = new List<Vector3>(d.VertexCount);
        for (int i = 0; i < d.VertexCount; i++)
        {
            Vec3 v = d.Vertices[i], n = d.Normals[i];
            verts.Add(new Vector3(v.X, v.Y, v.Z));
            norms.Add(new Vector3(n.X, n.Y, n.Z));
        }
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.subMeshCount = MeshData.SlotCount;
        for (int s = 0; s < MeshData.SlotCount; s++)
            mesh.SetTriangles(d.Triangles[s], s, false);
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh CachedMesh(string key, System.Func<MeshData> build)
    {
        if (meshCache.TryGetValue(key, out Mesh m) && m != null) return m;
        m = ToMesh(build(), key);
        meshCache[key] = m;
        return m;
    }

    // ---------------------------------------------------------------- Malzeme

    static Shader Lit
    {
        get
        {
            if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");
            return litShader;
        }
    }

    public static Color ToColor(RGBA c) => new Color(c.R, c.G, c.B, c.A);

    public static Material MaterialFor(MatSlot slot, RGBA rgba)
    {
        string key = (int)slot + "_" + rgba.R.ToString("0.000") + "_" + rgba.G.ToString("0.000") + "_" + rgba.B.ToString("0.000");
        if (matCache.TryGetValue(key, out Material mat) && mat != null) return mat;

        mat = new Material(Lit) { name = "Bey_" + slot };
        Color c = ToColor(rgba);
        float metal = slot == MatSlot.Metal ? 0.6f : 0.08f;
        float smooth = slot == MatSlot.Metal ? 0.65f : (slot == MatSlot.Dark ? 0.35f : 0.55f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metal);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smooth);
        if (slot == MatSlot.Accent && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", c * 0.55f);
        }
        matCache[key] = mat;
        return mat;
    }

    static Material[] MaterialsFor(BeyPalette p)
    {
        var mats = new Material[MeshData.SlotCount];
        for (int s = 0; s < MeshData.SlotCount; s++)
            mats[s] = MaterialFor((MatSlot)s, p[(MatSlot)s]);
        return mats;
    }

    static GameObject MakeRendererObject(string name, Transform parent, Mesh mesh, BeyPalette palette)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer r = go.AddComponent<MeshRenderer>();
        r.sharedMaterials = MaterialsFor(palette);
        r.shadowCastingMode = ShadowCastingMode.On;
        return go;
    }

    // ---------------------------------------------------------------- Bey

    /// <summary>
    /// Layer + disk + uç'tan bey kurar. Kökün yerel ekseninde layer yarıçapı = radius.
    /// contactY: ucun zemine değen noktasının köke göre yüksekliği (negatif).
    /// </summary>
    public static GameObject BuildBey(BeyVisual v, Transform parent, float radius, out float contactY)
    {
        var root = new GameObject("ProceduralBey");
        root.transform.SetParent(parent, false);
        root.transform.localScale = Vector3.one * radius;

        string lk = "L_" + v.Layer.Shape + v.Layer.Blades;
        string dk = "D_" + v.Disk.Shape;
        string tk = "T_" + v.Tip.Shape;
        Mesh layerMesh = CachedMesh(lk, () => BeyMeshBuilder.BuildLayer(v.Layer).Mesh);
        Mesh diskMesh = CachedMesh(dk, () => BeyMeshBuilder.BuildDisk(v.Disk).Mesh);
        Mesh tipMesh = CachedMesh(tk, () => BeyMeshBuilder.BuildTip(v.Tip).Mesh);

        MakeRendererObject("Layer", root.transform, layerMesh, BeyMeshBuilder.LayerPalette(v.Layer));
        MakeRendererObject("Disk", root.transform, diskMesh, BeyMeshBuilder.DiskPalette(v.Disk));
        MakeRendererObject("Tip", root.transform, tipMesh, BeyMeshBuilder.TipPalette(v.Tip));

        contactY = BeyMeshBuilder.TipContactY(v.Tip.Shape) * radius;
        return root;
    }

    public static BeyVisual VisualForLoadout(string layerId, string diskId, string tipId)
        => BeyDesignCatalog.Assemble(layerId, diskId, tipId);

    /// <summary>Tek parça (mağaza/beylocker ikonu için), orijin merkezde.</summary>
    public static GameObject BuildPart(PartType type, string partId, Transform parent, float radius)
    {
        var root = new GameObject("Part_" + partId);
        root.transform.SetParent(parent, false);
        root.transform.localScale = Vector3.one * radius;
        switch (type)
        {
            case PartType.Layer:
            {
                LayerVisual v = BeyDesignCatalog.Layer(partId);
                Mesh mesh = CachedMesh("L_" + v.Shape + v.Blades, () => BeyMeshBuilder.BuildLayer(v).Mesh);
                MakeRendererObject("Layer", root.transform, mesh, BeyMeshBuilder.LayerPalette(v));
                break;
            }
            case PartType.WeightDisk:
            {
                DiskVisual v = BeyDesignCatalog.Disk(partId);
                Mesh mesh = CachedMesh("D_" + v.Shape, () => BeyMeshBuilder.BuildDisk(v).Mesh);
                MakeRendererObject("Disk", root.transform, mesh, BeyMeshBuilder.DiskPalette(v));
                break;
            }
            default:
            {
                TipVisual v = BeyDesignCatalog.Tip(partId);
                Mesh mesh = CachedMesh("T_" + v.Shape, () => BeyMeshBuilder.BuildTip(v).Mesh);
                GameObject t = MakeRendererObject("Tip", root.transform, mesh, BeyMeshBuilder.TipPalette(v));
                // Uç yandan daha iyi okunur: biraz büyüt ve ortala
                t.transform.localScale = Vector3.one * 2.2f;
                t.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                break;
            }
        }
        return root;
    }

    // ---------------------------------------------------------------- Stadyum

    public static GameObject BuildStadium(Transform parent)
    {
        PartMesh pm = BeyMeshBuilder.BuildStadium();
        Mesh mesh = CachedMesh("Stadium", () => pm.Mesh);
        // Not: palet her seferinde hesaplanır (ucuz), mesh önbellekten gelir
        GameObject go = MakeRendererObject("Stadium", parent, mesh, pm.Palette);
        go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }
}
