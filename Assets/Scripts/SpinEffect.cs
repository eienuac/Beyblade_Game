using UnityEngine;

/// <summary>
/// Modern Beyblade FX: enerji halkası, yumuşak izler, zemin kıvılcımı, çarpışma şoku.
/// Trail'ler görsel spin çocuğuna bağlıdır; fizik kökü dönmediği için titremez.
/// </summary>
public class SpinEffect : MonoBehaviour
{
    private BeybladeController controller;
    private Rigidbody body;
    private TrailRenderer[] trails;
    private ParticleSystem energyRing;
    private ParticleSystem groundSparks;
    private ParticleSystem impactSparks;
    private ParticleSystem shockwave;
    private ParticleSystem flashBurst;
    private Light auraLight;
    private Color baseColor;
    private float radius = 0.45f;
    private static Texture2D softParticleTex;
    private static Material additiveMat;

    private void Start()
    {
        controller = GetComponent<BeybladeController>();
        body = GetComponent<Rigidbody>();
        ResolveColorAndRadius();
        EnsureSharedAssets();

        Transform fxParent = controller != null && controller.VisualPivot != null
            ? controller.VisualPivot
            : transform;
        Transform spinParent = controller != null && controller.VisualSpin != null
            ? controller.VisualSpin
            : transform;

        CreateTrails(spinParent);
        energyRing = CreateEnergyRing(fxParent);
        groundSparks = CreateGroundSparks(fxParent);
        impactSparks = CreateImpactSparks(fxParent);
        shockwave = CreateShockwave(fxParent);
        flashBurst = CreateFlashBurst(fxParent);
        auraLight = CreateAuraLight(fxParent);
    }

    private void ResolveColorAndRadius()
    {
        if (controller != null)
        {
            switch (controller.beybladeType)
            {
                case BeybladeType.Saldiri:
                    baseColor = new Color(1f, 0.28f, 0.08f);
                    break;
                case BeybladeType.Savunma:
                    baseColor = new Color(0.18f, 0.48f, 1f);
                    break;
                case BeybladeType.Dayaniklilik:
                    baseColor = new Color(0.15f, 0.95f, 0.45f);
                    break;
                default:
                    baseColor = new Color(0.72f, 0.45f, 1f);
                    break;
            }
        }
        else
        {
            baseColor = new Color(0.35f, 0.7f, 1f);
        }

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) radius = Mathf.Max(0.22f, col.radius * 0.85f);
    }

    private static void EnsureSharedAssets()
    {
        if (softParticleTex == null)
        {
            const int size = 64;
            softParticleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            softParticleTex.wrapMode = TextureWrapMode.Clamp;
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    softParticleTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            softParticleTex.Apply();
        }

        if (additiveMat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            additiveMat = new Material(shader);
            additiveMat.SetTexture("_BaseMap", softParticleTex);
            additiveMat.SetTexture("_MainTex", softParticleTex);
            additiveMat.SetColor("_BaseColor", Color.white);
            additiveMat.SetColor("_Color", Color.white);
            additiveMat.SetFloat("_Surface", 1f);
            additiveMat.SetFloat("_Blend", 2f);
            additiveMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            additiveMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            additiveMat.renderQueue = 3000;
            additiveMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            additiveMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            additiveMat.SetInt("_ZWrite", 0);
        }
    }

    private void CreateTrails(Transform parent)
    {
        trails = new TrailRenderer[2];
        for (int i = 0; i < trails.Length; i++)
        {
            GameObject obj = new GameObject("MotionTrail_" + i);
            obj.transform.SetParent(parent, false);
            float angle = i * Mathf.PI;
            obj.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.04f, Mathf.Sin(angle) * radius);

            TrailRenderer tr = obj.AddComponent<TrailRenderer>();
            tr.time = 0.08f;
            tr.startWidth = 0.05f;
            tr.endWidth = 0.0f;
            tr.minVertexDistance = 0.06f;
            tr.numCapVertices = 2;
            tr.numCornerVertices = 2;
            tr.alignment = LineAlignment.View;
            tr.receiveShadows = false;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.emitting = false;
            tr.textureMode = LineTextureMode.Stretch;

            Color bright = Color.Lerp(baseColor, Color.white, 0.45f);
            bright.a = 1f;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(bright, 0f),
                    new GradientColorKey(baseColor, 0.45f),
                    new GradientColorKey(baseColor * 0.4f, 1f)
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.7f, 0f),
                    new GradientAlphaKey(0.25f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            tr.colorGradient = grad;

            AnimationCurve width = new AnimationCurve();
            width.AddKey(0f, 1f);
            width.AddKey(0.4f, 0.45f);
            width.AddKey(1f, 0f);
            tr.widthCurve = width;
            tr.material = new Material(additiveMat);
            trails[i] = tr;
        }
    }

    private ParticleSystem CreateEnergyRing(Transform parent)
    {
        GameObject obj = new GameObject("EnergyRing");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.duration = 1f;
        main.startLifetime = 0.28f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            Color.Lerp(baseColor, Color.white, 0.35f),
            baseColor
        );
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;

        var emission = ps.emission;
        emission.rateOverTime = 55f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = radius;
        shape.donutRadius = 0.03f;
        shape.radiusThickness = 0f;
        shape.arc = 360f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(baseColor, 0.4f),
                new GradientColorKey(baseColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.2f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = g;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalY = 2.5f;

        ApplyRenderer(ps, ParticleSystemRenderMode.Billboard);
        return ps;
    }

    private ParticleSystem CreateGroundSparks(Transform parent)
    {
        GameObject obj = new GameObject("GroundSparks");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.75f, 0.25f));
        main.maxParticles = 40;
        main.gravityModifier = 1.4f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.65f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ApplyRenderer(ps, ParticleSystemRenderMode.Billboard);
        return ps;
    }

    private ParticleSystem CreateImpactSparks(Transform parent)
    {
        GameObject obj = new GameObject("ImpactSparks");
        obj.transform.SetParent(parent, false);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.82f, 0.35f));
        main.maxParticles = 80;
        main.gravityModifier = 2.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 28) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.12f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = 0.45f;
        trails.lifetime = 0.12f;
        trails.dieWithParticles = true;

        ParticleSystemRenderer psr = ApplyRenderer(ps, ParticleSystemRenderMode.Stretch);
        psr.lengthScale = 2.4f;
        psr.velocityScale = 0.12f;
        psr.trailMaterial = new Material(additiveMat);
        return ps;
    }

    private ParticleSystem CreateShockwave(Transform parent)
    {
        GameObject obj = new GameObject("Shockwave");
        obj.transform.SetParent(parent, false);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.28f;
        main.startSpeed = 0f;
        main.startSize = 0.4f;
        main.startColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.9f);
        main.maxParticles = 4;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.25f, 1f, 1f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(baseColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = g;

        ApplyRenderer(ps, ParticleSystemRenderMode.HorizontalBillboard);
        return ps;
    }

    private ParticleSystem CreateFlashBurst(Transform parent)
    {
        GameObject obj = new GameObject("ImpactFlash");
        obj.transform.SetParent(parent, false);
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.12f;
        main.startSpeed = 0f;
        main.startSize = 0.9f;
        main.startColor = Color.white;
        main.maxParticles = 2;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.4f));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(baseColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        ApplyRenderer(ps, ParticleSystemRenderMode.Billboard);
        return ps;
    }

    private Light CreateAuraLight(Transform parent)
    {
        GameObject obj = new GameObject("AuraLight");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = baseColor;
        light.intensity = 0f;
        light.range = 3.2f;
        light.shadows = LightShadows.None;
        return light;
    }

    private ParticleSystemRenderer ApplyRenderer(ParticleSystem ps, ParticleSystemRenderMode mode)
    {
        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = mode;
        psr.material = new Material(additiveMat);
        psr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        psr.receiveShadows = false;
        return psr;
    }

    private void Update()
    {
        if (controller == null) return;

        bool active = controller.isSpinning && controller.currentSpinRPM > 80f;
        float rpmRatio = controller.maxSpinRPM > 0.01f
            ? Mathf.Clamp01(controller.currentSpinRPM / controller.maxSpinRPM)
            : 0f;
        float speed = 0f;
        if (body != null)
        {
            Vector3 v = body.linearVelocity;
            v.y = 0f;
            speed = v.magnitude;
        }

        if (trails != null)
        {
            for (int i = 0; i < trails.Length; i++)
            {
                if (trails[i] == null) continue;
                trails[i].emitting = active;
                if (!active) continue;
                trails[i].time = Mathf.Lerp(0.04f, 0.1f, rpmRatio);
                trails[i].startWidth = Mathf.Lerp(0.025f, 0.07f, rpmRatio);
            }
        }

        if (energyRing != null)
        {
            var emission = energyRing.emission;
            emission.rateOverTime = active ? Mathf.Lerp(18f, 70f, rpmRatio) : 0f;
            var vel = energyRing.velocityOverLifetime;
            vel.orbitalY = Mathf.Lerp(1.2f, 5.5f, rpmRatio);
        }

        if (groundSparks != null)
        {
            var emission = groundSparks.emission;
            bool scrape = active && controller.isLaunched && speed > 2.2f;
            emission.rateOverDistance = scrape ? Mathf.Lerp(4f, 14f, rpmRatio) : 0f;
            if (scrape && !groundSparks.isPlaying) groundSparks.Play();
            if (!scrape && groundSparks.isPlaying) groundSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (auraLight != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (8f + rpmRatio * 10f));
            auraLight.intensity = active ? Mathf.Lerp(0.6f, 2.4f, rpmRatio) * (0.75f + pulse * 0.25f) : 0f;
            auraLight.color = Color.Lerp(baseColor, Color.white, rpmRatio * 0.25f);
        }
    }

    public void PlayCollisionSparks()
    {
        PlayImpact(transform.position, Vector3.up, 8f);
    }

    public void PlayImpact(Vector3 worldPoint, Vector3 normal, float damage)
    {
        float mag = Mathf.Clamp(damage * 0.12f, 0.7f, 1.6f);

        if (impactSparks != null)
        {
            impactSparks.transform.position = worldPoint;
            impactSparks.transform.rotation = Quaternion.LookRotation(normal == Vector3.zero ? Vector3.up : normal);
            var emission = impactSparks.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(16 * mag), (short)(28 * mag)) });
            impactSparks.Play();
        }

        if (shockwave != null)
        {
            shockwave.transform.position = worldPoint + Vector3.up * 0.04f;
            var main = shockwave.main;
            main.startSize = 0.55f * mag;
            shockwave.Play();
        }

        if (flashBurst != null)
        {
            flashBurst.transform.position = worldPoint + Vector3.up * 0.08f;
            var main = flashBurst.main;
            main.startSize = 0.75f * mag;
            flashBurst.Play();
        }

        if (auraLight != null)
            auraLight.intensity = Mathf.Max(auraLight.intensity, 4.5f * mag);
    }
}
