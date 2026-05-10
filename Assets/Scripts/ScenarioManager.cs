using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance;

    [Header("Bileşenler")]
    public ElectricLeak electricLeak;
    public DangerTimer dangerTimer;
    public UIController uiController;

    [Header("Sesler")]
    public AudioSource anonsSource;
    public AudioClip kacakTespitAnons;
    public AudioClip tebriklerAnons;

    
    private string sceneName;
    public enum ScenarioState
    {
        Intro,
        InProgress,
        Completed
    }

    public ScenarioState currentState = ScenarioState.Intro;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
    }

    void Update() { }

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