using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerSaveData
{
    public int gold = 0;
    
    // Sahip olunan parçaların ID'leri
    public List<string> unlockedParts = new List<string>();
    
    // Şu an takılı olan parçaların ID'leri
    public string equippedLayerID = "";
    public string equippedDiskID = "";
    public string equippedTipID = "";

    // Görev/Kariyer ilerlemesi
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
        Debug.Log("Oyun Kaydedildi!");
    }

    public void LoadData()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            data = JsonUtility.FromJson<PlayerSaveData>(json);
        }
        else
        {
            data = new PlayerSaveData();
            // Başlangıç parçaları (Default)
            data.unlockedParts.Add("layer_basic");
            data.unlockedParts.Add("disk_basic");
            data.unlockedParts.Add("tip_basic");

            data.equippedLayerID = "layer_basic";
            data.equippedDiskID = "disk_basic";
            data.equippedTipID = "tip_basic";
            
            SaveData();
        }
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
