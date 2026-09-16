using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TaskManager : MonoBehaviour
{
    public PartDatabase partDatabase;
    public GameObject rewardPanel; // 3 Parça seçim ekranı
    
    [Header("Ödül Slotları (UI)")]
    public Button rewardButton1;
    public Button rewardButton2;
    public Button rewardButton3;

    private void Start()
    {
        if (rewardPanel != null) rewardPanel.SetActive(false);
    }

    /// <summary>
    /// Bir görev tamamlandığında çağrılır. 3 farklı kategoriden (Atak, Savunma, Stamina) aynı seviyede 3 rastgele parça sunar.
    /// </summary>
    public void CompleteTaskAndShowRewards(int rewardTier = 1)
    {
        if (partDatabase == null || rewardPanel == null) return;

        List<BeybladePart> attackParts = new List<BeybladePart>();
        List<BeybladePart> defenseParts = new List<BeybladePart>();
        List<BeybladePart> staminaParts = new List<BeybladePart>();

        // Veritabanındaki parçaları tiplerine ve seviyelerine göre ayır
        foreach (BeybladePart p in partDatabase.allParts)
        {
            if (p.tier == rewardTier)
            {
                if (p.type == PartType.Layer) attackParts.Add(p); // Layer genelde Atak
                else if (p.type == PartType.WeightDisk) defenseParts.Add(p); // Disk genelde Savunma
                else if (p.type == PartType.PerformanceTip) staminaParts.Add(p); // Tip genelde Stamina
            }
        }

        // Rastgele 3 parça seç
        BeybladePart r1 = attackParts.Count > 0 ? attackParts[Random.Range(0, attackParts.Count)] : partDatabase.allParts[0];
        BeybladePart r2 = defenseParts.Count > 0 ? defenseParts[Random.Range(0, defenseParts.Count)] : partDatabase.allParts[1];
        BeybladePart r3 = staminaParts.Count > 0 ? staminaParts[Random.Range(0, staminaParts.Count)] : partDatabase.allParts[2];

        // UI Butonlarına ata
        SetupRewardButton(rewardButton1, r1);
        SetupRewardButton(rewardButton2, r2);
        SetupRewardButton(rewardButton3, r3);

        rewardPanel.SetActive(true);
    }

    private void SetupRewardButton(Button btn, BeybladePart part)
    {
        if (btn == null || part == null) return;

        Text txt = btn.GetComponentInChildren<Text>();
        if (txt != null) txt.text = part.partName + "\n(Tier " + part.tier + ")";

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => ClaimReward(part));
    }

    private void ClaimReward(BeybladePart part)
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.UnlockPart(part.partID);
            Debug.Log("Ödül alındı: " + part.partName);
        }

        if (rewardPanel != null) rewardPanel.SetActive(false);
    }

    // Test için (Kullanıcı arayüzüne buton koyup çağırabilir)
    public void DebugCompleteRandomTask()
    {
        CompleteTaskAndShowRewards(1);
    }
}
