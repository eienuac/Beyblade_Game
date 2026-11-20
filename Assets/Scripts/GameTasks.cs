using System;
using System.Collections.Generic;

/// <summary>Görevler: oyun istatistiklerinden ilerler, tamamlanınca beycoin ödülü alınır.</summary>
public static class GameTasks
{
    public class Def
    {
        public string id, title, desc;
        public int goal, reward;
        public Func<PlayerSaveData, int> progress;
    }

    public static readonly List<Def> All = new List<Def>
    {
        new Def { id = "first_win", title = "İLK ZAFER", desc = "Bir maç kazan", goal = 1, reward = 50, progress = d => d.totalWins },
        new Def { id = "perfect_launch", title = "KUSURSUZ FIRLATIŞ", desc = "Altın bölgede fırlat", goal = 1, reward = 60, progress = d => d.perfectLaunches },
        new Def { id = "ring_out", title = "ARENA DIŞI", desc = "Rakibi arenadan at", goal = 1, reward = 80, progress = d => d.ringOutWins },
        new Def { id = "win_3", title = "SERİ GALİBİYET", desc = "3 maç kazan", goal = 3, reward = 100, progress = d => d.totalWins },
        new Def { id = "play_10", title = "DENEYİM", desc = "10 maç oyna", goal = 10, reward = 120, progress = d => d.totalMatches },
        new Def { id = "career_5", title = "YÜKSELİŞ", desc = "Kariyerde 5. seviyeye ulaş", goal = 5, reward = 200, progress = d => d.currentPathingLevel },
        new Def { id = "win_10", title = "USTALIK", desc = "10 maç kazan", goal = 10, reward = 250, progress = d => d.totalWins },
    };

    public static int Progress(Def t, PlayerSaveData d) => d == null ? 0 : Math.Min(t.goal, t.progress(d));
    public static bool IsComplete(Def t, PlayerSaveData d) => d != null && t.progress(d) >= t.goal;
    public static bool IsClaimed(Def t, PlayerSaveData d) => d != null && d.claimedTasks != null && d.claimedTasks.Contains(t.id);

    public static int ClaimableCount(PlayerSaveData d)
    {
        int n = 0;
        foreach (Def t in All) if (IsComplete(t, d) && !IsClaimed(t, d)) n++;
        return n;
    }

    public static bool Claim(Def t)
    {
        PlayerDataManager pdm = PlayerDataManager.Instance;
        if (pdm == null) return false;
        if (!IsComplete(t, pdm.data) || IsClaimed(t, pdm.data)) return false;
        pdm.data.claimedTasks.Add(t.id);
        pdm.AddGold(t.reward); // kaydeder
        return true;
    }
}
