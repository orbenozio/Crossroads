using System.Collections.Generic;

namespace Crossroads.Engine
{
    public enum IssueSeverity { Error, Warning }

    public readonly struct ValidationIssue
    {
        public readonly IssueSeverity Severity;
        public readonly string NodeId;
        public readonly string Message;
        public ValidationIssue(IssueSeverity severity, string nodeId, string message)
        {
            Severity = severity; NodeId = nodeId; Message = message;
        }
        public override string ToString() => $"[{Severity}] {NodeId}: {Message}";
    }

    // ולידציה של דאטה (ספק 14.1 / 15.2, J9/M9). מורצת בזמן-עריכה (CI) ובזמן-טעינה (חובה).
    // אין כשל-שקט - כל בעיה מדווחת עם node.id והשדה הבעייתי.
    public static class StoryValidator
    {
        public static List<ValidationIssue> Validate(StoryData story, ResourceSet resources)
        {
            var issues = new List<ValidationIssue>();
            if (story == null) { issues.Add(new ValidationIssue(IssueSeverity.Error, "<story>", "story is null")); return issues; }

            var nodeIds = new HashSet<string>();
            var writtenFlags = new HashSet<string>();
            var readFlags = new List<(string flag, string nodeId)>();

            foreach (var n in story.Nodes)
            {
                if (string.IsNullOrEmpty(n.Id))
                    issues.Add(new ValidationIssue(IssueSeverity.Error, "<node>", "node with empty id"));
                else if (!nodeIds.Add(n.Id))
                    issues.Add(new ValidationIssue(IssueSeverity.Error, n.Id, "duplicate node id"));

                // בדיוק שתי בחירות עם side שונה (ספק 14.1 - "אין כיוון מת").
                if (n.Choices.Count != 2)
                    issues.Add(new ValidationIssue(IssueSeverity.Error, n.Id, $"node must have exactly 2 choices, found {n.Choices.Count}"));
                else if (n.Choices[0].Side == n.Choices[1].Side)
                    issues.Add(new ValidationIssue(IssueSeverity.Error, n.Id, "both choices share the same side"));

                foreach (var ch in n.Choices)
                {
                    foreach (var eff in ch.Effects)
                    {
                        if (resources != null && resources.Find(eff.ResourceId) == null)
                            issues.Add(new ValidationIssue(IssueSeverity.Error, n.Id, $"effect references undefined resource '{eff.ResourceId}'"));
                    }
                    foreach (var kv in ch.SetFlags) writtenFlags.Add(kv.Key);
                }

                CollectConditionRefs(n.AppearWhen, n.Id, resources, readFlags, issues);
            }

            // startNodeId ו-next מפורש חייבים להצביע ל-id קיים.
            if (string.IsNullOrEmpty(story.StartNodeId) || !nodeIds.Contains(story.StartNodeId))
                issues.Add(new ValidationIssue(IssueSeverity.Error, "<story>", $"startNodeId '{story.StartNodeId}' does not exist"));

            foreach (var n in story.Nodes)
                foreach (var ch in n.Choices)
                    if (!string.IsNullOrEmpty(ch.Next) && !nodeIds.Contains(ch.Next))
                        issues.Add(new ValidationIssue(IssueSeverity.Error, n.Id, $"choice.next '{ch.Next}' does not exist"));

            // Flag endings (when:{flag}) read a flag too - count them as reads so the "orphan" warning stays accurate.
            foreach (var e in story.Endings)
                if (!string.IsNullOrEmpty(e.Flag)) readFlags.Add((e.Flag, "<ending>"));

            // flag נקרא שלא נכתב בשום מקום - אזהרה (flags יתומים), לא שגיאה.
            foreach (var (flag, nodeId) in readFlags)
                if (!writtenFlags.Contains(flag))
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, nodeId, $"flag '{flag}' is read but never set"));

            // אין ending fallback - אזהרה: שבירת-משאב ללא ending תואם תיפול ל-default אגנוסטי גנרי.
            bool hasFallbackEnding = false;
            foreach (var e in story.Endings) if (e.Fallback) { hasFallbackEnding = true; break; }
            if (!hasFallbackEnding)
                issues.Add(new ValidationIssue(IssueSeverity.Warning, "<story>", "no fallback ending defined; an unmatched resource break will show a generic message"));

            return issues;
        }

        private static void CollectConditionRefs(Condition cond, string nodeId, ResourceSet resources,
            List<(string, string)> readFlags, List<ValidationIssue> issues)
        {
            if (cond == null) return;
            foreach (var c in cond.AllOf)
            {
                if (c.Kind == ConditionKind.Flag)
                {
                    readFlags.Add((c.Flag, nodeId));
                }
                else if (resources != null && resources.Find(c.Resource) == null)
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Error, nodeId, $"appearWhen references undefined resource '{c.Resource}'"));
                }
            }
        }
    }
}
