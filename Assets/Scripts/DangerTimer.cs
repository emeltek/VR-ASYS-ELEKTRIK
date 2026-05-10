using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DangerTimer : MonoBehaviour
{
    [Header("Ayarlar")]
    public float uyariSuresi = 20f;
    public float tekrarAraligi = 15f;

    [Header("Ses")]
    public AudioSource uyariSesi;
    public AudioClip tehlikeClip;   // "Tehlike devam ediyor..."

    private float timer = 0f;
    private bool aktif = false;
    private bool ilkUyariVerildi = false;
    
    public CalculateScore calculateScore;
    public int negativeScore = 10;
    private string sceneName;

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
            UyariVer();
        }

        if (ilkUyariVerildi && timer >= uyariSuresi + tekrarAraligi)
        {
            timer = uyariSuresi;
            UyariVer();
        }
    }

    void UyariVer()
    {
        if (sceneName == "VR-ASYS-ELEKTIRIK")
        {
            uyariSesi.PlayOneShot(tehlikeClip);

        }
        ScenarioManager.Instance.TehlikeUyarisi();
        calculateScore.ScoreManagement(negativeScore);
    }

    public void BaslatTimer()
    {
        aktif = true;
        timer = 0f;
    }

    public void TimeriDurdur()
    {
        aktif = false;
        timer = 0f;
        ilkUyariVerildi = false;
    }
}