using UnityEngine;

/// <summary>
/// İmport stadium: collider + URP materyal + makul ölçek.
/// Orijinal asset görünümünü bozmadan oynanabilir hale getirir.
/// </summary>
public class ImportedArenaFixer : MonoBehaviour
{
    [Header("Otomatik Kurulum")]
    public bool fixRotation = true;
    public bool addColliders = true;
    public bool addSafetyFloor = true;
    public bool removeUnwantedCamerasAndLights = true;
    public bool fixMaterials = true;
    public bool tuneScale = true;
    public bool addVisualPolish = true;

    [Tooltip("Stadium world scale")]
    public float preferredScale = 0.78f;

    static Material floorMat;
    static Material wallMat;
    static Material rimMat;
    static Material accentMat;

    void Start()
    {
        FixArena();
    }

    [ContextMenu("Arenayı Şimdi Düzelt!")]
    public void FixArena()
    {
        if (fixRotation)
        {
            float x = transform.eulerAngles.x;
            bool alreadyFlat = Mathf.Abs(Mathf.DeltaAngle(x, 90f)) < 8f
                               || Mathf.Abs(Mathf.DeltaAngle(x, -90f)) < 8f
                               || Mathf.Abs(Mathf.DeltaAngle(x, 270f)) < 8f;
            if (!alreadyFlat && (x < 5f || x > 355f))
                transform.rotation = Quaternion.Euler(-90f, transform.eulerAngles.y, 0f);
        }

        if (tuneScale)
        {
            float s = preferredScale;
            Vector3 ls = transform.localScale;
            if (ls.x < 0.4f || ls.x > 1.15f || Mathf.Abs(ls.x - 0.5f) < 0.03f)
                transform.localScale = new Vector3(s, s, s);
        }

        if (addColliders)
        {
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;
                if (mf.GetComponent<Collider>() != null) continue;
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
                PhysicsMaterial arenaMat = new PhysicsMaterial("ArenaPhysics");
                arenaMat.bounciness = 0.05f;
                arenaMat.dynamicFriction = 0.12f;
                arenaMat.staticFriction = 0.12f;
                mc.material = arenaMat;
            }
        }

        if (fixMaterials)
            ApplyStadiumLook();

        if (addVisualPolish)
            SpawnPolishOverlays();

        if (addSafetyFloor)
        {
            GameObject existingFloor = GameObject.Find("GlobalSafetyFloor");
            if (existingFloor == null)
            {
                GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorObj.name = "GlobalSafetyFloor";
                floorObj.transform.position = new Vector3(0f, -0.85f, 0f);
                floorObj.transform.localScale = new Vector3(80f, 1f, 80f);
                var mr = floorObj.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
                PhysicsMaterial floorPhys = new PhysicsMaterial("FloorPhysics");
                floorPhys.dynamicFriction = 0.4f;
                floorPhys.staticFriction = 0.4f;
                floorObj.GetComponent<BoxCollider>().material = floorPhys;
            }
        }

        if (removeUnwantedCamerasAndLights)
        {
            foreach (Camera cam in GetComponentsInChildren<Camera>(true))
            {
                if (cam != null && cam.gameObject != gameObject)
                    Destroy(cam.gameObject);
            }
            foreach (Light l in GetComponentsInChildren<Light>(true))
            {
                if (l != null && l.type != LightType.Directional)
                    Destroy(l.gameObject);
            }
        }

        // Sahne ışığını arena için yumuşat
        Light sun = FindFirstObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.intensity = Mathf.Clamp(sun.intensity, 0.9f, 1.45f);
            sun.color = Color.Lerp(sun.color, new Color(1f, 0.96f, 0.9f), 0.35f);
            sun.shadows = LightShadows.Soft;
        }

        Camera mainCam = Camera.main;
        if (mainCam != null && RenderSettings.skybox == null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.04f, 0.05f, 0.08f);
        }
    }

    void ApplyStadiumLook()
    {
        EnsureMats();
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer mr in renderers)
        {
            if (mr == null) continue;
            if (mr.gameObject.name.StartsWith("ArenaPolish_")) continue;
            mr.SetPropertyBlock(null);

            string n = mr.gameObject.name.ToLower();
            Material use = ClassifyMat(n, mr.bounds);

            Material[] mats = mr.sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                mr.sharedMaterial = use;
                continue;
            }

            bool needsFix = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || mats[i].shader == null
                    || mats[i].shader.name.Contains("Error")
                    || mats[i].shader.name.Contains("Hidden/Internal"))
                {
                    needsFix = true;
                    break;
                }
                if (mats[i].shader.name == "Standard" || mats[i].shader.name.Contains("Legacy"))
                {
                    needsFix = true;
                    break;
                }
            }

            if (needsFix)
            {
                Material[] neu = new Material[mats.Length];
                for (int i = 0; i < neu.Length; i++) neu[i] = use;
                mr.sharedMaterials = neu;
            }
        }
    }

    Material ClassifyMat(string n, Bounds b)
    {
        if (n.Contains("line") || n.Contains("mark") || n.Contains("led") || n.Contains("neon") || n.Contains("light"))
            return accentMat;
        if (n.Contains("rim") || n.Contains("ring") || n.Contains("edge") || n.Contains("lip"))
            return rimMat;
        if (n.Contains("wall") || n.Contains("side") || n.Contains("barrier") || n.Contains("fence"))
            return wallMat;
        if (n.Contains("floor") || n.Contains("ground") || n.Contains("base") || n.Contains("plate") || n.Contains("bottom") || n.Contains("disk") || n.Contains("disc"))
            return floorMat;

        // Yükseklik oranı: alçak = zemin, ince dik = kenar, kalın dik = duvar
        float h = b.size.y;
        float w = Mathf.Max(b.size.x, b.size.z);
        if (h < 0.35f || h < w * 0.12f) return floorMat;
        if (h < w * 0.28f) return rimMat;
        return wallMat;
    }

    void SpawnPolishOverlays()
    {
        if (GameObject.Find("ArenaPolish_FloorGlow") != null) return;

        EnsureMats();

        // World-space: stadium X rot'undan etkilenmesin
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        floor.name = "ArenaPolish_FloorGlow";
        Object.Destroy(floor.GetComponent<Collider>());
        floor.transform.SetPositionAndRotation(new Vector3(0f, 0.015f, 0f), Quaternion.identity);
        floor.transform.localScale = new Vector3(9.4f, 0.012f, 9.4f);
        var fmr = floor.GetComponent<MeshRenderer>();
        if (fmr != null) fmr.sharedMaterial = floorMat;

        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "ArenaPolish_Rim";
        Object.Destroy(rim.GetComponent<Collider>());
        rim.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, 0f), Quaternion.identity);
        rim.transform.localScale = new Vector3(10.5f, 0.07f, 10.5f);
        var rmr = rim.GetComponent<MeshRenderer>();
        if (rmr != null) rmr.sharedMaterial = rimMat;

        GameObject neon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        neon.name = "ArenaPolish_Neon";
        Object.Destroy(neon.GetComponent<Collider>());
        neon.transform.SetPositionAndRotation(new Vector3(0f, 0.16f, 0f), Quaternion.identity);
        neon.transform.localScale = new Vector3(10.65f, 0.018f, 10.65f);
        var nmr = neon.GetComponent<MeshRenderer>();
        if (nmr != null) nmr.sharedMaterial = accentMat;
    }

    static void EnsureMats()
    {
        if (floorMat != null) return;
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");

        floorMat = new Material(lit);
        floorMat.name = "StadiumFloor_Runtime";
        // Koyu metalik zemin — gri plastik değil
        SetLitColor(floorMat, new Color(0.09f, 0.11f, 0.15f), 0.72f, 0.55f);

        wallMat = new Material(lit);
        wallMat.name = "StadiumWall_Runtime";
        // Koyu lacivert-gri duvar, parlak kırmızı blok değil
        SetLitColor(wallMat, new Color(0.16f, 0.12f, 0.18f), 0.45f, 0.2f);

        rimMat = new Material(lit);
        rimMat.name = "StadiumRim_Runtime";
        SetLitColor(rimMat, new Color(0.72f, 0.12f, 0.16f), 0.55f, 0.35f);

        accentMat = new Material(lit);
        accentMat.name = "StadiumAccent_Runtime";
        SetLitColor(accentMat, new Color(0.2f, 0.7f, 1f), 0.25f, 0.15f);
        if (accentMat.HasProperty("_EmissionColor"))
        {
            accentMat.EnableKeyword("_EMISSION");
            accentMat.SetColor("_EmissionColor", new Color(0.15f, 0.55f, 1f) * 1.4f);
        }
    }

    static void SetLitColor(Material m, Color c, float smooth, float metal)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
    }
}
