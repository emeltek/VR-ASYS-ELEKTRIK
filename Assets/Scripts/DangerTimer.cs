using UnityEngine;

public class DangerTimer : MonoBehaviour
{
    [Header("Ayarlar")]
    public float uyariSuresi = 12f;
    public float tekrarAraligi = 15f;

    [Header("Ses")]
    public AudioSource uyariSesi;
    public AudioClip tehlikeClip;   // "Tehlike devam ediyor..."

    private float timer = 0f;
    private bool aktif = false;
    private bool ilkUyariVerildi = false;

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
        uyariSesi.PlayOneShot(tehlikeClip);
        ScenarioManager.Instance.TehlikeUyarisi();
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