using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Enerjili fişe / prize dokunma. Tüm FalseBehaviourController'lar tek bir temas durumu paylaşır:
/// iki el aynı anda (aynı ya da farklı prizde) tek temas sayılır.
///   - İlk temas: firstTouchPenalty
///   - Sonraki her yeni temas: repeatTouchPenalty
///   - Temas sürdükçe saniye başına penaltyPerSecond (üst sınır yok: kalınca tehlike büyür)
/// Ceza yalnızca kaçak sürerken (ScenarioState.InProgress) işlenir.
/// </summary>
public class FalseBehaviourController : MonoBehaviour
{
    public AudioSource wrongBehaviourAudio;
    public CalculateScore calculateScore;

    [Header("Dokunma Cezası")]
    public int firstTouchPenalty = 30;
    public int repeatTouchPenalty = 10;
    public float penaltyPerSecond = 5f;

    private string sceneName;

    // Ortak temas durumu (tüm prizler)
    private static readonly HashSet<Collider> touchingHands = new HashSet<Collider>();
    private static int touchCount;
    private static float touchSeconds;
    private static int timePenaltyDeducted;
    private static int lastUpdateFrame = -1;

    public static int TouchCount => touchCount;
    public static bool IsTouching => touchingHands.Count > 0;

    /// <summary>Sahne yüklenişinde statik durum (StaticStateReset çağırır).</summary>
    public static void ResetState()
    {
        touchingHands.Clear();
        touchCount = 0;
        touchSeconds = 0f;
        timePenaltyDeducted = 0;
        lastUpdateFrame = -1;
    }

    private void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;
    }

    private static bool IsHand(Collider other) => other.CompareTag("LeftHand") || other.CompareTag("RightHand");

    private static bool IsLeakActive() =>
        ScenarioManager.Instance != null && ScenarioManager.Instance.currentState == ScenarioManager.ScenarioState.InProgress;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsHand(other) || !IsLeakActive()) return;

        bool newTouch = touchingHands.Count == 0;
        if (!touchingHands.Add(other) || !newTouch) return;

        if (sceneName == "VR-ASYS-ELEKTIRIK" && wrongBehaviourAudio != null)
            wrongBehaviourAudio.Play();

        touchCount++;
        int penalty = touchCount == 1 ? firstTouchPenalty : repeatTouchPenalty;
        if (calculateScore != null)
            calculateScore.Deduct(penalty, $"Enerjili ekipmana dokunma ({touchCount}. temas, {name})");
        if (ScenarioManager.Instance != null) ScenarioManager.Instance.YanlisMudahale();
    }

    private void OnTriggerExit(Collider other)
    {
        touchingHands.Remove(other);
    }

    private void Update()
    {
        // Birden fazla priz var; süre cezası karede bir kez işlenir
        if (lastUpdateFrame == Time.frameCount) return;
        lastUpdateFrame = Time.frameCount;

        // Kapanan / yok olan el collider'ı OnTriggerExit göndermez
        touchingHands.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        if (touchingHands.Count == 0 || !IsLeakActive()) return;

        touchSeconds += Time.deltaTime;
        int target = Mathf.FloorToInt(touchSeconds * penaltyPerSecond);
        int penalty = target - timePenaltyDeducted;
        if (penalty <= 0 || calculateScore == null) return;

        timePenaltyDeducted = target;
        calculateScore.Deduct(penalty, $"Temas sürüyor ({touchSeconds:F0} s)");
    }
}
