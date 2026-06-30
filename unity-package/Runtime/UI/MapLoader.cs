using System.Collections.Generic;
using Crossroads.Engine;
using Newtonsoft.Json.Linq;

namespace Crossroads.UI
{
    // פרסור map.json ל-MapData (ספק 14.3) - overlay-המסע מעל story.json. תשתית משותפת (מחוץ ל-Engine),
    // כמו StoryLoader. format: { startNodeId, goalNodeId, edges: { nodeId: [neighbor, ...] } }.
    public static class MapLoader
    {
        public static MapData Parse(string json)
        {
            var o = JObject.Parse(json);
            var map = new MapData
            {
                StartNodeId = (string)o["startNodeId"],
                GoalNodeId = (string)o["goalNodeId"]
            };
            if (o["edges"] is JObject edges)
                foreach (var p in edges.Properties())
                {
                    var list = new List<string>();
                    foreach (var t in (JArray)p.Value) list.Add((string)t);
                    map.Edges[p.Name] = list;
                }
            return map;
        }
    }
}
