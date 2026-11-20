#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Beyblade > Denge Testi menüsü: Play moduna girip AI vs AI maçları gerçek fizikle oynatır.</summary>
[InitializeOnLoad]
public static class BalanceTestMenu
{
    const string PendingKey = "Beyblade.BalanceTest.Pending";

    static BalanceTestMenu()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Beyblade/Denge Testi (AI vs AI, gerçek fizik)/Hızlı (eşleşme başına 6 maç)")]
    static void RunQuick() => Request(6);

    [MenuItem("Beyblade/Denge Testi (AI vs AI, gerçek fizik)/Detaylı (eşleşme başına 20 maç)")]
    static void RunDetailed() => Request(20);

    static void Request(int perPairing)
    {
        if (EditorApplication.isPlaying)
        {
            BalanceTestRunner.Begin(perPairing, 5, 4f);
            return;
        }
        EditorPrefs.SetInt(PendingKey, perPairing);
        EditorApplication.isPlaying = true;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        int pending = EditorPrefs.GetInt(PendingKey, 0);
        if (pending <= 0) return;
        EditorPrefs.DeleteKey(PendingKey);
        BalanceTestRunner.Begin(pending, 5, 4f);
    }
}
#endif
