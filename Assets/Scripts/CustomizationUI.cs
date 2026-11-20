using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using BeybladeCore;

/// <summary>
/// Beylocker: açılmış parçaları listeler, takılanı vurgular, statları savaşla aynı formülle gösterir.
/// </summary>
public class CustomizationUI : MonoBehaviour
{
    public PartDatabase partDatabase;

    [Header("UI Elementleri (Arayüz)")]
    public Transform layerListContainer;
    public Transform diskListContainer;
    public Transform tipListContainer;

    public GameObject partButtonPrefab; // Listedeki parça butonunun şablonu

    [Header("Stat Barları")]
    public Image attackBar;
    public Image defenseBar;
    public Image staminaBar;
    public Image weightBar;
    [Tooltip("Opsiyonel: hesaplanan tip (Saldırı/Savunma/...)")]
    public TextMeshProUGUI typeLabel;
    [Tooltip("Opsiyonel: seçili parçanın açıklaması")]
    public TextMeshProUGUI detailLabel;

    static readonly Color EquippedColor = new Color(0.12f, 0.82f, 1f, 0.55f);
    static readonly Color IdleColor = new Color(0.14f, 0.22f, 0.36f, 0.28f);

    private void OnEnable()
    {
        if (layerListContainer != null && diskListContainer != null && tipListContainer != null)
            RefreshUI();
    }

    public void RefreshUI()
    {
        if (layerListContainer == null || diskListContainer == null || tipListContainer == null) return;
        partDatabase = PartDatabase.Resolve(partDatabase);
        ClearLists();
        PopulateLists();
        UpdateStatBars();
    }

    private void ClearLists()
    {
        foreach (Transform child in layerListContainer) Destroy(child.gameObject);
        foreach (Transform child in diskListContainer) Destroy(child.gameObject);
        foreach (Transform child in tipListContainer) Destroy(child.gameObject);
    }

    private void PopulateLists()
    {
        if (PlayerDataManager.Instance == null || partDatabase == null || partButtonPrefab == null) return;

        var data = PlayerDataManager.Instance.data;
        List<string> unlocked = data.unlockedParts;

        // Veritabanı sırasıyla göster ki liste her açılışta aynı olsun
        foreach (BeybladePart part in partDatabase.allParts)
        {
            if (part == null || !unlocked.Contains(part.partID)) continue;

            Transform targetContainer = null;
            string equippedId = null;
            if (part.type == PartType.Layer) { targetContainer = layerListContainer; equippedId = data.equippedLayerID; }
            else if (part.type == PartType.WeightDisk) { targetContainer = diskListContainer; equippedId = data.equippedDiskID; }
            else if (part.type == PartType.PerformanceTip) { targetContainer = tipListContainer; equippedId = data.equippedTipID; }
            if (targetContainer == null) continue;

            bool equipped = part.partID == equippedId;
            GameObject btnObj = Instantiate(partButtonPrefab, targetContainer);
            btnObj.name = "Part_" + part.partID;
            btnObj.SetActive(true);

            string label = part.partName;
            Text txt = btnObj.GetComponentInChildren<Text>();
            if (txt != null) txt.text = label;
            TMP_Text tmp = btnObj.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = label;
                tmp.color = equipped ? Color.white : ModernUIKit.Dim;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 12f;
                tmp.fontSizeMax = 20f;
            }

            Image bg = btnObj.GetComponent<Image>();
            if (bg != null) bg.color = equipped ? EquippedColor : IdleColor;

            // Sol tarafta parçanın canlı 3B ikonu
            RawImage icon = new GameObject("Icon").AddComponent<RawImage>();
            icon.transform.SetParent(btnObj.transform, false);
            icon.raycastTarget = false;
            RectTransform irt = icon.rectTransform;
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(0f, 0.5f);
            irt.pivot = new Vector2(0f, 0.5f);
            irt.anchoredPosition = new Vector2(4f, 0f);
            irt.sizeDelta = new Vector2(56f, 56f);
            ShopUI.SetPartIcon(icon, part);
            if (tmp != null)
            {
                RectTransform trt = tmp.rectTransform;
                trt.offsetMin = new Vector2(58f, trt.offsetMin.y);
                tmp.alignment = TextAlignmentOptions.Left;
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                BeybladePart captured = part;
                btn.onClick.AddListener(() => EquipPart(captured));
            }
        }
    }

    private void EquipPart(BeybladePart part)
    {
        if (PlayerDataManager.Instance == null) return;
        PlayerDataManager.Instance.EquipPart(part.type, part.partID);
        if (detailLabel != null) detailLabel.text = part.partName + "   " + ShopUI.StatLine(part);
        RefreshUI();
    }

    public static string ArchetypeName(BeyArchetype a) => PlayerLoadout.ArchetypeName(a);

    private void UpdateStatBars()
    {
        if (partDatabase == null) return;
        BeyStatBlock s = PlayerLoadout.ComputeStats(partDatabase);

        // Bar 0'da tamamen boş görünmesin diye küçük bir taban
        if (attackBar != null) attackBar.fillAmount = Mathf.Lerp(0.06f, 1f, s.AttackNorm);
        if (defenseBar != null) defenseBar.fillAmount = Mathf.Lerp(0.06f, 1f, s.DefenseNorm);
        if (staminaBar != null) staminaBar.fillAmount = Mathf.Lerp(0.06f, 1f, s.StaminaNorm);
        if (weightBar != null) weightBar.fillAmount = Mathf.Lerp(0.06f, 1f, s.WeightNorm);
        if (typeLabel != null) typeLabel.text = "TİP · " + ArchetypeName(s.Archetype);
    }
}
