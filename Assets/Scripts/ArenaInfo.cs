using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sahnedeki stadyumun merkez / yarıçap / taban yüksekliği. Sahne değişince yeniden hesaplanır.
/// </summary>
public static class ArenaInfo
{
    static bool computed;
    static bool known;
    static Vector3 center;
    static float radius;
    static float minY;

    public static bool Known { get { Ensure(); return known; } }
    public static Vector3 Center { get { Ensure(); return center; } }
    public static float Radius { get { Ensure(); return radius; } }
    public static float MinY { get { Ensure(); return minY; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        computed = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        computed = false;
    }

    static void Ensure()
    {
        if (computed) return;
        computed = true;
        known = false;
        center = Vector3.zero;
        radius = 0f;
        minY = 0f;

        ImportedArenaFixer fixer = Object.FindFirstObjectByType<ImportedArenaFixer>();
        if (fixer != null)
        {
            Renderer[] renderers = fixer.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds b = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r.gameObject.name.StartsWith("ArenaPolish_")) continue;
                if (!hasBounds) { b = r.bounds; hasBounds = true; }
                else b.Encapsulate(r.bounds);
            }
            if (hasBounds)
            {
                SetFromBounds(b);
                return;
            }
        }

        ArenaGenerator gen = Object.FindFirstObjectByType<ArenaGenerator>();
        if (gen != null)
        {
            Renderer r = gen.GetComponent<Renderer>();
            if (r != null) SetFromBounds(r.bounds);
        }
    }

    static void SetFromBounds(Bounds b)
    {
        center = new Vector3(b.center.x, 0f, b.center.z);
        radius = Mathf.Max(b.extents.x, b.extents.z);
        minY = b.min.y;
        known = radius > 0.5f;
    }
}
