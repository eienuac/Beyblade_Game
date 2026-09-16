using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Her Beyblade'in üstünde %100 GÖRÜNÜR Stamina barı gösterir.
/// Screen Space Overlay Canvas kullanır, WorldToScreenPoint ile hedefin tepesinde konumlanır.
/// </summary>
public class StaminaBarUI : MonoBehaviour
{
    [Header("Referans")]
    public BeybladeController beyblade;

    [Header("Ayarlar")]
    public Vector3 offset = new Vector3(0, 1.15f, 0);
    public Vector2 barSize = new Vector2(170f, 24f);
    public Color fullColor = new Color(0.1f, 0.9f, 1f); // Mavi (varsayılan)
    public Color emptyColor = new Color(1f, 0.2f, 0.15f); // Kırmızı

    private Canvas overlayCanvas;
    private Image fillBar;
    private Image bgBar;
    private Text staminaText;
    private Text nameText;
    private Transform barTransform;

    private void Start()
    {
        if (beyblade == null)
            beyblade = GetComponent<BeybladeController>();

        // Oyuncu veya Düşman olmasına göre renk ve isim ayarla
        if (gameObject.name.Contains("Enemy") || gameObject.name.Contains("Rakip") || gameObject.name.Contains("AI"))
        {
            fullColor = new Color(1f, 0.35f, 0.1f); // Düşman Turuncu/Kırmızı
        }
        else
        {
            fullColor = new Color(0.1f, 0.85f, 1f); // Oyuncu Canlı Mavi
        }

        CreateOverlayBar();
    }

    private void LateUpdate()
    {
        if (beyblade == null || barTransform == null) return;

        // Pozisyon: 3D Dünya pozisyonunu 2D Ekran pozisyonuna dönüştür
        if (Camera.main != null)
        {
            Vector3 worldPos = beyblade.transform.position + offset;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // Kamera arkasında değilse ekranda göster
            if (screenPos.z > 0)
            {
                barTransform.position = screenPos;

                float ratio = Mathf.Clamp01(beyblade.currentStamina / beyblade.maxStamina);
                fillBar.fillAmount = ratio;
                fillBar.color = Color.Lerp(emptyColor, fullColor, ratio);

                if (staminaText != null)
                {
                    staminaText.text = "%" + Mathf.RoundToInt(ratio * 100f);
                }

                // Beyblade durduğunda ve staminası 0 olduğunda barı sakla
                if (!beyblade.isSpinning && ratio <= 0f)
                {
                    if (barTransform.gameObject.activeSelf)
                        barTransform.gameObject.SetActive(false);
                }
                else
                {
                    if (!barTransform.gameObject.activeSelf)
                        barTransform.gameObject.SetActive(true);
                }
            }
            else
            {
                if (barTransform.gameObject.activeSelf)
                    barTransform.gameObject.SetActive(false);
            }
        }
    }

    private void CreateOverlayBar()
    {
        // Screen Space Overlay Canvas (%100 Görünürlük Garantisi)
        GameObject canvasObj = new GameObject("StaminaBarCanvas_" + gameObject.name);
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 90; // Ön planda çizilsin

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        // Ana Bar Kapsayıcısı (Root Panel)
        GameObject barPanel = new GameObject("BarPanel");
        barPanel.transform.SetParent(canvasObj.transform, false);
        barTransform = barPanel.transform;
        RectTransform panelRt = barPanel.AddComponent<RectTransform>();
        panelRt.sizeDelta = barSize;

        // Dış Çerçeve (Koyu Metalik Border)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(barPanel.transform, false);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = new Color(0.04f, 0.06f, 0.1f, 0.95f);
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);

        // Arka Plan (Koyu Yarı Saydam)
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(barPanel.transform, false);
        bgBar = bgObj.AddComponent<Image>();
        bgBar.color = new Color(0.12f, 0.14f, 0.18f, 0.9f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        // Dolgu (Stamina Renkli Fill)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barPanel.transform, false);
        fillBar = fillObj.AddComponent<Image>();
        fillBar.color = fullColor;
        fillBar.type = Image.Type.Filled;
        fillBar.fillMethod = Image.FillMethod.Horizontal;
        fillBar.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0.02f, 0.08f);
        fillRect.anchorMax = new Vector2(0.98f, 0.92f);
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

        // İsim Etiketi (OYUNCU / RAKİP)
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(barPanel.transform, false);
        nameText = nameObj.AddComponent<Text>();
        nameText.text = gameObject.name.Contains("Enemy") || gameObject.name.Contains("Rakip") || gameObject.name.Contains("AI") ? "RAKİP" : "OYUNCU";
        nameText.fontSize = 13;
        nameText.fontStyle = FontStyle.Bold;
        nameText.color = Color.white;
        nameText.alignment = TextAnchor.MiddleLeft;
        nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameObj.AddComponent<Outline>().effectColor = Color.black;
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.05f, 0f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.offsetMin = nameRect.offsetMax = Vector2.zero;

        // Stamina Yüzde Metni (%100)
        GameObject txtObj = new GameObject("StaminaText");
        txtObj.transform.SetParent(barPanel.transform, false);
        staminaText = txtObj.AddComponent<Text>();
        staminaText.text = "%100";
        staminaText.fontSize = 13;
        staminaText.fontStyle = FontStyle.Bold;
        staminaText.color = Color.yellow;
        staminaText.alignment = TextAnchor.MiddleRight;
        staminaText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txtObj.AddComponent<Outline>().effectColor = Color.black;
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0.5f, 0f);
        txtRect.anchorMax = new Vector2(0.95f, 1f);
        txtRect.offsetMin = txtRect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (overlayCanvas != null)
            Destroy(overlayCanvas.gameObject);
    }
}
