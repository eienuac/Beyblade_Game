using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PartDatabase", menuName = "Beyblade/Part Database")]
public class PartDatabase : ScriptableObject
{
    public List<BeybladePart> allParts = new List<BeybladePart>();

    static PartDatabase shared;

    public BeybladePart GetPartByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (BeybladePart p in allParts)
        {
            if (p != null && p.partID == id) return p;
        }
        return null;
    }

    public List<BeybladePart> GetPartsOfType(PartType type)
    {
        var list = new List<BeybladePart>();
        foreach (BeybladePart p in allParts)
            if (p != null && p.type == type) list.Add(p);
        return list;
    }

    /// <summary>
    /// Oyunun kullandığı tek parça listesi: Inspector'daki asset (varsa) + koddaki varsayılan parçalar.
    /// Asset'teki parça aynı ID'ye sahipse varsayılanın yerine geçer, yani parçaları Inspector'dan ezebilirsin.
    /// Asset'in kendisine dokunulmaz; çalışma zamanında ayrı bir kopya oluşturulur.
    /// </summary>
    public static PartDatabase Shared
    {
        get
        {
            if (shared == null) shared = Build(FindAsset());
            return shared;
        }
    }

    /// <summary>Sahnede atanmış bir asset varsa onu da hesaba katar.</summary>
    public static PartDatabase Resolve(PartDatabase assigned)
    {
        if (shared == null) shared = Build(assigned != null ? assigned : FindAsset());
        return shared;
    }

    static PartDatabase FindAsset()
    {
        PartDatabase asset = Resources.Load<PartDatabase>("GlobalPartDatabase");
        if (asset != null) return asset;
        PartDatabase[] loaded = Resources.FindObjectsOfTypeAll<PartDatabase>();
        foreach (PartDatabase db in loaded)
            if (db != null && db != shared && !string.IsNullOrEmpty(db.name)) return db;
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<PartDatabase>("Assets/GlobalPartDatabase.asset");
#else
        return null;
#endif
    }

    static PartDatabase Build(PartDatabase source)
    {
        PartDatabase db = CreateInstance<PartDatabase>();
        db.name = "RuntimePartDatabase";
        db.hideFlags = HideFlags.DontSave;

        if (source != null)
        {
            foreach (BeybladePart p in source.allParts)
                if (p != null && !string.IsNullOrEmpty(p.partID) && db.GetPartByID(p.partID) == null)
                    db.allParts.Add(p);
        }

        foreach (BeybladePart p in DefaultPartCatalog.CreateAll())
            if (db.GetPartByID(p.partID) == null)
                db.allParts.Add(p);
            else
                DestroyImmediate(p);

        return db;
    }
}
