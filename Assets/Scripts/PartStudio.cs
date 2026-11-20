using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Canlı 3B ikon stüdyosu: parçaları/beyleri sahnenin çok altında bir ızgaraya dizer, tek bir kamera
/// hepsini bir RenderTexture atlasına çizer. UI'da RawImage + uvRect ile her hücre ayrı ikon olur.
/// Modeller yavaşça döner (mağaza ve Beylocker'da canlı görünür). Camera.Render() gerekmez.
/// </summary>
public class PartStudio : MonoBehaviour
{
    const int Grid = 4;
    const int CellPixels = 256;
    const float CellWorld = 2.6f;
    static readonly Vector3 StudioOrigin = new Vector3(0f, -2000f, 0f);

    static PartStudio instance;

    RenderTexture atlas;
    Camera cam;
    readonly Dictionary<string, int> cellOf = new Dictionary<string, int>();
    readonly Transform[] pivots = new Transform[Grid * Grid];
    readonly Transform[] spinners = new Transform[Grid * Grid];
    int nextCell;

    public static PartStudio Get()
    {
        if (instance != null) return instance;
        var go = new GameObject("PartStudio");
        go.transform.position = StudioOrigin;
        instance = go.AddComponent<PartStudio>();
        instance.Init();
        return instance;
    }

    public Texture Atlas => atlas;

    void Init()
    {
        atlas = new RenderTexture(Grid * CellPixels, Grid * CellPixels, 24, RenderTextureFormat.ARGB32)
        {
            name = "PartStudioAtlas",
            antiAliasing = 4
        };
        atlas.Create();

        var camGo = new GameObject("StudioCamera");
        camGo.transform.SetParent(transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0f, -20f);
        camGo.transform.localRotation = Quaternion.identity;
        cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = Grid * CellWorld * 0.5f;
        cam.aspect = 1f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.nearClipPlane = 1f;
        cam.farClipPlane = 40f;
        cam.targetTexture = atlas;
        cam.depth = -50f;

        AddLight(new Vector3(-4f, 6f, -10f), 3.5f, new Color(1f, 0.97f, 0.92f));
        AddLight(new Vector3(6f, -3f, -6f), 2f, new Color(0.45f, 0.8f, 1f));

        for (int i = 0; i < Grid * Grid; i++)
        {
            var pivot = new GameObject("Cell" + i).transform;
            pivot.SetParent(transform, false);
            int cx = i % Grid, cy = i / Grid;
            pivot.localPosition = new Vector3((cx - (Grid - 1) * 0.5f) * CellWorld, ((Grid - 1) * 0.5f - cy) * CellWorld, 0f);
            // Üstten 3/4 görünüm: model yukarı ekseni kameraya doğru yatırılır
            pivot.localRotation = Quaternion.Euler(-58f, 0f, 0f);
            pivots[i] = pivot;
            var spinner = new GameObject("Spin").transform;
            spinner.SetParent(pivot, false);
            spinners[i] = spinner;
        }
    }

    void AddLight(Vector3 localPos, float intensity, Color color)
    {
        var l = new GameObject("StudioLight").AddComponent<Light>();
        l.transform.SetParent(transform, false);
        l.transform.localPosition = localPos;
        l.type = LightType.Point;
        l.range = 60f;
        l.intensity = intensity;
        l.color = color;
    }

    /// <summary>Anahtar için hücre ayırır (yoksa build ile modeli kurar) ve atlas içindeki UV'yi döner.</summary>
    public Rect IconRect(string key, System.Func<Transform, GameObject> build, float tiltDegrees = -58f)
    {
        if (!cellOf.TryGetValue(key, out int cell))
        {
            cell = nextCell % (Grid * Grid);
            nextCell++;
            // Hücre doluysa (16'dan fazla ikon) en eskisini yeniden kullan
            foreach (var kv in new List<KeyValuePair<string, int>>(cellOf))
                if (kv.Value == cell) cellOf.Remove(kv.Key);
            Transform spinner = spinners[cell];
            for (int c = spinner.childCount - 1; c >= 0; c--) Destroy(spinner.GetChild(c).gameObject);
            build(spinner);
            pivots[cell].localRotation = Quaternion.Euler(tiltDegrees, 0f, 0f);
            cellOf[key] = cell;
        }
        int cx = cell % Grid, cy = cell / Grid;
        return new Rect(cx / (float)Grid, (Grid - 1 - cy) / (float)Grid, 1f / Grid, 1f / Grid);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < spinners.Length; i++)
            if (spinners[i] != null && spinners[i].childCount > 0)
                spinners[i].localRotation = Quaternion.Euler(0f, Time.unscaledTime * 35f + i * 40f, 0f);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        if (atlas != null) atlas.Release();
    }
}
