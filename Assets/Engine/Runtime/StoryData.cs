using System.Collections.Generic;

namespace Crossroads.Engine
{
    // ה-DTO המפורס של story.json (ספק 14.1). ה-Game bootstrap מפרס JSON לתוך הטיפוס הזה
    // ומזריק ל-EventEngine. ה-Engine עצמו לא קורא קבצים (ספק 12.3).
    public sealed class StoryData
    {
        public int SchemaVersion = 1;
        public string StartNodeId;

        // Optional run length (spec 7.5): >0 = the run ends in victory after this many decisions
        // (e.g. "the heir comes of age"). 0 = unbounded (classic Reigns / journey - ends on a
        // resource break or reaching the goal).
        public int MaxTurns = 0;

        public List<EventNode> Nodes = new List<EventNode>();
        public List<Ending> Endings = new List<Ending>();

        public EventNode FindNode(string id)
        {
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i].Id == id) return Nodes[i];
            }
            return null;
        }
    }

    public enum ResourceEdge { Min, Max }

    // An ending (spec 14.1). Resource break (a loss), generic fallback, ReachedGoal (journey win,
    // R6 §14.1), or Flag - a branching survival ending chosen at MaxTurns based on a state flag
    // (when:{flag:"usurper"}).
    public sealed class Ending
    {
        public bool Fallback;
        public string ResourceId;     // used when Fallback=false and no Flag (a resource-break ending)
        public ResourceEdge Edge;
        public bool ReachedGoal;      // when:{reachedGoal:true} - the journey's win text (spec 14.1, R6)
        public string Flag;           // when:{flag:"x"} - branching survival ending, chosen at MaxTurns by flag state
        public bool FlagIs = true;
        public string Text;
    }
}
