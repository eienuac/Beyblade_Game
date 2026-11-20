using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerSaveData
{
    public int gold = 50;
    public int gems = 10;
    public string playerName = "Blader";
    public string rankTitle = "Çaylak";

    public List<string> unlockedParts = new List<string>();

    public string equippedLayerID = "";
    public string equippedDiskID = "";
    public string equippedTipID = "";

    public int currentPathingLevel = 1;

    // İstatistikler (profil + görevler)
    public bool hasProfile = false;
    public int totalMatches = 0;
    public int totalWins = 0;
    public int totalLosses = 0;
    public int ringOutWins = 0;
    public int perfectLaunches = 0;
    public int bestCareerLevel = 1;
    public List<string> claimedTasks = new List<string>();

    // Ayarlar
    public float masterVolume = 1f;
    public int qualityLevel = -1;
}

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    public PlayerSaveData data;

    private const string SAVE_KEY = "BeybladeSaveData";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
            ApplySettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveData()
    {
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            data = JsonUtility.FromJson<PlayerSaveData>(json);
            if (data == null) data = CreateDefault();
            if (string.IsNullOrEmpty(data.playerName)) data.playerName = "Blader";
            if (string.IsNullOrEmpty(data.rankTitle) || data.rankTitle == "Newbie") data.rankTitle = RankFor(data.totalWins);
            if (EnsureStarterParts(data)) SaveData();
        }
        else
        {
            data = CreateDefault();
            SaveData();
        }
    }

    /// <summary>Eski kayıtlarda başlangıç parçaları eksikse ekler. Değişiklik olduysa true döner.</summary>
    static bool EnsureStarterParts(PlayerSaveData d)
    {
        bool changed = false;
        if (d.unlockedParts == null) { d.unlockedParts = new List<string>(); changed = true; }
        foreach (string id in new[] { DefaultPartCatalog.StarterLayer, DefaultPartCatalog.StarterDisk, DefaultPartCatalog.StarterTip })
        {
            if (!d.unlockedParts.Contains(id)) { d.unlockedParts.Add(id); changed = true; }
        }
        if (string.IsNullOrEmpty(d.equippedLayerID)) { d.equippedLayerID = DefaultPartCatalog.StarterLayer; changed = true; }
        if (string.IsNullOrEmpty(d.equippedDiskID)) { d.equippedDiskID = DefaultPartCatalog.StarterDisk; changed = true; }
        if (string.IsNullOrEmpty(d.equippedTipID)) { d.equippedTipID = DefaultPartCatalog.StarterTip; changed = true; }
        if (d.currentPathingLevel < 1) { d.currentPathingLevel = 1; changed = true; }
        if (d.claimedTasks == null) { d.claimedTasks = new List<string>(); changed = true; }
        // Eski kayıt: isim girilmişse profil var say
        if (!d.hasProfile && !string.IsNullOrEmpty(d.playerName) && d.playerName != "Blader") { d.hasProfile = true; changed = true; }
        return changed;
    }

    PlayerSaveData CreateDefault()
    {
        var d = new PlayerSaveData();
        d.unlockedParts.Add("layer_basic");
        d.unlockedParts.Add("disk_basic");
        d.unlockedParts.Add("tip_basic");
        d.equippedLayerID = "layer_basic";
        d.equippedDiskID = "disk_basic";
        d.equippedTipID = "tip_basic";
        return d;
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (data.gold < amount) return false;
        data.gold -= amount;
        SaveData();
        return true;
    }

    public bool SpendGems(int amount)
    {
        if (amount <= 0) return true;
        if (data.gems < amount) return false;
        data.gems -= amount;
        SaveData();
        return true;
    }

    public void AddGold(int amount)
    {
        data.gold += Mathf.Max(0, amount);
        SaveData();
    }

    /// <summary>Maç sonucu istatistiği (BattleManager çağırır).</summary>
    public void RecordMatch(bool won, bool draw, bool byRingOut)
    {
        data.totalMatches++;
        if (won) data.totalWins++;
        else if (!draw) data.totalLosses++;
        if (won && byRingOut) data.ringOutWins++;
        data.bestCareerLevel = Mathf.Max(data.bestCareerLevel, data.currentPathingLevel);
        data.rankTitle = RankFor(data.totalWins);
        SaveData();
    }

    public void RecordPerfectLaunch()
    {
        data.perfectLaunches++;
        SaveData();
    }

    public static string RankFor(int wins)
    {
        if (wins >= 50) return "Şampiyon";
        if (wins >= 25) return "Usta";
        if (wins >= 10) return "Yetenekli";
        if (wins >= 3) return "Amatör";
        return "Çaylak";
    }

    public void SetPlayerName(string name)
    {
        name = string.IsNullOrWhiteSpace(name) ? "Blader" : name.Trim();
        if (name.Length > 16) name = name.Substring(0, 16);
        data.playerName = name;
        data.hasProfile = true;
        SaveData();
    }

    /// <summary>Ses ve grafik ayarlarını uygular.</summary>
    public void ApplySettings()
    {
        AudioListener.volume = Mathf.Clamp01(data.masterVolume);
        if (data.qualityLevel >= 0 && data.qualityLevel < QualitySettings.names.Length)
            QualitySettings.SetQualityLevel(data.qualityLevel, true);
    }

    public void ResetAll()
    {
        data = CreateDefault();
        SaveData();
        ApplySettings();
    }

    public void UnlockPart(string partID)
    {
        if (!data.unlockedParts.Contains(partID))
        {
            data.unlockedParts.Add(partID);
            SaveData();
        }
    }

    public void EquipPart(PartType type, string partID)
    {
        if (!data.unlockedParts.Contains(partID)) return;

        switch (type)
        {
            case PartType.Layer: data.equippedLayerID = partID; break;
            case PartType.WeightDisk: data.equippedDiskID = partID; break;
            case PartType.PerformanceTip: data.equippedTipID = partID; break;
        }
        SaveData();
    }
}
