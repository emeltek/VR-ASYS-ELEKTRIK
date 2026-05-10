using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class SagCable : MonoBehaviour
{
    [Header("Bağlantı Noktaları")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Sarkma Ayarları")]
    [Range(0f, 2f)]
    public float sagAmount = 0.4f;   // Ne kadar sarkacak
    public int segments = 20;         // Kablo yumuşaklığı (10-30 arası iyi)

    private LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments;
    }

    void Update()
    {
        DrawSagCable();
    }

    void DrawSagCable()
    {
        if (startPoint == null || endPoint == null) return;

        Vector3 start = startPoint.position;
        Vector3 end   = endPoint.position;

        // Ortadaki kontrol noktası — kablo en çok ortada sarkar
        Vector3 mid = (start + end) / 2f;
        mid.y -= sagAmount;  // Aşağı çek

        lr.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);

            // Quadratic Bezier: start → mid(sarkık) → end
            Vector3 pos = Mathf.Pow(1 - t, 2) * start
                          + 2 * (1 - t) * t * mid
                          + Mathf.Pow(t, 2) * end;

            lr.SetPosition(i, pos);
        }
    }
}