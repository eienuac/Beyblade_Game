using UnityEngine;
using BeybladeCore;

/// <summary>
/// Menüden savaşa taşınan maç bilgisi (kariyer mi, hangi seviye) + denge testi modu.
/// Eski PlayerPrefs anahtarlarıyla uyumludur.
/// </summary>
public static class MatchContext
{
    const string CareerKey = "IsPathingMatch";
    const string LevelKey = "BotDifficultyLevel";

    /// <summary>Hızlı savaşta botun oyun seviyesi (orta).</summary>
    public const int QuickMatchLevel = 5;

    /// <summary>Hızlı savaşta rakip: oyuncunun beyiyle eşit güçte Savunma beyi.</summary>
    public const BeyArchetype QuickMatchRivalType = BeyArchetype.Defense;

    public static bool IsCareer => !IsBalanceTest && PlayerPrefs.GetInt(CareerKey, 0) == 1;

    public static int CareerLevel => Mathf.Max(1, PlayerPrefs.GetInt(LevelKey, 1));

    public static int BotLevel => IsBalanceTest ? balanceLevel : (IsCareer ? CareerLevel : QuickMatchLevel);

    public static BotDifficulty Difficulty => BotDifficulty.ForLevel(BotLevel);

    public static void SetupQuickMatch()
    {
        PlayerPrefs.SetInt(CareerKey, 0);
        PlayerPrefs.Save();
    }

    public static void SetupCareerMatch(int level)
    {
        PlayerPrefs.SetInt(CareerKey, 1);
        PlayerPrefs.SetInt(LevelKey, Mathf.Max(1, level));
        PlayerPrefs.Save();
    }

    public static void Clear() => SetupQuickMatch();

    // ---------------- Denge testi (AI vs AI, gerçek fizik) ----------------

    static bool balanceTest;
    static BeyArchetype balancePlayer, balanceEnemy;
    static int balanceLevel = 5;

    /// <summary>İki bey de bot tarafından oynanır, oyuncu girişi ve menü/ödül yok.</summary>
    public static bool IsBalanceTest => balanceTest;

    public static void BeginBalanceTest(BeyArchetype playerSide, BeyArchetype enemySide, int level)
    {
        balanceTest = true;
        balancePlayer = playerSide;
        balanceEnemy = enemySide;
        balanceLevel = Mathf.Max(1, level);
    }

    public static void EndBalanceTest() => balanceTest = false;

    public static bool TryGetForcedArchetype(bool isPlayerSide, out BeyArchetype type)
    {
        type = isPlayerSide ? balancePlayer : balanceEnemy;
        return balanceTest;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => balanceTest = false;
}
