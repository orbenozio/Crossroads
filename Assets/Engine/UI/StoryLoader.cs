using System;
using Crossroads.Engine;
using Newtonsoft.Json.Linq;

namespace Crossroads.UI
{
    // פרסור story.json ל-StoryData (ספק 12.3/13 - הפרסור מחוץ ל-Engine). תשתית משותפת (לא פר-משחק):
    // ה-Game bootstrap רק מספק את המחרוזת; כך שכפול אינו מעתיק קוד-פרסור (משרת M7).
    // הערה ל-spec: §12.3 מציב את הפרסור ב-"Game bootstrap"; כאן הוא מרוכז ב-Crossroads.UI כתשתית
    // משותפת שאינה Engine ואינה פר-משחק - עדיף למודל-השכפול (אפס קוד בשכפול).
    public static class StoryLoader
    {
        public static StoryData Parse(string json)
        {
            var root = JObject.Parse(json);
            var story = new StoryData
            {
                SchemaVersion = (int?)root["schemaVersion"] ?? 1,
                StartNodeId = (string)root["startNodeId"],
                MaxTurns = (int?)root["maxTurns"] ?? 0   // optional run length (spec 7.5); 0 = unbounded
            };

            foreach (var nodeTok in root["nodes"] ?? new JArray())
                story.Nodes.Add(ParseNode((JObject)nodeTok));

            foreach (var endTok in root["endings"] ?? new JArray())
                story.Endings.Add(ParseEnding((JObject)endTok));

            return story;
        }

        private static EventNode ParseNode(JObject o)
        {
            var node = new EventNode
            {
                Id = (string)o["id"],
                Speaker = (string)o["speaker"],
                Body = (string)o["body"],
                Weight = (int?)o["weight"] ?? 1,
                AppearWhen = ParseCondition(o["appearWhen"] as JObject)
            };
            foreach (var chTok in o["choices"] ?? new JArray())
                node.Choices.Add(ParseChoice((JObject)chTok));
            return node;
        }

        private static Choice ParseChoice(JObject o)
        {
            var choice = new Choice
            {
                Side = ParseSide((string)o["side"]),
                Label = (string)o["label"],
                Next = (string)o["next"]
            };
            foreach (var effTok in o["effects"] ?? new JArray())
                choice.Effects.Add(new ResourceEffect
                {
                    ResourceId = (string)effTok["resource"],
                    Delta = (int)effTok["delta"]
                });
            if (o["setFlags"] is JObject flags)
                foreach (var f in flags.Properties())
                    choice.SetFlags[f.Name] = (bool)f.Value;
            return choice;
        }

        private static Condition ParseCondition(JObject o)
        {
            if (o == null) return null;
            var cond = new Condition();
            foreach (var clauseTok in o["allOf"] ?? new JArray())
            {
                var c = (JObject)clauseTok;
                if (c["flag"] != null)
                    cond.AllOf.Add(new ConditionClause
                    {
                        Kind = ConditionKind.Flag,
                        Flag = (string)c["flag"],
                        FlagIs = (bool?)c["is"] ?? true
                    });
                else
                    cond.AllOf.Add(new ConditionClause
                    {
                        Kind = ConditionKind.Resource,
                        Resource = (string)c["resource"],
                        Op = ParseOp((string)c["op"]),
                        Value = (int?)c["value"] ?? 0
                    });
            }
            return cond;
        }

        private static Ending ParseEnding(JObject o)
        {
            if ((bool?)o["fallback"] == true)
                return new Ending { Fallback = true, Text = (string)o["text"] };
            var when = (JObject)o["when"];
            // ניצחון-מסע (R6): when:{reachedGoal:true} - overlay שאינו משנה את סכמת Reigns.
            if ((bool?)when?["reachedGoal"] == true)
                return new Ending { ReachedGoal = true, Text = (string)o["text"] };
            // Branching survival ending: when:{flag:"x", is:true} - chosen at MaxTurns by flag state.
            if (when?["flag"] != null)
                return new Ending { Flag = (string)when["flag"], FlagIs = (bool?)when["is"] ?? true, Text = (string)o["text"] };
            return new Ending
            {
                Fallback = false,
                ResourceId = (string)when?["resource"],
                Edge = ParseEdge((string)when?["edge"]),
                Text = (string)o["text"]
            };
        }

        private static ChoiceSide ParseSide(string s) =>
            string.Equals(s, "right", StringComparison.OrdinalIgnoreCase) ? ChoiceSide.Right : ChoiceSide.Left;

        private static ResourceEdge ParseEdge(string s) =>
            string.Equals(s, "max", StringComparison.OrdinalIgnoreCase) ? ResourceEdge.Max : ResourceEdge.Min;

        private static CompareOp ParseOp(string s)
        {
            switch (s)
            {
                case ">=": return CompareOp.Gte;
                case "<=": return CompareOp.Lte;
                case ">":  return CompareOp.Gt;
                case "<":  return CompareOp.Lt;
                case "!=": return CompareOp.Neq;
                default:   return CompareOp.Eq;
            }
        }
    }
}
