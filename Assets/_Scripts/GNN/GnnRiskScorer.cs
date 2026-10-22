using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sınav sahnesinde kaçak noktasına yaklaşma cezasını GNN risk haritasından verir.
///
/// Kaçak sürerken oyuncu GNN'in "Tehlikeli" dediği alana (enerjili ekipmana ~1 m'den yakın) girerse:
///
///   ceza = bütçe × [ girişPayı × w_giriş + (1 − girişPayı) × (1 − e^(−E / τ)) ]
///
///   w_giriş : ilk girişteki GNN tehlike olasılığı
///   E       : tehlikeli alanda geçen, GNN tehlike olasılığıyla ağırlıklandırılmış süre (s)
///   τ       : maruziyet zaman sabiti
///
/// Ceza bütçeyi aşamaz; çıkıp girmek giriş payını tekrar kesmez. "Dikkat" alanı yalnızca uyarıdır.
///
/// Süre dolup kaçak yangına dönüştüğünde (ScenarioManager.KacakYanginaDonustu) oyuncunun o anki
/// konumu ayrıca cezalandırılır: Dikkat alanı −15, Tehlikeli alan −30. GNN hazır değilse aynı sınıflar
/// enerjili ekipman kutularına uzaklıktan (1 m / 2.5 m) bulunur.
/// </summary>
public class GnnRiskScorer : MonoBehaviour
{
    private static GnnRiskScorer active;

    public static bool IsScoring => active != null && active.riskPredictor != null && active.riskPredictor.IsReady;

    [Header("Referanslar (boşsa otomatik bulunur)")]
    [SerializeField] private GnnRiskPredictor riskPredictor;
    [SerializeField] private CalculateScore calculateScore;
    [Tooltip("Konumu puanlanan obje. Boşsa ana kamera (oyuncunun başı)")]
    [SerializeField] private Transform trackedTarget;

    [Header("Kaçağa Yaklaşma Bütçesi (en fazla ceza)")]
    [SerializeField] private float proximityBudget = 30f;

    [Header("Ceza Eğrisi")]
    [Tooltip("Bütçenin girişte hemen kesilen payı")]
    [SerializeField, Range(0f, 1f)] private float entryShare = 0.4f;
    [Tooltip("Maruziyet zaman sabiti (s): bu kadar kalınca kalan payın %63'ü kesilmiş olur")]
    [SerializeField] private float exposureTimeConstant = 5f;

    [Header("Süre Dolduğunda Konum Cezası (kaçak yangına dönüştü)")]
    [SerializeField] private int timeoutCautionPenalty = 15;
    [SerializeField] private int timeoutDangerPenalty = 30;
    [Tooltip("GNN hazır değilse kullanılan mesafeler (m)")]
    [SerializeField] private float fallbackDangerDistance = 1f;
    [SerializeField] private float fallbackCautionDistance = 2.5f;

    [Header("Zamanlama")]
    [SerializeField] private float updateInterval = 0.5f;
    [Tooltip("Dikkat alanından çıkıp bu süreden (s) önce tekrar girilirse yeniden uyarı verilmez")]
    [SerializeField] private float reentryCooldown = 3f;

    [Header("Uyarı Sesi (isteğe bağlı)")]
    [SerializeField] private AudioSource warningSound;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private bool entered;
    private float entryWeight;
    private float exposure;
    private int deducted;
    private float nextUpdate;
    private float lastTick = float.NegativeInfinity;
    private GnnRiskClass currentClass = GnnRiskClass.Safe;
    private float cautionLeftAt = float.NegativeInfinity;

    private void Start()
    {
        if (riskPredictor == null) riskPredictor = GnnRiskPredictor.Instance;
        if (riskPredictor == null) riskPredictor = FindObjectOfType<GnnRiskPredictor>();
        if (riskPredictor == null) riskPredictor = gameObject.AddComponent<GnnRiskPredictor>();
        if (calculateScore == null) calculateScore = FindObjectOfType<CalculateScore>();
        if (trackedTarget == null && Camera.main != null) trackedTarget = Camera.main.transform;
    }

    private void OnEnable() => active = this;

    private void OnDisable()
    {
        if (active == this) active = null;
    }

    private void Update()
    {
        if (Time.time < nextUpdate) return;
        nextUpdate = Time.time + updateInterval;

        float dt = Mathf.Min(Time.time - lastTick, updateInterval * 2f);
        lastTick = Time.time;

        if (!IsLeakActive() || trackedTarget == null || !riskPredictor.IsReady) return;

        GnnPrediction p = riskPredictor.GetPrediction(trackedTarget.position);
        if (!p.IsValid) return;

        GnnRiskClass previous = currentClass;
        currentClass = p.RiskClass;

        if (p.RiskClass == GnnRiskClass.Danger)
        {
            UpdateProximity(p, previous != GnnRiskClass.Danger, dt);
        }
        else if (p.RiskClass == GnnRiskClass.Caution && previous == GnnRiskClass.Safe)
        {
            if (Time.time - cautionLeftAt >= reentryCooldown) EnterCaution(p);
        }

        if (previous == GnnRiskClass.Caution && p.RiskClass == GnnRiskClass.Safe) cautionLeftAt = Time.time;
    }

    private void UpdateProximity(GnnPrediction p, bool entering, float dt)
    {
        float w = p.DangerProbability;

        if (!entered)
        {
            entered = true;
            entryWeight = w;
        }
        else if (!entering)
        {
            exposure += w * dt;
        }
        if (entering && warningSound != null) warningSound.Play();

        float target = proximityBudget * (entryShare * entryWeight + (1f - entryShare) * (1f - Mathf.Exp(-exposure / exposureTimeConstant)));
        int penalty = Mathf.RoundToInt(target) - deducted;
        if (penalty <= 0 || calculateScore == null) return;

        deducted += penalty;
        calculateScore.Deduct(penalty, null);
        if (showDebugLogs)
        {
            Debug.Log($"<color=red>[GNN] KAÇAĞA YAKIN (p={w:F2}, maruziyet={exposure:F1} s): -{penalty} " +
                      $"(yaklaşma toplam {deducted}/{Mathf.RoundToInt(proximityBudget)}) → Puan: {CalculateScore.Score}</color>");
        }
    }

    private void EnterCaution(GnnPrediction p)
    {
        if (warningSound != null) warningSound.Play();
        if (showDebugLogs)
            Debug.Log($"<color=orange>[GNN] DİKKAT ALANI (risk={p.RiskScore:F2}): yalnızca uyarı → Puan: {CalculateScore.Score}</color>");
    }

    /// <summary>Süre dolduğunda oyuncunun konumuna göre ek ceza (ScenarioManager çağırır).</summary>
    public static void ApplyTimeoutPositionPenalty(CalculateScore score)
    {
        GnnRiskScorer scorer = active != null ? active : FindObjectOfType<GnnRiskScorer>();
        Transform target = scorer != null && scorer.trackedTarget != null ? scorer.trackedTarget
            : Camera.main != null ? Camera.main.transform : null;
        if (target == null || score == null) return;

        GnnRiskClass zone = ClassifyAt(scorer, target.position, out string source);
        int caution = scorer != null ? scorer.timeoutCautionPenalty : 15;
        int danger = scorer != null ? scorer.timeoutDangerPenalty : 30;

        if (zone == GnnRiskClass.Danger)
            score.Deduct(danger, $"Yangın başladığında kaçağın yanındaydı ({source})");
        else if (zone == GnnRiskClass.Caution)
            score.Deduct(caution, $"Yangın başladığında kaçağın çevresindeydi ({source})");
        else
            Debug.Log($"[Puan] Yangın başladığında güvenli alandaydı ({source}): ek ceza yok");
    }

    private static GnnRiskClass ClassifyAt(GnnRiskScorer scorer, Vector3 position, out string source)
    {
        if (scorer != null && scorer.riskPredictor != null && scorer.riskPredictor.IsReady)
        {
            GnnPrediction p = scorer.riskPredictor.GetPrediction(position);
            if (p.IsValid)
            {
                source = $"GNN p_tehlike={p.DangerProbability:F2}";
                return p.RiskClass;
            }
        }

        float dangerDist = scorer != null ? scorer.fallbackDangerDistance : 1f;
        float cautionDist = scorer != null ? scorer.fallbackCautionDistance : 2.5f;
        float nearest = float.MaxValue;
        List<GnnHazardZone> zones = GnnGraphBuilder.FindHazardZones();
        foreach (GnnHazardZone z in zones)
            nearest = Mathf.Min(nearest, z.ContainsHorizontally(position) ? 0f : z.HorizontalDistance(position));

        source = $"mesafe {nearest:F1} m";
        if (nearest <= dangerDist) return GnnRiskClass.Danger;
        if (nearest <= cautionDist) return GnnRiskClass.Caution;
        return GnnRiskClass.Safe;
    }

    private static bool IsLeakActive() =>
        ScenarioManager.Instance != null && ScenarioManager.Instance.currentState == ScenarioManager.ScenarioState.InProgress;
}
