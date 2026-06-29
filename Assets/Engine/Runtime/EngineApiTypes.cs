using System.Collections.Generic;

namespace Crossroads.Engine
{
    // הטיפוסים הציבוריים שה-Engine חושף ל-UI ול-Game (ספק 14.4).
    // אף טיפוס כאן לא מכיר תוכן ספציפי (NewbornKing) - הכל דרך data. תומך M7/M8.

    public enum ChoiceSide { Left, Right }

    public enum GameStatus { Running, GameOver }

    // ResourceBroken/NoMoreEvents = a loss; ReachedGoal/Survived = a win
    // (journey = reached the goal, Reigns = survived to MaxTurns; spec 7.5.6/9.4).
    public enum GameOverReason { ResourceBroken, NoMoreEvents, ReachedGoal, Survived }

    // שלוש דרגות-סכנה מובחנות (ספק 10.4 / 11.1) - לא-תלוי-צבע ב-UI.
    public enum DangerLevel { None, Approaching, WillBreak }

    public readonly struct ResourceDelta
    {
        public readonly string ResourceId;
        public readonly int Delta;
        public ResourceDelta(string resourceId, int delta) { ResourceId = resourceId; Delta = delta; }
    }

    public readonly struct DangerHint
    {
        public readonly string ResourceId;
        public readonly DangerLevel Level;
        public DangerHint(string resourceId, DangerLevel level) { ResourceId = resourceId; Level = level; }
    }

    // חישוב-יבש pure (ספק 14.4): deltas + danger בלבד, בלי setFlags/next. לא תוצאה חלקית של Resolve.
    public readonly struct ChoicePreview
    {
        public readonly IReadOnlyList<ResourceDelta> Deltas;
        public readonly IReadOnlyList<DangerHint> Dangers;
        public ChoicePreview(IReadOnlyList<ResourceDelta> deltas, IReadOnlyList<DangerHint> dangers)
        {
            Deltas = deltas;
            Dangers = dangers;
        }
    }

    // תוצאת Resolve: ה-deltas שהוחלו בפועל (אחרי clamp, לא נומינלי) + הסטטוס הבא.
    // אין כאן NextEvent - הופרד ל-Advance/EnterNode (ספק 12.4).
    public readonly struct ResolveResult
    {
        public readonly IReadOnlyList<ResourceDelta> AppliedDeltas;
        public readonly GameStatus NextStatus;
        public ResolveResult(IReadOnlyList<ResourceDelta> appliedDeltas, GameStatus nextStatus)
        {
            AppliedDeltas = appliedDeltas;
            NextStatus = nextStatus;
        }
    }

    public readonly struct GameOverInfo
    {
        public readonly GameOverReason Reason;
        public readonly string Text;
        public readonly string Image;   // optional per-ending backdrop key (from the matched Ending; null = default key-art)
        public GameOverInfo(GameOverReason reason, string text) : this(reason, text, null) { }
        public GameOverInfo(GameOverReason reason, string text, string image) { Reason = reason; Text = text; Image = image; }
    }
}
