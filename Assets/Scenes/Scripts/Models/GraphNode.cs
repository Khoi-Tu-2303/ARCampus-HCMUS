// Models/GraphNode.cs
using System.Collections.Generic;

[System.Serializable]
public class GraphNode
{
    public string id;
    public double lat;
    public double lng;
    public string name;
    public List<string> edges = new List<string>();
}