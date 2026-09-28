using UnityEngine;

/// <summary>
/// Savaş skill sistemi: enerji birikimi, timing kalitesi, Attack/Defense efektleri.
/// Denge: skill güçlü ama kısa; kötü timing zayıf etki.
/// </summary>
public class SkillSystem : MonoBehaviour
{
    public enum Phase
    {
        ChargingEnergy,
        Ready,
        Timing,
        Active,
        Cooldown
    }

    [Header("Enerji")]
    public float maxEnergy = 100f;
    public float energy = 0f;
    [Tooltip("Saniye başına pasif enerji (fırlatıldıktan sonra)")]
    public float passiveEnergyPerSec = 5.5f;
    [Tooltip("Her çarpışmada ek enerji")]
    public float collisionEnergy = 9f;

    [Header("Denge")]
    public float cooldownSeconds = 2.2f;
    public float aiReactionDelay = 0.35f;

    public Phase phase = Phase.ChargingEnergy;
    public float lastTimingQuality = 0f;

    public bool OverridesMovement { get; private set; }
    public bool IsShieldActive { get; private set; }
    public bool IsRushing { get; private set; }
    public float DamageTakenMultiplier { get; private set; } = 1f;
    public float RushDamageMultiplier { get; private set; } = 1f;
    public float RushPushForce { get; private set; }

    BeybladeController bey;
    Rigidbody rb;
    SkillUI ui;
    BeybladeController enemy;

    float phaseTimer;
    float orbitAngle;
    float activeDuration;
    float skillQuality;
    bool aiQueued;
    GameObject shieldVisual;

    void Awake()
    {
        bey = GetComponent<BeybladeController>();
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (bey != null && bey.isPlayer)
        {
            ui = gameObject.AddComponent<SkillUI>();
            ui.Bind(this);
        }
    }

    void Update()
    {
        if (bey == null || !bey.isLaunched || !bey.isSpinning || bey.hasToppled)
        {
            if (phase == Phase.Active) EndSkill();
            return;
        }

        if (enemy == null || !enemy.isLaunched) FindEnemy();

        switch (phase)
        {
            case Phase.ChargingEnergy:
                energy = Mathf.Min(maxEnergy, energy + passiveEnergyPerSec * Time.deltaTime);
                if (energy >= maxEnergy)
                {
                    energy = maxEnergy;
                    phase = Phase.Ready;
                    if (!bey.isPlayer) QueueAiSkill();
                }
                break;

            case Phase.Ready:
                if (!bey.isPlayer && aiQueued)
                {
                    aiReactionDelay -= Time.deltaTime;
                    if (aiReactionDelay <= 0f)
                    {
                        aiQueued = false;
                        float q = Random.Range(0.42f, 0.88f);
                        BeginSkill(q);
                    }
                }
                break;

            case Phase.Timing:
                // SkillUI yönetir
                break;

            case Phase.Active:
                phaseTimer += Time.deltaTime;
                TickActiveSkill();
                break;

            case Phase.Cooldown:
                phaseTimer += Time.deltaTime;
                if (phaseTimer >= cooldownSeconds)
                {
                    phase = Phase.ChargingEnergy;
                    phaseTimer = 0f;
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (phase != Phase.Active || !OverridesMovement || bey == null || rb == null || !bey.isLaunched)
            return;
        if (rb.isKinematic) return;

        if (bey.beybladeType == BeybladeType.Saldiri || bey.beybladeType == BeybladeType.Denge)
            TickAttackMovement();
    }

    public float EnergyRatio => Mathf.Clamp01(energy / maxEnergy);
    public bool CanActivate => phase == Phase.Ready && bey != null && bey.isPlayer && bey.isLaunched && bey.isSpinning;

    public void OnCollisionEnergy()
    {
        if (phase != Phase.ChargingEnergy && phase != Phase.Ready) return;
        if (bey == null || !bey.isLaunched) return;
        energy = Mathf.Min(maxEnergy, energy + collisionEnergy);
        if (energy >= maxEnergy && phase == Phase.ChargingEnergy)
        {
            energy = maxEnergy;
            phase = Phase.Ready;
            if (!bey.isPlayer) QueueAiSkill();
        }
    }

    public void PlayerRequestSkill()
    {
        if (!CanActivate) return;
        phase = Phase.Timing;
        if (ui != null) ui.OpenTimingBar();
    }

    public void CompleteTiming(float quality)
    {
        if (phase != Phase.Timing) return;
        BeginSkill(quality);
    }

    public void CancelTiming()
    {
        if (phase != Phase.Timing) return;
        phase = Phase.Ready;
    }

    void BeginSkill(float quality)
    {
        skillQuality = Mathf.Clamp01(quality);
        lastTimingQuality = skillQuality;
        energy = 0f;
        phase = Phase.Active;
        phaseTimer = 0f;
        orbitAngle = Random.Range(0f, 360f);
        OverridesMovement = false;
        IsShieldActive = false;
        IsRushing = false;
        DamageTakenMultiplier = 1f;
        RushDamageMultiplier = 1f;
        RushPushForce = 0f;

        switch (bey.beybladeType)
        {
            case BeybladeType.Saldiri:
                StartAttackSkill();
                break;
            case BeybladeType.Savunma:
                StartDefenseSkill();
                break;
            case BeybladeType.Dayaniklilik:
                StartStaminaSkill();
                break;
            default:
                // Denge: kısa saldırı + hafif kalkan
                StartAttackSkill();
                activeDuration *= 0.75f;
                break;
        }

        if (ui != null) ui.ShowSkillFeedback(skillQuality, bey.beybladeType);
    }

    void StartAttackSkill()
    {
        // Orbit ~1.1s + rush 3.6..5.0s
        float orbit = 1.05f;
        float rush = Mathf.Lerp(3.6f, 5.0f, skillQuality);
        activeDuration = orbit + rush;
        OverridesMovement = true;
        IsRushing = false;
        RushDamageMultiplier = Mathf.Lerp(1.04f, 1.14f, skillQuality);
        RushPushForce = Mathf.Lerp(1.8f, 4.2f, skillQuality);
        // Kısa RPM boost
        bey.currentSpinRPM = Mathf.Min(bey.maxSpinRPM, bey.currentSpinRPM * Mathf.Lerp(1.08f, 1.22f, skillQuality));
    }

    void StartDefenseSkill()
    {
        activeDuration = Mathf.Lerp(3.4f, 5.0f, skillQuality);
        IsShieldActive = true;
        DamageTakenMultiplier = Mathf.Lerp(0.62f, 0.38f, skillQuality); // %38-62 hasar alır
        SpawnShieldVisual();
    }

    void StartStaminaSkill()
    {
        activeDuration = Mathf.Lerp(2.8f, 4.0f, skillQuality);
        // Stamina yenile + hafif itme alanı
        float heal = Mathf.Lerp(8f, 18f, skillQuality);
        bey.currentStamina = Mathf.Min(bey.maxStamina, bey.currentStamina + heal);
        IsShieldActive = true;
        DamageTakenMultiplier = Mathf.Lerp(0.85f, 0.7f, skillQuality);
        SpawnShieldVisual(new Color(0.2f, 1f, 0.55f, 0.35f));
    }

    void TickActiveSkill()
    {
        if (phaseTimer >= activeDuration)
        {
            EndSkill();
            return;
        }

        if (bey.beybladeType == BeybladeType.Saldiri || bey.beybladeType == BeybladeType.Denge)
        {
            float orbitEnd = 1.15f * (bey.beybladeType == BeybladeType.Denge ? 0.75f : 1f);
            IsRushing = phaseTimer >= orbitEnd;

            // Rush sırasında ekstra hasar SADECE gerçek çarpışmada OnCollisionEnter üzerinden
            // (burada sürekli itmek iç içe girmeye yol açıyordu)
        }
    }

    void TickAttackMovement()
    {
        if (enemy == null || !enemy.isLaunched)
        {
            FindEnemy();
            if (enemy == null) return;
        }

        // Çarpışma sekmesi görünsün diye skill çekimini de kısa kes
        if (bey.IsSeekStunned) return;

        float orbitEnd = 1.15f * (bey.beybladeType == BeybladeType.Denge ? 0.75f : 1f);
        Vector3 myPos = transform.position;
        Vector3 enemyPos = enemy.transform.position;
        Vector3 toEnemy = enemyPos - myPos;
        toEnemy.y = 0f;
        float dist = toEnemy.magnitude;
        if (dist < 0.01f) return;

        // Collider yarıçaplarına göre güvenli orbit — iç içe girmez
        float myR = 0.45f;
        float enR = 0.45f;
        CapsuleCollider mc = GetComponent<CapsuleCollider>();
        CapsuleCollider ec = enemy.GetComponent<CapsuleCollider>();
        if (mc != null) myR = mc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        if (ec != null) enR = ec.radius * Mathf.Max(enemy.transform.lossyScale.x, enemy.transform.lossyScale.z);
        float safeOrbit = myR + enR + Mathf.Lerp(0.55f, 0.35f, skillQuality);

        if (phaseTimer < orbitEnd)
        {
            orbitAngle += Mathf.Lerp(220f, 320f, skillQuality) * Time.fixedDeltaTime;
            float rad = orbitAngle * Mathf.Deg2Rad;
            Vector3 desired = new Vector3(enemyPos.x, myPos.y, enemyPos.z)
                + new Vector3(Mathf.Cos(rad) * safeOrbit, 0f, Mathf.Sin(rad) * safeOrbit);
            Vector3 to = desired - myPos;
            to.y = 0f;
            // Hızı ezme — sadece kuvvet (GitHub fizik/çarpışma bozulmaz)
            rb.AddForce(to.normalized * Mathf.Lerp(14f, 20f, skillQuality), ForceMode.Acceleration);
        }
        else
        {
            // Hücum: düşmana doğru agresif çekim; temas Unity çarpışması + knockback ile olur
            float pull = Mathf.Lerp(12f, 18f, skillQuality);
            rb.AddForce(toEnemy.normalized * pull, ForceMode.Acceleration);

            // Çok yaklaşırsa içeri itme — radial ayrılma kuvveti
            if (dist < safeOrbit * 0.92f)
            {
                Vector3 away = -toEnemy.normalized;
                rb.AddForce(away * 10f, ForceMode.Acceleration);
            }
        }

        // Skill sırasında da hız tavanı (GitHub ile aynı)
        Vector3 flat = rb.linearVelocity;
        flat.y = 0f;
        if (flat.magnitude > 15f)
            rb.linearVelocity = new Vector3(flat.normalized.x * 15f, rb.linearVelocity.y, flat.normalized.z * 15f);
    }

    public void OnShieldParry(BeybladeController attacker, Vector3 awayFromMe)
    {
        if (!IsShieldActive || attacker == null) return;
        Rigidbody arb = attacker.GetComponent<Rigidbody>();
        if (arb == null || arb.isKinematic) return;

        Vector3 dir = awayFromMe;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
            dir = (attacker.transform.position - transform.position);

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        dir.Normalize();

        float force = Mathf.Lerp(4.5f, 8.5f, skillQuality);
        arb.AddForce(dir * force, ForceMode.Impulse);

        // Hafif kendi stamina maliyeti yok; saldırgana küçük tepki hasarı
        attacker.currentStamina -= Mathf.Lerp(1.2f, 2.8f, skillQuality);
    }

    void EndSkill()
    {
        phase = Phase.Cooldown;
        phaseTimer = 0f;
        OverridesMovement = false;
        IsShieldActive = false;
        IsRushing = false;
        DamageTakenMultiplier = 1f;
        RushDamageMultiplier = 1f;
        RushPushForce = 0f;
        ClearShieldVisual();
    }

    void QueueAiSkill()
    {
        aiQueued = true;
        aiReactionDelay = Random.Range(0.25f, 0.7f);
    }

    void FindEnemy()
    {
        BeybladeController[] all = FindObjectsByType<BeybladeController>(FindObjectsSortMode.None);
        foreach (var b in all)
        {
            if (b != bey)
            {
                enemy = b;
                break;
            }
        }
    }

    void SpawnShieldVisual(Color? color = null)
    {
        ClearShieldVisual();
        Color c = color ?? new Color(0.35f, 0.85f, 1f, 0.72f);

        shieldVisual = new GameObject("SkillShield");
        shieldVisual.transform.SetParent(transform, false);
        shieldVisual.transform.localPosition = Vector3.up * 0.08f;

        float r = 0.55f;
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) r = Mathf.Clamp(col.radius * 1.45f, 0.42f, 0.85f);

        // İnce halka (düz silindir) — büyük mavi küre değil
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(ring.GetComponent<Collider>());
        ring.name = "ShieldRing";
        ring.transform.SetParent(shieldVisual.transform, false);
        ring.transform.localScale = new Vector3(r * 2.05f, 0.018f, r * 2.05f);
        ApplyShieldMat(ring, new Color(c.r, c.g, c.b, 0.85f));

        GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(halo.GetComponent<Collider>());
        halo.name = "ShieldHalo";
        halo.transform.SetParent(shieldVisual.transform, false);
        halo.transform.localScale = new Vector3(r * 2.35f, 0.008f, r * 2.35f);
        ApplyShieldMat(halo, new Color(c.r, c.g, c.b, 0.22f));

        GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(dome.GetComponent<Collider>());
        dome.name = "ShieldDome";
        dome.transform.SetParent(shieldVisual.transform, false);
        dome.transform.localScale = Vector3.one * (r * 1.55f);
        dome.transform.localPosition = Vector3.up * 0.02f;
        ApplyShieldMat(dome, new Color(c.r, c.g, c.b, 0.12f));
    }

    static void ApplyShieldMat(GameObject go, Color c)
    {
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) return;
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material mat = new Material(sh);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        mat.color = c;
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void ClearShieldVisual()
    {
        if (shieldVisual != null)
        {
            Destroy(shieldVisual);
            shieldVisual = null;
        }
    }

    void OnDestroy()
    {
        ClearShieldVisual();
    }
}
