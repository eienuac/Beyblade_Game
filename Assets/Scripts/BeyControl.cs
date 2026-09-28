using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Odak barıyla sınırlı yönlendirme + atılma. Oyuncu ve AI aynı kuralları kullanır.
/// </summary>
[RequireComponent(typeof(BeybladeController))]
public class BeyControl : MonoBehaviour
{
    [Header("Odak")]
    public float maxFocus = 100f;
    public float steerDrainPerSec = 30f;
    public float regenPerSec = 16f;
    public float regenDelay = 0.6f;
    [Tooltip("Bar sıfırlanınca yönlendirme bu seviyeye dolana kadar kilitli kalır")]
    public float exhaustedUnlock = 25f;

    [Header("Atılma")]
    public float dashCost = 35f;
    public float dashCooldown = 2.2f;

    public float Focus { get; private set; }
    public float FocusRatio => maxFocus > 0f ? Focus / maxFocus : 0f;
    public bool IsSteering { get; private set; }
    public bool IsExhausted { get; private set; }
    public float DashCooldownRatio => Mathf.Clamp01((Time.time - lastDashTime) / dashCooldown);
    public bool Active => bey != null && bey.isLaunched && bey.isSpinning && !bey.hasToppled;
    public bool CanDash => Active && !bey.MovementOverridden && Focus >= dashCost && Time.time - lastDashTime >= dashCooldown;
    public Vector3 AimDirection { get; private set; }
    public bool IsManualAim { get; private set; }

    BeybladeController bey;
    Rigidbody rb;
    ControlUI ui;
    DashAimIndicator aimIndicator;
    BeybladeController enemy;
    float lastUseTime = -10f;
    float lastDashTime = -10f;
    bool dashQueued;
    Vector3 queuedDir;
    Vector3 aiSteer;
    float aiThinkTimer;

    void Awake()
    {
        bey = GetComponent<BeybladeController>();
        rb = GetComponent<Rigidbody>();
        Focus = maxFocus;
    }

    void Start()
    {
        if (bey.isPlayer)
        {
            ui = gameObject.AddComponent<ControlUI>();
            ui.Bind(this);
            aimIndicator = DashAimIndicator.Create();
        }
    }

    void OnDestroy()
    {
        if (aimIndicator != null) Destroy(aimIndicator.gameObject);
    }

    void Update()
    {
        if (!Active)
        {
            IsSteering = false;
            bey.steerInput = Vector3.zero;
            dashQueued = false;
            if (aimIndicator != null) aimIndicator.SetAim(transform.position, Vector3.forward, 0f, 0f, Color.white);
            return;
        }

        Vector3 wish;
        if (bey.isPlayer)
        {
            wish = ReadPlayerSteer();
            UpdatePlayerAim(wish);
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasReleasedThisFrame && TryGetCursorDir(out Vector3 cursorDir))
                QueueDash(cursorDir);
            else if (ReadPlayerDashPressed())
                QueueDash(AimDirection);
        }
        else
        {
            ThinkAI();
            wish = aiSteer;
        }

        if (IsExhausted && Focus >= exhaustedUnlock) IsExhausted = false;

        bool wantsSteer = wish.sqrMagnitude > 0.0025f && !bey.MovementOverridden;
        if (wantsSteer && !IsExhausted && Focus > 0f)
        {
            IsSteering = true;
            Focus = Mathf.Max(0f, Focus - steerDrainPerSec * Time.deltaTime);
            lastUseTime = Time.time;
            bey.steerInput = Vector3.ClampMagnitude(wish, 1f);
            if (Focus <= 0f) IsExhausted = true;
        }
        else
        {
            IsSteering = false;
            bey.steerInput = Vector3.zero;
            if (Time.time - lastUseTime >= regenDelay)
                Focus = Mathf.Min(maxFocus, Focus + regenPerSec * Time.deltaTime);
        }

        if (dashQueued)
        {
            dashQueued = false;
            TryDash(queuedDir.sqrMagnitude > 0.001f ? queuedDir : DefaultDashDir());
        }

        UpdateAimIndicator();
    }

    /// <summary>UI'dan atılma. screenDrag sıfırsa mevcut nişan yönü kullanılır.</summary>
    public void RequestDash(Vector2 screenDrag)
    {
        if (bey == null || !bey.isPlayer) return;
        QueueDash(screenDrag.sqrMagnitude > 0.01f ? CameraRelative(screenDrag.normalized) : AimDirection);
    }

    void QueueDash(Vector3 dir)
    {
        dashQueued = true;
        queuedDir = dir;
    }

    void UpdatePlayerAim(Vector3 wish)
    {
        IsManualAim = false;
        Vector3 dir;
        if (ui != null && ui.DashAiming)
        {
            dir = CameraRelative(ui.DashDrag.normalized);
            IsManualAim = true;
        }
        else if (Mouse.current != null && Mouse.current.rightButton.isPressed && TryGetCursorDir(out Vector3 cursorDir))
        {
            dir = cursorDir;
            IsManualAim = true;
        }
        else if (wish.sqrMagnitude > 0.0025f)
        {
            dir = wish;
        }
        else
        {
            dir = DefaultDashDir();
        }
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) AimDirection = dir.normalized;
    }

    bool TryGetCursorDir(out Vector3 dir)
    {
        dir = Vector3.zero;
        Camera cam = Camera.main;
        if (cam == null || Mouse.current == null) return false;
        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane ground = new Plane(Vector3.up, transform.position);
        if (!ground.Raycast(ray, out float t)) return false;
        dir = ray.GetPoint(t) - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.04f;
    }

    void UpdateAimIndicator()
    {
        if (aimIndicator == null) return;
        bool blocked = bey.MovementOverridden;
        float strength = blocked ? 0f : IsManualAim ? 1f : CanDash ? 0.4f : 0f;
        Color c = CanDash ? ModernUIKit.Heat : new Color(0.6f, 0.65f, 0.75f);
        float length = Mathf.Lerp(1.8f, 2.8f, bey.AttackNorm);
        aimIndicator.SetAim(transform.position + Vector3.down * 0.15f, AimDirection, length, strength, c);
    }

    public bool TryDash(Vector3 dir)
    {
        if (!CanDash) return false;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return false;

        Focus -= dashCost;
        lastUseTime = Time.time;
        lastDashTime = Time.time;
        bey.Dash(dir, Mathf.Lerp(7f, 10f, bey.AttackNorm));
        if (ui != null) ui.OnDash();
        return true;
    }

    Vector3 DefaultDashDir()
    {
        if (enemy == null || !enemy.isLaunched) enemy = FindEnemy();
        if (enemy != null) return enemy.transform.position - transform.position;
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        return v.sqrMagnitude > 0.01f ? v : CameraRelative(Vector2.up);
    }

    // ---------------- Oyuncu girişi ----------------

    Vector3 ReadPlayerSteer()
    {
        Vector2 input = Vector2.zero;
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
        }

        Gamepad gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            if (stick.sqrMagnitude > input.sqrMagnitude) input = stick;
        }

        if (ui != null)
        {
            Vector2 joy = ui.JoystickValue;
            if (joy.sqrMagnitude > input.sqrMagnitude) input = joy;
        }

        if (input.sqrMagnitude < 0.02f) return Vector3.zero;
        return CameraRelative(Vector2.ClampMagnitude(input, 1f));
    }

    bool ReadPlayerDashPressed()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame))
            return true;
        Gamepad gp = Gamepad.current;
        return gp != null && gp.buttonSouth.wasPressedThisFrame;
    }

    static Vector3 CameraRelative(Vector2 input)
    {
        Camera cam = Camera.main;
        if (cam == null) return new Vector3(input.x, 0f, input.y);
        TopDownCamera tdc = cam.GetComponent<TopDownCamera>();
        Vector3 fwd = tdc != null ? tdc.ControlForward : cam.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f)
        {
            fwd = cam.transform.up;
            fwd.y = 0f;
        }
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd);
        return right * input.x + fwd * input.y;
    }

    // ---------------- AI ----------------

    void ThinkAI()
    {
        aiThinkTimer -= Time.deltaTime;
        if (aiThinkTimer > 0f) return;
        aiThinkTimer = Random.Range(0.12f, 0.22f);
        aiSteer = Vector3.zero;

        if (enemy == null || !enemy.isLaunched) enemy = FindEnemy();

        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;

        // Kenara savruluyorsa merkeze dön; tehlikeliyse atılarak kurtul
        if (ArenaInfo.Known)
        {
            Vector3 fromCenter = transform.position - ArenaInfo.Center;
            fromCenter.y = 0f;
            float dist = fromCenter.magnitude;
            bool outward = Vector3.Dot(vel, fromCenter) > 0f;
            if (dist > ArenaInfo.Radius * 0.72f && outward)
            {
                aiSteer = -fromCenter.normalized;
                if (dist > ArenaInfo.Radius * 0.85f && vel.magnitude > bey.MaxSpeed * 0.8f && Focus >= dashCost + 10f)
                    TryDash(-fromCenter);
                return;
            }
        }

        if (enemy == null) return;
        Vector3 toEnemy = enemy.transform.position - transform.position;
        toEnemy.y = 0f;
        float d = toEnemy.magnitude;
        if (d < 0.01f) return;

        switch (bey.beybladeType)
        {
            case BeybladeType.Saldiri:
            case BeybladeType.Denge:
                if (d < 4f && d > 1.2f && Focus >= dashCost + 5f && Random.value < 0.35f)
                    TryDash(toEnemy);
                else if (d > 4f && Focus > 50f)
                    aiSteer = toEnemy.normalized * 0.7f;
                break;

            case BeybladeType.Savunma:
                // Kaçmaz; atılan rakibe karşı merkeze yaslanır
                if (enemy.IsDashing && d < 3f && Focus > 20f && ArenaInfo.Known)
                {
                    Vector3 toCenter = ArenaInfo.Center - transform.position;
                    toCenter.y = 0f;
                    if (toCenter.sqrMagnitude > 0.04f) aiSteer = toCenter.normalized * 0.6f;
                }
                break;

            case BeybladeType.Dayaniklilik:
                // Atılan rakibin yolundan yana kayar
                if (enemy.IsDashing && d < 3f && Focus > 20f)
                    aiSteer = Vector3.Cross(Vector3.up, toEnemy.normalized);
                break;
        }
    }

    BeybladeController FindEnemy()
    {
        BeybladeController[] all = FindObjectsByType<BeybladeController>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != bey) return all[i];
        return null;
    }
}
