using UnityEngine;

namespace Crossroads.Engine.Tests
{
    // עזרי-בנייה לבדיקות (קלפים/משאבים מינימליים בלי קבצים).
    internal static class TestData
    {
        // קלף עם שתי בחירות; אותו אפקט בשני הצדדים (effects מועברים כ-(resourceId, delta)).
        public static EventNode Node(string id, params (string id, int delta)[] effects)
        {
            var node = new EventNode { Id = id, Body = id };
            node.Choices.Add(MakeChoice(ChoiceSide.Left, effects, null));
            node.Choices.Add(MakeChoice(ChoiceSide.Right, effects, null));
            return node;
        }

        // קלף שהבחירה הימנית בו מגדירה next מפורש.
        public static EventNode NodeWithNext(string id, string next)
        {
            var node = new EventNode { Id = id, Body = id };
            node.Choices.Add(MakeChoice(ChoiceSide.Left, new (string, int)[] { ("e", -1) }, null));
            node.Choices.Add(MakeChoice(ChoiceSide.Right, new (string, int)[] { ("e", -1) }, next));
            return node;
        }

        private static Choice MakeChoice(ChoiceSide side, (string id, int delta)[] effects, string next)
        {
            var c = new Choice { Side = side, Label = side.ToString(), Next = next };
            foreach (var e in effects)
                c.Effects.Add(new ResourceEffect { ResourceId = e.id, Delta = e.delta });
            return c;
        }

        public static StoryData Story(string startId, params EventNode[] nodes)
        {
            var s = new StoryData { StartNodeId = startId };
            s.Nodes.AddRange(nodes);
            s.Endings.Add(new Ending { Fallback = true, Text = "end" });
            return s;
        }

        public static ResourceSet Resources(params (string id, int start)[] defs)
        {
            var set = ScriptableObject.CreateInstance<ResourceSet>();
            set.resources = new ResourceDef[defs.Length];
            for (int i = 0; i < defs.Length; i++)
                set.resources[i] = new ResourceDef
                {
                    id = defs[i].id, displayName = defs[i].id,
                    min = 0, max = 10, start = defs[i].start, breakOn = BreakOn.Both, dangerBand = 1
                };
            return set;
        }
    }
}
