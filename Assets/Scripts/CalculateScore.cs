using TMPro;
using UnityEngine;

/// <summary>
/// Puan 100'den başlar, hatalarda düşer (en az 0). Sonuçta standart risk etiketi gösterilir:
/// 75+ Güvenli, 50–74 Orta Riskli, 25–49 Riskli, 25 altı Yüksek Riskli.
/// Senaryo bitince (şalter kapatıldı / süre doldu) puan kesinleşir; sonra ceza işlenmez.
/// </summary>
public class CalculateScore : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    private static int score = 100;
    private static bool finalized;
    private static string resultNote = "";

    public static int Score => score;
    public static bool IsFinalized => finalized;

    private void Start()
    {
        ResetScore();
        GetScore();
    }

    /// <summary>Sahne yüklenişinde statik durum (StaticStateReset çağırır).</summary>
    public static void ResetScore()
    {
        score = 100;
        finalized = false;
        resultNote = "";
    }

    /// <summary>
    /// Eski API: puanı |amount| kadar düşürür. Hem eski (negatif) hem Dev'deki (pozitif) işaret
    /// kuralıyla çağrılabilir; sahnelerde değerler negatif kaldığı için puan hiçbir zaman artmaz.
    /// </summary>
    public void ScoreManagement(int amount)
    {
        Deduct(Mathf.Abs(amount), null);
    }

    /// <summary>Puan eklemek / düzeltmek için (en fazla 100).</summary>
    public void AddScore(int amount)
    {
        if (finalized) return;
        score = Mathf.Clamp(score + amount, 0, 100);
        GetScore();
    }

    public int GetCurrentScore() => score;

    /// <summary>Puanı düşürür (amount pozitif). Senaryo bittiyse etkisizdir.</summary>
    public void Deduct(int amount, string reason)
    {
        if (finalized || amount <= 0) return;
        score = Mathf.Max(0, score - amount);
        if (!string.IsNullOrEmpty(reason))
            Debug.Log($"<color=red>[Puan] {reason}: -{amount} → Puan: {score} ({GetRiskLabel(score)})</color>");
        GetScore();
    }

    /// <summary>Puanı kesinleştirir; not (ör. "Kaçak yangına dönüştü") puanın altında gösterilir.</summary>
    public void FinalizeScore(string note)
    {
        if (finalized) return;
        finalized = true;
        resultNote = note ?? "";
        Debug.Log($"<color=cyan>[Puan] Sonuç: {score} ({GetRiskLabel(score)}){(resultNote.Length > 0 ? " — " + resultNote : "")}</color>");
        GetScore();
    }

    public void GetScore()
    {
        if (scoreText == null) return;
        string text = $"Puan: {score}\n<size=75%><color={GetRiskColor(score)}>{GetRiskLabel(score)}</color></size>";
        if (resultNote.Length > 0) text += $"\n<size=55%>{resultNote}</size>";
        scoreText.text = text;
    }

    public static string GetRiskLabel(int value)
    {
        if (value >= 75) return "Güvenli";
        if (value >= 50) return "Orta Riskli";
        if (value >= 25) return "Riskli";
        return "Yüksek Riskli";
    }

    public static string GetRiskColor(int value)
    {
        if (value >= 75) return "#2ECC71";
        if (value >= 50) return "#F1C40F";
        if (value >= 25) return "#E67E22";
        return "#E74C3C";
    }
}
