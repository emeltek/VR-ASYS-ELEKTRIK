using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Enerjili ekipman kutusu (FalseBehaviourController trigger'ı: fiş, 3'lü priz).</summary>
public struct GnnHazardZone
{
    public BoxCollider Box;
    public GnnHazardType Type;

    /// <summary>Kutu devre dışı olsa da çalışır (Collider.bounds kapalı collider'da boş döner).</summary>
    public bool Contains(Vector3 worldPoint)
    {
        Vector3 local = Box.transform.InverseTransformPoint(worldPoint) - Box.center;
        Vector3 half = Box.size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
    }

    /// <summary>Yatay (XZ) uzaklık: prizler yerde / alçakta olduğu için dikey fark sayılmaz.</summary>
    public float HorizontalDistance(Vector3 worldPoint)
    {
        Vector3 local = Box.transform.InverseTransformPoint(worldPoint) - Box.center;
        Vector3 half = Box.size * 0.5f;
        Vector3 clamped = new Vector3(Mathf.Clamp(local.x, -half.x, half.x), local.y, Mathf.Clamp(local.z, -half.z, half.z));
        Vector3 closest = Box.transform.TransformPoint(clamped + Box.center);
        return new Vector2(closest.x - worldPoint.x, closest.z - worldPoint.z).magnitude;
    }

    /// <summary>Yatay olarak kutunun içinde mi (dikey sınır yok).</summary>
    public bool ContainsHorizontally(Vector3 worldPoint)
    {
        Vector3 local = Box.transform.InverseTransformPoint(worldPoint) - Box.center;
        Vector3 half = Box.size * 0.5f;
        return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z;
    }

    public Bounds WorldBounds()
    {
        var b = new Bounds(Box.transform.TransformPoint(Box.center), Vector3.zero);
        Vector3 h = Box.size * 0.5f;
        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z);
            b.Encapsulate(Box.transform.TransformPoint(Box.center + corner));
        }
        return b;
    }
}

/// <summary>
/// Ofisin zemininden GNN grafı kurar. Eğitim verisi ve runtime grafı aynı kodla Editor'de
/// üretilir; böylece özellikler birebir aynı hesaplanır.
///
/// ÖNEMLİ: FalseBehaviourController trigger kutuları özellik olarak KULLANILMAZ (trigger'lar fizik
/// sorgularından hariç tutulur). Kutular yalnızca eğitimde etiket kaynağıdır (ComputeLabels).
/// </summary>
public sealed class GnnGraphBuilder
{
    public static readonly string[] FeatureNames =
    {
        "leak_distance",          // 0: kaçak noktasına düz uzaklık
        "leak_path_distance",     // 1: kaçak noktasına zemin üzerinden (masaları dolanarak) uzaklık
        "equipment_distance",     // 2: en yakın elektrikli ekipmana (priz, fiş, kablo, kasa, monitör) uzaklık
        "cable_density",          // 3: çevredeki kablo sayısı (takılma / temas riski)
        "panel_distance",         // 4: şalter panosuna uzaklık (enerjiyi kesme yolu)
        "min_clearance",          // 5: 8 yönde en yakın engel
        "mean_clearance",         // 6: 8 yönde ortalama engel uzaklığı
        "openness"                // 7: açık yön oranı (kaçış alanı)
    };

    public static readonly string[] ClassNames = { "Safe", "Caution", "Danger" };
    public static int FeatureCount => FeatureNames.Length;

    private const float ChestHeight = 1.2f;
    private const float WaistHeight = 0.8f;
    private const float KneeHeight = 0.4f;
    private const float CableNormalizer = 10f;

    private static readonly Vector3[] Dirs8 =
    {
        new Vector3(1, 0, 0), new Vector3(-1, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, -1),
        new Vector3(0.7071f, 0, 0.7071f), new Vector3(-0.7071f, 0, 0.7071f),
        new Vector3(0.7071f, 0, -0.7071f), new Vector3(-0.7071f, 0, -0.7071f)
    };

    private static readonly Vector2Int[] ForwardNeighbors =
    {
        new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, -1)
    };

    private readonly GnnGraphSettings settings;
    private readonly HashSet<Collider> ignored = new HashSet<Collider>();
    private readonly List<Vector3> leakSources;
    private readonly List<Vector3> panels;
    private readonly List<Bounds> equipment = new List<Bounds>();
    private readonly List<Bounds> cables = new List<Bounds>();
    private readonly RaycastHit[] hitBuffer = new RaycastHit[64];
    private readonly Collider[] colliderBuffer = new Collider[512];
    private readonly Bounds area;
    private readonly float groundY;
    private readonly float minWalkableNormalY;

    public GnnGraph Graph { get; } = new GnnGraph();
    public bool HasArea { get; }
    public int LeakSourceCount => leakSources.Count;
    public int PanelCount => panels.Count;
    public int EquipmentCount => equipment.Count;
    public int CableCount => cables.Count;
    public float GroundY => groundY;

    /// <param name="siteArea">Grafın kurulacağı alan (dünya). Boyutu sıfırsa graf kurulmaz.</param>
    /// <param name="leakSources">Elektrik kaçağı noktaları (ör. ElectricLeak.kivircimEfekti)</param>
    /// <param name="panels">Şalter panosu konumları (EnergyManager, MainSwitchboard)</param>
    /// <param name="ignoreRoots">Collider'ları ve renderer'ları yok sayılacak objeler (oyuncu, tehlike kutuları)</param>
    /// <param name="groundReference">Zemin yüksekliğini belirlemek için zemindeki / zemin üstündeki bir nokta</param>
    public GnnGraphBuilder(GnnGraphSettings settings, Bounds siteArea, List<Vector3> leakSources, List<Vector3> panels,
        IEnumerable<Transform> ignoreRoots, Vector3 groundReference)
    {
        this.settings = settings;
        this.leakSources = leakSources;
        this.panels = panels;
        area = siteArea;
        HasArea = siteArea.size.x > 0f && siteArea.size.z > 0f;
        minWalkableNormalY = Mathf.Cos(settings.maxSlope * Mathf.Deg2Rad);

        var roots = new List<Transform>();
        foreach (Transform root in ignoreRoots)
        {
            if (root == null) continue;
            roots.Add(root);
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
                ignored.Add(c);
        }

        groundY = FindGroundHeight(groundReference);
        if (HasArea) CollectEquipment(roots);

        Graph.FeatureCount = FeatureCount;
        Graph.CellSize = settings.cellSize;
        Graph.GridOrigin = new Vector3(area.min.x, groundY, area.min.z);
        Graph.GridOffset = settings.gridOffset;
    }

    /// <summary>Grafı birkaç kareye yayarak kurar.</summary>
    public IEnumerator Build(int nodesPerStep)
    {
        if (!HasArea) yield break;
        Physics.SyncTransforms();

        float cs = Graph.CellSize;
        int countX = Mathf.CeilToInt(area.size.x / cs);
        int countZ = Mathf.CeilToInt(area.size.z / cs);
        int budget = 0;

        // 1) Düğümler: yürünebilir ve boş zemin hücreleri
        for (int ix = 0; ix < countX; ix++)
        {
            for (int iz = 0; iz < countZ; iz++)
            {
                var cell = new Vector2Int(ix, iz);
                if (TrySampleGround(CellCenter(cell), out Vector3 floorPoint))
                {
                    Graph.CellToNode[cell] = Graph.Positions.Count;
                    Graph.Positions.Add(floorPoint);
                    Graph.Cells.Add(cell);
                }

                if (++budget >= nodesPerStep * 2) { budget = 0; yield return null; }
            }
        }

        // 2) Düğüm özellikleri (yol uzaklığı kenarlar kurulduktan sonra)
        int n = Graph.NodeCount;
        int f = FeatureCount;
        Graph.Features = new float[n * f];
        for (int i = 0; i < n; i++)
        {
            ComputeFeatures(Graph.Positions[i], Graph.Features, i * f);
            if (++budget >= nodesPerStep) { budget = 0; yield return null; }
        }

        // 3) Kenarlar: arada engel olmayan 8-komşular
        var adjacency = new List<int>[n];
        for (int i = 0; i < n; i++) adjacency[i] = new List<int>(8);
        for (int i = 0; i < n; i++)
        {
            Vector2Int cell = Graph.Cells[i];
            foreach (Vector2Int d in ForwardNeighbors)
            {
                if (!Graph.CellToNode.TryGetValue(cell + d, out int j)) continue;
                if (!IsConnected(Graph.Positions[i], Graph.Positions[j])) continue;
                adjacency[i].Add(j);
                adjacency[j].Add(i);
            }
            if (++budget >= nodesPerStep * 2) { budget = 0; yield return null; }
        }

        Graph.NeighborOffsets = new int[n + 1];
        for (int i = 0; i < n; i++) Graph.NeighborOffsets[i + 1] = Graph.NeighborOffsets[i] + adjacency[i].Count;
        Graph.NeighborIndices = new int[Graph.NeighborOffsets[n]];
        for (int i = 0; i < n; i++) adjacency[i].CopyTo(Graph.NeighborIndices, Graph.NeighborOffsets[i]);

        // 4) Kaçak noktasına zemin üzerinden uzaklık (masalar / duvarlar dolanılarak)
        ComputePathDistances();
    }

    public GnnGraph BuildImmediate()
    {
        IEnumerator routine = Build(int.MaxValue);
        while (routine.MoveNext()) { }
        return Graph;
    }

    private Vector3 CellCenter(Vector2Int cell)
    {
        float cs = Graph.CellSize;
        return new Vector3(
            Graph.GridOrigin.x + (cell.x + 0.5f + settings.gridOffset.x) * cs,
            groundY,
            Graph.GridOrigin.z + (cell.y + 0.5f + settings.gridOffset.y) * cs);
    }

    /// <summary>
    /// Hücredeki zemin noktası: zemin yüksekliği bandındaki en üst yürünebilir yüzey.
    /// Dar bant, masa / dolap üstlerini zemin saymaz; masanın altı ise gövde testinden geçemez.
    /// </summary>
    private bool TrySampleGround(Vector3 center, out Vector3 floorPoint)
    {
        floorPoint = center;
        float top = groundY + 2.5f;
        Vector3 origin = new Vector3(center.x, top, center.z);
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, hitBuffer, 6f, settings.layerMask, QueryTriggerInteraction.Ignore);

        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (ignored.Contains(h.collider) || h.normal.y < minWalkableNormalY) continue;
            if (Mathf.Abs(h.point.y - groundY) > settings.floorBand) continue;
            if (!found || h.point.y > floorPoint.y) { floorPoint = h.point; found = true; }
        }
        if (!found) return false;

        // Hücre bir yapı / eşya tarafından dolduruluyorsa düğüm değil
        float r = settings.bodyRadius;
        Vector3 bottom = floorPoint + Vector3.up * (r + 0.15f);
        Vector3 head = floorPoint + Vector3.up * 1.6f;
        return OverlapCapsule(bottom, head, r) == 0;
    }

    private void ComputeFeatures(Vector3 fp, float[] output, int offset)
    {
        Vector3 up = Vector3.up;
        float probe = settings.hazardProbeDistance;

        // 0: Kaçak noktasına düz uzaklık
        Vector3 waist = fp + up * WaistHeight;
        float leak = probe;
        foreach (Vector3 s in leakSources) leak = Mathf.Min(leak, Vector3.Distance(s, waist));

        // 2: En yakın elektrikli ekipman
        float equip = probe;
        foreach (Bounds b in equipment) equip = Mathf.Min(equip, Mathf.Sqrt(b.SqrDistance(waist)));

        // 3: Çevredeki kablolar (yatay uzaklık)
        int cableCount = 0;
        float cableRadius = settings.cableRadius;
        foreach (Bounds b in cables)
        {
            Vector3 c = b.ClosestPoint(new Vector3(fp.x, b.center.y, fp.z));
            if (new Vector2(c.x - fp.x, c.z - fp.z).sqrMagnitude <= cableRadius * cableRadius) cableCount++;
        }

        // 4: Şalter panosu
        Vector3 chest = fp + up * ChestHeight;
        float panel = probe;
        foreach (Vector3 p in panels) panel = Mathf.Min(panel, Vector3.Distance(p, chest));

        // 5-7: Çevre açıklığı
        float clearProbe = settings.clearanceProbeDistance;
        float minClear = clearProbe, sumClear = 0f;
        int open = 0;
        foreach (Vector3 dir in Dirs8)
        {
            float d = NearestHit(waist, dir, clearProbe, out RaycastHit h) ? h.distance : clearProbe;
            minClear = Mathf.Min(minClear, d);
            sumClear += d;
            if (d >= clearProbe * 0.5f) open++;
        }

        output[offset + 0] = leak / probe;
        output[offset + 1] = 1f; // ComputePathDistances doldurur
        output[offset + 2] = equip / probe;
        output[offset + 3] = Mathf.Min(1f, cableCount / CableNormalizer);
        output[offset + 4] = panel / probe;
        output[offset + 5] = minClear / clearProbe;
        output[offset + 6] = sumClear / Dirs8.Length / clearProbe;
        output[offset + 7] = open / (float)Dirs8.Length;
    }

    /// <summary>Çok kaynaklı Dijkstra: her kaynağın en yakın düğümünden graf kenarları boyunca uzaklık.</summary>
    private void ComputePathDistances()
    {
        int n = Graph.NodeCount;
        if (n == 0) return;

        float probe = settings.hazardProbeDistance;
        var dist = new float[n];
        for (int i = 0; i < n; i++) dist[i] = float.MaxValue;

        var queue = new SortedSet<(float d, int node)>();
        foreach (Vector3 s in leakSources)
        {
            int start = NearestNode(s);
            if (start < 0) continue;
            float d0 = Vector3.Distance(s, Graph.Positions[start] + Vector3.up * WaistHeight);
            if (d0 < dist[start]) { queue.Remove((dist[start], start)); dist[start] = d0; queue.Add((d0, start)); }
        }

        while (queue.Count > 0)
        {
            var (d, i) = queue.Min;
            queue.Remove(queue.Min);
            if (d > probe) break;
            for (int e = Graph.NeighborOffsets[i]; e < Graph.NeighborOffsets[i + 1]; e++)
            {
                int j = Graph.NeighborIndices[e];
                float nd = d + Vector3.Distance(Graph.Positions[i], Graph.Positions[j]);
                if (nd >= dist[j]) continue;
                if (dist[j] < float.MaxValue) queue.Remove((dist[j], j));
                dist[j] = nd;
                queue.Add((nd, j));
            }
        }

        int f = FeatureCount;
        for (int i = 0; i < n; i++)
            Graph.Features[i * f + 1] = Mathf.Min(dist[i], probe) / probe;
    }

    private int NearestNode(Vector3 p)
    {
        int best = -1;
        float bestDist = float.MaxValue;
        for (int i = 0; i < Graph.NodeCount; i++)
        {
            float d = (Graph.Positions[i] - p).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    private bool IsConnected(Vector3 a, Vector3 b)
    {
        if (Mathf.Abs(a.y - b.y) > settings.maxStep) return false;
        Vector3 from = a + Vector3.up * KneeHeight;
        Vector3 delta = b - a;
        return !NearestHit(from, delta.normalized, delta.magnitude, out _);
    }

    /// <summary>
    /// Referansın altındaki zemin: referanstan en az 0.5 m aşağıdaki en üst yürünebilir yüzey
    /// (oyuncu kapsülü zeminden ~1 m yukarıda olduğu için masa üstleri seçilmez).
    /// </summary>
    private float FindGroundHeight(Vector3 reference)
    {
        Vector3 origin = reference + Vector3.up * 1f;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, hitBuffer, 20f, settings.layerMask, QueryTriggerInteraction.Ignore);
        float best = float.MinValue, fallback = float.MinValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (ignored.Contains(h.collider) || h.normal.y < minWalkableNormalY) continue;
            fallback = Mathf.Max(fallback, h.point.y);
            if (h.point.y <= reference.y - 0.5f) best = Mathf.Max(best, h.point.y);
        }
        if (best > float.MinValue) return best;
        return fallback > float.MinValue ? fallback : reference.y;
    }

    private bool NearestHit(Vector3 origin, Vector3 dir, float length, out RaycastHit best)
    {
        int count = Physics.RaycastNonAlloc(origin, dir, hitBuffer, length, settings.layerMask, QueryTriggerInteraction.Ignore);
        best = default;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = hitBuffer[i];
            if (ignored.Contains(h.collider)) continue;
            if (!found || h.distance < best.distance) { best = h; found = true; }
        }
        return found;
    }

    private int OverlapCapsule(Vector3 a, Vector3 b, float radius)
    {
        int count = Physics.OverlapCapsuleNonAlloc(a, b, radius, colliderBuffer, settings.layerMask, QueryTriggerInteraction.Ignore);
        int kept = 0;
        for (int i = 0; i < count; i++)
        {
            if (ignored.Contains(colliderBuffer[i])) continue;
            colliderBuffer[kept++] = colliderBuffer[i];
        }
        return kept;
    }

    private void CollectEquipment(List<Transform> ignoreRoots)
    {
        Bounds searchArea = area;
        searchArea.Expand(new Vector3(2f, 0f, 2f) * settings.cableRadius);

        foreach (Renderer r in Object.FindObjectsOfType<Renderer>())
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer || r is LineRenderer)) continue;
            if (!r.enabled || !searchArea.Intersects(r.bounds) || IsUnder(r.transform, ignoreRoots)) continue;
            if (MatchesAny(r, settings.cableKeywords)) cables.Add(r.bounds);
            if (MatchesAny(r, settings.equipmentKeywords)) equipment.Add(r.bounds);
        }
    }

    /// <summary>Renderer'ın kendi adı, üst objelerinin adı veya materyali anahtar kelimeyi içeriyor mu.</summary>
    private static bool MatchesAny(Renderer r, string[] keywords)
    {
        for (Transform t = r.transform; t != null; t = t.parent)
        {
            if (ContainsKeyword(t.name, keywords)) return true;
        }
        foreach (Material m in r.sharedMaterials)
        {
            if (m != null && ContainsKeyword(m.name, keywords)) return true;
        }
        return false;
    }

    private static bool ContainsKeyword(string text, string[] keywords)
    {
        string lower = text.ToLowerInvariant();
        foreach (string k in keywords)
        {
            if (!string.IsNullOrEmpty(k) && lower.Contains(k.ToLowerInvariant())) return true;
        }
        return false;
    }

    private static bool IsUnder(Transform t, List<Transform> roots)
    {
        foreach (Transform root in roots)
        {
            if (t.IsChildOf(root)) return true;
        }
        return false;
    }

    /// <summary>Sahnedeki (kapalı olanlar dahil) FalseBehaviourController trigger kutuları.</summary>
    public static List<GnnHazardZone> FindHazardZones()
    {
        var zones = new List<GnnHazardZone>();
        foreach (FalseBehaviourController f in Object.FindObjectsOfType<FalseBehaviourController>(true))
        {
            foreach (BoxCollider box in f.GetComponents<BoxCollider>())
            {
                if (box.isTrigger) zones.Add(new GnnHazardZone { Box = box, Type = GnnHazardType.Electric });
            }
        }
        return zones;
    }

    /// <summary>
    /// Eğitim etiketleri: enerjili kutuya (yatayda) dangerDistance'tan yakın Danger, cautionDistance'tan
    /// yakın Caution, kalanı Safe. Alçak gerilimde sınırlı yaklaşma mesafesi ~1 m (NFPA 70E: 1.07 m).
    /// </summary>
    public static int[] ComputeLabels(GnnGraph graph, List<GnnHazardZone> zones, float dangerDistance, float cautionDistance)
    {
        var labels = new int[graph.NodeCount];
        for (int i = 0; i < graph.NodeCount; i++)
        {
            Vector3 p = graph.Positions[i];
            int label = (int)GnnRiskClass.Safe;
            foreach (GnnHazardZone z in zones)
            {
                float d = z.ContainsHorizontally(p) ? 0f : z.HorizontalDistance(p);
                if (d <= dangerDistance) { label = (int)GnnRiskClass.Danger; break; }
                if (d <= cautionDistance) label = (int)GnnRiskClass.Caution;
            }
            labels[i] = label;
        }
        return labels;
    }
}
