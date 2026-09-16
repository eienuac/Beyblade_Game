using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Gelişmiş Fırlatma Sistemi (Ripcord İp Çekme & Güç Göstergesi Arayüzü)
/// </summary>
public class LaunchSystem : MonoBehaviour
{
    [Header("Referanslar")]
    public BeybladeController targetBeyblade;

    [Header("Güç Göstergesi Ayarları")]
    public float minFillDuration = 3.5f;
    public float maxFillDuration = 5.5f;

    [Header("Fırlatma Kuvveti")]
    public float maxLaunchForce = 16f;

    // Durum
    private bool launched = false;
    private float fillDuration;
    private float fillTimer;
    private float currentPower;

    // İp Çekme
    private bool isDragging = false;
    private float dragStartY;
    private float pullThreshold = 180f;

    // UI Bileşenleri
    private Canvas uiCanvas;
    private Image powerBarFill;
    private Image powerBarGlow;
    private Text powerPercentText;
    private Text instructionText;
    private Text resultText;
    private RectTransform ripcordHandleRect;
    private float ripcordInitialY;
    private Image ripcordHandleImage;
    private Text handlePercentText;
    private Outline handleOutline;

    private void Start()
    {
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

    private void Update()
    {
        if (launched) return;

        // === GÜÇ GÖSTERGESİ DOLUMU ===
        fillTimer += Time.deltaTime;
        currentPower = Mathf.PingPong(fillTimer / fillDuration, 1.0f);

        if (powerBarFill != null)
        {
            powerBarFill.fillAmount = currentPower;

            Color barColor;
            if (currentPower < 0.4f)
                barColor = Color.Lerp(new Color(0f, 0.8f, 1f), Color.green, currentPower * 2.5f);
            else if (currentPower < 0.85f)
                barColor = Color.Lerp(Color.green, Color.yellow, (currentPower - 0.4f) * 2.22f);
            else
                barColor = Color.Lerp(Color.yellow, new Color(1f, 0.1f, 0.1f), (currentPower - 0.85f) * 6.66f);

            powerBarFill.color = barColor;

            if (powerBarGlow != null)
            {
                powerBarGlow.color = new Color(barColor.r, barColor.g, barColor.b, currentPower * 0.5f);
            }
        }

        int pctInt = Mathf.RoundToInt(currentPower * 100f);
        if (powerPercentText != null)
        {
            powerPercentText.text = "%" + pctInt;
            if (pctInt >= 95)
                powerPercentText.text = "%" + pctInt + " MAX!";
        }

        // === INPUT SİSTEMİ İLE İP ÇEKME ===
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
            if (touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Began ||
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Moved ||
                touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Stationary)
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
                float dragDelta = dragStartY - inputPos.y;
                float pullRatio = Mathf.Clamp01(dragDelta / pullThreshold);

                UpdateRipcordVisual(pullRatio);

                if (pullRatio >= 0.95f)
                {
                    LaunchBeyblade();
                }
            }
        }
        else
        {
            if (isDragging)
            {
                isDragging = false;
                UpdateRipcordVisual(0f);
            }
        }
    }

    private void UpdateRipcordVisual(float pull)
    {
        if (ripcordHandleRect != null)
        {
            float newY = ripcordInitialY - (pull * 280f);
            ripcordHandleRect.anchoredPosition = new Vector2(ripcordHandleRect.anchoredPosition.x, newY);
        }

        if (ripcordHandleImage != null)
        {
            ripcordHandleImage.color = Color.Lerp(new Color(1f, 0.55f, 0f), new Color(1f, 0.85f, 0f), pull);
        }

        if (handlePercentText != null)
        {
            handlePercentText.text = "ÇEK\n%" + Mathf.RoundToInt(pull * 100f);
        }

        if (handleOutline != null)
        {
            handleOutline.effectColor = Color.Lerp(Color.black, Color.yellow, pull);
        }
    }

    private void LaunchBeyblade()
    {
        launched = true;
        isDragging = false;
        float launchPower = currentPower;

        string quality;
        Color qColor;
        if (launchPower >= 0.92f) { quality = "★ MÜKEMMEL! ★"; qColor = new Color(1f, 0.85f, 0f); }
        else if (launchPower >= 0.78f) { quality = "HARİKA!"; qColor = Color.green; }
        else if (launchPower >= 0.60f) { quality = "İ Y İ!"; qColor = new Color(0.4f, 1f, 0.9f); }
        else if (launchPower >= 0.40f) { quality = "ORTA"; qColor = Color.yellow; }
        else { quality = "ZAYIF"; qColor = new Color(1f, 0.3f, 0.3f); }

        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = quality + "\n%" + Mathf.RoundToInt(launchPower * 100f) + " GÜÇ!";
            resultText.color = qColor;
        }

        if (instructionText != null)
            instructionText.text = "LET IT RIP!";

        if (targetBeyblade != null)
        {
            Rigidbody rb = targetBeyblade.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                // ZIPLAMA ÖNLEYİCİ: Tamamen yatay fırlatma vektörü (launchDir.y = 0)
                Vector3 launchDir = (Vector3.zero - targetBeyblade.transform.position);
                launchDir.y = 0f;
                launchDir = launchDir.normalized;

                // Fırlatma gücü artık doğrudan yüzdeye bağlı! (eski min kaldırıldı)
                float effectiveLaunchPower = Mathf.Max(launchPower, 0.08f); // Min %8 ki en azından düşsün
                rb.AddForce(launchDir * maxLaunchForce * effectiveLaunchPower, ForceMode.Impulse);
            }

            // STAMINA VE RPM ARTIK TAM OLARAK FIRLATMA GÜCÜNE BAĞLI!
            // %5 = %5 stamina (5 can), %100 = %100 stamina (100 can)
            // Kötü fırlatma = erken ölüm!
            float effectiveStaminaPower = Mathf.Max(launchPower, 0.08f);
            targetBeyblade.currentStamina = targetBeyblade.maxStamina * effectiveStaminaPower;
            targetBeyblade.currentSpinRPM = targetBeyblade.maxSpinRPM * effectiveStaminaPower;
            targetBeyblade.isLaunched = true;
            targetBeyblade.isSpinning = true;
            CapsuleCollider col = targetBeyblade.GetComponent<CapsuleCollider>();
            if (col != null) col.enabled = true;
        }

        Invoke("HideUI", 2f);
    }

    private void HideUI()
    {
        if (uiCanvas != null) uiCanvas.gameObject.SetActive(false);
        enabled = false;
    }

    private void CreateUI()
    {
        GameObject canvasObj = new GameObject("LaunchUI");
        uiCanvas = canvasObj.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 150;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // SOLDAGİ GÜÇ BARI
        GameObject barFrame = CreateUIElement(canvasObj.transform, "BarFrame", new Vector2(0.06f, 0.5f), new Vector2(90, 520), new Color(0.06f, 0.08f, 0.12f, 0.92f));
        barFrame.AddComponent<Outline>().effectColor = new Color(0.2f, 0.6f, 1f, 0.8f);

        GameObject barBG = CreateUIElement(barFrame.transform, "BarBG", new Vector2(0.5f, 0.5f), new Vector2(68, 498), new Color(0.1f, 0.12f, 0.16f, 1f));

        GameObject fillObj = new GameObject("PowerFill");
        fillObj.transform.SetParent(barBG.transform, false);
        powerBarFill = fillObj.AddComponent<Image>();
        powerBarFill.type = Image.Type.Filled;
        powerBarFill.fillMethod = Image.FillMethod.Vertical;
        powerBarFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        powerBarFill.fillAmount = 0f;
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;

        GameObject glowObj = new GameObject("PowerGlow");
        glowObj.transform.SetParent(barBG.transform, false);
        powerBarGlow = glowObj.AddComponent<Image>();
        powerBarGlow.color = new Color(1f, 1f, 1f, 0.2f);
        RectTransform glowRt = glowObj.GetComponent<RectTransform>();
        glowRt.anchorMin = Vector2.zero;
        glowRt.anchorMax = Vector2.one;
        glowRt.offsetMin = glowRt.offsetMax = Vector2.zero;

        powerPercentText = CreateTextElement(barFrame.transform, "PowerPercentText", new Vector2(0.5f, 1.08f), new Vector2(160, 50), "%0", 32, Color.white);
        powerPercentText.fontStyle = FontStyle.Bold;
        powerPercentText.gameObject.AddComponent<Outline>().effectColor = Color.black;

        Text barLabel = CreateTextElement(barFrame.transform, "BarLabel", new Vector2(0.5f, -0.06f), new Vector2(140, 40), "GÜÇ", 24, new Color(0.8f, 0.9f, 1f));
        barLabel.fontStyle = FontStyle.Bold;

        // SAĞDAKİ ÇEKME İPİ (RIPCORD)
        GameObject ropeTrack = CreateUIElement(canvasObj.transform, "RopeTrack", new Vector2(0.9f, 0.5f), new Vector2(24, 480), new Color(0.2f, 0.22f, 0.26f, 0.9f));
        ropeTrack.AddComponent<Outline>().effectColor = new Color(0.8f, 0.6f, 0.1f, 0.7f);

        CreateUIElement(ropeTrack.transform, "RopePattern", new Vector2(0.5f, 0.5f), new Vector2(10, 460), new Color(0.9f, 0.75f, 0.2f, 0.8f));

        GameObject handleObj = CreateUIElement(canvasObj.transform, "RipcordHandle", new Vector2(0.9f, 0.78f), new Vector2(130, 95), new Color(1f, 0.55f, 0f));
        ripcordHandleRect = handleObj.GetComponent<RectTransform>();
        ripcordInitialY = ripcordHandleRect.anchoredPosition.y;
        ripcordHandleImage = handleObj.GetComponent<Image>();
        handleOutline = handleObj.AddComponent<Outline>();
        handleOutline.effectColor = Color.black;

        handlePercentText = CreateTextElement(handleObj.transform, "HandleLbl", new Vector2(0.5f, 0.5f), new Vector2(120, 80), "▼\nÇEK", 22, Color.white);
        handlePercentText.fontStyle = FontStyle.Bold;

        // METİNLER
        instructionText = CreateTextElement(canvasObj.transform, "InstructionText", new Vector2(0.5f, 0.92f), new Vector2(800, 60),
            "SAĞDAKİ İPİ AŞAĞI SÜRÜKLE VE FIRLAT!", 28, Color.yellow);
        instructionText.fontStyle = FontStyle.Bold;
        instructionText.gameObject.AddComponent<Outline>().effectColor = Color.black;

        resultText = CreateTextElement(canvasObj.transform, "ResultText", new Vector2(0.5f, 0.5f), new Vector2(600, 140), "", 46, Color.white);
        resultText.fontStyle = FontStyle.Bold;
        resultText.gameObject.AddComponent<Outline>().effectColor = Color.black;
        resultText.gameObject.SetActive(false);
    }

    private GameObject CreateUIElement(Transform parent, string name, Vector2 anchor, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        return go;
    }

    private Text CreateTextElement(Transform parent, string name, Vector2 anchor, Vector2 size, string text, int fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text txt = go.AddComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleCenter;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.sizeDelta = size;
        return txt;
    }
}
