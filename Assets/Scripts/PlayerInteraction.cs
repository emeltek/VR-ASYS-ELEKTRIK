using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    // Bu script priz objesine eklenir
    // Priz objesinde isTrigger = true Collider olmalı

    [Header("Ses")]
    public AudioSource uyariSesi;
    public AudioClip yanlisMudahaleClip;  // "Elektrik çarpması riski!"
    public AudioClip salterUyariClip;     // "Elektrik kesilmeden müdahale edilmemeli"

    private bool tehlikeli = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("RightHand") && !other.CompareTag("LeftHand")) return;

        if (ScenarioManager.Instance.currentState
            != ScenarioManager.ScenarioState.InProgress) return;

        if (tehlikeli)
        {
            uyariSesi.PlayOneShot(yanlisMudahaleClip);
            ScenarioManager.Instance.YanlisMudahale();
        }
        else
        {
            uyariSesi.PlayOneShot(salterUyariClip);
        }
    }

    public void TehlikeyiKaldir()
    {
        tehlikeli = false;
    }
}