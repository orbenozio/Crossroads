using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Crossroads.Engine
{
    // overlay של מפת-המסע (ספק 14.3): מי שכן של מי + צומת-יעד. מפנה ל-node.id קיימים ב-story.json
    // בלי לשנות אותו. נצרך ע"י MapGraph; נטען מ-map.json דרך MapLoader (שכבת UI, מחוץ ל-Engine).
    public sealed class MapData
    {
        public string StartNodeId;
        public string GoalNodeId;
        public Dictionary<string, List<string>> Edges = new Dictionary<string, List<string>>();

        private static readonly List<string> None = new List<string>();
        public IReadOnlyList<string> Neighbors(string nodeId) =>
            Edges.TryGetValue(nodeId, out var n) ? n : None;
    }

    // מקור-האירועים של המסע (ספק 12.4/14.3). אותו EventEngine, מקור אחר: הניווט מפורש (EnterNode)
    // לפי שכני-המפה, ויש יעד. מצב-המיקום נשמר ב-bag האטום GameState.MapState (ספק 12.3/14.5),
    // כך ש-save/resume של מסע עובד דרך אותו SaveSystem בלי ידע-מפה ב-Engine. מוכיח G2.
    public sealed class MapGraph : IEventSource, IGoalAwareSource
    {
        private readonly StoryData _story;
        private readonly MapData _map;

        public MapGraph(StoryData story, MapData map)
        {
            _story = story;
            _map = map;
        }

        // מיקום נוכחי מתוך ה-bag (ברירת-מחדל: צומת-ההתחלה אם עוד לא נכתב).
        private string PosOf(GameState state)
        {
            if (string.IsNullOrEmpty(state.MapState)) return _map.StartNodeId;
            var o = JObject.Parse(state.MapState);
            return (string)o["pos"] ?? _map.StartNodeId;
        }

        private void SetPos(GameState state, string nodeId)
        {
            state.MapState = new JObject { ["pos"] = nodeId }.ToString(Formatting.None);
        }

        // ניווט: כניסה מפורשת לשכן שנבחר במפה (ספק 7.5). מעדכן את ה-bag ומחזיר את הצומת.
        public EventNode EnterNode(GameState state, string nodeId)
        {
            var node = _story.FindNode(nodeId);
            if (node == null) return null;
            SetPos(state, nodeId);
            return node;
        }

        // המסע מנווט מפורשות; NextEvent מחזיר את צומת-המיקום הנוכחי (אינו "מתקדם" לבדו).
        public EventNode NextEvent(GameState state) => _story.FindNode(PosOf(state));

        // לא מבוי-סתום אם יש שכנים, או שאנחנו ביעד (ואז מסתיים בניצחון - לא תקיעה).
        public bool HasNext(GameState state)
        {
            string pos = PosOf(state);
            return pos == _map.GoalNodeId || _map.Neighbors(pos).Count > 0;
        }

        public bool IsAtGoal(GameState state) => PosOf(state) == _map.GoalNodeId;

        // השכנים שאליהם אפשר לנוע מהמיקום הנוכחי (ל-UI/בוחר-המפה).
        public IReadOnlyList<string> NeighborsOf(GameState state) => _map.Neighbors(PosOf(state));
    }
}
