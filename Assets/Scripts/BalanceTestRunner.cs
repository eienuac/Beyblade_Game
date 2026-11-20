using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using BeybladeCore;

/// <summary>
/// Gerçek Unity fiziğiyle AI vs AI denge testi. Editor menüsü: Beyblade > Denge Testi.
/// Her eşleşme iki tarafta da oynanır (taraf avantajı silinir), sonuç Console'a tablo olarak yazılır.
/// Simülasyondaki (BattleSim) oranlarla karşılaştırarak stadyum geometrisinin dengeyi bozup bozmadığını görürsün.
/// </summary>
public class BalanceTestRunner : MonoBehaviour
{
    class Pairing
    {
        public BeyArchetype a, b;
        public int winsA, winsB, draws, ringOuts;
        public float totalTime;
        public int Played => winsA + winsB + draws;
    }

    static BalanceTestRunner instance;

    readonly List<Pairing> pairings = new List<Pairing>();
    int matchesPerPairing = 10;
    int level = 5;
    float speed = 4f;
    int pairIndex;
    int matchIndex;
    bool sideSwapped;
    float matchStartRealtime;
    bool waitingNext;
    const float MaxMatchSeconds = 240f;

    public static bool Running => instance != null;

    public static void Begin(int perPairing, int botLevel, float timeScale)
    {
        if (instance != null) return;
        GameObject go = new GameObject("BalanceTestRunner");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BalanceTestRunner>();
        instance.matchesPerPairing = Mathf.Max(2, perPairing);
        instance.level = botLevel;
        instance.speed = Mathf.Clamp(timeScale, 1f, 8f);

        BeyArchetype[] types = { BeyArchetype.Attack, BeyArchetype.Defense, BeyArchetype.Stamina, BeyArchetype.Balance };
        for (int i = 0; i < types.Length; i++)
            for (int j = i + 1; j < types.Length; j++)
                instance.pairings.Add(new Pairing { a = types[i], b = types[j] });

        Debug.Log($"[Denge Testi] Başladı: {instance.pairings.Count} eşleşme × {instance.matchesPerPairing} maç, bot seviyesi {botLevel}, hız x{instance.speed}");
        instance.StartNext();
    }

    void StartNext()
    {
        if (pairIndex >= pairings.Count)
        {
            Finish();
            return;
        }

        waitingNext = false;
        Pairing p = pairings[pairIndex];
        sideSwapped = (matchIndex & 1) == 1;
        MatchContext.BeginBalanceTest(sideSwapped ? p.b : p.a, sideSwapped ? p.a : p.b, level);
        Time.timeScale = speed;
        matchStartRealtime = Time.realtimeSinceStartup;
        SceneManager.LoadScene("SampleScene");
    }

    /// <summary>BattleManager maç bitince çağırır. outcome: oyuncu (sol) tarafın sonucu.</summary>
    public static void Report(MatchOutcome outcome, bool ringOut, float duration)
    {
        if (instance == null) return;
        instance.Record(outcome, ringOut, duration);
    }

    void Record(MatchOutcome outcome, bool ringOut, float duration)
    {
        if (pairIndex >= pairings.Count || waitingNext) return;
        waitingNext = true;
        Pairing p = pairings[pairIndex];

        bool aIsPlayerSide = !sideSwapped;
        if (outcome == MatchOutcome.Draw) p.draws++;
        else if ((outcome == MatchOutcome.Win) == aIsPlayerSide) p.winsA++;
        else p.winsB++;
        if (ringOut) p.ringOuts++;
        p.totalTime += duration;

        matchIndex++;
        if (matchIndex >= matchesPerPairing)
        {
            matchIndex = 0;
            pairIndex++;
        }
        // Sahne yükleme bir kare sonra (BattleManager'ın Update'i bitsin)
        Invoke(nameof(StartNext), 0.1f);
    }

    void Update()
    {
        if (pairIndex >= pairings.Count || waitingNext) return;
        // Takılan maç (ör. iki bey de duvara sıkıştı) → beraberlik say
        if (Time.timeSinceLevelLoad > MaxMatchSeconds && SceneManager.GetActiveScene().name == "SampleScene")
            Record(MatchOutcome.Draw, false, Time.timeSinceLevelLoad);
    }

    void Finish()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[Denge Testi] Bitti — bot seviyesi {level}, eşleşme başına {matchesPerPairing} maç (gerçek fizik)");
        sb.AppendLine("Eşleşme                       A kazanır   B kazanır   Berabere   Arena dışı   Ort. süre");
        foreach (Pairing p in pairings)
        {
            int n = Mathf.Max(1, p.Played);
            sb.AppendLine($"{p.a,-12} vs {p.b,-12}   {100f * p.winsA / n,6:0}%     {100f * p.winsB / n,6:0}%     {100f * p.draws / n,5:0}%     {100f * p.ringOuts / n,5:0}%     {p.totalTime / n,5:0}s");
        }
        sb.AppendLine("Hedef (simülasyon): Saldırı>Dayanıklılık ~%63, Dayanıklılık>Savunma ~%59, Savunma>Saldırı ~%60, Denge %40-55");
        Debug.Log(sb.ToString());

        MatchContext.EndBalanceTest();
        Time.timeScale = 1f;
        instance = null;
        Destroy(gameObject);
        SceneManager.LoadScene("MainMenu");
    }

    void OnGUI()
    {
        if (pairIndex >= pairings.Count) return;
        Pairing p = pairings[pairIndex];
        GUI.color = Color.white;
        GUI.Box(new Rect(10, Screen.height - 70, 520, 60),
            $"DENGE TESTİ  {pairIndex + 1}/{pairings.Count}:  {p.a} vs {p.b}   maç {matchIndex + 1}/{matchesPerPairing}\n" +
            $"A {p.winsA}  ·  B {p.winsB}  ·  berabere {p.draws}     (Play'den çıkarak iptal)");
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            MatchContext.EndBalanceTest();
            Time.timeScale = 1f;
        }
    }
}
