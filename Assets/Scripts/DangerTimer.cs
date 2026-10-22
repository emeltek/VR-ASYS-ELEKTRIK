using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Kaçak başladıktan sonra enerjinin kesilme süresini izler.
///   - Sesli uyarı: uyariSuresi saniyede bir kez, sonra tekrarAraligi'nda bir
///   - Tepki süresi cezası: ilk freeResponseTime saniye ücretsiz, sonra
///       ceza = responseBudget × (1 − e^(−gecikme / responseTimeConstant))   (en fazla responseBudget)
///     Puan canlı düşer; şalter kapatılınca durur.
///   - scenarioTimeLimit içinde enerji kesilmezse kaçak yangına dönüşür (ScenarioManager.KacakYanginaDonustu).
/// </summary>
public class DangerTimer : MonoBehaviour
{
    [Header("Ayarlar")]
    public float uyariSuresi = 20f;
    public float tekrarAraligi = 15f;

    [Header("Ses")]
    public AudioSource uyariSesi;
    public AudioClip tehlikeClip;   // "Tehlike devam ediyor..."

    [Header("Tepki Süresi Puanı")]
    public CalculateScore calculateScore;
    [Tooltip("Bu süre (s) içinde enerjiyi kesen kullanıcıdan puan düşülmez")]
    public float freeResponseTime = 20f;
    [Tooltip("Geç müdahale için en fazla düşülecek puan")]
    public float responseBudget = 30f;
    [Tooltip("Ceza zaman sabiti (s): ücretsiz süreden bu kadar sonra bütçenin %63'ü düşülmüş olur")]
    public float responseTimeConstant = 30f;
    [Tooltip("Bu süre (s) içinde enerji kesilmezse kaçak yangına dönüşür ve senaryo biter")]
    public float scenarioTimeLimit = 120f;

    private float timer = 0f;
    private bool aktif = false;
    private bool ilkUyariVerildi = false;
    private float sonUyari;
    private int responseDeducted;
    private string sceneName;

    public float ElapsedTime => timer;

    private void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
    }

    void Update()
    {
        if (!aktif) return;

        timer += Time.deltaTime;

        if (!ilkUyariVerildi && timer >= uyariSuresi)
        {
            ilkUyariVerildi = true;
            sonUyari = timer;
            UyariVer();
        }
        else if (ilkUyariVerildi && timer - sonUyari >= tekrarAraligi)
        {
            sonUyari = timer;
            UyariVer();
        }

        UpdateResponsePenalty(false);

        if (timer >= scenarioTimeLimit)
        {
            UpdateResponsePenalty(true);
            aktif = false;
            if (ScenarioManager.Instance != null) ScenarioManager.Instance.KacakYanginaDonustu();
        }
    }

    /// <param name="full">Süre doldu: bütçenin tamamı düşülür</param>
    private void UpdateResponsePenalty(bool full)
    {
        float delay = Mathf.Max(0f, timer - freeResponseTime);
        float target = full ? responseBudget : responseBudget * (1f - Mathf.Exp(-delay / responseTimeConstant));
        int penalty = Mathf.RoundToInt(target) - responseDeducted;
        if (penalty <= 0 || calculateScore == null) return;

        responseDeducted += penalty;
        calculateScore.Deduct(penalty, $"Geç müdahale ({timer:F0} s, toplam {responseDeducted}/{Mathf.RoundToInt(responseBudget)})");
    }

    void UyariVer()
    {
        if (sceneName == "VR-ASYS-ELEKTIRIK" && uyariSesi != null && tehlikeClip != null)
        {
            uyariSesi.PlayOneShot(tehlikeClip);
        }
        ScenarioManager.Instance.TehlikeUyarisi();
    }

    public void BaslatTimer()
    {
        aktif = true;
        timer = 0f;
        ilkUyariVerildi = false;
        responseDeducted = 0;
    }

    public void TimeriDurdur()
    {
        aktif = false;
        ilkUyariVerildi = false;
    }
}
