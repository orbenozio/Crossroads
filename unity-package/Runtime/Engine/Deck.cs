using System.Collections.Generic;

namespace Crossroads.Engine
{
    // מקור-האירועים של Reigns (ספק 12.4). מסנן קלפים לפי appearWhen/flags ובוחר את הבא.
    // ב-NOW הבחירה לפי סדר (weight שמור ל-LATER). מממש IEventSource כך שה-EventEngine אגנוסטי למקור.
    public sealed class Deck : IEventSource
    {
        private readonly StoryData _story;
        private DeterministicRng _rng;

        public Deck(StoryData story)
        {
            _story = story;
        }

        public EventNode NextEvent(GameState state)
        {
            // בחירה משוקללת (ספק 14.1) מבין הקלפים התקפים שאינם הנוכחי (מונע חזרה מיידית).
            // אם רק הנוכחי תקף - מותר לחזור עליו. הבחירה דטרמיניסטית מ-state.Rng (M5).
            var valid = new List<EventNode>();
            EventNode firstValidInclCurrent = null;
            for (int i = 0; i < _story.Nodes.Count; i++)
            {
                EventNode n = _story.Nodes[i];
                if (!Conditions.IsMet(n.AppearWhen, state)) continue;
                if (firstValidInclCurrent == null) firstValidInclCurrent = n;
                if (n.Id != state.CurrentNodeId) valid.Add(n);
            }
            if (valid.Count == 0) return firstValidInclCurrent;
            return WeightedPick(valid, state);
        }

        // בחירה לפי weight (ברירת-מחדל 1 = אחיד). מקדם את ה-RNG ושומר את drawCount בחזרה ל-state (M5).
        private EventNode WeightedPick(List<EventNode> valid, GameState state)
        {
            if (_rng == null) _rng = new DeterministicRng(state.Rng.Seed, state.Rng.DrawCount);

            int total = 0;
            for (int i = 0; i < valid.Count; i++) total += System.Math.Max(1, valid[i].Weight);

            double r = _rng.NextDouble() * total;
            EventNode chosen = valid[valid.Count - 1];
            int acc = 0;
            for (int i = 0; i < valid.Count; i++)
            {
                acc += System.Math.Max(1, valid[i].Weight);
                if (r < acc) { chosen = valid[i]; break; }
            }

            state.Rng = new RngState(state.Rng.Seed, _rng.DrawCount);  // המיקום החדש נשמר (fast-forward בטעינה)
            return chosen;
        }

        public EventNode EnterNode(GameState state, string nodeId)
        {
            // כניסה מפורשת - מתעלמת מ-appearWhen (ניתוב next מפורש / re-entry של שמירה, ספק 12.4).
            return _story.FindNode(nodeId);
        }

        public bool HasNext(GameState state)
        {
            for (int i = 0; i < _story.Nodes.Count; i++)
            {
                if (Conditions.IsMet(_story.Nodes[i].AppearWhen, state)) return true;
            }
            return false;
        }
    }

    // הערכת תנאי-הופעה (appearWhen). null = תמיד תקף.
    internal static class Conditions
    {
        public static bool IsMet(Condition cond, GameState state)
        {
            if (cond == null) return true;
            for (int i = 0; i < cond.AllOf.Count; i++)
            {
                if (!ClauseMet(cond.AllOf[i], state)) return false;
            }
            return true;
        }

        private static bool ClauseMet(ConditionClause c, GameState state)
        {
            if (c.Kind == ConditionKind.Flag)
            {
                return state.GetFlag(c.Flag) == c.FlagIs;
            }

            int v = state.GetResource(c.Resource);
            switch (c.Op)
            {
                case CompareOp.Eq:  return v == c.Value;
                case CompareOp.Neq: return v != c.Value;
                case CompareOp.Gte: return v >= c.Value;
                case CompareOp.Lte: return v <= c.Value;
                case CompareOp.Gt:  return v > c.Value;
                case CompareOp.Lt:  return v < c.Value;
                default: return false;
            }
        }
    }
}
