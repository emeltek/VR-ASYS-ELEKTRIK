using UnityEngine;

public class ElectricLeak : MonoBehaviour
{
    [Header("Efektler")]
    public ParticleSystem kivircimEfekti;
    public ParticleSystem dumanEfekti;
    public AudioSource cizztiSesi;
    public MonitorFlicker[] monitorler;

    public void LeakiAktifEt()
    {
        if (kivircimEfekti != null) kivircimEfekti.Play();
        if (dumanEfekti != null)    dumanEfekti.Play();
        if (cizztiSesi != null)     cizztiSesi.Play();

        foreach (MonitorFlicker m in monitorler)
            m.enabled = true;
    }

    public void LeakiDurdur()
    {
        if (kivircimEfekti != null) kivircimEfekti.Stop();
        if (dumanEfekti != null)    dumanEfekti.Stop();
        if (cizztiSesi != null)     cizztiSesi.Stop();

        foreach (MonitorFlicker m in monitorler)
            m.ElektrigiKes();
    }
}