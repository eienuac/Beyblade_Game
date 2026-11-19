#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Koddaki varsayılan parçaları (DefaultPartCatalog) Inspector'dan düzenlenebilir asset'lere çevirir
/// ve GlobalPartDatabase'e ekler. Var olan parçalara dokunmaz.
/// </summary>
public static class PartAssetGenerator
{
    const string DatabasePath = "Assets/GlobalPartDatabase.asset";
    const string PartsFolder = "Assets/Parts";

    [MenuItem("Beyblade/Varsayılan Parça Asset'lerini Oluştur")]
    public static void Generate()
    {
        PartDatabase db = AssetDatabase.LoadAssetAtPath<PartDatabase>(DatabasePath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<PartDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }

        if (!AssetDatabase.IsValidFolder(PartsFolder))
            AssetDatabase.CreateFolder("Assets", "Parts");

        int created = 0;
        foreach (DefaultPartCatalog.Def def in DefaultPartCatalog.Definitions)
        {
            if (db.GetPartByID(def.id) != null) continue;

            string path = Path.Combine(PartsFolder, def.id + ".asset").Replace('\\', '/');
            BeybladePart part = AssetDatabase.LoadAssetAtPath<BeybladePart>(path);
            if (part == null)
            {
                part = ScriptableObject.CreateInstance<BeybladePart>();
                DefaultPartCatalog.Apply(def, part);
                AssetDatabase.CreateAsset(part, path);
                created++;
            }
            db.allParts.Add(part);
        }

        db.allParts.RemoveAll(p => p == null);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Beyblade] {created} parça asset'i oluşturuldu. Veritabanında toplam {db.allParts.Count} parça var.");
        Selection.activeObject = db;
    }

    [MenuItem("Beyblade/Kayıt Verisini Sıfırla")]
    public static void ResetSave()
    {
        if (!EditorUtility.DisplayDialog("Kayıt sıfırlansın mı?", "Beycoin, açılmış parçalar ve kariyer seviyesi silinecek.", "Sıfırla", "Vazgeç"))
            return;
        PlayerPrefs.DeleteKey("BeybladeSaveData");
        PlayerPrefs.DeleteKey("IsPathingMatch");
        PlayerPrefs.DeleteKey("BotDifficultyLevel");
        PlayerPrefs.Save();
        Debug.Log("[Beyblade] Kayıt verisi sıfırlandı.");
    }

    [MenuItem("Beyblade/Test: 1000 Beycoin Ekle")]
    public static void AddTestGold()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddGold(1000);
            Debug.Log("[Beyblade] +1000 beycoin (oyun çalışırken).");
            return;
        }
        Debug.LogWarning("[Beyblade] Önce Play'e bas, sonra tekrar dene.");
    }
}
#endif
