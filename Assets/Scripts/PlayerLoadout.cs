using System.Collections.Generic;
using BeybladeCore;

/// <summary>Oyuncunun takılı parçalarından stat hesabı (Beylocker, hızlı savaş rakibi ve savaş aynı sonucu görür).</summary>
public static class PlayerLoadout
{
    public static BeyStatBlock ComputeStats(PartDatabase db)
    {
        db = PartDatabase.Resolve(db);
        var d = PlayerDataManager.Instance != null ? PlayerDataManager.Instance.data : null;
        BeybladePart l = d != null ? db.GetPartByID(d.equippedLayerID) : null;
        BeybladePart k = d != null ? db.GetPartByID(d.equippedDiskID) : null;
        BeybladePart t = d != null ? db.GetPartByID(d.equippedTipID) : null;
        if (l == null) l = db.GetPartByID(DefaultPartCatalog.StarterLayer);
        if (k == null) k = db.GetPartByID(DefaultPartCatalog.StarterDisk);
        if (t == null) t = db.GetPartByID(DefaultPartCatalog.StarterTip);

        var list = new List<PartStats>(3);
        if (l != null) list.Add(l.ToStats());
        if (k != null) list.Add(k.ToStats());
        if (t != null) list.Add(t.ToStats());
        return BeyStatCalculator.Compute(list.ToArray());
    }

    public static FighterProfile ComputeProfile(PartDatabase db) => FighterProfile.FromStatBlock(ComputeStats(db));

    public static string ArchetypeName(BeyArchetype a)
    {
        switch (a)
        {
            case BeyArchetype.Attack: return "SALDIRI";
            case BeyArchetype.Defense: return "SAVUNMA";
            case BeyArchetype.Stamina: return "DAYANIKLILIK";
            default: return "DENGE";
        }
    }
}
