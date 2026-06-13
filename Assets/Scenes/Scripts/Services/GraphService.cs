




using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using SimpleJSON;

public class GraphService : MonoBehaviour
{
    public static GraphService Instance;

    public Dictionary<string, GraphNode> Nodes = new Dictionary<string, GraphNode>();
    public List<GraphEdge> Edges = new List<GraphEdge>();
    public bool IsLoaded = false;

    void Awake() => Instance = this;

    void Start() => StartCoroutine(LoadGraph());

    IEnumerator LoadGraph()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "campus_graph_full.geojson");
        UnityWebRequest req = UnityWebRequest.Get(path);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("❌ Không đọc được GeoJSON: " + req.error);
            yield break;
        }
        ParseGeoJSON(req.downloadHandler.text);
    }

    void ParseGeoJSON(string json)
    {
        var root = JSON.Parse(json);
        var features = root["features"];
        Nodes.Clear();
        Edges.Clear();

        
        for (int i = 0; i < features.Count; i++)
        {
            var feature = features[i];
            if (feature["geometry"]["type"].Value != "Point") continue;
            string id = feature["properties"]["node_id"].Value;
            if (string.IsNullOrEmpty(id)) continue;
            var coords = feature["geometry"]["coordinates"];
            Nodes[id] = new GraphNode
            {
                id = id,
                lng = coords[0].AsDouble,
                lat = coords[1].AsDouble,
                name = feature["properties"].HasKey("display_name") ? feature["properties"]["display_name"].Value : id,
                edges = new List<string>()
            };
        }

        
        for (int i = 0; i < features.Count; i++)
        {
            var feature = features[i];
            if (feature["geometry"]["type"].Value != "LineString") continue;
            var props = feature["properties"];
            string source = props["source"].Value;
            string target = props["target"].Value;

            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) continue;
            if (!Nodes.ContainsKey(source) || !Nodes.ContainsKey(target)) continue;

            
            float dist = GeoMath.Haversine(Nodes[source].lat, Nodes[source].lng,
                                           Nodes[target].lat, Nodes[target].lng);

            Edges.Add(new GraphEdge { from = source, to = target, distance_m = dist, direction_deg = 0f });

            if (!Nodes[source].edges.Contains(target)) Nodes[source].edges.Add(target);
            if (!Nodes[target].edges.Contains(source)) Nodes[target].edges.Add(source);
        }

        IsLoaded = true;
        Debug.Log($"✅ Graph loaded: {Nodes.Count} nodes, {Edges.Count} edges");
    }
}
