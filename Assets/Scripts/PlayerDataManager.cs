using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerSaveData
{
    public int gold = 50;
    public int gems = 10;
    public string playerName = "Blader";
    public string rankTitle = "Newbie";

    public List<string> unlockedParts = new List<string>();

    public string equippedLayerID = "";
    public string equippedDiskID = "";
    public string equippedTipID = "";

    public int currentPathingLevel = 1;
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
            if (string.IsNullOrEmpty(data.rankTitle)) data.rankTitle = "Newbie";
        }
        else
        {
            data = CreateDefault();
            SaveData();
        }
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
