using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    [Header("UI Paneli")]
    public GameObject startPanel;   // Start butonunu içeren panel

    [Header("Tamamlanma Paneli (İsteğe bağlı)")]
    public GameObject completedPanel;
    public GameObject scorePanel;
    public CalculateScore calculateScore;

    void Start()
    {
        if (startPanel != null)
            startPanel.SetActive(true);

        if (completedPanel != null)
            completedPanel.SetActive(false);
    }

    public void OnStartButtonClicked()
    {
        if (startPanel != null)
            startPanel.SetActive(false);

        ScenarioManager.Instance.StartButton();
    }

    // ScenarioManager'ın Completed durumuna geçtiğinde çağırılabilir
    public void SenaryoBitti()
    {
        if (completedPanel != null)
            completedPanel.SetActive(true);
        if (scorePanel != null)
            scorePanel.SetActive(true);

        if (calculateScore != null)
            calculateScore.GetScore();
    }

    public void SahneYenidenBaslat()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SonrakiSahne()
    {
        int sonrakiIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (sonrakiIndex < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(sonrakiIndex);
        else
            Debug.LogWarning("Sonraki sahne bulunamadı. Build Settings'te sahne ekli mi?");
    }

    public void UygulamaKapat()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
