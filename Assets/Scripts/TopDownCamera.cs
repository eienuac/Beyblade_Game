using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Yayın (broadcast) tarzı savaş kamerası.
///  • Normal: oyuncunun arkasından rakibe bakar, iki beyi birlikte kadrajda tutar. Rastgele taraf
///    değiştirmez (yönlendirme kameraya göre olduğu için açı sakin ve öngörülebilir kalır).
///  • Fırlatma öncesi: arenayı yukarıdan gören genel plan.
///  • Çarpışma: hafif FOV vuruşu + sarsıntı (kadraj zıplamaz).
///  • Özel güç: yavaşça yakınlaşır ama iki bey de rahatça görünür; hafif yörünge + kısa ağır çekim.
///    Özellik bitince (EndSkillCinematic) yumuşakça normal kameraya döner.
/// </summary>
public class TopDownCamera : MonoBehaviour
{
    [Header("Takip Hedefleri")]
    public Transform playerTarget;
    public Transform enemyTarget;

    [Header("Normal Kadraj")]
    public float fieldOfView = 50f;
    [Tooltip("Beyler yan yanayken kamera uzaklığı")]
    public float frameMinDistance = 6.8f;
    public float frameMaxDistance = 11.5f;
    [Tooltip("Beyler arası mesafe başına ek uzaklık")]
    public float frameDistancePerMeter = 0.55f;
    [Tooltip("Yükseklik / uzaklık oranı (bakış açısı)")]
    public float framePitch = 0.62f;
    [Tooltip("Kadrajın oyuncuya kayması (0 = orta nokta, 1 = oyuncu)")]
    public float playerBias = 0.4f;
    public float shoulderOffset = 0.8f;
    public float positionSmoothTime = 0.32f;
    public float rotationSharpness = 6f;
    public float directionSharpness = 2.4f;

    [Header("Özel Güç Kadrajı")]
    [Tooltip("Normal uzaklığa göre yakınlaşma oranı (1 = hiç)")]
    public float skillZoom = 0.74f;
    [Tooltip("Ne kadar yakınlaşırsa yaklaşsın, iki beyi sığdırmak için bu kadar uzak kal")]
    public float skillMinDistance = 5.2f;
    public float skillPitch = 0.46f;
    public float skillFovDelta = -5f;
    public float skillOrbitDegPerSec = 7f;
    [Tooltip("Yakınlaşma hızı (1/sn) — düşük = daha yavaş")]
    public float skillBlendIn = 1.5f;
    public float skillBlendOut = 1.25f;
    public float skillSlowMo = 0.6f;
    public float skillSlowMoDuration = 0.35f;
    [Tooltip("Güvenlik: EndSkillCinematic gelmezse bu süre sonra çık")]
    public float skillMaxDuration = 9f;

    [Header("Sarsıntı")]
    public float shakeIntensity = 0.14f;
    public float shakeDuration = 0.14f;

    // Durum
    Camera cam;
    bool snapped;
    Vector3 velocityRef;
    Vector3 smoothedForward = Vector3.forward;
    Vector3 controlForward = Vector3.forward;
    float sideSign = 1f;
    float launchBlend;

    float currentShake, shakeTimer, fovPunch;

    Transform cineUser, cineOther;
    bool cineActive;
    float cineTimer, cineExitTimer = -1f, cineBlend, cineOrbitDeg, cineOrbitDir = 1f;
    float lastFinaleTime = -10f;

    float slowTimer;
    bool ownsTimeScale;

    Canvas barsCanvas;
    RectTransform barTop, barBottom;
    Image accentTop, accentBottom;
    TextMeshProUGUI cineTitle;

    BeybladeController playerBey, enemyBey;

    /// <summary>Yönlendirme için sabit referans; sinematik sırasında dönen kameradan etkilenmez.</summary>
    public Vector3 ControlForward => controlForward;
    public bool InCinematic => cineBlend > 0.01f;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
            cam.nearClipPlane = 0.12f;
        }
        sideSign = Random.value > 0.5f ? 1f : -1f;
        FindTargets();
    }

    void FindTargets()
    {
        if (playerTarget == null)
        {
            GameObject p = GameObject.Find("PlayerBeyblade");
            if (p != null) playerTarget = p.transform;
        }
        if (enemyTarget == null)
        {
            GameObject e = GameObject.Find("EnemyBeyblade");
            if (e != null) enemyTarget = e.transform;
        }
        if (playerTarget != null && playerBey == null) playerBey = playerTarget.GetComponent<BeybladeController>();
        if (enemyTarget != null && enemyBey == null) enemyBey = enemyTarget.GetComponent<BeybladeController>();
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        UpdateTimeFx(dt);

        if (playerTarget == null)
        {
            FindTargets();
            if (playerTarget == null) return;
        }

        Vector3 center = ArenaInfo.Known ? ArenaInfo.Center : Vector3.zero;
        float arenaR = ArenaInfo.Known ? ArenaInfo.Radius : 5.2f;
        Vector3 p = playerTarget.position;
        Vector3 e = enemyTarget != null ? enemyTarget.position : center;

        // ---- Bakış yönü: oyuncudan rakibe, yavaş dönen ----
        Vector3 toEnemy = e - p;
        toEnemy.y = 0f;
        float sep = toEnemy.magnitude;
        if (sep > 1.1f)
        {
            Vector3 desired = toEnemy / sep;
            smoothedForward = snapped
                ? Vector3.Slerp(smoothedForward, desired, 1f - Mathf.Exp(-directionSharpness * dt))
                : desired;
        }
        else if (!snapped)
        {
            Vector3 fromCenter = p - center;
            fromCenter.y = 0f;
            smoothedForward = fromCenter.sqrMagnitude > 0.05f ? -fromCenter.normalized : Vector3.forward;
        }
        Vector3 forward = smoothedForward.sqrMagnitude > 0.001f ? smoothedForward.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        // ---- Normal kadraj ----
        float dist = Mathf.Clamp(frameMinDistance + sep * frameDistancePerMeter, frameMinDistance, frameMaxDistance);
        Vector3 focus = Vector3.Lerp(p, e, 0.5f * (1f - playerBias));
        focus.y = Mathf.Lerp(p.y, e.y, 0.5f);
        Vector3 desiredPos = focus - forward * dist + right * (shoulderOffset * sideSign) + Vector3.up * (dist * framePitch);
        Vector3 lookPoint = Vector3.Lerp(p, e, 0.45f) + Vector3.up * 0.3f;

        // ---- Fırlatma öncesi genel plan ----
        bool launched = playerBey == null || playerBey.isLaunched;
        launchBlend = Mathf.MoveTowards(launchBlend, launched ? 1f : 0f, dt * 1.4f);
        if (launchBlend < 0.999f)
        {
            Vector3 spawnDir = p - center;
            spawnDir.y = 0f;
            spawnDir = spawnDir.sqrMagnitude > 0.05f ? spawnDir.normalized : -Vector3.forward;
            Vector3 overviewPos = center + spawnDir * (arenaR * 1.55f) + Vector3.up * (arenaR * 1.35f);
            Vector3 overviewLook = Vector3.Lerp(center, p, 0.2f);
            float k = Smooth(launchBlend);
            desiredPos = Vector3.Lerp(overviewPos, desiredPos, k);
            lookPoint = Vector3.Lerp(overviewLook, lookPoint, k);
        }

        // ---- Özel güç kadrajı ----
        UpdateCinematic(dt, p, e, forward, sep, dist, ref desiredPos, ref lookPoint);

        if (!snapped)
        {
            transform.position = desiredPos;
            transform.rotation = Quaternion.LookRotation(lookPoint - desiredPos, Vector3.up);
            snapped = true;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocityRef, positionSmoothTime, Mathf.Infinity, dt);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookPoint - transform.position, Vector3.up),
                1f - Mathf.Exp(-rotationSharpness * dt));
        }

        // ---- Sarsıntı ----
        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            float falloff = Mathf.Clamp01(shakeTimer / Mathf.Max(0.01f, shakeDuration));
            float t = Time.unscaledTime;
            transform.position += transform.right * (Mathf.Sin(t * 38f) * currentShake * falloff)
                                  + Vector3.up * (Mathf.Cos(t * 29f) * currentShake * 0.35f * falloff);
        }

        if (cam != null)
        {
            fovPunch = Mathf.MoveTowards(fovPunch, 0f, dt * 3.5f);
            cam.fieldOfView = fieldOfView + skillFovDelta * Smooth(cineBlend) - fovPunch * 4f;
        }

        if (cineBlend < 0.01f)
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f) controlForward = f.normalized;
        }

        UpdateBars();
    }

    void UpdateCinematic(float dt, Vector3 p, Vector3 e, Vector3 forward, float sep, float normalDist,
                         ref Vector3 desiredPos, ref Vector3 lookPoint)
    {
        if (cineActive)
        {
            cineTimer += dt;
            if (cineUser == null || cineTimer > skillMaxDuration) cineActive = false;
            else if (cineExitTimer >= 0f)
            {
                cineExitTimer -= dt;
                if (cineExitTimer < 0f) cineActive = false;
            }
        }

        float rate = cineActive ? skillBlendIn : skillBlendOut;
        cineBlend = Mathf.MoveTowards(cineBlend, cineActive ? 1f : 0f, dt * rate);
        if (cineBlend <= 0.0001f) return;

        // İki beyin orta noktası; yakınlaşma sınırlı → ikisi de kadrajda
        Vector3 mid = Vector3.Lerp(p, e, 0.5f);
        float zoomDist = Mathf.Max(normalDist * skillZoom, skillMinDistance + sep * 0.5f);
        zoomDist = Mathf.Min(zoomDist, normalDist);

        cineOrbitDeg += dt * skillOrbitDegPerSec * cineOrbitDir;
        Vector3 viewDir = Quaternion.AngleAxis(cineOrbitDeg, Vector3.up) * forward;
        Vector3 right = Vector3.Cross(Vector3.up, viewDir).normalized;

        Vector3 cinePos = mid - viewDir * zoomDist + right * (shoulderOffset * 0.6f * sideSign) + Vector3.up * (zoomDist * skillPitch);
        Vector3 cineLook = mid + Vector3.up * 0.25f;

        float k = Smooth(cineBlend);
        desiredPos = Vector3.Lerp(desiredPos, cinePos, k);
        lookPoint = Vector3.Lerp(lookPoint, cineLook, k);
    }

    static float Smooth(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // ================= Dış API =================

    /// <summary>Özel güç başlayınca çağrılır. holdUntilFinale geriye dönük uyumluluk içindir.</summary>
    public void BeginSkillCinematic(Transform user, Transform other, Color accent, string title, bool holdUntilFinale = false)
    {
        if (user == null) return;
        cineUser = user;
        cineOther = other;
        cineActive = true;
        cineTimer = 0f;
        cineExitTimer = -1f;
        if (cineBlend < 0.05f)
        {
            cineOrbitDir = Random.value > 0.5f ? 1f : -1f;
            cineOrbitDeg = 0f;
        }

        EnsureBars();
        accentTop.color = accent;
        accentBottom.color = accent;
        cineTitle.text = title;
        cineTitle.color = Color.Lerp(accent, Color.white, 0.35f);

        SetSlowMo(skillSlowMo, skillSlowMoDuration);
    }

    /// <summary>Özel güç bitti: kamera yumuşakça normal kadraja döner.</summary>
    public void EndSkillCinematic(Transform user)
    {
        if (!cineActive || user != cineUser) return;
        // Ulti finali yeni olduysa darbeyi gösterecek kadar bekle
        bool finaleJustHappened = Time.unscaledTime - lastFinaleTime < 0.15f;
        cineExitTimer = finaleJustHappened ? 0.55f : 0.05f;
    }

    /// <summary>İki beyblade çarpıştığında çağrılır.</summary>
    public void NotifyBeyImpact(Transform a, Transform b)
    {
        if (!cineActive) return;
        fovPunch = Mathf.Max(fovPunch, 0.5f);
    }

    /// <summary>Ulti finali: kısa hit-stop + sarsıntı. Çıkışı EndSkillCinematic yönetir.</summary>
    public void CinematicFinale(Transform user, bool impact)
    {
        if (!cineActive || user != cineUser || !impact) return;
        lastFinaleTime = Time.unscaledTime;
        fovPunch = 1.3f;
        currentShake = 0.32f;
        shakeTimer = shakeDuration * 2.6f;
        SetSlowMo(0.12f, 0.14f);
    }

    public void TriggerShake(float intensity = -1f)
    {
        if (intensity < 0f) intensity = shakeIntensity;
        // Devam eden daha güçlü sarsıntıyı ezme
        if (shakeTimer <= 0f || intensity >= currentShake)
        {
            currentShake = intensity;
            shakeTimer = shakeDuration;
        }
        fovPunch = Mathf.Max(fovPunch, Mathf.Clamp01(intensity * 4f));
    }

    /// <summary>Kamerayı oyuncunun diğer omzuna alır (artık otomatik çağrılmaz).</summary>
    public void FlipSide() => sideSign = -sideSign;

    // ================= Zaman efekti =================

    void SetSlowMo(float scale, float realDuration)
    {
        if (MatchContext.IsBalanceTest) return;
        Time.timeScale = Mathf.Clamp(scale, 0.05f, 1f);
        slowTimer = realDuration;
        ownsTimeScale = true;
    }

    void UpdateTimeFx(float dt)
    {
        if (!ownsTimeScale) return;
        if (slowTimer > 0f)
        {
            slowTimer -= dt;
            return;
        }
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, dt * 2.5f);
        if (Time.timeScale >= 1f) ownsTimeScale = false;
    }

    // ================= Sinema şeritleri =================

    void EnsureBars()
    {
        if (barsCanvas != null) return;
        ModernUIKit.EnsureFonts();
        barsCanvas = ModernUIKit.CreateCanvas("CinematicBars", 180);
        barsCanvas.GetComponent<GraphicRaycaster>().enabled = false;

        barTop = MakeBar("Top", true, out accentTop);
        barBottom = MakeBar("Bottom", false, out accentBottom);

        cineTitle = ModernUIKit.Label(barBottom, "Title", "", 28, Color.white, ModernUIKit.FontRole.Display);
        cineTitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
        cineTitle.characterSpacing = 14f;
        cineTitle.raycastTarget = false;
        ModernUIKit.Stretch(cineTitle.rectTransform, 0, 0, 8, 0);
    }

    RectTransform MakeBar(string name, bool top, out Image accent)
    {
        Image bar = ModernUIKit.MakeImage(barsCanvas.transform, "Bar" + name, new Color(0f, 0f, 0f, 0.92f), null, false);
        bar.sprite = null;
        bar.raycastTarget = false;
        RectTransform rt = bar.rectTransform;
        float y = top ? 1f : 0f;
        ModernUIKit.Anchor(rt, new Vector2(0f, y), new Vector2(1f, y), new Vector2(0.5f, y), Vector2.zero, new Vector2(0f, 0f));

        accent = ModernUIKit.MakeImage(rt, "Accent", ModernUIKit.Cyan, null, false);
        accent.sprite = null;
        accent.raycastTarget = false;
        float edge = top ? 0f : 1f;
        ModernUIKit.Anchor(accent.rectTransform, new Vector2(0f, edge), new Vector2(1f, edge), new Vector2(0.5f, edge), Vector2.zero, new Vector2(0f, 2f));
        return rt;
    }

    void UpdateBars()
    {
        if (barsCanvas == null) return;
        bool visible = cineBlend > 0.01f;
        if (barsCanvas.gameObject.activeSelf != visible) barsCanvas.gameObject.SetActive(visible);
        if (!visible) return;

        float h = 64f * Smooth(cineBlend);
        barTop.sizeDelta = new Vector2(0f, h);
        barBottom.sizeDelta = new Vector2(0f, h);
        Color c = cineTitle.color;
        cineTitle.color = new Color(c.r, c.g, c.b, Smooth(cineBlend));
    }

    void OnDisable()
    {
        if (ownsTimeScale) Time.timeScale = 1f;
        ownsTimeScale = false;
    }

    void OnDestroy()
    {
        if (barsCanvas != null) Destroy(barsCanvas.gameObject);
    }
}
