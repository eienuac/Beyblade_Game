using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    private void OnEnable()
    {
        if (layerListContainer != null && diskListContainer != null && tipListContainer != null)
            RefreshUI();
    }

    public void RefreshUI()
    {
        if (layerListContainer == null || diskListContainer == null || tipListContainer == null) return;
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
        if (PlayerDataManager.Instance == null || partDatabase == null) return;

        List<string> unlocked = PlayerDataManager.Instance.data.unlockedParts;

        foreach (string partID in unlocked)
        {
            BeybladePart part = partDatabase.GetPartByID(partID);
            if (part == null) continue;

            Transform targetContainer = null;
            if (part.type == PartType.Layer) targetContainer = layerListContainer;
            else if (part.type == PartType.WeightDisk) targetContainer = diskListContainer;
            else if (part.type == PartType.PerformanceTip) targetContainer = tipListContainer;

            if (targetContainer != null && partButtonPrefab != null)
            {
                GameObject btnObj = Instantiate(partButtonPrefab, targetContainer);
                
                // Text ve İkon ayarlaması (UI'da Text veya Image componenti olduğunu varsayıyoruz)
                Text txt = btnObj.GetComponentInChildren<Text>();
                if (txt != null) txt.text = part.partName;
                TMP_Text tmp = btnObj.GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = part.partName;

                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => EquipPart(part));
                }
            }
        }
    }

    private void EquipPart(BeybladePart part)
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.EquipPart(part.type, part.partID);
            UpdateStatBars();
        }
    }

    private void UpdateStatBars()
    {
        if (PlayerDataManager.Instance == null || partDatabase == null) return;

        string lID = PlayerDataManager.Instance.data.equippedLayerID;
        string dID = PlayerDataManager.Instance.data.equippedDiskID;
        string tID = PlayerDataManager.Instance.data.equippedTipID;

        BeybladePart l = partDatabase.GetPartByID(lID);
        BeybladePart d = partDatabase.GetPartByID(dID);
        BeybladePart t = partDatabase.GetPartByID(tID);

        float totalAtk = 0, totalDef = 0, totalStm = 0, totalWgt = 0;

        if (l != null) { totalAtk += l.attackBonus; totalDef += l.defenseBonus; totalStm += l.staminaBonus; totalWgt += l.weightBonus; }
        if (d != null) { totalAtk += d.attackBonus; totalDef += d.defenseBonus; totalStm += d.staminaBonus; totalWgt += d.weightBonus; }
        if (t != null) { totalAtk += t.attackBonus; totalDef += t.defenseBonus; totalStm += t.staminaBonus; totalWgt += t.weightBonus; }

        // Barları güncelle (Maksimum değerlere bölerek %0-1 arası doldurur)
        if (attackBar != null) attackBar.fillAmount = totalAtk / 20f;
        if (defenseBar != null) defenseBar.fillAmount = totalDef / 2f;
        if (staminaBar != null) staminaBar.fillAmount = totalStm / 10f;
        if (weightBar != null) weightBar.fillAmount = totalWgt / 6f;
    }
}
