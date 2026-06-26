using System.Collections.Generic;
using System.Text;
using Crossroads.Engine;

namespace Crossroads.UI
{
    // ממיר טיפוסי-Engine ל-view DTOs (ספק 12.3). מרכז את הכרעת displayName (ספק 14.2)
    // ואת חישוב מצב-הסכנה (דרך ResourceRules המשותף) - כך ה-views כבר-מוכרעים ל-UI.
    public static class ViewMapper
    {
        public static EventNodeView BuildNodeView(EventNode node)
        {
            if (node == null) return new EventNodeView(string.Empty, string.Empty, string.Empty, string.Empty);
            var left = node.GetChoice(ChoiceSide.Left);
            var right = node.GetChoice(ChoiceSide.Right);
            return new EventNodeView(
                node.Body,
                node.Speaker,
                left != null ? left.Label : string.Empty,
                right != null ? right.Label : string.Empty);
        }

        public static IReadOnlyList<ResourceView> BuildResourceViews(GameState state, ResourceSet resources, Theme theme)
        {
            var views = new List<ResourceView>();
            if (resources == null) return views;

            // הסדר במערך הוא סדר-המדים הקבוע (ספק 8.2).
            foreach (var def in resources.resources)
            {
                int value = state != null ? state.GetResource(def.id) : def.start;
                string label = theme != null ? theme.GetResourceLabelOverride(def.id) : null;
                if (string.IsNullOrEmpty(label)) label = def.displayName;
                var danger = ResourceRules.DangerFor(def, value);
                views.Add(new ResourceView(def.id, label, value, def.min, def.max, danger));
            }
            return views;
        }

        // מחרוזת-תקציר של delta-ים לתצוגה על הקלף (משוב swipe, §10.3). שורה למשאב מושפע:
        // "<תווית> <סימן><ערך>" עם קדימות displayName של ה-Theme (J8). delta אפס מושמט.
        public static string FormatDeltas(IReadOnlyList<ResourceDelta> deltas, ResourceSet resources, Theme theme)
        {
            if (deltas == null) return string.Empty;
            var sb = new StringBuilder();
            for (int i = 0; i < deltas.Count; i++)
            {
                if (deltas[i].Delta == 0) continue;
                string label = theme != null ? theme.GetResourceLabelOverride(deltas[i].ResourceId) : null;
                if (string.IsNullOrEmpty(label))
                {
                    var def = resources != null ? resources.Find(deltas[i].ResourceId) : null;
                    label = def != null ? def.displayName : deltas[i].ResourceId;
                }
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(label).Append(' ').Append(deltas[i].Delta > 0 ? "+" : "").Append(deltas[i].Delta);
            }
            return sb.ToString();
        }
    }
}
