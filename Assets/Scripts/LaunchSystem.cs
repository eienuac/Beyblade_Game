using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Modern ripcord fırlatma UI — güç barı + ip çekme.
/// </summary>
public class LaunchSystem : MonoBehaviour
{
    [Header("Referanslar")]
    public BeybladeController targetBeyblade;

    [Header("Güç Göstergesi")]
    public float minFillDuration = 3.5f;
    public float maxFillDuration = 5.5f;

    [Header("Fırlatma")]
    public float maxLaunchForce = 16f;

    bool launched;
    float fillDuration;
    float fillTimer;
    float currentPower;

    bool isDragging;
    float dragStartY;
    float pullThreshold = 180f;

    Canvas uiCanvas;
    Image powerFill;
    Image powerGlow;
    Image powerCap;
    TextMeshProUGUI powerPct;
    TextMeshProUGUI powerTitle;
    TextMeshProUGUI instructionText;
    TextMeshProUGUI resultText;
    RectTransform ripcordHandle;
    float ripcordInitialY;
    Image ripcordImage;
    Image ropeFill;
    TextMeshProUGUI handleLabel;
    Image flashOverlay;
    float resultPulse;

    void Start()
    {
        ModernUIKit.EnsureEventSystem();
        ModernUIKit.EnsureFonts();
        CreateUI();

        fillDuration = Random.Range(minFillDuration, maxFillDuration);
        fillTimer = 0f;
        currentPower = 0f;

        if (targetBeyblade == null)
        {
            GameObject player = GameObject.Find("PlayerBeyblade");
            if (player != null) targetBeyblade = player.GetComponent<BeybladeController>();
        }

        if (targetBeyblade != null)
        {
            targetBeyblade.isLaunched = false;
            targetBeyblade.isSpinning = true;
            Rigidbody rb = targetBeyblade.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (launched)
        {
            if (resultPulse > 0f && resultText != null)
            {
                resultPulse -= Time.unscaledDeltaTime;
                float s = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 10f);
                resultText.transform.localScale = Vector3.one * s;
                if (flashOverlay != null)
                    flashOverlay.color = new Color(1f, 1f, 1f, Mathf.Clamp01(resultPulse) * 0.12f);
            }
            return;
        }

        fillTimer += Time.deltaTime;
        currentPower = Mathf.PingPong(fillTimer / fillDuration, 1f);

        Color barColor;
        if (currentPower < 0.4f)
            barColor = Color.Lerp(ModernUIKit.Cyan, ModernUIKit.Mint, currentPower * 2.5f);
        else if (currentPower < 0.85f)
            barColor = Color.Lerp(ModernUIKit.Mint, ModernUIKit.Gold, (currentPower - 0.4f) * 2.22f);
        else
            barColor = Color.Lerp(ModernUIKit.Gold, ModernUIKit.Heat, (currentPower - 0.85f) * 6.66f);

        if (powerFill != null)
        {
            powerFill.fillAmount = currentPower;
            powerFill.color = barColor;
        }
        if (powerGlow != null)
            powerGlow.color = new Color(barColor.r, barColor.g, barColor.b, 0.12f + currentPower * 0.45f);
        if (powerCap != null)
        {
            float y = Mathf.Lerp(-230f, 230f, currentPower);
            powerCap.rectTransform.anchoredPosition = new Vector2(0f, y);
            powerCap.color = barColor;
        }

        int pct = Mathf.RoundToInt(currentPower * 100f);
        if (powerPct != null)
        {
            powerPct.text = pct + "%";
            powerPct.color = pct >= 95 ? ModernUIKit.Gold : Color.white;
        }
        if (powerTitle != null)
            powerTitle.color = Color.Lerp(ModernUIKit.Dim, barColor, 0.65f);

        bool isPressed = false;
        Vector2 inputPos = Vector2.zero;

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPressed = true;
            inputPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            var phase = touch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.Began ||
                phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                isPressed = true;
                inputPos = touch.position.ReadValue();
            }
        }

        if (isPressed)
        {
            if (!isDragging)
            {
                isDragging = true;
                dragStartY = inputPos.y;
            }
            else
            {
                float pullRatio = Mathf.Clamp01((dragStartY - inputPos.y) / pullThreshold);
                UpdateRipcordVisual(pullRatio);
                if (pullRatio >= 0.95f)
                    LaunchBeyblade();
            }
        }
        else if (isDragging)
        {
            isDragging = false;
            UpdateRipcordVisual(0f);
        }
    }

    void UpdateRipcordVisual(float pull)
    {
        if (ripcordHandle != null)
            ripcordHandle.anchoredPosition = new Vector2(ripcordHandle.anchoredPosition.x, ripcordInitialY - pull * 300f);

        if (ripcordImage != null)
            ripcordImage.color = Color.Lerp(ModernUIKit.Heat, ModernUIKit.Gold, pull);

        if (ropeFill != null)
        {
            ropeFill.fillAmount = pull;
            ropeFill.color = Color.Lerp(new Color(1f, 0.55f, 0.15f, 0.55f), ModernUIKit.Gold, pull);
        }

        if (handleLabel != null)
            handleLabel.text = pull < 0.05f ? "CEK" : (Mathf.RoundToInt(pull * 100f) + "%");

        if (instructionText != null && pull > 0.1f)
            instructionText.text = "DEVAM ET  ·  " + Mathf.RoundToInt(pull * 100f) + "%";
    }

    void LaunchBeyblade()
    {
        launched = true;
        isDragging = false;
        float launchPower = currentPower;
        resultPulse = 1.6f;

        string quality;
        Color qColor;
        if (launchPower >= 0.92f) { quality = "MUKEMMEL"; qColor = ModernUIKit.Gold; }
        else if (launchPower >= 0.78f) { quality = "HARIKA"; qColor = ModernUIKit.Battle; }
        else if (launchPower >= 0.60f) { quality = "IYI"; qColor = ModernUIKit.Cyan; }
        else if (launchPower >= 0.40f) { quality = "ORTA"; qColor = ModernUIKit.Heat; }
        else { quality = "ZAYIF"; qColor = ModernUIKit.Danger; }

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = quality + "\n<size=70%>" + Mathf.RoundToInt(launchPower * 100f) + "% GUC</size>";
            resultText.color = qColor;
        }

        if (instructionText != null)
        {
            instructionText.text = "LET IT RIP!";
            instructionText.color = qColor;
        }

        if (flashOverlay != null)
            flashOverlay.color = new Color(qColor.r, qColor.g, qColor.b, 0.18f);

        if (targetBeyblade != null)
        {
            Rigidbody rb = targetBeyblade.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                Vector3 launchDir = -targetBeyblade.transform.position;
                launchDir.y = 0f;
                if (launchDir.sqrMagnitude < 0.01f) launchDir = Vector3.forward;
                launchDir.Normalize();

                float effectiveLaunchPower = Mathf.Max(launchPower, 0.08f);
                rb.AddForce(launchDir * maxLaunchForce * effectiveLaunchPower, ForceMode.Impulse);
            }

            float effectiveStaminaPower = Mathf.Max(launchPower, 0.08f);
            targetBeyblade.currentStamina = targetBeyblade.maxStamina * effectiveStaminaPower;
            targetBeyblade.currentSpinRPM = targetBeyblade.maxSpinRPM * effectiveStaminaPower;
            targetBeyblade.isLaunched = true;
            targetBeyblade.isSpinning = true;
            CapsuleCollider col = targetBeyblade.GetComponent<CapsuleCollider>();
            if (col != null) col.enabled = true;
        }

        Invoke(nameof(HideUI), 1.8f);
    }

    void HideUI()
    {
        if (uiCanvas != null) uiCanvas.gameObject.SetActive(false);
        enabled = false;
    }

    void CreateUI()
    {
        uiCanvas = ModernUIKit.CreateCanvas("LaunchUI", 150);

        flashOverlay = ModernUIKit.MakeImage(uiCanvas.transform, "Flash", new Color(1f, 1f, 1f, 0f), null, false);
        flashOverlay.sprite = null;
        ModernUIKit.Stretch(flashOverlay.rectTransform);
        flashOverlay.raycastTarget = false;

        // Üst banner
        Image topBanner = ModernUIKit.MakeImage(uiCanvas.transform, "TopBanner", new Color(0.04f, 0.06f, 0.12f, 0.72f), ModernUIKit.SoftCardSprite);
        ModernUIKit.Anchor(topBanner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(920f, 88f));
        Outline topOl = topBanner.gameObject.AddComponent<Outline>();
        topOl.effectColor = new Color(ModernUIKit.Cyan.r, ModernUIKit.Cyan.g, ModernUIKit.Cyan.b, 0.35f);
        topOl.effectDistance = new Vector2(1.5f, -1.5f);

        instructionText = ModernUIKit.Label(topBanner.transform, "Instruction", "IPI ASAGI CEK  ·  FIRLAT", 30, ModernUIKit.Text);
        instructionText.fontStyle = FontStyles.Bold;
        instructionText.characterSpacing = 4f;
        ModernUIKit.Stretch(instructionText.rectTransform, 20, 20, 12, 12);

        // Sol güç paneli
        Image powerCard = ModernUIKit.MakeGlassCard(uiCanvas.transform, "PowerCard", new Vector2(120f, 520f));
        ModernUIKit.Anchor(powerCard.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(36f, 0f), new Vector2(120f, 520f));

        powerTitle = ModernUIKit.Label(powerCard.transform, "Title", "GUC", 18, ModernUIKit.Dim);
        powerTitle.fontStyle = FontStyles.Bold;
        powerTitle.characterSpacing = 6f;
        ModernUIKit.Anchor(powerTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(100f, 28f));

        Image track = ModernUIKit.MakeImage(powerCard.transform, "Track", new Color(1f, 1f, 1f, 0.06f), ModernUIKit.RoundSprite);
        ModernUIKit.Anchor(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -8f), new Vector2(48f, 400f));

        powerGlow = ModernUIKit.MakeImage(track.transform, "Glow", new Color(ModernUIKit.Cyan.r, ModernUIKit.Cyan.g, ModernUIKit.Cyan.b, 0.2f), ModernUIKit.RoundSprite);
        ModernUIKit.Stretch(powerGlow.rectTransform, -10, -10, -10, -10);
        powerGlow.raycastTarget = false;

        powerFill = ModernUIKit.MakeImage(track.transform, "Fill", ModernUIKit.Cyan, ModernUIKit.RoundSprite);
        ModernUIKit.Stretch(powerFill.rectTransform, 4, 4, 4, 4);
        powerFill.type = Image.Type.Filled;
        powerFill.fillMethod = Image.FillMethod.Vertical;
        powerFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        powerFill.fillAmount = 0f;
        powerFill.raycastTarget = false;

        powerCap = ModernUIKit.MakeImage(track.transform, "Cap", ModernUIKit.Cyan, ModernUIKit.CircleSprite, false);
        ModernUIKit.Anchor(powerCap.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -230f), new Vector2(56f, 14f));
        powerCap.raycastTarget = false;

        powerPct = ModernUIKit.Label(powerCard.transform, "Pct", "0%", 34, Color.white);
        powerPct.fontStyle = FontStyles.Bold;
        ModernUIKit.Anchor(powerPct.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(110f, 42f));

        // Sağ ripcord
        Image ropeCard = ModernUIKit.MakeGlassCard(uiCanvas.transform, "RopeCard", new Vector2(140f, 520f));
        ModernUIKit.Anchor(ropeCard.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-36f, 0f), new Vector2(140f, 520f));

        TextMeshProUGUI ropeTitle = ModernUIKit.Label(ropeCard.transform, "RopeTitle", "RIPCORD", 16, ModernUIKit.Dim);
        ropeTitle.fontStyle = FontStyles.Bold;
        ropeTitle.characterSpacing = 3f;
        ModernUIKit.Anchor(ropeTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(120f, 28f));

        Image ropeTrack = ModernUIKit.MakeImage(ropeCard.transform, "RopeTrack", new Color(1f, 1f, 1f, 0.08f), ModernUIKit.RoundSprite);
        ModernUIKit.Anchor(ropeTrack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(18f, 380f));

        ropeFill = ModernUIKit.MakeImage(ropeTrack.transform, "RopeFill", ModernUIKit.Heat, ModernUIKit.RoundSprite);
        ModernUIKit.Stretch(ropeFill.rectTransform, 2, 2, 2, 2);
        ropeFill.type = Image.Type.Filled;
        ropeFill.fillMethod = Image.FillMethod.Vertical;
        ropeFill.fillOrigin = (int)Image.OriginVertical.Top;
        ropeFill.fillAmount = 0f;
        ropeFill.raycastTarget = false;

        Image handle = ModernUIKit.MakeImage(ropeCard.transform, "Handle", ModernUIKit.Heat, ModernUIKit.SoftCardSprite);
        ModernUIKit.Anchor(handle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(108f, 78f));
        ripcordHandle = handle.rectTransform;
        ripcordInitialY = ripcordHandle.anchoredPosition.y;
        ripcordImage = handle;
        Outline hOl = handle.gameObject.AddComponent<Outline>();
        hOl.effectColor = new Color(1f, 0.7f, 0.2f, 0.7f);
        hOl.effectDistance = new Vector2(2f, -2f);
        Shadow hSh = handle.gameObject.AddComponent<Shadow>();
        hSh.effectColor = new Color(1f, 0.4f, 0.1f, 0.4f);
        hSh.effectDistance = new Vector2(0f, -8f);

        handleLabel = ModernUIKit.Label(handle.transform, "HandleLbl", "CEK", 26, Color.white);
        handleLabel.fontStyle = FontStyles.Bold;
        handleLabel.characterSpacing = 2f;
        ModernUIKit.Stretch(handleLabel.rectTransform, 4, 4, 4, 4);

        TextMeshProUGUI pullHint = ModernUIKit.Label(ropeCard.transform, "PullHint", "ASAGI SURUKLE", 14, ModernUIKit.Dim);
        ModernUIKit.Anchor(pullHint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(130f, 28f));

        // Sonuç
        resultText = ModernUIKit.Label(uiCanvas.transform, "Result", "", 64, Color.white);
        resultText.fontStyle = FontStyles.Bold;
        resultText.characterSpacing = 2f;
        ModernUIKit.Anchor(resultText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 160f));
        resultText.gameObject.SetActive(false);
    }
}
