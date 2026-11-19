using UnityEngine;
using BeybladeCore;

/// <summary>
/// Menüden savaşa taşınan maç bilgisi (kariyer mi, hangi seviye).
/// Eski PlayerPrefs anahtarlarıyla uyumludur.
/// </summary>
public static class MatchContext
{
    const string CareerKey = "IsPathingMatch";
    const string LevelKey = "BotDifficultyLevel";

    /// <summary>Kariyer dışı hızlı maçta botun seviyesi.</summary>
    public const int QuickMatchLevel = 3;

    public static bool IsCareer => PlayerPrefs.GetInt(CareerKey, 0) == 1;

    public static int CareerLevel => Mathf.Max(1, PlayerPrefs.GetInt(LevelKey, 1));

    public static int BotLevel => IsCareer ? CareerLevel : QuickMatchLevel;

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
}
