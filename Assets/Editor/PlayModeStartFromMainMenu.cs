#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Play'e basınca her zaman MainMenu sahnesinden başlar.
/// (Editor'da açık sahne SampleScene olsa bile.)
/// </summary>
[InitializeOnLoad]
public static class PlayModeStartFromMainMenu
{
    const string MainMenuPath = "Assets/MainMenu.unity";

    static PlayModeStartFromMainMenu()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        if (sceneAsset != null)
            EditorSceneManager.playModeStartScene = sceneAsset;
    }

    [MenuItem("Beyblade/Play Mode: MainMenu'den Başla")]
    static void Enable()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        EditorSceneManager.playModeStartScene = sceneAsset;
        Debug.Log("Play Mode başlangıç sahnesi: MainMenu");
    }

    [MenuItem("Beyblade/Play Mode: Açık Sahneden Başla")]
    static void Disable()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("Play Mode başlangıç sahnesi: açık sahne");
    }
}
#endif
