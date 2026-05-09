using System;
using TMPro;
using UnityEngine;


public class CalculateScore : MonoBehaviour
{
    public TextMeshProUGUI  scoreText;
    private static int score;

    private void Start()
    {
        score = 100;
    }

    public void ScoreManagement(int amount)
    {
        score = Mathf.Max(0, score + amount);
        GetScore();
    }

    public void GetScore()
    {
        scoreText.text = $"Puan: {score}";
    }
}