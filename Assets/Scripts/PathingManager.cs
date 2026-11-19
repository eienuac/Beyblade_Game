using UnityEngine;
using UnityEngine.SceneManagement;

public class PathingManager : MonoBehaviour
{
    [Header("Kariyer Ayarları")]
    public string battleSceneName = "SampleScene"; // Savaşların yapılacağı ana sahnenin adı

    public void StartNextMatch()
    {
        int level = PlayerDataManager.Instance != null ? PlayerDataManager.Instance.data.currentPathingLevel : 1;
        StartMatchAtLevel(level);
    }

    public void StartMatchAtLevel(int level)
    {
        Debug.Log("Kariyer Maçına Giriliyor! Seviye: " + level);
        MatchContext.SetupCareerMatch(level);
        SceneManager.LoadScene(battleSceneName);
    }

    /// <summary>
    /// Kariyer maçı kazanıldığında BattleManager çağırır. Sadece en yüksek açık seviye oynandıysa ilerletir.
    /// Ödül (beycoin) BattleManager tarafında MatchRewards ile verilir.
    /// </summary>
    public static void OnMatchWon()
    {
        if (!MatchContext.IsCareer || PlayerDataManager.Instance == null) return;

        var data = PlayerDataManager.Instance.data;
        if (MatchContext.CareerLevel >= data.currentPathingLevel)
        {
            data.currentPathingLevel = MatchContext.CareerLevel + 1;
            PlayerDataManager.Instance.SaveData();
            Debug.Log("Kariyer maçı kazanıldı! Yeni seviye: " + data.currentPathingLevel);
        }
    }
}
