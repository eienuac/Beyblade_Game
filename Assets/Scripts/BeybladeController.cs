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

    public float maxStamina = 100f;
    public float currentStamina = 100f;
    public float staminaDecayRate = 2f;

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
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (capsuleCollider == null) capsuleCollider = GetComponent<CapsuleCollider>();

        LoadParts(); // Parçaları yükle
        ApplyStats(); // Statları hesapla
        FlattenVisualToRealisticShape();

        // StaminaBarUI bileşeni yoksa otomatik ekle
        if (GetComponent<StaminaBarUI>() == null)
        {
            gameObject.AddComponent<StaminaBarUI>();
        }

        // BattleManager yoksa sahneye otomatik ekle
        if (FindFirstObjectByType<BattleManager>() == null)
        {
            GameObject bm = new GameObject("BattleManager");
            bm.AddComponent<BattleManager>();
        }

        // Fizik gövdesi hiç dönmez — titreme ve Euler gimbal kilitlenmesi buradan geliyordu
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.linearDamping = 0.35f;
        rb.angularDamping = 2f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.angularVelocity = Vector3.zero;
        transform.rotation = Quaternion.identity;

        // 3D MODEL DIŞ YARIÇAPINA BİREBİR KUSURSUZ TEMAS COLLIDER AYARI
        FitColliderToVisualMesh();
        SeatVisualOnCollider();

        // ANİME DÖNME EFEKTİ (Trail + Kıvılcım) otomatik ekle
        if (GetComponent<SpinEffect>() == null)
            gameObject.AddComponent<SpinEffect>();

        // ERKEN ÇARPIŞMA ÖNLEYİCİ: Fırlatılana kadar fizikleri tamamen kapat!
        isLaunched = false;
        if (capsuleCollider != null) capsuleCollider.enabled = false;
        if (rb != null) rb.isKinematic = true; 

        // Kamera referansını bul
        topDownCamera = FindFirstObjectByType<TopDownCamera>();

        // Düşman hedefini bul
        FindEnemy();
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

    /// <summary>
    /// Parçaları Envanterden (veya Bot ise Inspectordan) yükler.
    /// </summary>
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

    /// <summary>
    /// Statları (Saldırı, Savunma, Ağırlık) parçalara göre toplar. Eğer parça yoksa eski sistemi kullanır.
    /// </summary>
    private void ApplyStats()
    {
        if (equippedLayer != null && equippedDisk != null && equippedTip != null)
        {
            // Temel değerler
            attackPower = 0;
            defensePower = 0;
            staminaDecayRate = 0;
            weight = 0;

            // Layer Etkisi (Ağırlıklı Saldırı)
            attackPower += equippedLayer.attackBonus;
            defensePower += equippedLayer.defenseBonus;
            staminaDecayRate += equippedLayer.staminaBonus;
            weight += equippedLayer.weightBonus;

            // Disk Etkisi (Ağırlıklı Savunma ve Kütle)
            attackPower += equippedDisk.attackBonus;
            defensePower += equippedDisk.defenseBonus;
            staminaDecayRate += equippedDisk.staminaBonus;
            weight += equippedDisk.weightBonus;

            // Tip Etkisi (Ağırlıklı Stamina)
            attackPower += equippedTip.attackBonus;
            defensePower += equippedTip.defenseBonus;
            staminaDecayRate += equippedTip.staminaBonus;
            weight += equippedTip.weightBonus;

            // Tür belirleme (Hangi stat daha yüksekse tipi ona çekebiliriz veya görsel olarak)
            if (attackPower > defensePower && attackPower > (5f - staminaDecayRate)) beybladeType = BeybladeType.Saldiri;
            else if (defensePower > attackPower && defensePower > (5f - staminaDecayRate)) beybladeType = BeybladeType.Savunma;
            else beybladeType = BeybladeType.Dayaniklilik;
            
        }
        else
        {
            ApplyTypeStats(); // Parça yoksa eski sisteme düş (Örn: Boş botlar)
        }

        // KÜTLE KORUMASI: Eğer oyuncu parçaların ağırlığını girmeyi unuttuysa ve ağırlık 0 ise,
        // fizik motoru patlayıp Beyblade'i 31 milyon km uzağa ışınlamasın diye minimum 0.5 yapıyoruz.
        if (weight < 0.5f) weight = 0.5f;

        if (rb != null) rb.mass = weight;
    }

    /// <summary>
    /// Taş-Kağıt-Makas Dengesi (Sadece parça takılı değilse çalışır)
    /// </summary>
    private void ApplyTypeStats()
    {
        switch (beybladeType)
        {
            case BeybladeType.Saldiri: // Attack
                weight = 2.0f;           // Orta Hafif (Hareketli olması için)
                attackPower = 14f;       // Çok yüksek hasar
                defensePower = 0.2f;     // Çok düşük savunma
                staminaDecayRate = 3.5f; // Çok hızlı yorulur
                break;
                
            case BeybladeType.Savunma: // Defense
                weight = 4.0f;           // Çok Ağır (Yerinden oynamaz)
                attackPower = 4f;        // Düşük hasar
                defensePower = 0.8f;     // %80 hasar azaltma
                staminaDecayRate = 2.8f; // Ağır olduğu için hızlı yorulur
                break;
                
            case BeybladeType.Dayaniklilik: // Stamina
                weight = 1.6f;           // Çok Hafif (Uzun dönebilmek için)
                attackPower = 3f;        // Çok düşük hasar
                defensePower = 0.1f;     // Çok düşük savunma
                staminaDecayRate = 1.2f; // Çok yavaş yorulur
                break;
                
            case BeybladeType.Denge: // Balance
                weight = 2.5f;
                attackPower = 7f;
                defensePower = 0.4f;
                staminaDecayRate = 2.5f;
                break;
        }

        if (rb != null) rb.mass = weight;
    }

    private void FitColliderToVisualMesh()
    {
        Bounds combinedBounds = GetVisualWorldBounds();
        float worldRadius = 0.45f;
        float worldHeight = 0.32f;

        if (combinedBounds.size.sqrMagnitude > 0.0001f)
        {
            worldRadius = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.z);
            worldHeight = combinedBounds.size.y;
        }

        float rootScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        if (rootScale < 0.001f) rootScale = 1.0f;
        
        float localRadius = (worldRadius / rootScale) * colliderRadiusMultiplier;
        float localHeight = Mathf.Clamp(worldHeight / rootScale, 0.18f, 0.55f);

        if (capsuleCollider != null)
        {
            capsuleCollider.radius = Mathf.Max(0.12f, localRadius);
            capsuleCollider.height = Mathf.Max(localHeight, capsuleCollider.radius * 1.15f);
            capsuleCollider.direction = 1; // Y-axis
            capsuleCollider.center = Vector3.zero;
            capsuleCollider.contactOffset = 0.005f;
        }

        PhysicsMaterial noBounceMat = new PhysicsMaterial("BeybladeNoBounceMat");
        noBounceMat.bounciness = 0.0f;
        noBounceMat.dynamicFriction = 0.15f;
        noBounceMat.staticFriction = 0.15f;
        noBounceMat.bounceCombine = PhysicsMaterialCombine.Minimum;
        noBounceMat.frictionCombine = PhysicsMaterialCombine.Minimum;
        if (capsuleCollider != null) capsuleCollider.material = noBounceMat;
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
        // FIRLATILMADIYSA FİZİK VE ÇARPIŞMAYI KAPAT, AMA GÖRSEL OLARAK DÖNMEYE İZİN VER
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

        // Fırlatıldıysa Stamina azalsın
        if (isLaunched && currentStamina > 0)
        {
            currentStamina -= staminaDecayRate * Time.deltaTime;
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
        if (speed > 0.4f)
        {
            float staminaRatio = Mathf.Clamp01(currentStamina / maxStamina);
            float maxTilt = Mathf.Lerp(10f, 4f, staminaRatio);
            float tiltAngle = Mathf.Clamp(speed * 0.7f, 0f, maxTilt);
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
        VisualPivot.localRotation = Quaternion.Slerp(VisualPivot.localRotation, targetTilt, 1f - Mathf.Exp(-10f * dt));
    }

    private void FixedUpdate()
    {
        if (!isSpinning || rb == null || !isLaunched) return;

        // Düşman hedefi yoksa bul
        if (enemyTarget == null) FindEnemy();

        // Zemin: şiddetli Y kesme + downforce mesh collider'da titreme yapıyordu
        Vector3 velY = rb.linearVelocity;
        if (velY.y > 0.25f) velY.y *= 0.4f;
        if (velY.y < -6f) velY.y = -6f;
        rb.linearVelocity = velY;
        rb.AddForce(Vector3.down * 10f, ForceMode.Acceleration);

        // ====================================
        //  BEYBLADE HAREKET FİZİĞİ
        // ====================================
        
        // HEDEF BELİRLEME: Saldırı tipi düşmana, diğerleri merkeze
        Vector3 targetPoint;
        
        if (beybladeType == BeybladeType.Saldiri && enemyTarget != null && enemyTarget.isLaunched && enemyTarget.isSpinning)
        {
            // SALDIRI TİPİ: Düşmana doğru agresif yönelme!
            targetPoint = enemyTarget.transform.position;
        }
        else
        {
            // DİĞER TİPLER: Arena merkezine yönel
            targetPoint = Vector3.zero;
        }

        Vector3 dirToTarget = (targetPoint - transform.position);
        dirToTarget.y = 0;
        float distToTarget = dirToTarget.magnitude;

        // HAREKET AGRESİFLİĞİ (Tipe göre)
        float basePull;
        float orbitForce;
        
        if (beybladeType == BeybladeType.Saldiri)
        {
            // Saldırı: Düşmana doğru güçlü çekim, yaklaştıkça hızlanır
            basePull = 8.0f;
            orbitForce = 0.15f; // Az yörünge, daha çok düz saldırı
            
            // Düşmana yaklaştıkça daha agresif
            if (distToTarget < 5f && enemyTarget != null)
            {
                basePull = 14.0f; // Yakınken çok agresif
            }
        }
        else if (beybladeType == BeybladeType.Dayaniklilik)
        {
            basePull = 8.0f;   // Hızla merkeze oturur
            orbitForce = 0.2f;
        }
        else if (beybladeType == BeybladeType.Savunma)
        {
            basePull = 6.0f;   // Merkezde ağır durur
            orbitForce = 0.15f;
        }
        else
        {
            basePull = 5.0f;
            orbitForce = 0.3f;
        }

        float rpmFactor = currentSpinRPM / maxSpinRPM;
        float totalPull = basePull + rpmFactor * 4f;
        rb.AddForce(dirToTarget.normalized * totalPull, ForceMode.Acceleration);

        // YÖRÜNGESEL DÖNÜŞ (Orbital Swirl) - Sadece merkeze yönelenlerde belirgin
        if (distToTarget > 0.3f)
        {
            Vector3 tangent = Vector3.Cross(dirToTarget.normalized, Vector3.up);
            rb.AddForce(tangent * (totalPull * orbitForce), ForceMode.Acceleration);
        }

        // Pürüzsüz Perlin sapması (Random.insideUnitCircle her fizik karesinde titretiyordu)
        float t = Time.time * 0.55f;
        float drift = rpmFactor * 0.55f;
        Vector3 sway = new Vector3(
            (Mathf.PerlinNoise(t, noiseSeed) - 0.5f) * 2f,
            0f,
            (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f
        ) * drift;
        rb.AddForce(sway, ForceMode.Acceleration);

        // HIZ LİMİTİ: Beyblade'in saçma hızlara ulaşmasını engelle
        Vector3 flatVel = rb.linearVelocity;
        flatVel.y = 0;
        float maxSpeed = 15f;
        if (flatVel.magnitude > maxSpeed)
        {
            flatVel = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(flatVel.x, rb.linearVelocity.y, flatVel.z);
        }
    }

    // Başka Beyblade ile Çarpışma
    private void OnCollisionEnter(Collision collision)
    {
        BeybladeController enemy = collision.gameObject.GetComponent<BeybladeController>();

        if (enemy != null && isSpinning && isLaunched && enemy.isLaunched && Time.time - lastCollisionTime > 0.15f)
        {
            lastCollisionTime = Time.time;

            Vector3 hitDirection = (transform.position - collision.transform.position);
            hitDirection.y = 0;

            if (hitDirection.sqrMagnitude < 0.01f)
            {
                hitDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            }

            Vector3 normalHit = hitDirection.normalized;
            Vector3 sideScatter = Vector3.Cross(normalHit, Vector3.up) * Random.Range(-0.3f, 0.3f);
            Vector3 finalBounceDir = (normalHit + sideScatter).normalized;
            finalBounceDir.y = 0f;

            // 1. DÜŞMANDAN ALINAN HASAR HESAPLAMASI
            float incomingDamage = enemy.attackPower / (1f + this.defensePower);
            
            // Dönüş hızına bağlı bonus güç
            float rpmBonusDamage = (enemy.currentSpinRPM / 400f);
            float totalDamage = incomingDamage + rpmBonusDamage;

            // Stamina Düşüşü
            currentStamina -= totalDamage;

            // 2. FİZİKSEL GERİ SEKME (KNOCKBACK) - Çok azaltıldı!
            float rawKnockback = totalDamage * (1.2f / this.weight);
            float knockbackForce = Mathf.Clamp(rawKnockback, 0.5f, 8f);
            
            rb.AddForce(finalBounceDir * knockbackForce, ForceMode.Impulse);

            // 3. ÇARPIŞMA SAVRULMASI (sadece görsel pivot)
            wobbleIntensity = Mathf.Clamp(totalDamage * 0.35f, 1.2f, 6f);
            wobbleTimer = 0.35f;

            // 4. RECOIL (Saldırganın kendi aldığı tepme hasarı)
            if (this.attackPower > this.defensePower && enemy.defensePower > enemy.attackPower)
            {
                currentStamina -= (knockbackForce * 0.3f);
            }

            if (topDownCamera != null)
            {
                topDownCamera.TriggerShake(Mathf.Clamp(totalDamage * 0.012f, 0.04f, 0.18f));
            }

            Vector3 impactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Vector3 impactNormal = collision.contactCount > 0 ? collision.GetContact(0).normal : Vector3.up;
            SpinEffect spinFx = GetComponent<SpinEffect>();
            if (spinFx != null) spinFx.PlayImpact(impactPoint, impactNormal, totalDamage);
            SpinEffect enemyFx = enemy.GetComponent<SpinEffect>();
            if (enemyFx != null) enemyFx.PlayImpact(impactPoint, impactNormal, totalDamage);
        }
    }
}
