using UnityEngine;

/// <summary>
/// Oyuncunun atılma yönünü yerde gösteren ok.
/// </summary>
public class DashAimIndicator : MonoBehaviour
{
    LineRenderer lr;
    float alpha;
    float targetAlpha;
    Color color = Color.white;

    public static DashAimIndicator Create()
    {
        GameObject go = new GameObject("DashAimIndicator");
        go.transform.rotation = Quaternion.LookRotation(Vector3.up);
        DashAimIndicator ind = go.AddComponent<DashAimIndicator>();
        ind.Build();
        return ind;
    }

    void Build()
    {
        lr = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.TransformZ;
        lr.positionCount = 2;
        lr.numCapVertices = 0;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        AnimationCurve width = new AnimationCurve(
            new Keyframe(0f, 0.16f),
            new Keyframe(0.68f, 0.16f),
            new Keyframe(0.69f, 0.62f),
            new Keyframe(1f, 0f));
        for (int i = 0; i < width.length; i++)
            SetLinear(width, i);
        lr.widthCurve = width;
        lr.widthMultiplier = 1f;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material mat = new Material(shader);
        mat.renderQueue = 3100;
        lr.material = mat;
        lr.enabled = false;
    }

    static void SetLinear(AnimationCurve c, int i)
    {
        Keyframe k = c[i];
        if (i > 0)
        {
            Keyframe p = c[i - 1];
            k.inTangent = (k.value - p.value) / Mathf.Max(0.0001f, k.time - p.time);
        }
        if (i < c.length - 1)
        {
            Keyframe n = c[i + 1];
            k.outTangent = (n.value - k.value) / Mathf.Max(0.0001f, n.time - k.time);
        }
        c.MoveKey(i, k);
    }

    /// <param name="strength">0 = gizli, ~0.35 = hazır, 1 = nişan alınıyor</param>
    public void SetAim(Vector3 origin, Vector3 dir, float length, float strength, Color c)
    {
        targetAlpha = strength;
        color = c;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) { targetAlpha = 0f; return; }
        dir.Normalize();
        Vector3 from = origin + dir * 0.55f;
        lr.SetPosition(0, from);
        lr.SetPosition(1, from + dir * length);
    }

    void LateUpdate()
    {
        alpha = Mathf.MoveTowards(alpha, targetAlpha, Time.deltaTime * 5f);
        lr.enabled = alpha > 0.01f;
        if (!lr.enabled) return;
        Color start = new Color(color.r, color.g, color.b, alpha * 0.25f);
        Color end = new Color(color.r, color.g, color.b, alpha);
        lr.startColor = start;
        lr.endColor = end;
    }
}
