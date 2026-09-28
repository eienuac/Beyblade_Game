using UnityEngine;

/// <summary>
/// Sadece fizik/collider ve bozuk shader düzeltmesi.
/// Orijinal beyblade ve arena materyallerine dokunmaz.
/// </summary>
public class VisualFixer : MonoBehaviour
{
    private void Start()
    {
        FixEverything();
        Destroy(this);
    }

    private void FixEverything()
    {
        Camera cam = Camera.main;
        if (cam != null && cam.clearFlags == CameraClearFlags.Skybox)
        {
            // Skybox yoksa koyu arka plan; skybox varsa bırak
            if (RenderSettings.skybox == null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.1f, 0.14f);
            }
        }

        FixImportedStadiumModels();
        ClearForcedBeybladeTints("PlayerBeyblade");
        ClearForcedBeybladeTints("EnemyBeyblade");

        Light sun = FindFirstObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            if (sun.intensity > 2.5f) sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
        }
    }

    private void FixImportedStadiumModels()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            string lowerName = obj.name.ToLower();
            if (!(lowerName.Contains("stadium") || lowerName.Contains("arena")) || obj.name.Contains("ArenaGenerator"))
                continue;

            MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;

                MeshCollider mc = mf.GetComponent<MeshCollider>();
                if (mc == null)
                    mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;

                // Sadece kırık shader'ı düzelt — renk/materyale dokunma
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null) continue;
                Material[] mats = mr.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || mats[i].shader == null) continue;
                    string sn = mats[i].shader.name;
                    if (sn.Contains("InternalErrorShader") || sn.Contains("Error") || sn.Contains("Hidden/InternalError"))
                    {
                        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
                        if (urp == null) urp = Shader.Find("Standard");
                        if (urp != null)
                        {
                            mats[i].shader = urp;
                            changed = true;
                        }
                    }
                }
                if (changed) mr.sharedMaterials = mats;
            }
        }
    }

    /// <summary>Eski VisualFixer'ın PropertyBlock ile ezdiği renkleri temizle.</summary>
    private void ClearForcedBeybladeTints(string objName)
    {
        GameObject go = GameObject.Find(objName);
        if (go == null) return;

        MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer r in renderers)
        {
            if (r == null) continue;
            if (r.gameObject.name.Contains("StaminaBar") || r.gameObject.name.Contains("Canvas")) continue;
            if (r.gameObject.name.StartsWith("Trail") || r.gameObject.name.StartsWith("Motion")) continue;
            r.SetPropertyBlock(null);
        }
    }
}
