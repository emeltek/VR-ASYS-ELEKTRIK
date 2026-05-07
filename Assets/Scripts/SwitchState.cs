using Oculus.Interaction;
using UnityEngine;

public class SwitchState : MonoBehaviour
{
    public bool isOn = false;
    public float offThreshold = -115f;

    private AudioSource switchoffVoice;

    private bool wasOn;
    private bool initialized = false;

    private void Start()
    {
        GameObject audiosObj = GameObject.Find("Audios");
        if (audiosObj != null)
            switchoffVoice = audiosObj.GetComponent<AudioSource>();
        else
            Debug.LogWarning($"{gameObject.name}: 'Audios' adında bir GameObject bulunamadı.");
    }

    private void Update()
    {
        float currentAngle = transform.localEulerAngles.x;

        if (currentAngle > 180)
            currentAngle -= 360;

        Debug.Log($"Angle X: {currentAngle:F1}");

        // -140 açık, -90 kapalı olduğu için mantık tersine döndü
        isOn = currentAngle < offThreshold;

        if (!initialized)
        {
            wasOn = isOn;
            initialized = true;
        }

        if (!isOn && wasOn)
        {
            Debug.Log($"{gameObject.name} - Switch KAPALI");

            if (switchoffVoice != null)
                switchoffVoice.Play();
        }
        else if (isOn && !wasOn)
        {
            Debug.Log($"{gameObject.name} - Switch AÇIK");
        }

        wasOn = isOn;
    }
}
