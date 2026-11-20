using UnityEngine;
using BeybladeCore;

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
    [Tooltip("1 = temas tam kanat ucunda. Küçültmek iç içe girme, büyütmek boşlukta çarpma gibi görünür.")]
    [Range(0.9f, 1.1f)]
    public float rimContactScale = 1f;

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

    // Stat → davranış. Formüller BeybladeCore.Combat'ta (simülasyonla ortak).
    /// <summary>Normalize statlar + tip. Çarpışma, hareket ve dönüş kaybı bunu kullanır.</summary>
    public FighterProfile Profile => FighterProfile.FromRaw((BeyArchetype)(int)beybladeType, attackPower, defensePower, staminaDecayRate, weight);
    public float AttackNorm => Profile.A;
    public float DefenseNorm => Profile.D;
    public float WeightNorm => Profile.W;
    public float MaxSpeed => Combat.MaxSpeed(Profile, CombatTuning.Default);
    public float KnockbackResist => Combat.KnockbackResist(Profile, CombatTuning.Default);
    public float StaminaRatio => Mathf.Clamp01(currentStamina / Mathf.Max(1f, maxStamina));
    public float RpmRatio => Mathf.Clamp01(currentSpinRPM / Mathf.Max(1f, maxSpinRPM));
    /// <summary>Stamina bittikçe 1 → 1.8: yorgun bey daha çok savrulur.</summary>
    public float LowStaminaKnockbackMult => Combat.FatigueKnockMult(StaminaRatio);
    /// <summary>Stamina bittikçe darbe yalpası büyür.</summary>
    public float FatigueWobbleScale => Mathf.Lerp(1.9f, 1f, StaminaRatio);
    /// <summary>Savrulma hızına uygulanacak toplam ölçek (direnç + yorgunluk).</summary>
    public float KnockbackTakenScale => (1f - KnockbackResist) * LowStaminaKnockbackMult;
    /// <summary>Son FixedUpdate sonundaki yatay hız (çarpışma öncesi hız olarak kullanılır).</summary>
    public Vector3 LastFlatVelocity => lastFlatVelocity;
    public SkillSystem Skill => cachedSkill != null ? cachedSkill : (cachedSkill = GetComponent<SkillSystem>());

    // Görsel rig: fizik gövdesi dik kalır, mesh ayrı döner (titreme/gimbal yok)
    public Transform VisualPivot { get; private set; }
    public Transform VisualSpin { get; private set; }
    private float wobbleTimer = 0f;
    private float wobbleIntensity = 0f;
    private float spinAngleY = 0f;
    private float leftoverSpinDeg = 0f;
    private Quaternion targetTilt = Quaternion.identity;
    private float noiseSeed;
    private float orbitSign = 1f;
    private float precessionPhase;
    private Vector3 lastFlatVelocity;
    private MovementProfile movementProfile;
    private FighterProfile? profileOverride;

    [Header("Görünüm")]
    [Tooltip("Açık: takılı parçalara göre koddan üretilen özgün model. Kapalı: sahnedeki model.")]
    public bool useProceduralVisual = true;
    [Tooltip("Prosedürel modelin dünya yarıçapı (m)")]
    public float proceduralRadius = 0.5f;

    [Header("Arena")]
    [Tooltip("Stadyum eğimine ek merkeze çekim (m/s², kenarda). Stadyum modeli yeterince çukur değilse artır.")]
    public float arenaCentering = 1.6f;

    // Düşman Takip (Attack tipi için)
    private BeybladeController enemyTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        noiseSeed = Random.Range(0f, 1000f);
        orbitSign = Random.value > 0.5f ? 1f : -1f;
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

        ApplyProceduralVisual();
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

    /// <summary>Sahnedeki modeli gizler, parçalara göre üretilen özgün modeli VisualSpin altına koyar.</summary>
    private void ApplyProceduralVisual()
    {
        if (!useProceduralVisual || VisualSpin == null) return;
        foreach (Renderer r in VisualSpin.GetComponentsInChildren<Renderer>(true)) r.enabled = false;

        BeyVisual look = ResolveVisual();
        float spinScale = Mathf.Max(0.0001f, Mathf.Abs(VisualSpin.lossyScale.x));
        GameObject model = ProceduralBeyFactory.BuildBey(look, VisualSpin, proceduralRadius / spinScale, out _);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
    }

    /// <summary>Görünüm: oyuncu → takılı parçalar; bot → tipine uygun set.</summary>
    private BeyVisual ResolveVisual()
    {
        if (MatchContext.TryGetForcedArchetype(isPlayer, out BeyArchetype forced))
            return BeyDesignCatalog.ForArchetype(forced);
        if (equippedLayer != null && equippedDisk != null && equippedTip != null)
            return BeyDesignCatalog.Assemble(equippedLayer.partID, equippedDisk.partID, equippedTip.partID);
        return BeyDesignCatalog.ForArchetype((BeyArchetype)(int)beybladeType);
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
        partDatabase = PartDatabase.Resolve(partDatabase);
        profileOverride = null;

        if (MatchContext.TryGetForcedArchetype(isPlayer, out BeyArchetype forced))
        {
            // Denge testi: parçasız, eşit bütçeli tip şablonu
            equippedLayer = equippedDisk = equippedTip = null;
            profileOverride = ArchetypePresets.Get(forced);
            return;
        }

        if (isPlayer && PlayerDataManager.Instance != null)
        {
            var d = PlayerDataManager.Instance.data;
            equippedLayer = partDatabase.GetPartByID(d.equippedLayerID) ?? partDatabase.GetPartByID(DefaultPartCatalog.StarterLayer);
            equippedDisk = partDatabase.GetPartByID(d.equippedDiskID) ?? partDatabase.GetPartByID(DefaultPartCatalog.StarterDisk);
            equippedTip = partDatabase.GetPartByID(d.equippedTipID) ?? partDatabase.GetPartByID(DefaultPartCatalog.StarterTip);
        }
        else if (!isPlayer)
        {
            equippedLayer = equippedDisk = equippedTip = null;
            if (MatchContext.IsCareer)
            {
                // Kariyer botu: tip seviyeye göre döner
                profileOverride = ArchetypePresets.Get(BotDifficulty.ArchetypeForLevel(MatchContext.CareerLevel));
            }
            else
            {
                // Hızlı savaş: oyuncunun beyiyle eşit güçte (aynı stat bütçesi) Savunma beyi
                float budget = PlayerLoadout.ComputeProfile(partDatabase).Budget;
                profileOverride = ArchetypePresets.ForBudget(MatchContext.QuickMatchRivalType, budget);
            }
        }
    }

    private void ApplyStats()
    {
        maxStamina = CombatTuning.Default.MaxStamina;

        if (profileOverride.HasValue)
        {
            SetFromProfile(profileOverride.Value);
        }
        else if (equippedLayer != null && equippedDisk != null && equippedTip != null)
        {
            BeyStatBlock s = BeyStatCalculator.Compute(equippedLayer.ToStats(), equippedDisk.ToStats(), equippedTip.ToStats());
            attackPower = s.Attack;
            defensePower = s.Defense;
            staminaDecayRate = s.StaminaDecay;
            weight = s.Weight;
            beybladeType = (BeybladeType)(int)s.Archetype;
        }
        else
        {
            // Inspector'dan tip seçilmiş, parça yok: o tipin eşit bütçeli şablonu
            SetFromProfile(ArchetypePresets.Get((BeyArchetype)(int)beybladeType));
        }

        // Kariyerde bot seviyesi arttıkça biraz daha güçlü (hızlı savaş eşit kalır)
        if (!isPlayer && MatchContext.IsCareer && !MatchContext.IsBalanceTest)
        {
            BotDifficulty diff = MatchContext.Difficulty;
            attackPower *= diff.AttackScale;
            staminaDecayRate *= diff.StaminaDecayScale;
        }

        defensePower = Mathf.Clamp(defensePower, BeyStatCalculator.DefenseMin, BeyStatCalculator.DefenseMax);
        attackPower = Mathf.Clamp(attackPower, BeyStatCalculator.AttackMin, BeyStatCalculator.AttackMax);
        staminaDecayRate = Mathf.Clamp(staminaDecayRate, BeyStatCalculator.DecayMin, BeyStatCalculator.DecayMax);
        weight = Mathf.Clamp(weight, BeyStatCalculator.WeightMin, BeyStatCalculator.WeightMax);

        if (rb != null) rb.mass = weight;
        movementProfile = Movement.ProfileFor(Profile);
    }

    private void SetFromProfile(FighterProfile p)
    {
        attackPower = p.AttackPower;
        defensePower = p.DefensePower;
        staminaDecayRate = p.StaminaDecay;
        weight = p.Mass;
        beybladeType = (BeybladeType)(int)p.Archetype;
    }

    private void FitColliderToVisualMesh()
    {
        Bounds combinedBounds = GetVisualWorldBounds();
        float worldRadius = 0.55f;
        float worldHeight = 0.32f;

        if (combinedBounds.size.sqrMagnitude > 0.0001f)
        {
            worldRadius = MeasureTipRadius(combinedBounds);
            worldHeight = combinedBounds.size.y;
        }

        worldRadius = Mathf.Clamp(worldRadius, 0.3f, 2.5f);
        worldHeight = Mathf.Clamp(worldHeight, 0.22f, 0.55f);

        float rootScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        if (rootScale < 0.001f) rootScale = 1.0f;

        float localRadius = (worldRadius / rootScale) * rimContactScale;
        float localHeight = Mathf.Clamp(worldHeight / rootScale, 0.2f, 0.55f);

        if (capsuleCollider != null)
        {
            capsuleCollider.radius = Mathf.Max(0.2f, localRadius);
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

    /// <summary>
    /// Spin ekseninden en uzak vertex'in yatay mesafesi (kanat ucu).
    /// AABB dönen kanatlarda ucu kaçırır; mesh okunamazsa ona düşer.
    /// </summary>
    private float MeasureTipRadius(Bounds fallback)
    {
        float best = 0f;
        bool any = false;
        Vector3 axis = VisualSpin != null ? VisualSpin.position : transform.position;

        MeshFilter[] filters = VisualSpin != null ? VisualSpin.GetComponentsInChildren<MeshFilter>() : new MeshFilter[0];
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter mf = filters[i];
            if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable) continue;
            MeshRenderer r = mf.GetComponent<MeshRenderer>();
            if (r == null || !r.enabled) continue;
            string n = mf.gameObject.name;
            if (n.StartsWith("Trail") || n.StartsWith("Motion")) continue;

            Vector3[] verts = mf.sharedMesh.vertices;
            Matrix4x4 m = mf.transform.localToWorldMatrix;
            for (int v = 0; v < verts.Length; v++)
            {
                Vector3 w = m.MultiplyPoint3x4(verts[v]);
                float dx = w.x - axis.x;
                float dz = w.z - axis.z;
                float d2 = dx * dx + dz * dz;
                if (d2 > best) best = d2;
            }
            any = true;
        }

        if (any && best > 0.0001f) return Mathf.Sqrt(best);
        return Mathf.Max(fallback.extents.x, fallback.extents.z);
    }

    private void SeatVisualOnCollider()
    {
        if (VisualSpin == null || capsuleCollider == null) return;

        Bounds world = GetVisualWorldBounds();
        if (world.size.sqrMagnitude < 0.0001f) return;

        float scaleY = Mathf.Abs(transform.lossyScale.y);
        if (scaleY < 0.001f) scaleY = 1f;
        float rootScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
        // Yükseklik < 2r olunca kapsül küreye döner; gerçek alt nokta yarıçaptır
        float halfExtentY = Mathf.Max(capsuleCollider.height * 0.5f * scaleY, capsuleCollider.radius * rootScale);
        float colliderBottom = transform.position.y + capsuleCollider.center.y * scaleY - halfExtentY;
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
            currentStamina -= Combat.NaturalDrainPerSecond(Profile, CombatTuning.Default) * Time.deltaTime;
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

        float staminaRatio = StaminaRatio;
        float fatigue = 1f - staminaRatio;

        Quaternion lean = Quaternion.identity;
        if (speed > 0.15f)
        {
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

        // Stamina azaldıkça sürekli presesyon yalpası (dönen eğim), sona doğru sertleşir
        float precessAmp = Mathf.Lerp(0.6f, 14f, fatigue * fatigue);
        precessionPhase += dt * Mathf.Lerp(9f, 5.5f, fatigue);
        float nutation = 1f + 0.3f * fatigue * Mathf.Sin(Time.time * 3.3f + noiseSeed);
        lean *= Quaternion.Euler(
            Mathf.Sin(precessionPhase) * precessAmp * nutation,
            0f,
            Mathf.Cos(precessionPhase) * precessAmp * nutation);

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
        if (cachedSkill != null && cachedSkill.OverridesMovement)
        {
            CacheFlatVelocity();
            return;
        }

        CombatTuning tune = CombatTuning.Default;
        // Çarpışma sonrası kısa stun: otomatik hareket sekmeyi yemesin
        bool stunned = Time.time < seekStunUntil;
        bool steering = steerInput.sqrMagnitude > 0.0025f;

        Vector3 center = ArenaInfo.Known ? ArenaInfo.Center : Vector3.zero;
        float arenaRadius = ArenaInfo.Known ? ArenaInfo.Radius : tune.ArenaRadius;
        Vector3 rel = transform.position - center;
        rel.y = 0f;
        Vector3 v = rb.linearVelocity;
        Vec2 pos = new Vec2(rel.x, rel.z);
        Vec2 vel = new Vec2(v.x, v.z);

        // Stadyum çukuru: kenara gittikçe merkeze çeker (her zaman)
        float r = rel.magnitude;
        if (r > 0.001f && arenaCentering > 0f)
            rb.AddForce(-rel / r * (arenaCentering * Mathf.Min(1.2f, r / Mathf.Max(1f, arenaRadius))), ForceMode.Acceleration);

        if (!stunned)
        {
            // Tipin kendi hareketi: Saldırı dış halkada hızlı tur, Denge orta halka, Dayanıklılık merkeze yakın süzülme, Savunma merkezi tutar
            // Rakibe dalgalı çekim: gelir, çarpışır, açılır, tekrar gelir (kilitlenmeden)
            bool hasEnemy = enemyTarget != null && enemyTarget.isLaunched && !enemyTarget.hasToppled;
            float engage = hasEnemy ? Movement.EngageWeight(movementProfile, Time.time, noiseSeed) : 0f;
            Vec2 auto = Movement.AutoAccel(movementProfile, pos, vel, arenaRadius, RpmRatio, orbitSign, engage);
            if (hasEnemy)
            {
                Vector3 ep = enemyTarget.transform.position - center;
                auto += Movement.EngageAccel(movementProfile, pos, vel, new Vec2(ep.x, ep.z), RpmRatio, engage);
            }
            if (steering) auto *= 0.35f;
            rb.AddForce(new Vector3(auto.X, 0f, auto.Z), ForceMode.Acceleration);

            float t = Time.time * 0.55f;
            Vector3 sway = new Vector3(
                (Mathf.PerlinNoise(t, noiseSeed) - 0.5f) * 2f,
                0f,
                (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f
            ) * movementProfile.Wander * RpmRatio;
            rb.AddForce(sway, ForceMode.Acceleration);

            // Yorgun bey yolunda da yalpalar (küçük dairesel salınım)
            float fatigue = 1f - StaminaRatio;
            if (fatigue > 0.3f)
            {
                float amp = Mathf.Lerp(0f, 3f, (fatigue - 0.3f) / 0.7f);
                float p = Time.time * 5.5f + noiseSeed;
                rb.AddForce(new Vector3(Mathf.Sin(p), 0f, Mathf.Cos(p)) * amp, ForceMode.Acceleration);
            }
        }

        if (steering)
        {
            float steerAccel = Movement.SteerAccel(Profile);
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

        // Savunma: ekstra sürtünme ile kaçışı kes (ağır, yerinde duran bey)
        if (beybladeType == BeybladeType.Savunma && !steering && !IsDashing && !stunned)
        {
            Vector3 dv = rb.linearVelocity;
            float k = Mathf.Exp(-tune.DefenseFriction * Time.fixedDeltaTime);
            dv.x *= k;
            dv.z *= k;
            rb.linearVelocity = dv;
        }

        CacheFlatVelocity();
    }

    private void CacheFlatVelocity()
    {
        if (rb == null) return;
        Vector3 v = rb.linearVelocity;
        lastFlatVelocity = new Vector3(v.x, 0f, v.z);
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

    /// <summary>Anlık güçlü savrulma (ulti finali). Hız tavanı kısa süre açılır.</summary>
    public void ApplyBurstKnockback(Vector3 dir, float speed)
    {
        if (rb == null || rb.isKinematic || !isLaunched || !isSpinning) return;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        Vector3 v = rb.linearVelocity;
        Vector3 flat = new Vector3(v.x, 0f, v.z);
        float along = Vector3.Dot(flat, dir);
        flat += dir * (Mathf.Max(0f, -along) + speed);
        rb.linearVelocity = new Vector3(flat.x, v.y, flat.z);

        overspeedUntil = Time.time + 0.7f;
        overspeedMult = Mathf.Max(1.7f, (flat.magnitude + 1f) / Mathf.Max(1f, MaxSpeed));
        seekStunUntil = Mathf.Max(seekStunUntil, Time.time + 0.7f);
        wobbleIntensity = 9f * FatigueWobbleScale;
        wobbleTimer = 0.55f;
    }

    public void ForceRingOut()
    {
        if (!isLaunched || hasToppled) return;
        WasRingedOut = true;
        currentStamina = 0f;
    }

    // Başka Beyblade ile çarpışma: hasar, sekme ve savrulma BeybladeCore.Combat ile hesaplanır
    // (denge simülasyonuyla birebir aynı formüller).
    private void OnCollisionEnter(Collision collision)
    {
        BeybladeController enemy = collision.gameObject.GetComponent<BeybladeController>();
        if (enemy == null || !isSpinning || !isLaunched || !enemy.isLaunched) return;

        CombatTuning tune = CombatTuning.Default;
        if (Time.time - lastCollisionTime < tune.CollisionCooldown) return;
        lastCollisionTime = Time.time;
        seekStunUntil = Time.time + (beybladeType == BeybladeType.Savunma ? 0.3f : 0.28f);

        Vector3 toEnemy = enemy.transform.position - transform.position;
        toEnemy.y = 0f;
        if (toEnemy.sqrMagnitude < 0.0001f)
            toEnemy = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        Vector3 n = toEnemy.normalized;
        Vec2 n2 = new Vec2(n.x, n.z);

        // Çarpışma öncesi hızlar (Unity çözücüsü hızı değiştirmiş olabilir)
        Vec2 myVel = new Vec2(lastFlatVelocity.x, lastFlatVelocity.z);
        Vec2 enVel = new Vec2(enemy.LastFlatVelocity.x, enemy.LastFlatVelocity.z);
        float myApproach = Vec2.Dot(myVel, n2);
        float enApproach = Vec2.Dot(enVel, -n2);

        if (cachedSkill == null) cachedSkill = GetComponent<SkillSystem>();
        SkillSystem enemySkill = enemy.Skill;
        bool myShield = cachedSkill != null && cachedSkill.IsShieldActive;
        bool selfUlt = cachedSkill != null && cachedSkill.IsUltDragging;
        bool enemyUlt = enemySkill != null && enemySkill.IsUltDragging;
        bool enemyRush = enemySkill != null && enemySkill.IsRushing;

        var hit = Combat.ResolveHit(new HitInput
        {
            Self = Profile,
            Other = enemy.Profile,
            SelfRpm = RpmRatio,
            OtherRpm = enemy.RpmRatio,
            SelfStaminaRatio = StaminaRatio,
            SelfApproach = myApproach,
            OtherApproach = enApproach,
            OtherDashing = enemy.IsDashing,
            SelfDamageTakenMult = cachedSkill != null ? cachedSkill.DamageTakenMultiplier : 1f,
            OtherDamageMult = enemyRush ? enemySkill.RushDamageMultiplier : (enemyUlt ? 1.1f : 1f),
            SelfKnockbackMult = myShield ? SkillTuning.ShieldKnockMult : 1f,
            OtherPushBonus = enemyRush ? enemySkill.RushPushForce * 0.35f : 0f
        }, tune);

        currentStamina -= hit.Damage;

        // Ulti sırasında sahibi savrulmaz, rakip savrulmak yerine sürüklenir (SkillSystem)
        if (!selfUlt && !enemyUlt && rb != null)
        {
            Rigidbody erb = enemy.GetComponent<Rigidbody>();
            float enemyMass = erb != null ? erb.mass : enemy.weight;
            Vec2 bounced = Combat.BounceVelocity(myVel, enVel, n2, rb.mass, enemyMass, tune.Restitution);
            Vec2 side = n2.Perp * (Random.Range(-0.2f, 0.2f) * hit.KnockbackSpeed);
            Vec2 final = bounced - n2 * hit.KnockbackSpeed + side;
            rb.linearVelocity = new Vector3(final.X, rb.linearVelocity.y, final.Z);
        }

        if (myShield) cachedSkill.OnShieldParry(enemy, n);
        if (cachedSkill != null) cachedSkill.OnCollisionEnergy();

        wobbleIntensity = Mathf.Clamp(hit.Damage * 0.4f, 2f, 8f) * FatigueWobbleScale;
        wobbleTimer = 0.4f;

        if (topDownCamera != null)
        {
            topDownCamera.TriggerShake(Mathf.Clamp(hit.Damage * 0.014f + hit.Impact * 0.04f, 0.04f, 0.2f));
            topDownCamera.NotifyBeyImpact(transform, enemy.transform);
        }

        Vector3 impactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position + n * 0.4f;
        Vector3 impactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector3.up;
        SpinEffect spinFx = GetComponent<SpinEffect>();
        if (spinFx != null) spinFx.PlayImpact(impactPoint, impactNormal, hit.Damage * 1.6f);
    }
}
