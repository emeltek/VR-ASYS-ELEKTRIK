using UnityEngine;

/// <summary>
/// Statik değişkenler sahne geçişinde (eğitim → sınav, yeniden başlat) sıfırlanmaz.
/// Her sahne yüklenişinde puan ve temas durumunu sıfırlar.
/// </summary>
public static class StaticStateReset
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        CalculateScore.ResetScore();
        FalseBehaviourController.ResetState();
    }
}
