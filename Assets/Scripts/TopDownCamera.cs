using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Oyuncu Beyblade'inin arkasından aksiyon kamerası.
/// Normalde biraz uzak; çarpışmada kısa süre yakınlaşır.
/// Özel yetenekte ilk çarpışmaya kadar sinematik moda geçer.
/// </summary>
public class TopDownCamera : MonoBehaviour
{
    [Header("Takip Hedefleri")]
    public Transform playerTarget;
    public Transform enemyTarget;

    [Header("Kilitli Takip")]
    public float backDistance = 7.4f;
    public float height = 3.6f;
    public float sideAmplitude = 2.35f;
    public float followSpeed = 5.4f;
    public float rotationSpeed = 8f;
    public float lookAhead = 0.38f;
    public float fieldOfView = 48f;

    [Header("Çarpışma Zoom")]
    public float impactZoomDistance = 5.2f;
    public float impactZoomHeight = 2.7f;
    public float impactZoomDuration = 0.55f;

    [Header("Sarsılma Efekti")]
    public float shakeIntensity = 0.14f;
    public float shakeDuration = 0.14f;

    [Header("Yetenek Sineması")]
    public float cineMaxDuration = 3.2f;
    public float cineDistance = 2.8f;
    public float cineHeight = 1.25f;
    public float cineFov = 36f;
    public float cineStartSlowMo = 0.35f;

    private float currentShake = 0f;
    private float shakeTimer = 0f;
    private float impactZoomTimer = 0f;
    private Vector3 smoothedForward = Vector3.forward;
    private Vector3 velocityRef;
    private Camera cam;
    private bool snapped;

    private float targetSide = 1f;
    private float currentSide = 1f;
    private float sideTimer = 5f;
    private float heightBob;
    private float zoomBlend;

    private Transform cineFocus;
    private Transform cineOther;
    private bool cineActive;
    private bool cineHold;
    private float cineTimer;
    private float cineExitTimer = -1f;
    private float cineBlend;
    private float cineOrbitDeg;
    private float cineOrbitDir = 1f;
    private float fovPunch;
    private float slowTimer;
    private bool ownsTimeScale;
    private Vector3 controlForward = Vector3.forward;

    private Canvas barsCanvas;
    private RectTransform barTop;
    private RectTransform barBottom;
    private Image accentTop;
    private Image accentBottom;
    private TextMeshProUGUI cineTitle;

    /// <summary>Yönlendirme için sabit referans; sinematik sırasında dönen kameradan etkilenmez.</summary>
    public Vector3 ControlForward => controlForward;
    public bool InCinematic => cineBlend > 0.01f;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
            cam.nearClipPlane = 0.12f;
        }
        targetSide = Random.value > 0.5f ? 1f : -1f;
        currentSide = targetSide;
        sideTimer = Random.Range(4.2f, 7.5f);
        FindTargets();
    }

    private void FindTargets()
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
    }

    private void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        UpdateTimeFx(dt);

        if (playerTarget == null)
        {
            FindTargets();
            if (playerTarget == null) return;
        }

        sideTimer -= dt;
        if (sideTimer <= 0f) FlipSide();

        if (impactZoomTimer > 0f)
            impactZoomTimer -= dt;

        float wantZoom = impactZoomTimer > 0f ? 1f : 0f;
        zoomBlend = Mathf.Lerp(zoomBlend, wantZoom, 1f - Mathf.Exp(-6f * dt));

        currentSide = Mathf.Lerp(currentSide, targetSide, 1f - Mathf.Exp(-1.35f * dt));
        heightBob = Mathf.Lerp(heightBob, Mathf.Sin(Time.unscaledTime * 0.35f) * 0.14f, dt * 2f);

        Vector3 playerPos = playerTarget.position;
        Vector3 enemyPos = enemyTarget != null ? enemyTarget.position : playerPos + Vector3.forward * 5f;

        Vector3 toEnemy = enemyPos - playerPos;
        toEnemy.y = 0f;

        if (toEnemy.sqrMagnitude > 0.12f)
        {
            Vector3 desiredForward = toEnemy.normalized;
            if (!snapped) smoothedForward = desiredForward;
            else smoothedForward = Vector3.Slerp(smoothedForward, desiredForward, 1f - Mathf.Exp(-4.2f * dt));
        }
        else if (!snapped)
        {
            Vector3 fromCenter = playerPos;
            fromCenter.y = 0f;
            smoothedForward = fromCenter.sqrMagnitude > 0.05f ? fromCenter.normalized : Vector3.forward;
        }

        Vector3 forward = smoothedForward.sqrMagnitude > 0.001f ? smoothedForward.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float fightDist = Vector3.Distance(playerPos, enemyPos);
        float backFar = Mathf.Clamp(backDistance + fightDist * 0.18f, 6.2f, 10.5f);
        float heightFar = Mathf.Clamp(height + fightDist * 0.08f, 3.1f, 5.2f);
        float backNear = Mathf.Clamp(impactZoomDistance + fightDist * 0.08f, 4.4f, 6.8f);
        float heightNear = Mathf.Clamp(impactZoomHeight + fightDist * 0.05f, 2.3f, 3.6f);

        float back = Mathf.Lerp(backFar, backNear, zoomBlend);
        float camHeight = Mathf.Lerp(heightFar, heightNear, zoomBlend) + heightBob;

        Vector3 desiredPos = playerPos
            - forward * back
            + right * (sideAmplitude * currentSide)
            + Vector3.up * camHeight;

        Vector3 lookPoint = Vector3.Lerp(playerPos, enemyPos, lookAhead) + Vector3.up * 0.32f;

        UpdateCinematic(dt, forward, ref desiredPos, ref lookPoint);

        if (!snapped)
        {
            transform.position = desiredPos;
            transform.rotation = Quaternion.LookRotation(lookPoint - desiredPos, Vector3.up);
            snapped = true;
        }
        else
        {
            float smoothTime = Mathf.Lerp(1f / followSpeed, 0.07f, cineBlend);
            float rotSpeed = Mathf.Lerp(rotationSpeed, 14f, cineBlend);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocityRef, smoothTime, Mathf.Infinity, dt);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(lookPoint - transform.position, Vector3.up),
                1f - Mathf.Exp(-rotSpeed * dt));
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= dt;
            float falloff = Mathf.Clamp01(shakeTimer / Mathf.Max(0.01f, shakeDuration));
            float t = Time.unscaledTime;
            transform.position += right * (Mathf.Sin(t * 38f) * currentShake * falloff)
                                  + Vector3.up * (Mathf.Cos(t * 29f) * currentShake * 0.35f * falloff);
        }

        if (cam != null)
        {
            fovPunch = Mathf.MoveTowards(fovPunch, 0f, dt * 3.5f);
            cam.fieldOfView = Mathf.Lerp(fieldOfView, cineFov, cineBlend) + fovPunch * 12f;
        }

        if (cineBlend < 0.01f)
        {
            Vector3 f = transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude > 0.01f) controlForward = f.normalized;
        }

        UpdateBars();
    }

    private void UpdateCinematic(float dt, Vector3 forward, ref Vector3 desiredPos, ref Vector3 lookPoint)
    {
        if (cineActive)
        {
            cineTimer += dt;
            float limit = cineHold ? cineMaxDuration + 3f : cineMaxDuration;
            if (cineFocus == null || cineTimer > limit)
                EndCinematic();
            else if (cineExitTimer >= 0f)
            {
                cineExitTimer -= dt;
                if (cineExitTimer < 0f) EndCinematic();
            }
        }

        cineBlend = Mathf.Lerp(cineBlend, cineActive ? 1f : 0f, 1f - Mathf.Exp(-7f * dt));
        if (cineBlend < 0.001f || cineFocus == null) return;

        Vector3 focus = cineFocus.position;
        Vector3 other = cineOther != null ? cineOther.position : focus + forward * 3f;
        Vector3 dir = other - focus;
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.01f ? dir.normalized : forward;

        cineOrbitDeg += dt * 16f * cineOrbitDir;
        float push = Mathf.Clamp01(cineTimer / 1.6f);
        float dist = Mathf.Lerp(cineDistance * 1.25f, cineDistance * 0.85f, push);
        Vector3 backDir = Quaternion.AngleAxis(cineOrbitDeg, Vector3.up) * -dir;

        Vector3 cinePos = focus + backDir * dist + Vector3.up * cineHeight;
        Vector3 cineLook = Vector3.Lerp(focus, other, 0.3f) + Vector3.up * 0.2f;

        desiredPos = Vector3.Lerp(desiredPos, cinePos, cineBlend);
        lookPoint = Vector3.Lerp(lookPoint, cineLook, cineBlend);
    }

    /// <summary>Özel yetenek başlayınca çağrılır; ilk çarpışmaya (veya zaman aşımına) kadar sürer.</summary>
    /// <param name="holdUntilFinale">true: çarpışmalar sinematiği bitirmez, CinematicFinale beklenir.</param>
    public void BeginSkillCinematic(Transform user, Transform other, Color accent, string title, bool holdUntilFinale = false)
    {
        if (user == null) return;
        cineFocus = user;
        cineOther = other;
        cineActive = true;
        cineHold = holdUntilFinale;
        cineTimer = 0f;
        cineExitTimer = -1f;
        cineOrbitDir = Random.value > 0.5f ? 1f : -1f;
        cineOrbitDeg = 38f * cineOrbitDir;

        EnsureBars();
        accentTop.color = accent;
        accentBottom.color = accent;
        cineTitle.text = title;
        cineTitle.color = Color.Lerp(accent, Color.white, 0.35f);

        SetSlowMo(cineStartSlowMo, 0.45f);
    }

    /// <summary>İki beyblade çarpıştığında çağrılır (her iki taraftan da gelebilir).</summary>
    public void NotifyBeyImpact(Transform a, Transform b)
    {
        if (!cineActive || cineExitTimer >= 0f) return;
        if (a != cineFocus && b != cineFocus) return;

        if (cineHold)
        {
            fovPunch = Mathf.Max(fovPunch, 0.4f);
            return;
        }

        cineExitTimer = 0.55f;
        fovPunch = 1f;
        currentShake = 0.3f;
        shakeTimer = shakeDuration * 2.2f;
        SetSlowMo(0.08f, 0.16f);
    }

    /// <summary>Tutulan sinematiği bitirir. impact: son darbe efekti (hit-stop + sarsıntı).</summary>
    public void CinematicFinale(Transform user, bool impact)
    {
        if (!cineActive || user != cineFocus) return;
        cineHold = false;
        if (!impact)
        {
            EndCinematic();
            return;
        }
        cineExitTimer = 0.7f;
        fovPunch = 1.3f;
        currentShake = 0.42f;
        shakeTimer = shakeDuration * 2.8f;
        SetSlowMo(0.06f, 0.24f);
    }

    private void EndCinematic()
    {
        cineActive = false;
        cineHold = false;
        cineExitTimer = -1f;
    }

    private void SetSlowMo(float scale, float realDuration)
    {
        Time.timeScale = Mathf.Clamp(scale, 0.05f, 1f);
        slowTimer = realDuration;
        ownsTimeScale = true;
    }

    private void UpdateTimeFx(float dt)
    {
        if (!ownsTimeScale) return;
        if (slowTimer > 0f)
        {
            slowTimer -= dt;
            return;
        }
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, 1f, dt * 3f);
        if (Time.timeScale >= 1f) ownsTimeScale = false;
    }

    private void EnsureBars()
    {
        if (barsCanvas != null) return;
        ModernUIKit.EnsureFonts();
        barsCanvas = ModernUIKit.CreateCanvas("CinematicBars", 180);
        barsCanvas.GetComponent<GraphicRaycaster>().enabled = false;

        barTop = MakeBar("Top", true, out accentTop);
        barBottom = MakeBar("Bottom", false, out accentBottom);

        cineTitle = ModernUIKit.Label(barBottom, "Title", "", 30, Color.white, ModernUIKit.FontRole.Display);
        cineTitle.fontStyle = FontStyles.Bold | FontStyles.Italic;
        cineTitle.characterSpacing = 12f;
        cineTitle.raycastTarget = false;
        ModernUIKit.Stretch(cineTitle.rectTransform, 0, 0, 14, 0);
    }

    private RectTransform MakeBar(string name, bool top, out Image accent)
    {
        Image bar = ModernUIKit.MakeImage(barsCanvas.transform, "Bar" + name, Color.black, null, false);
        bar.sprite = null;
        bar.raycastTarget = false;
        RectTransform rt = bar.rectTransform;
        float y = top ? 1f : 0f;
        ModernUIKit.Anchor(rt, new Vector2(0f, y), new Vector2(1f, y), new Vector2(0.5f, y), Vector2.zero, new Vector2(0f, 0f));

        accent = ModernUIKit.MakeImage(rt, "Accent", ModernUIKit.Cyan, null, false);
        accent.sprite = null;
        accent.raycastTarget = false;
        float edge = top ? 0f : 1f;
        ModernUIKit.Anchor(accent.rectTransform, new Vector2(0f, edge), new Vector2(1f, edge), new Vector2(0.5f, edge), Vector2.zero, new Vector2(0f, 3f));
        return rt;
    }

    private void UpdateBars()
    {
        if (barsCanvas == null) return;
        bool visible = cineBlend > 0.01f;
        if (barsCanvas.gameObject.activeSelf != visible) barsCanvas.gameObject.SetActive(visible);
        if (!visible) return;

        float h = 120f * cineBlend;
        barTop.sizeDelta = new Vector2(0f, h);
        barBottom.sizeDelta = new Vector2(0f, h);
        Color c = cineTitle.color;
        cineTitle.color = new Color(c.r, c.g, c.b, cineBlend);
    }

    private void OnDisable()
    {
        if (ownsTimeScale) Time.timeScale = 1f;
        ownsTimeScale = false;
    }

    private void OnDestroy()
    {
        if (barsCanvas != null) Destroy(barsCanvas.gameObject);
    }

    public void FlipSide()
    {
        targetSide = currentSide >= 0f ? -1f : 1f;
        sideTimer = Random.Range(4.5f, 8.5f);
    }

    public void TriggerShake(float intensity = -1f)
    {
        if (intensity < 0f) intensity = shakeIntensity;
        // Devam eden daha güçlü sarsıntıyı (sinematik darbe) ezme
        if (shakeTimer <= 0f || intensity >= currentShake)
        {
            currentShake = intensity;
            shakeTimer = shakeDuration;
        }
        impactZoomTimer = impactZoomDuration;
        if (Random.value > 0.55f && !cineActive) FlipSide();
    }
}
