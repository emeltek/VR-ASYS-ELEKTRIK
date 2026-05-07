using UnityEngine;

public class EnergyManager : MonoBehaviour
{
    public SwitchState[] switches;
   public GameObject electricEffect;
    public AudioSource audioSource;


    private bool wasAllOff = false; // önceki durumu takip et

    void Update()
    {
        bool allOff = true;

        foreach (var sw in switches)
        {
            if (sw.isOn)
            {
                allOff = false;
                break;
            }
        }

        electricEffect.SetActive(!allOff);

        // Sadece durum değiştiğinde çal
        if (allOff && !wasAllOff)
        {
            audioSource.Play();

            // Senaryo hâlâ devam ediyorsa şalter kapatıldı olarak bildir
            if (ScenarioManager.Instance != null &&
                ScenarioManager.Instance.currentState == ScenarioManager.ScenarioState.InProgress)
            {
                ScenarioManager.Instance.SalterKapatildi();
            }
        }

        wasAllOff = allOff;
    }
}