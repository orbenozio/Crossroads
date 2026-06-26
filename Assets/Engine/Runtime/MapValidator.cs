using System.Collections.Generic;

namespace Crossroads.Engine
{
    // ולידציה של map.json מול story.json (ספק 14.3/15.2, M9 לפורמט המסע). מורצת בזמן-טעינה:
    // overlay-מפה שמפנה ל-node שלא קיים, או יעד שאי-אפשר להגיע אליו, = משחק שבור בשקט - כאן זה נתפס.
    public static class MapValidator
    {
        public static List<ValidationIssue> Validate(StoryData story, MapData map)
        {
            var issues = new List<ValidationIssue>();
            if (story == null) { issues.Add(Err("<map>", "story is null")); return issues; }
            if (map == null) { issues.Add(Err("<map>", "map is null")); return issues; }

            var nodeIds = new HashSet<string>();
            foreach (var n in story.Nodes) if (!string.IsNullOrEmpty(n.Id)) nodeIds.Add(n.Id);

            bool startOk = !string.IsNullOrEmpty(map.StartNodeId) && nodeIds.Contains(map.StartNodeId);
            bool goalOk = !string.IsNullOrEmpty(map.GoalNodeId) && nodeIds.Contains(map.GoalNodeId);
            if (!startOk) issues.Add(Err("<map>", $"startNodeId '{map.StartNodeId}' is not a story node"));
            if (!goalOk) issues.Add(Err("<map>", $"goalNodeId '{map.GoalNodeId}' is not a story node"));

            foreach (var kv in map.Edges)
            {
                if (!nodeIds.Contains(kv.Key))
                    issues.Add(Err(kv.Key, "edge source is not a story node"));
                foreach (var target in kv.Value)
                    if (!nodeIds.Contains(target))
                        issues.Add(Err(kv.Key, $"edge target '{target}' is not a story node"));
            }

            // היעד חייב להיות בר-הגעה מצומת-ההתחלה (אחרת המסע בלתי-ניתן-לניצחון).
            if (startOk && goalOk && !Reachable(map, map.StartNodeId, map.GoalNodeId))
                issues.Add(Err("<map>", $"goal '{map.GoalNodeId}' is not reachable from start '{map.StartNodeId}'"));

            // ה-Engine נכנס ל-story.StartNodeId; אם המפה מתחילה במקום אחר - אזהרה.
            if (startOk && map.StartNodeId != story.StartNodeId)
                issues.Add(Warn("<map>", $"map startNodeId '{map.StartNodeId}' differs from story startNodeId '{story.StartNodeId}'; the engine enters the story start"));

            bool hasGoalEnding = false;
            foreach (var e in story.Endings) if (e.ReachedGoal) { hasGoalEnding = true; break; }
            if (!hasGoalEnding)
                issues.Add(Warn("<map>", "no reachedGoal ending defined; the victory will show a generic message"));

            return issues;
        }

        private static bool Reachable(MapData map, string from, string to)
        {
            if (from == to) return true;
            var seen = new HashSet<string> { from };
            var q = new Queue<string>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                foreach (var n in map.Neighbors(q.Dequeue()))
                {
                    if (n == to) return true;
                    if (seen.Add(n)) q.Enqueue(n);
                }
            }
            return false;
        }

        private static ValidationIssue Err(string node, string msg) => new ValidationIssue(IssueSeverity.Error, node, msg);
        private static ValidationIssue Warn(string node, string msg) => new ValidationIssue(IssueSeverity.Warning, node, msg);
    }
}
