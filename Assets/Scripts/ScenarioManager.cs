using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance;

    [Header("Bileşenler")]
    public ElectricLeak electricLeak;
    public DangerTimer dangerTimer;

    [Header("Sesler")]
    public AudioSource anonsSource;
    public AudioClip girisAnons;
    public AudioClip kacakTespitAnons;
    public AudioClip tebriklerAnons;

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
        StartScenario();
    }

    void Update()
    {
        // Şimdilik boş bırakıyoruz (hata almamak için)
    }

    void StartScenario()
    {
        currentState = ScenarioState.Intro;
        anonsSource.PlayOneShot(girisAnons);
        Invoke(nameof(LeakiBaslat), girisAnons.length + 1f);
    }

    void LeakiBaslat()
    {
        currentState = ScenarioState.InProgress;
        electricLeak.LeakiAktifEt();
        dangerTimer.BaslatTimer();
        anonsSource.PlayOneShot(kacakTespitAnons);
    }

    public void SalterKapatildi()
    {
        if (currentState != ScenarioState.InProgress) return;

        currentState = ScenarioState.Completed;
        dangerTimer.TimeriDurdur();
        electricLeak.LeakiDurdur();
        anonsSource.PlayOneShot(tebriklerAnons);
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