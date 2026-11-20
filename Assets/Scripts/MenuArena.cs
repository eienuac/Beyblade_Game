using UnityEngine;
using BeybladeCore;

/// <summary>
/// Menünün arkasında canlı savaş: koddan üretilen stadyumda iki bot bey savaşır.
/// Hareket ve kararlar BattleSim + BotBrain'den gelir (gerçek oyundaki AI ve denge ile aynı).
/// Biri düşünce kısa bir bekleyişten sonra yeni tur başlar; kamera yavaşça arenanın etrafında döner.
/// </summary>
public class MenuArena : MonoBehaviour
{
    public static MenuArena Instance { get; private set; }

    [Header("Kamera")]
    public float orbitDegPerSec = 5f;
    public float orbitRadius = 11.4f;
    public float cameraHeight = 7.8f;
    [Tooltip("Bakış noktası: aşağı kaydırmak arenayı ekranda yukarı taşır (alt kartların arkasında kalmasın)")]
    public Vector3 lookOffset = new Vector3(0f, -2.4f, 0f);

    [Header("Savaş")]
    public float simSpeed = 1f;
    public int botLevel = 7;
    public float beyRadius = 0.5f;

    class Actor
    {
        public Transform root, tilt, spin;
        public float contactY;
        public float spinAngle;
        public float fallT, outT;
        public Vector3 fallAxis, outVel, lastPos;
        public bool wasOut;
    }

    Camera cam;
    BattleSim sim;
    readonly Actor[] actors = new Actor[2];
    float acc, endTimer, orbitAngle, shake;
    int lastCollisions;
    ParticleSystem sparks;
    Light impactLight;
    Transform stadium;
    System.Random rng = new System.Random();

    public static MenuArena Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("MenuArena");
        return go.AddComponent<MenuArena>();
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("MenuCamera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.035f, 0.07f, 1f);
        cam.fieldOfView = 44f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 200f;

        stadium = ProceduralBeyFactory.BuildStadium(transform).transform;

        // Neon dolgu ışığı (arena ortasının üstünde)
        var fill = new GameObject("ArenaFill").AddComponent<Light>();
        fill.transform.SetParent(transform, false);
        fill.transform.localPosition = new Vector3(0f, 4.5f, 0f);
        fill.type = LightType.Point;
        fill.range = 14f;
        fill.intensity = 2.2f;
        fill.color = new Color(0.45f, 0.75f, 1f);

        impactLight = new GameObject("ImpactLight").AddComponent<Light>();
        impactLight.transform.SetParent(transform, false);
        impactLight.type = LightType.Point;
        impactLight.range = 4f;
        impactLight.intensity = 0f;
        impactLight.color = new Color(1f, 0.75f, 0.35f);

        // Sahnede yön ışığı yoksa ekle
        if (!HasDirectionalLight())
        {
            var sun = new GameObject("MenuSun").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(52f, 30f, 0f);
        }

        sparks = CreateSparks(transform);
        orbitAngle = (float)rng.NextDouble() * 360f;
        StartRound();
        UpdateCamera(0f);
    }

    static bool HasDirectionalLight()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional && l.enabled) return true;
        return false;
    }

    // ------------------------------------------------------------------ Tur

    void StartRound()
    {
        for (int i = 0; i < 2; i++)
            if (actors[i] != null && actors[i].root != null) Destroy(actors[i].root.gameObject);

        BeyArchetype a = (BeyArchetype)rng.Next(4);
        BeyArchetype b = (BeyArchetype)rng.Next(4);
        if (b == a && rng.NextDouble() < 0.75) b = (BeyArchetype)(((int)a + 1 + rng.Next(3)) % 4);

        var setup = new SimSetup
        {
            A = ArchetypePresets.Get(a),
            B = ArchetypePresets.Get(b),
            PowerA = 0.8f + (float)rng.NextDouble() * 0.15f,
            PowerB = 0.8f + (float)rng.NextDouble() * 0.15f,
            BrainA = BotDifficulty.ForLevel(botLevel),
            BrainB = BotDifficulty.ForLevel(botLevel),
            Seed = rng.Next(1, int.MaxValue),
            MaxTime = 75f
        };
        sim = new BattleSim(setup);
        lastCollisions = 0;
        acc = 0f;
        endTimer = -1f;

        actors[0] = SpawnActor(VisualFor(a), sim.View(0));
        actors[1] = SpawnActor(VisualFor(b), sim.View(1));
    }

    BeyVisual VisualFor(BeyArchetype t)
    {
        // Saldırı tipine çeşitlilik: bazen Yıldırım
        if (t == BeyArchetype.Attack && rng.NextDouble() < 0.5)
            return BeyDesignCatalog.Assemble("layer_yildirim", "disk_basic", "tip_duz");
        return BeyDesignCatalog.ForArchetype(t);
    }

    Actor SpawnActor(BeyVisual look, FighterView v)
    {
        var act = new Actor();
        act.root = new GameObject("MenuBey").transform;
        act.root.SetParent(transform, false);
        act.tilt = new GameObject("Tilt").transform;
        act.tilt.SetParent(act.root, false);
        act.spin = new GameObject("Spin").transform;
        act.spin.SetParent(act.tilt, false);
        ProceduralBeyFactory.BuildBey(look, act.spin, beyRadius, out act.contactY);
        act.spinAngle = (float)rng.NextDouble() * 360f;
        act.lastPos = FloorPoint(v.Pos, act.contactY);
        act.root.localPosition = act.lastPos;
        return act;
    }

    static Vector3 FloorPoint(Vec2 p, float contactY)
    {
        float r = p.Length;
        return new Vector3(p.X, BeyMeshBuilder.StadiumFloorY(r) - contactY, p.Z);
    }

    // ------------------------------------------------------------------ Döngü

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        if (sim == null) return;

        acc += dt * simSpeed;
        int steps = 0;
        while (acc >= BattleSim.Dt && steps < 8)
        {
            sim.StepOnce();
            acc -= BattleSim.Dt;
            steps++;
        }

        if (sim.Collisions > lastCollisions)
        {
            lastCollisions = sim.Collisions;
            Vec2 ip = sim.LastImpactPoint;
            Vector3 point = new Vector3(ip.X, BeyMeshBuilder.StadiumFloorY(ip.Length) + 0.25f, ip.Z);
            EmitSparks(point);
            impactLight.transform.localPosition = point + Vector3.up * 0.3f;
            impactLight.intensity = 6f;
            shake = Mathf.Max(shake, 0.06f);
        }
        impactLight.intensity = Mathf.MoveTowards(impactLight.intensity, 0f, dt * 30f);

        for (int i = 0; i < 2; i++) UpdateActor(actors[i], sim.View(i), dt);

        if (sim.IsOver)
        {
            if (endTimer < 0f) endTimer = 2.6f;
            endTimer -= dt;
            if (endTimer <= 0f) StartRound();
        }

        UpdateCamera(dt);
    }

    void UpdateActor(Actor a, FighterView v, float dt)
    {
        if (a == null || a.root == null) return;

        if (v.Out)
        {
            // Arenadan uçuş: dışa ve yukarı, sonra düş + küçül
            if (!a.wasOut)
            {
                a.wasOut = true;
                Vector3 dir = new Vector3(v.Vel.X, 0f, v.Vel.Z);
                if (dir.sqrMagnitude < 0.01f) dir = new Vector3(v.Pos.X, 0f, v.Pos.Z);
                a.outVel = dir.normalized * Mathf.Max(4f, dir.magnitude) + Vector3.up * 3.5f;
            }
            a.outT += dt;
            a.outVel += Vector3.down * 12f * dt;
            a.root.localPosition += a.outVel * dt;
            a.root.localScale = Vector3.one * Mathf.Clamp01(1f - (a.outT - 0.6f));
            a.spinAngle += 900f * dt;
            a.spin.localRotation = Quaternion.Euler(0f, a.spinAngle, 0f);
            a.tilt.localRotation = Quaternion.Euler(a.outT * 200f, 0f, a.outT * 90f);
            return;
        }

        Vector3 target = FloorPoint(v.Pos, a.contactY);
        a.root.localPosition = target;

        if (v.Toppled)
        {
            // Devrilme: yana yatar, dönüş yavaşlar
            if (a.fallT <= 0f)
            {
                Vector3 vel = new Vector3(v.Vel.X, 0f, v.Vel.Z);
                a.fallAxis = Vector3.Cross(Vector3.up, vel.sqrMagnitude > 0.01f ? vel.normalized : Random.onUnitSphere).normalized;
                if (a.fallAxis.sqrMagnitude < 0.01f) a.fallAxis = Vector3.right;
            }
            a.fallT += dt;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(a.fallT / 0.7f));
            a.tilt.localRotation = Quaternion.AngleAxis(72f * k, a.fallAxis);
            a.root.localPosition = target + Vector3.down * (0.12f * k);
            a.spinAngle += Mathf.Lerp(700f, 0f, Mathf.Clamp01(a.fallT / 2.2f)) * dt;
            a.spin.localRotation = Quaternion.Euler(0f, a.spinAngle, 0f);
            return;
        }

        // Dönüş + harekete doğru eğilme + yorgunluk yalpası
        float rpm = Mathf.Clamp01(v.Rpm);
        a.spinAngle += (900f + 2200f * rpm) * dt;
        a.spin.localRotation = Quaternion.Euler(0f, a.spinAngle, 0f);

        Vector3 flatVel = new Vector3(v.Vel.X, 0f, v.Vel.Z);
        float speed = flatVel.magnitude;
        Quaternion lean = Quaternion.identity;
        if (speed > 0.2f)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, flatVel / speed);
            lean = Quaternion.AngleAxis(Mathf.Clamp(speed * 1.6f, 0f, 14f), axis);
        }
        float fatigue = 1f - rpm;
        float wob = Mathf.Lerp(0.5f, 16f, fatigue * fatigue);
        float ph = Time.unscaledTime * Mathf.Lerp(9f, 5f, fatigue);
        lean *= Quaternion.Euler(Mathf.Sin(ph) * wob, 0f, Mathf.Cos(ph) * wob);
        a.tilt.localRotation = Quaternion.Slerp(a.tilt.localRotation, lean, 1f - Mathf.Exp(-12f * dt));
    }

    void UpdateCamera(float dt)
    {
        if (cam == null) return;
        orbitAngle += orbitDegPerSec * dt;
        float rad = orbitAngle * Mathf.Deg2Rad;
        Vector3 pos = transform.position + new Vector3(Mathf.Sin(rad) * orbitRadius, cameraHeight, Mathf.Cos(rad) * orbitRadius);
        shake = Mathf.MoveTowards(shake, 0f, dt * 0.4f);
        if (shake > 0f) pos += Random.insideUnitSphere * shake;
        cam.transform.position = pos;
        cam.transform.LookAt(transform.position + lookOffset);
    }

    // ------------------------------------------------------------------ Kıvılcım

    static ParticleSystem CreateSparks(Transform parent)
    {
        var go = new GameObject("Sparks");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.35f;
        main.startSpeed = 0f;
        main.startSize = 0.05f;
        main.gravityModifier = 1.2f;
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var shape = ps.shape;
        shape.enabled = false;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.06f;
        r.lengthScale = 1.5f;
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh) { name = "MenuSparks" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        // Toplamalı karışım (parlama)
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 2f);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3100;
        r.sharedMaterial = mat;
        ps.Play();
        return ps;
    }

    void EmitSparks(Vector3 point)
    {
        if (sparks == null) return;
        var p = new ParticleSystem.EmitParams();
        for (int i = 0; i < 22; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.6f + 0.2f;
            p.position = transform.TransformPoint(point);
            p.velocity = dir.normalized * Random.Range(3f, 7.5f);
            p.startColor = Color.Lerp(new Color(1f, 0.85f, 0.4f), new Color(1f, 1f, 1f), Random.value * 0.5f);
            p.startLifetime = Random.Range(0.18f, 0.4f);
            p.startSize = Random.Range(0.03f, 0.06f);
            sparks.Emit(p, 1);
        }
    }
}
