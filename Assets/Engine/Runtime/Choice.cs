using System.Collections.Generic;

namespace Crossroads.Engine
{
    // בחירה בודדת על קלף (ספק 14.1). side מרחבי-פיזי (Q2): Left=שמאל-פיזי על המסך.
    public sealed class Choice
    {
        public ChoiceSide Side;
        public string Label;
        public List<ResourceEffect> Effects = new List<ResourceEffect>();
        public Dictionary<string, bool> SetFlags = new Dictionary<string, bool>();
        // null = המקור בוחר את הבא; ערך = מעבר מפורש (סיפור מסועף J7). גובר על בחירת ה-Deck (ספק 12.4).
        public string Next;
    }

    public sealed class ResourceEffect
    {
        public string ResourceId;
        public int Delta;
    }

    // תנאי-הופעה appearWhen (ספק 14.1). null = תמיד תקף.
    public sealed class Condition
    {
        public List<ConditionClause> AllOf = new List<ConditionClause>();
    }

    public enum ConditionKind { Flag, Resource }
    public enum CompareOp { Eq, Neq, Gte, Lte, Gt, Lt }

    public sealed class ConditionClause
    {
        public ConditionKind Kind;
        // Flag
        public string Flag;
        public bool FlagIs;
        // Resource
        public string Resource;
        public CompareOp Op;
        public int Value;
    }
}
