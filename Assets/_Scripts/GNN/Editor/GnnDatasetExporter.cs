using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Açık sahneden GNN verisini üretir:
///   - GNN/data/&lt;sahne&gt;_graph_{0..3}.json : etiketli eğitim grafları (grid yarım hücre kaydırılarak 4 kopya)
///   - Assets/Resources/GNN/gnn_graph_&lt;sahne&gt;.json : oyunda kullanılan graf (runtime sahne taramaz)
/// Etiketler FalseBehaviourController (enerjili fiş / 3'lü priz) trigger kutularından gelir.
/// </summary>
public static class GnnDatasetExporter
{
    private const string ExamScenePath = "Assets/Scenes/VR-ASYS-ELEKTIRIK-SINAV.unity";

    private static readonly Vector2[] GridOffsets =
    {
        new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f)
    };

    public static string DataDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "GNN", "data");
    private static string ResourcesDirectory => Path.Combine(Application.dataPath, "Resources", "GNN");

    [MenuItem("Tools/GNN/Eğitim Verisini ve Grafı Dışa Aktar (Açık Sahne)")]
    public static void ExportOpenScene()
    {
        string summary = Export();
        EditorUtility.DisplayDialog("GNN Dışa Aktarım", summary, "Tamam");
    }

    /// <summary>
    /// Komut satırı: Unity -batchmode -quit -projectPath . -executeMethod GnnDatasetExporter.ExportFromCommandLine
    /// </summary>
    public static void ExportFromCommandLine()
    {
        EditorSceneManager.OpenScene(ExamScenePath, OpenSceneMode.Single);
        Debug.Log(Export());
    }

    private static string Export()
    {
        GnnRiskPredictor predictor = Object.FindObjectOfType<GnnRiskPredictor>();
        GameObject temp = null;
        if (predictor == null)
        {
            temp = new GameObject("GNN Exporter (temp)") { hideFlags = HideFlags.HideAndDontSave };
            predictor = temp.AddComponent<GnnRiskPredictor>();
        }

        try
        {
            List<GnnHazardZone> zones = GnnGraphBuilder.FindHazardZones();
            if (zones.Count == 0)
                return "Enerjili ekipman kutusu bulunamadı (FalseBehaviourController bileşenli trigger BoxCollider).";

            string sceneName = EditorSceneManager.GetActiveScene().name;
            string fileStem = GnnRiskPredictor.GraphResourceName(sceneName).Substring(GnnRiskPredictor.GraphResourcePrefix.Length);
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(ResourcesDirectory);
            foreach (string old in Directory.GetFiles(DataDirectory, fileStem + "_graph_*.json")) File.Delete(old);

            var log = new System.Text.StringBuilder();
            log.AppendLine($"{zones.Count} enerjili ekipman kutusu bulundu.");

            for (int k = 0; k < GridOffsets.Length; k++)
            {
                GnnGraphSettings settings = predictor.GraphSettings.Clone();
                settings.gridOffset = GridOffsets[k];

                GnnGraphBuilder builder = predictor.CreateBuilder(settings, zones);
                if (!builder.HasArea) return "Saha alanı hesaplanamadı.";
                if (k == 0)
                {
                    log.AppendLine($"Zemin yüksekliği: {builder.GroundY:F2} m | kaçak noktası: {builder.LeakSourceCount}, pano: {builder.PanelCount}, " +
                                   $"ekipman (isimden): {builder.EquipmentCount}, kablo: {builder.CableCount}");
                    if (builder.LeakSourceCount == 0)
                        return "Kaçak noktası bulunamadı. ElectricLeak > Kivircim Efekti atanmış olmalı ya da GnnRiskPredictor > Leak Sources doldurulmalı.";
                }

                GnnGraph graph = builder.BuildImmediate();
                graph.Labels = GnnGraphBuilder.ComputeLabels(graph, zones, settings.dangerDistance, settings.cautionDistance);
                GnnGraphExport export = graph.ToExport(sceneName, GnnGraphBuilder.FeatureNames, GnnGraphBuilder.ClassNames);
                string json = JsonUtility.ToJson(export);

                File.WriteAllText(Path.Combine(DataDirectory, $"{fileStem}_graph_{k}.json"), json);
                if (k == 0) File.WriteAllText(Path.Combine(ResourcesDirectory, $"gnn_graph_{fileStem}.json"), json);

                int[] counts = new int[3];
                foreach (int l in graph.Labels) counts[l]++;
                log.AppendLine($"graph_{k}: {graph.NodeCount} düğüm, {graph.EdgeCount / 2} kenar | Güvenli {counts[0]}, Dikkat {counts[1]}, Tehlikeli {counts[2]}");
            }

            AssetDatabase.Refresh();
            log.AppendLine("Eğitim verisi: " + DataDirectory);
            log.AppendLine($"Oyun grafı: Assets/Resources/GNN/gnn_graph_{fileStem}.json");
            return log.ToString();
        }
        finally
        {
            if (temp != null) Object.DestroyImmediate(temp);
        }
    }
}
