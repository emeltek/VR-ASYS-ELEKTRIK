using UnityEngine;

public class MainSwitchboard : MonoBehaviour
{
    [Header("Görsel")]
    public GameObject salterAcik;
    public GameObject salterKapali;

    [Header("Ses")]
    public AudioSource tikSesi;

    private bool kapatildi = false;

    void OnTriggerEnter(Collider other)
    {
        if (kapatildi) return;

        if (!other.CompareTag("RightHand") && !other.CompareTag("LeftHand")) return;

        if (ScenarioManager.Instance.currentState
            != ScenarioManager.ScenarioState.InProgress) return;

        kapatildi = true;

        if (salterAcik != null)  salterAcik.SetActive(false);
        if (salterKapali != null) salterKapali.SetActive(true);
        if (tikSesi != null) tikSesi.Play();

        ScenarioManager.Instance.SalterKapatildi();
    }
}