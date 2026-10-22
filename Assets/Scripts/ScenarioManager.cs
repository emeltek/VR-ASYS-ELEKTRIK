using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance;

    [Header("Bileşenler")]
    public ElectricLeak electricLeak;
    public DangerTimer dangerTimer;
    public UIController uiController;

    [Header("Puan")]
    public CalculateScore calculateScore;
    [Tooltip("2 dakika içinde enerji kesilmezse düşülen puan (tepki süresi cezasına ek)")]
    public int enerjiKesilmediCezasi = 30;

    [Header("Sesler")]
    public AudioSource anonsSource;
    public AudioClip kacakTespitAnons;
    public AudioClip tebriklerAnons;

    
    private string sceneName;
    public enum ScenarioState
    {
        Intro,
        InProgress,
        Completed,
        Failed      // süre doldu: kaçak yangına dönüştü
    }

    public ScenarioState currentState = ScenarioState.Intro;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
        if (calculateScore == null) calculateScore = FindObjectOfType<CalculateScore>();
    }

    // UI Start butonuna bağlanır
    public void StartButton()
    {
        if (currentState != ScenarioState.Intro) return;
        StartScenario();
    }

    void StartScenario()
    {
        currentState = ScenarioState.Intro;
        LeakiBaslat();
    }

    void LeakiBaslat()
    {
        currentState = ScenarioState.InProgress;
        electricLeak.LeakiAktifEt();
        dangerTimer.BaslatTimer();
        //anonsSource.PlayOneShot(kacakTespitAnons);
    }

    public void SalterKapatildi()
    {
        if (currentState != ScenarioState.InProgress) return;

        currentState = ScenarioState.Completed;
        dangerTimer.TimeriDurdur();
        electricLeak.LeakiDurdur();
        if (sceneName  == "VR-ASYS-ELEKTIRIK")
        {
            anonsSource.PlayOneShot(tebriklerAnons);

        }

        if (calculateScore != null)
            calculateScore.FinalizeScore(null);

        if (uiController != null)
            uiController.SenaryoBitti();
    }

    /// <summary>
    /// DangerTimer süre sınırında çağırır: enerji kesilmedi, kaçak yangına dönüştü.
    /// Efektler (kıvılcım, duman) sürer; puan, oyuncunun o anki konumuna göre ek ceza ile kesinleşir.
    /// </summary>
    public void KacakYanginaDonustu()
    {
        if (currentState != ScenarioState.InProgress) return;

        currentState = ScenarioState.Failed;
        dangerTimer.TimeriDurdur();

        if (calculateScore != null)
        {
            calculateScore.Deduct(enerjiKesilmediCezasi, "Enerji kesilmedi, kaçak yangına dönüştü");
            GnnRiskScorer.ApplyTimeoutPositionPenalty(calculateScore);
            calculateScore.FinalizeScore("Kaçak yangına dönüştü: enerji kesilmedi");
        }

        if (uiController != null)
            uiController.SenaryoBitti();
    }

    public void YanlisMudahale()
    {
        if (currentState != ScenarioState.InProgress) return;
    }

    public void TehlikeUyarisi()
    {
        if (currentState != ScenarioState.InProgress) return;
    }

}