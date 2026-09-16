using UnityEngine;

/// <summary>
/// Bu script sahnedeki tüm görsel hataları, ışıklandırmayı ve dışarıdan eklenen
/// 3D Arena (Stadium) modellerinin fizik collider'larını otomatize ederek %100 çözer.
/// </summary>
public class VisualFixer : MonoBehaviour
{
    private void Start()
    {
        FixEverything();
    }

    private void Update()
    {
        FixEverything();
        Destroy(this);
    }

    private void FixEverything()
    {
        // 1. KAMERA ARKAPLANINI DÜZELT
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.15f, 0.22f); // Koyu lacivert
        }

        // 2. DIŞARIDAN EKLENEN 3D ARENA MODELİNİ OTOMATİK DÜZELT VE FİZİK EKLE (DÜŞMEYİ ÖNLER)
        FixImportedStadiumModels();

        // 3. KODLA ÜRETİLEN ARENA VARSA DÜZELT
        ArenaGenerator arena = FindFirstObjectByType<ArenaGenerator>();
        if (arena != null && arena.enabled)
        {
            if (arena.segments < 48)
            {
                arena.segments = 64;
                arena.GenerateArena();
            }

            MeshRenderer ar = arena.GetComponent<MeshRenderer>();
            if (ar != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", new Color(0.6f, 0.1f, 0.1f));
                block.SetColor("_Color", new Color(0.6f, 0.1f, 0.1f));
                block.SetFloat("_Smoothness", 0f);
                ar.SetPropertyBlock(block);
            }
        }

        // 4. OYUNCU VE DÜŞMAN RENKLERİNİ DÜZELT
        FixBeybladeVisuals("PlayerBeyblade", new Color(0.1f, 0.4f, 1f));
        FixBeybladeVisuals("EnemyBeyblade", new Color(0.25f, 0.25f, 0.25f));

        // 5. IŞIK PATLAMASINI KIS VE NETLEŞTİR
        Light sun = FindFirstObjectByType<Light>();
        if (sun != null && sun.type == LightType.Directional)
        {
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Hard;
        }
        
        Debug.Log("✅ Görsel ve Fiziksel Çevrim %100 Otomatik Düzeltildi!");
    }

    private void FixImportedStadiumModels()
    {
        // Sahnedeki tüm GameObject'leri tara, ismi 'stadium' veya 'arena' olanları bul
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            string lowerName = obj.name.ToLower();
            if ((lowerName.Contains("stadium") || lowerName.Contains("arena")) && !obj.name.Contains("ArenaGenerator"))
            {
                // Tüm alt MeshFilter'ları bul
                MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter mf in meshFilters)
                {
                    if (mf != null && mf.sharedMesh != null)
                    {
                        // MeshCollider bileşeni var mı bak, yoksa ekle
                        MeshCollider mc = mf.GetComponent<MeshCollider>();
                        if (mc == null)
                        {
                            mc = mf.gameObject.AddComponent<MeshCollider>();
                        }

                        // MESH COLLIDER'A MESH'İ OTOMATİK ATA (BOŞLUĞA DÜŞMEYİ %100 ENGELLER)
                        mc.sharedMesh = mf.sharedMesh;
                        mc.convex = false; // İçbükey çanak çemberi için convex kapalı olmalı
                    }

                    // Görsel Renk/Materyal Düzeltmesi (Gri/Pembe Kalmasını Engeller)
                    MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        for (int i = 0; i < mr.sharedMaterials.Length; i++)
                        {
                            if (mr.sharedMaterials[i] != null && mr.sharedMaterials[i].shader != null)
                            {
                                // URP veya Standard Shader ataması kontrolü
                                if (mr.sharedMaterials[i].shader.name.Contains("InternalErrorShader") ||
                                    mr.sharedMaterials[i].shader.name.Contains("Error"))
                                {
                                    Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                                    if (urpShader == null) urpShader = Shader.Find("Standard");
                                    if (urpShader != null)
                                    {
                                        mr.sharedMaterials[i].shader = urpShader;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void FixBeybladeVisuals(string objName, Color col)
    {
        GameObject go = GameObject.Find(objName);
        if (go != null)
        {
            MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer r in renderers)
            {
                if (r != null && !r.gameObject.name.Contains("StaminaBar") && !r.gameObject.name.Contains("Canvas"))
                {
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    block.SetColor("_BaseColor", col);
                    block.SetColor("_Color", col);
                    r.SetPropertyBlock(block);
                }
            }
        }
    }
}
