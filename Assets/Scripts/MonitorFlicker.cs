using UnityEngine;

public class MonitorFlicker : MonoBehaviour
{
    [Header("Flicker Ayarları")]
    public float minBekle = 0.05f;
    public float maxBekle = 0.3f;

    private Material mat;
    private float timer;
    private float sonrakiFlicker;
    private bool beyazMi = true;

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        SonrakiSureAyarla();
    }

    void Update()
    {
        if (ScenarioManager.Instance.currentState
            != ScenarioManager.ScenarioState.InProgress)
        {
            EmissionAyarla(Color.white);
            return;
        }

        timer += Time.deltaTime;
        if (timer >= sonrakiFlicker)
        {
            timer = 0f;
            Flicker();
            SonrakiSureAyarla();
        }
    }

    void Flicker()
    {
        beyazMi = !beyazMi;
        EmissionAyarla(beyazMi ? Color.white : Color.black);
    }

    void EmissionAyarla(Color renk)
    {
        mat.SetColor(EmissionColor, renk);
    }

    void SonrakiSureAyarla()
    {
        sonrakiFlicker = Random.Range(minBekle, maxBekle);
    }

    public void ElektrigiKes()
    {
        enabled = false;
        EmissionAyarla(Color.black);
    }
}