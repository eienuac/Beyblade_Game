using UnityEngine;
using UnityEngine.SceneManagement;

public class PathingManager : MonoBehaviour
{
    [Header("Kariyer Ayarları")]
    public string battleSceneName = "SampleScene"; // Savaşların yapılacağı ana sahnenin adı

    public void StartNextMatch()
    {
        if (PlayerDataManager.Instance != null)
        {
            int currentLevel = PlayerDataManager.Instance.data.currentPathingLevel;
            Debug.Log("Kariyer Maçına Giriliyor! Seviye: " + currentLevel);

            // Burada bir GameManager'a veya PlayerPrefs'e "Gelecek maç kariyer modu maçıdır ve zorluğu budur" 
            // bilgisini aktarabiliriz. Şimdilik sadece sahneyi yüklüyoruz.
            PlayerPrefs.SetInt("IsPathingMatch", 1);
            PlayerPrefs.SetInt("BotDifficultyLevel", currentLevel);
            PlayerPrefs.Save();

            SceneManager.LoadScene(battleSceneName);
        }
    }

    /// <summary>
    /// Savaş bitip kazanıldığında çağrılır (BattleManager içinden çağırılacak)
    /// </summary>
    public static void OnMatchWon()
    {
        if (PlayerPrefs.GetInt("IsPathingMatch", 0) == 1)
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.data.currentPathingLevel++;
                PlayerDataManager.Instance.data.gold += 50; // Kazanma ödülü
                PlayerDataManager.Instance.SaveData();
                Debug.Log("Kariyer maçı kazanıldı! Yeni seviye: " + PlayerDataManager.Instance.data.currentPathingLevel);
            }
            PlayerPrefs.SetInt("IsPathingMatch", 0);
        }
    }
}
