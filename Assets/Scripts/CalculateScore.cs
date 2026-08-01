using System;
using TMPro;
using UnityEngine;

public class CalculateScore : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    private int score = 100;

    private void Start()
    {
        score = 100;
        GetScore();
    }

    // Yanlış davranış veya gecikmede puan düşürmek için (Pozitif değer girildiğinde puandan düşer)
    public void ScoreManagement(int amount)
    {
        score = Mathf.Max(0, score - amount);
        GetScore();
    }

    // Puan eklemek/düzeltmek istenirse
    public void AddScore(int amount)
    {
        score = Mathf.Clamp(score + amount, 0, 100);
        GetScore();
    }

    // UI üzerindeki metni günceller
    public void GetScore()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Puan: {score}";
        }
    }

    public int GetCurrentScore()
    {
        return score;
    }
}