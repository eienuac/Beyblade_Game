using UnityEngine;

public enum BeybladeType
{
    Saldiri,      // Attack
    Savunma,      // Defense
    Dayaniklilik, // Stamina
    Denge         // Balance
}

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class BeybladeController : MonoBehaviour
{
    [Header("Parça Sistemi")]
    public bool isPlayer = false;
    public PartDatabase partDatabase;

    [Tooltip("Botlar için parçaları buradan manuel verebilirsin (Player ise otomatik yüklenir)")]
    public BeybladePart equippedLayer;
    public BeybladePart equippedDisk;
    public BeybladePart equippedTip;

    [Header("Tip ve İstatistikler (Eski Sistem)")]
    [Tooltip("Eğer parça yoksa bu tipe göre çalışır.")]
    public BeybladeType beybladeType = BeybladeType.Denge;

    public float maxStamina = 160f;
    public float currentStamina = 160f;
    public float staminaDecayRate = 1.1f;

    [Tooltip("Dakikadaki Dönüş Hızı (RPM)")]
    public float maxSpinRPM = 1500f;
    public float currentSpinRPM = 1500f;

    [Header("RPG Statları (Otomatik Hesaplanır)")]
    public float attackPower = 5f;
    public float defensePower = 0.5f;
    public float weight = 2.5f;

    [Header("Çarpışma Hassasiyeti")]
    [Tooltip("Çarpışma kutusunu küçültmek/büyütmek için çarpan. (0.9 önerilir)")]
    [Range(0.5f, 1.5f)]
    public float colliderRadiusMultiplier = 0.9f;

    [Header("Durum")]
    public bool isSpinning = false;
    public bool hasToppled = false;
    public bool isLaunched = false;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private TopDownCamera topDownCamera;
    private float lastCollisionTime = 0f;
    private float seekStunUntil = 0f;
    private SkillSystem cachedSkill;
    public bool IsSeekStunned => Time.time < seekStunUntil;

    // BeyControl her karede yazar (dünya uzayında XZ, uzunluk 0–1)
    [HideInInspector] public Vector3 steerInput;
    private float overspeedUntil;
    private float overspeedMult = 1f;
    private float dashUntil;
    public bool IsDashing => Time.time < dashUntil;
    public bool WasRingedOut { get; private set; }
    public bool MovementOverridden => cachedSkill != null && cachedSkill.OverridesMovement;

    // Stat → davranış. Aralıklar ApplyStats clamp'leriyle uyumlu.
    public float AttackNorm => Mathf.InverseLerp(2f, 10f, attackPower);
    public float DefenseNorm => Mathf.InverseLerp(0.05f, 0.9f, defensePower);
    public float WeightNorm => Mathf.InverseLerp(1.4f, 6f, weight);
    public float MaxSpeed => Mathf.Lerp(4f, 15f, AttackNorm) * (1f - 0.15f * WeightNorm);
    public float KnockbackResist => 0.45f * DefenseNorm;
    public float StaminaDrainMultiplier => Mathf.Lerp(0.9f, 1.3f, AttackNorm);

    // Görsel rig: fizik gövdesi dik kalır, mesh ayrı döner (titreme/gimbal yok)
    public Transform VisualPivot { get; private set; }
    public Transform VisualSpin { get; private set; }
    private float wobbleTimer = 0f;
    private float wobbleIntensity = 0f;
    private float spinAngleY = 0f;
    private float leftoverSpinDeg = 0f;
    private Quaternion targetTilt = Quaternion.identity;
    private float noiseSeed;

    // Düşman Takip (Attack tipi için)
    private BeybladeController enemyTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        noiseSeed = Random.Range(0f, 1000f);
        BuildVisualRig();
    }

    /// <summary>
    /// Mesh'i fizik kökünden ayırır. Kök hiç dönmez; eğilme ve spin çocuklarda uygulanır.
    /// </summary>
    private void BuildVisualRig()
    {
        if (VisualPivot != null) return;

        GameObject pivotObj = new GameObject("VisualPivot");
        VisualPivot = pivotObj.transform;
        VisualPivot.SetParent(transform, false);

        GameObject spinObj = new GameObject("VisualSpin");
        VisualSpin = spinObj.transform;
        VisualSpin.SetParent(VisualPivot, false);

        var toMove = new System.Collections.Generic.List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == VisualPivot) continue;
            toMove.Add(child);
        }
        for (int i = 0; i < toMove.Count; i++)
            toMove[i].SetParent(VisualSpin, true);

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        // Kökte Unity Capsule varsa kopyalama — 2 birim boyu "uzun sap" gibi duruyordu
        if (mf != null && mf.sharedMesh != null && mr != null && IsCustomVisualMesh(mf.sharedMesh))
        {
            GameObject vis = new GameObject("MeshVisual");
            vis.transform.SetParent(VisualSpin, false);
            vis.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            MeshRenderer copy = vis.AddComponent<MeshRenderer>();
            copy.sharedMaterials = mr.sharedMaterials;
        }
    }

    private static bool IsCustomVisualMesh(Mesh mesh)
    {
        if (mesh == null) return false;
        string n = mesh.name;
        return n != "Capsule" && n != "Sphere" && n != "Cube" && n != "Cylinder" && n != "Plane";
    }

    /// <summary>
    /// Gerçek Beyblade oranına çeker: geniş disk, kısa uç. Alta oturtur.
    /// </summary>
    private void FlattenVisualToRealisticShape()
    {
        if (VisualSpin == null) return;

        Bounds world = GetVisualWorldBounds();
        if (world.size.sqrMagnitude < 0.0001f) return;

        float diameter = Mathf.Max(world.size.x, world.size.z);
        float height = world.size.y;
        if (diameter < 0.01f) return;

        // BX gerçek oran ~ çap 60mm / yükseklik 32-38mm
        const float targetHeightOverDiameter = 0.48f;
        float ratio = height / diameter;
        if (ratio > targetHeightOverDiameter)
        {
            float squash = targetHeightOverDiameter / ratio;
            Vector3 s = VisualSpin.localScale;
            VisualSpin.localScale = new Vector3(s.x, s.y * squash, s.z);
        }
    }

    private Bounds GetVisualWorldBounds()
    {
        if (VisualSpin == null) return new Bounds(transform.position, Vector3.zero);
        MeshRenderer[] renderers = VisualSpin.GetComponentsInChildren<MeshRenderer>();
        bool started = false;
        Bounds combined = new Bounds(transform.position, Vector3.zero);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer r = renderers[i];
            if (r == null || !r.enabled) continue;
            if (r.gameObject.name.StartsWith("Trail") || r.gameObject.name.StartsWith("Motion")) continue;
            if (!started)
            {
                combined = r.bounds;
                started = true;
            }
            else combined.Encapsulate(r.bounds);
        }
        return combined;
    }

    private void Start()
    {
        if (gameObject.name.Contains("Player") || gameObject.name == "PlayerBeyblade")
            isPlayer = true;

        if (rb == null) rb = GetComponent<Rigidbody>();
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider>();

        LoadParts();
        ApplyStats();

        if (GetComponent<StaminaBarUI>() == null)
            gameObject.AddComponent<StaminaBarUI>();

        if (FindFirstObjectByType<BattleManager>() == null)
        {
            GameObject bm = new GameObject("BattleManager");
            bm.AddComponent<BattleManager>();
        }

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.linearDamping = 0.35f;
        rb.angularDamping = 2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        transform.rotation = Quaternion.identity;

        FitColliderToVisualMesh();
        SeatVisualOnCollider();
        StripChildColliders();

        if (GetComponent<SpinEffect>() == null)
            gameObject.AddComponent<SpinEffect>();

        if (GetComponent<SkillSystem>() == null)
            gameObject.AddComponent<SkillSystem>();
        cachedSkill = GetComponent<SkillSystem>();

        if (GetComponent<BeyControl>() == null)
            gameObject.AddComponent<BeyControl>();

        isLaunched = false;
        if (capsuleCollider != null) capsuleCollider.enabled = false;
        if (rb != null)
        {
            bool wasKin = rb.isKinematic;
            if (wasKin) rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        topDownCamera = FindFirstObjectByType<TopDownCamera>();
        FindEnemy();
    }

    /// <summary>Modelden gelen ekstra collider'lar çift çarpışma / iç içe hissi yaratır.</summary>
    private void StripChildColliders()
    {
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] == null) continue;
            if (cols[i] == capsuleCollider) continue;
            Destroy(cols[i]);
        }
    }

    private void FindEnemy()
    {
        BeybladeController[] allBeys = FindObjectsByType<BeybladeController>(FindObjectsSortMode.None);
        foreach (var b in allBeys)
        {
            if (b != this)
            {
                enemyTarget = b;
                break;
            }
        }
    }

    private void LoadParts()
    {
        if (isPlayer && PlayerDataManager.Instance != null && partDatabase != null)
        {
            string lID = PlayerDataManager.Instance.data.equippedLayerID;
            string dID = PlayerDataManager.Instance.data.equippedDiskID;
            string tID = PlayerDataManager.Instance.data.equippedTipID;

            equippedLayer = partDatabase.GetPartByID(lID);
            equippedDisk = partDatabase.GetPartByID(dID);
            equippedTip = partDatabase.GetPartByID(tID);
        }
    }

    private void ApplyStats()
    {
        if (equippedLayer != null && equippedDisk != null && equippedTip != null)
        {
            attackPower = 0;
            defensePower = 0;
            staminaDecayRate = 0;
            weight = 0;

            attackPower += equippedLayer.attackBonus;
            defensePower += equippedLayer.defenseBonus;
            staminaDecayRate += equippedLayer.staminaBonus;
            weight += equippedLayer.weightBonus;

            attackPower += equippedDisk.attackBonus;
            defensePower += equippedDisk.defenseBonus;
            staminaDecayRate += equippedDisk.staminaBonus;
            weight += equippedDisk.weightBonus;

            attackPower += equippedTip.attackBonus;
            defensePower += equippedTip.defenseBonus;
            staminaDecayRate += equippedTip.staminaBonus;
            weight += equippedTip.weightBonus;

            if (attackPower > defensePower && attackPower > (5f - staminaDecayRate)) beybladeType = BeybladeType.Saldiri;
            else if (defensePower > attackPower && defensePower > (5f - staminaDecayRate)) beybladeType = BeybladeType.Savunma;
            else beybladeType = BeybladeType.Dayaniklilik;
        }
        else
        {
            ApplyTypeStats();
        }

        if (weight < 0.5f) weight = 0.5f;

        // Inspector'da yanlışlıkla 50/75 gibi yüzde yazılmışsa düzelt
        if (defensePower > 1.5f) defensePower = Mathf.Clamp01(defensePower / 100f);
        defensePower = Mathf.Clamp(defensePower, 0.05f, 0.9f);
        attackPower = Mathf.Clamp(attackPower, 2f, 20f);
        weight = Mathf.Clamp(weight, 1.4f, 6f);

        if (rb != null) rb.mass = weight;
    }

    private void ApplyTypeStats()
    {
        // Daha uzun maçlar: düşük decay + dengeli hasar
        switch (beybladeType)
        {
            case BeybladeType.Saldiri:
                weight = 2.0f;
                attackPower = 7.5f;
                defensePower = 0.28f;
                staminaDecayRate = 1.15f;
                break;

            case BeybladeType.Savunma:
                weight = 4.8f;
                attackPower = 3.2f;
                defensePower = 0.78f;
                staminaDecayRate = 0.85f;
                break;

            case BeybladeType.Dayaniklilik:
                weight = 1.7f;
                attackPower = 2.8f;
                defensePower = 0.22f;
                staminaDecayRate = 0.55f;
                break;

            case BeybladeType.Denge:
                weight = 2.5f;
                attackPower = 5.0f;
                defensePower = 0.42f;
                staminaDecayRate = 0.9f;
                break;
        }

        if (rb != null) rb.mass = weight;
    }

    private void FitColliderToVisualMesh()
    {
        Bounds combinedBounds = GetVisualWorldBounds();
        float worldRadius = 0.55f;
        float worldHeight = 0.32f;

        if (combinedBounds.size.sqrMagnitude > 0.0001f)
        {
            worldRadius = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.z);
            worldHeight = combinedBounds.size.y;
        }

        worldRadius = Mathf.Clamp(worldRadius, 0.4f, 1.05f);
        worldHeight = Mathf.Clamp(worldHeight, 0.22f, 0.55f);

        float rootScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        if (rootScale < 0.001f) rootScale = 1.0f;

        float localRadius = (worldRadius / rootScale) * colliderRadiusMultiplier;
        float localHeight = Mathf.Clamp(worldHeight / rootScale, 0.2f, 0.55f);

        if (capsuleCollider != null)
        {
            capsuleCollider.radius = Mathf.Max(0.35f, localRadius);
            capsuleCollider.height = Mathf.Max(localHeight, capsuleCollider.radius * 1.15f);
            capsuleCollider.direction = 1;
            capsuleCollider.center = Vector3.zero;
            capsuleCollider.contactOffset = 0.01f;
        }

        PhysicsMaterial mat = new PhysicsMaterial("BeybladeArcadeMat");
        mat.bounciness = 0f;
        mat.dynamicFriction = 0.02f;
        mat.staticFriction = 0.02f;
        mat.bounceCombine = PhysicsMaterialCombine.Minimum;
        mat.frictionCombine = PhysicsMaterialCombine.Minimum;
        if (capsuleCollider != null) capsuleCollider.material = mat;
    }

    private void SeatVisualOnCollider()
    {
        if (VisualSpin == null || capsuleCollider == null) return;

        Bounds world = GetVisualWorldBounds();
        if (world.size.sqrMagnitude < 0.0001f) return;

        float scaleY = Mathf.Abs(transform.lossyScale.y);
        if (scaleY < 0.001f) scaleY = 1f;
        float colliderBottom = transform.position.y + capsuleCollider.center.y - capsuleCollider.height * 0.5f * scaleY;
        float lift = (colliderBottom + 0.012f) - world.min.y;
        VisualSpin.localPosition += new Vector3(0f, lift, 0f);
    }

    private void Update()
    {
        if (!isLaunched)
        {
            if (capsuleCollider != null && capsuleCollider.enabled) capsuleCollider.enabled = false;
            if (rb != null && !rb.isKinematic) rb.isKinematic = true;
            transform.rotation = Quaternion.identity;
            if (VisualPivot != null) VisualPivot.localRotation = Quaternion.identity;
        }
        else
        {
            if (capsuleCollider != null && !capsuleCollider.enabled && isSpinning)
            {
                capsuleCollider.enabled = true;
                rb.isKinematic = false;
            }
        }

        if (!isSpinning)
        {
            UpdateVisualSpinAndTilt();
            return;
        }

        if (isLaunched && currentStamina > 0)
        {
            currentStamina -= staminaDecayRate * StaminaDrainMultiplier * Time.deltaTime;
            currentSpinRPM = (currentStamina / maxStamina) * maxSpinRPM;
        }
        else if (isLaunched && currentStamina <= 0)
        {
            leftoverSpinDeg = Mathf.Max(currentSpinRPM, 280f) * 6f;
            isSpinning = false;
            hasToppled = true;
            currentStamina = 0;
            currentSpinRPM = 0;

            if (rb != null)
            {
                rb.constraints = RigidbodyConstraints.None;
                Vector3 tiltDir = Random.onUnitSphere;
                tiltDir.y = -0.5f;
                rb.AddTorque(tiltDir * 4f, ForceMode.Impulse);
                rb.AddForce(tiltDir * 2f, ForceMode.Impulse);
            }
        }

        UpdateVisualSpinAndTilt();
    }

    private void UpdateVisualSpinAndTilt()
    {
        if (VisualSpin == null || VisualPivot == null) return;

        float dt = Time.deltaTime;
        if (isSpinning)
        {
            spinAngleY += currentSpinRPM * 6f * dt;
            leftoverSpinDeg = currentSpinRPM * 6f;
        }
        else if (hasToppled && leftoverSpinDeg > 8f)
        {
            leftoverSpinDeg = Mathf.Lerp(leftoverSpinDeg, 0f, dt * 1.6f);
            spinAngleY += leftoverSpinDeg * dt;
        }

        VisualSpin.localRotation = Quaternion.Euler(0f, spinAngleY, 0f);

        if (!isLaunched)
        {
            VisualPivot.localRotation = Quaternion.identity;
            return;
        }

        if (!isSpinning)
            return;

        Vector3 velocity = rb != null ? rb.linearVelocity : Vector3.zero;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        Quaternion lean = Quaternion.identity;
        if (speed > 0.15f)
        {
            float staminaRatio = Mathf.Clamp01(currentStamina / maxStamina);
            // Hareket yönüne belirgin eğilme (öne/arkaya)
            float maxTilt = Mathf.Lerp(18f, 8f, staminaRatio);
            float tiltAngle = Mathf.Clamp(speed * 1.15f, 3f, maxTilt);
            Vector3 tiltAxis = Vector3.Cross(Vector3.up, velocity.normalized);
            if (tiltAxis.sqrMagnitude > 0.0001f)
                lean = Quaternion.AngleAxis(tiltAngle, tiltAxis.normalized);
        }

        if (wobbleTimer > 0f)
        {
            wobbleTimer -= dt;
            float decay = Mathf.Clamp01(wobbleTimer / 0.35f);
            float wx = Mathf.Sin(wobbleTimer * 22f) * wobbleIntensity * decay;
            float wz = Mathf.Cos(wobbleTimer * 17f) * wobbleIntensity * decay * 0.65f;
            lean *= Quaternion.Euler(wx, 0f, wz);
        }

        targetTilt = lean;
        VisualPivot.localRotation = Quaternion.Slerp(VisualPivot.localRotation, targetTilt, 1f - Mathf.Exp(-14f * dt));
    }

    private void FixedUpdate()
    {
        if (!isSpinning || rb == null || !isLaunched) return;

        if (enemyTarget == null || !enemyTarget.isLaunched)
            FindEnemy();

        Vector3 velY = rb.linearVelocity;
        if (velY.y > 0.25f) velY.y *= 0.4f;
        if (velY.y < -6f) velY.y = -6f;
        rb.linearVelocity = velY;
        rb.AddForce(Vector3.down * 10f, ForceMode.Acceleration);

        if (cachedSkill == null) cachedSkill = GetComponent<SkillSystem>();
        if (cachedSkill != null && cachedSkill.OverridesMovement) return;

        // Çarpışma sonrası kısa stun: çekim sekme kuvvetini yemesin
        bool stunned = Time.time < seekStunUntil;

        Vector3 targetPoint;
        // Attack: rakip fırlatıldıysa ona yönel (spin kontrolü gereksizce arıyordu)
        if (beybladeType == BeybladeType.Saldiri && enemyTarget != null && enemyTarget.isLaunched)
            targetPoint = enemyTarget.transform.position;
        else
            targetPoint = Vector3.zero;

        Vector3 dirToTarget = targetPoint - transform.position;
        dirToTarget.y = 0f;
        float distToTarget = dirToTarget.magnitude;
        if (distToTarget < 0.001f) dirToTarget = transform.forward;
        else dirToTarget.Normalize();

        float basePull;
        float orbitForce;

        if (beybladeType == BeybladeType.Saldiri)
        {
            basePull = 9.0f;
            orbitForce = 0.12f;
            if (distToTarget < 5f && enemyTarget != null)
                basePull = 15.0f;
        }
        else if (beybladeType == BeybladeType.Dayaniklilik)
        {
            basePull = 8.0f;
            orbitForce = 0.2f;
        }
        else if (beybladeType == BeybladeType.Savunma)
        {
            // Ağır savunma: merkeze yapış, kaçma / kovalamaca yok
            basePull = 11.0f;
            orbitForce = 0.02f;
        }
        else
        {
            basePull = 5.0f;
            orbitForce = 0.3f;
        }

        bool steering = steerInput.sqrMagnitude > 0.0025f;

        if (!stunned)
        {
            float rpmFactor = Mathf.Clamp01(currentSpinRPM / Mathf.Max(1f, maxSpinRPM));
            float totalPull = (basePull + rpmFactor * 4f) * Mathf.Lerp(0.8f, 1.25f, AttackNorm);

            // Defense uzaklaştıysa merkeze daha sert çek
            if (beybladeType == BeybladeType.Savunma && distToTarget > 1.2f)
                totalPull *= 1.55f;

            // Oyuncu yön verirken otomatik çekim geri planda kalır
            if (steering)
                totalPull *= 0.35f;

            rb.AddForce(dirToTarget * totalPull, ForceMode.Acceleration);

            if (distToTarget > 0.3f && beybladeType != BeybladeType.Savunma)
            {
                Vector3 tangent = Vector3.Cross(dirToTarget, Vector3.up);
                rb.AddForce(tangent * (totalPull * orbitForce), ForceMode.Acceleration);
            }
            else if (beybladeType == BeybladeType.Savunma && distToTarget > 0.25f)
            {
                // Çok hafif yörünge — yerinde savunma hissi
                Vector3 tangent = Vector3.Cross(dirToTarget, Vector3.up);
                rb.AddForce(tangent * (totalPull * orbitForce), ForceMode.Acceleration);
            }

            float t = Time.time * 0.55f;
            float drift = rpmFactor * (beybladeType == BeybladeType.Savunma ? 0.15f : 0.55f);
            Vector3 sway = new Vector3(
                (Mathf.PerlinNoise(t, noiseSeed) - 0.5f) * 2f,
                0f,
                (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f
            ) * drift;
            rb.AddForce(sway, ForceMode.Acceleration);
        }

        if (steering)
        {
            float steerAccel = Mathf.Lerp(10f, 16f, AttackNorm);
            if (stunned) steerAccel *= 0.4f;
            rb.AddForce(steerInput * steerAccel, ForceMode.Acceleration);
        }

        Vector3 flatVel = rb.linearVelocity;
        flatVel.y = 0f;
        float cap = MaxSpeed;
        if (Time.time < overspeedUntil) cap *= overspeedMult;
        float speed = flatVel.magnitude;
        if (speed > cap)
        {
            // Savrulma anında yumuşak fren: darbe hızı tek karede silinmesin
            float decel = stunned ? 14f : 45f;
            float newSpeed = Mathf.MoveTowards(speed, cap, decel * Time.fixedDeltaTime);
            flatVel = flatVel * (newSpeed / speed);
            rb.linearVelocity = new Vector3(flatVel.x, rb.linearVelocity.y, flatVel.z);
        }

        // Defense: ekstra sürtünme ile kaçışı kes
        if (beybladeType == BeybladeType.Savunma && !steering && !IsDashing && !stunned)
        {
            Vector3 v = rb.linearVelocity;
            float k = Mathf.Exp(-3.5f * Time.fixedDeltaTime);
            v.x *= k;
            v.z *= k;
            rb.linearVelocity = v;
        }
    }

    public void Dash(Vector3 dir, float strength)
    {
        if (rb == null || rb.isKinematic || !isLaunched || !isSpinning) return;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        Vector3 v = rb.linearVelocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);
        float along = Vector3.Dot(flat, dir);
        // Ters yöndeki hızı sil, yan hızı koru
        flat += dir * (Mathf.Max(0f, -along) + strength);
        rb.linearVelocity = new Vector3(flat.x, v.y, flat.z);

        overspeedUntil = Time.time + 0.4f;
        overspeedMult = Mathf.Max(1.7f, (strength + 3f) / Mathf.Max(1f, MaxSpeed));
        dashUntil = Time.time + 0.45f;
        seekStunUntil = Mathf.Max(seekStunUntil, Time.time + 0.4f);
        wobbleIntensity = 3f;
        wobbleTimer = 0.2f;
    }

    public void ForceRingOut()
    {
        if (!isLaunched || hasToppled) return;
        WasRingedOut = true;
        currentStamina = 0f;
    }

    // Başka Beyblade ile Çarpışma — kenar teması + net geri sekme
    private void OnCollisionEnter(Collision collision)
    {
        BeybladeController enemy = collision.gameObject.GetComponent<BeybladeController>();

        if (enemy != null && isSpinning && isLaunched && enemy.isLaunched && Time.time - lastCollisionTime > 0.12f)
        {
            lastCollisionTime = Time.time;
            seekStunUntil = Time.time + (beybladeType == BeybladeType.Savunma ? 0.3f : 0.28f);

            Vector3 hitDirection = transform.position - collision.transform.position;
            hitDirection.y = 0f;

            if (hitDirection.sqrMagnitude < 0.01f)
                hitDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

            Vector3 normalHit = hitDirection.normalized;
            Vector3 sideScatter = Vector3.Cross(normalHit, Vector3.up) * Random.Range(-0.25f, 0.25f);
            Vector3 finalBounceDir = (normalHit + sideScatter).normalized;
            finalBounceDir.y = 0f;

            float incomingDamage = enemy.attackPower / (1f + this.defensePower);
            float rpmBonusDamage = enemy.currentSpinRPM / 900f;
            float totalDamage = (incomingDamage + rpmBonusDamage) * 0.55f;

            if (cachedSkill == null) cachedSkill = GetComponent<SkillSystem>();
            SkillSystem enemySkill = enemy.GetComponent<SkillSystem>();
            if (cachedSkill != null && cachedSkill.IsShieldActive)
                totalDamage *= cachedSkill.DamageTakenMultiplier;
            if (enemySkill != null && enemySkill.IsRushing)
                totalDamage *= enemySkill.RushDamageMultiplier;
            if (enemy.IsDashing)
                totalDamage *= 1.25f;

            currentStamina -= totalDamage;

            float safeWeight = Mathf.Max(this.weight, 1.5f);
            float rawKnockback = totalDamage * (2.1f / safeWeight);
            float knockbackForce = Mathf.Clamp(rawKnockback, 2.0f, 10f);

            knockbackForce *= 1f - KnockbackResist;
            if (enemy.IsDashing)
                knockbackForce = Mathf.Min(12f, knockbackForce * 1.3f);

            if (cachedSkill != null && cachedSkill.IsShieldActive)
            {
                knockbackForce *= 0.5f;
                cachedSkill.OnShieldParry(enemy, -finalBounceDir);
            }

            if (enemySkill != null && enemySkill.IsRushing)
                knockbackForce = Mathf.Min(12f, knockbackForce + enemySkill.RushPushForce * 0.35f);

            // 1) Hızı yansıt (smooth arcade sekme)
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            float awaySpeed = Mathf.Max(Vector3.Dot(v, -finalBounceDir), 0f);
            v += finalBounceDir * ((awaySpeed * 1.15f + 1.5f) * (1f - KnockbackResist * 0.6f));

            // Ağır beyler de çarpma hızına göre gözle görülür savrulsun
            Vector3 rel = collision.relativeVelocity;
            float relSpeed = new Vector3(rel.x, 0f, rel.z).magnitude;
            float minAway = relSpeed * 0.5f * (1f - KnockbackResist);
            float predictedAway = Vector3.Dot(v, finalBounceDir) + knockbackForce / Mathf.Max(rb.mass, 0.1f);
            if (predictedAway < minAway)
                v += finalBounceDir * (minAway - predictedAway);

            rb.linearVelocity = new Vector3(v.x, rb.linearVelocity.y, v.z);

            // 2) Ek impulse
            rb.AddForce(finalBounceDir * knockbackForce, ForceMode.Impulse);

            if (cachedSkill != null) cachedSkill.OnCollisionEnergy();

            wobbleIntensity = Mathf.Clamp(totalDamage * 0.4f, 2f, 8f);
            wobbleTimer = 0.4f;

            if (this.attackPower > this.defensePower && enemy.defensePower > enemy.attackPower)
                currentStamina -= knockbackForce * 0.25f;

            if (topDownCamera != null)
                topDownCamera.TriggerShake(Mathf.Clamp(totalDamage * 0.014f, 0.05f, 0.2f));

            Vector3 impactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Vector3 impactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector3.up;
            SpinEffect spinFx = GetComponent<SpinEffect>();
            if (spinFx != null) spinFx.PlayImpact(impactPoint, impactNormal, totalDamage);
            SpinEffect enemyFx = enemy.GetComponent<SpinEffect>();
            if (enemyFx != null) enemyFx.PlayImpact(impactPoint, impactNormal, totalDamage);
        }
    }
}
