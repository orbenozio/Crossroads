using System;
using System.Collections.Generic;

namespace Crossroads.Engine
{
    // ליבת המנוע (ספק 14.4). C# טהור, אגנוסטי למשחק ולפורמט. כל ה-state ב-GameState.
    // הפרדה קריטית: Resolve מחיל בחירה בלבד; Advance/EnterNode מבקשים את האירוע הבא (ספק 12.4).
    public sealed class EventEngine
    {
        private readonly StoryData _story;
        private readonly ResourceSet _resources;
        private readonly IEventSource _source;

        private GameState _state;
        private EventNode _current;
        private GameStatus _status;
        private string _pendingNext;   // next מפורש מהבחירה האחרונה שהוחלה; גובר ב-Advance (ספק 12.4)
        private GameOverInfo _lastGameOver;

        // ctor משותף - שמירת התלויות בלבד. ה-state נבנה בנתיב הפומבי (טרי) או ב-Resume (טעון).
        private EventEngine(StoryData story, ResourceSet resources, IEventSource source)
        {
            _story = story ?? throw new ArgumentNullException(nameof(story));
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _status = GameStatus.Running;
        }

        // ריצה טרייה: state חדש מ-start של כל משאב, כניסה ל-StartNodeId.
        public EventEngine(StoryData story, ResourceSet resources, IEventSource source, int seed)
            : this(story, resources, source)
        {
            _state = new GameState { SchemaVersion = 1, Rng = new RngState(seed, 0) };
            foreach (var def in _resources.resources)
                _state.Resources[def.id] = def.start;

            _current = _source.EnterNode(_state, _story.StartNodeId);
            _state.CurrentNodeId = _current != null ? _current.Id : null;
        }

        // המשך ריצה מ-GameState טעון (J4/M5, re-entry של שמירה ספק 12.4).
        // מחזיר null אם state ריק או שהצומת השמור כבר לא קיים בתוכן (תוכן השתנה) -
        // כך שהקורא יכול ליפול חזרה לריצה טרייה במקום להיכנס למצב לא-תקף.
        // נתיב ה-Deck דטרמיניסטי לחלוטין מתוך GameState (אין משיכות RNG כעת), לכן אין צורך
        // ב-fast-forward של ה-RNG בשלב זה (שמור כ-Todo ל-Phase 1 כשתתווסף בחירה אקראית).
        public static EventEngine Resume(StoryData story, ResourceSet resources, IEventSource source, GameState state)
        {
            if (state == null) return null;
            var engine = new EventEngine(story, resources, source);
            engine._state = state;

            // תוכן השתנה: משאב שהוגדר ב-ResourceSet אך חסר ב-state השמור (נוסף מד אחרי השמירה)
            // -> אתחל ל-start ולא ל-0, אחרת הוא עלול להיקרא כשבור מיד (ספק 10.7).
            foreach (var def in resources.resources)
                if (!state.Resources.ContainsKey(def.id))
                    state.Resources[def.id] = def.start;

            var node = source.EnterNode(state, state.CurrentNodeId);
            if (node == null) return null;

            engine._current = node;
            engine._state.CurrentNodeId = node.Id;
            return engine;
        }

        public GameState State => _state;
        public GameStatus Status => _status;
        public EventNode Current => _current;

        public event Action<ResolveResult> OnResolved;
        public event Action<GameOverInfo> OnGameOver;

        // חישוב-יבש pure (ספק 14.4): deltas + danger בלבד. לא נוגע ב-GameState ו-לא מחיל setFlags/next.
        public ChoicePreview Preview(ChoiceSide side)
        {
            var deltas = new List<ResourceDelta>();
            var dangers = new List<DangerHint>();
            var choice = _current != null ? _current.GetChoice(side) : null;
            if (choice == null) return new ChoicePreview(deltas, dangers);

            foreach (var eff in choice.Effects)
            {
                deltas.Add(new ResourceDelta(eff.ResourceId, eff.Delta));
                var def = _resources.Find(eff.ResourceId);
                if (def == null) continue;
                int projected = ResourceRules.Clamp(_state.GetResource(eff.ResourceId) + eff.Delta, def.min, def.max);
                dangers.Add(new DangerHint(eff.ResourceId, ResourceRules.DangerFor(def, projected)));
            }
            return new ChoicePreview(deltas, dangers);
        }

        // החלת בחירה בלבד (ספק 14.4): effects + setFlags, עדכון GameState, בדיקת game-over.
        // אינו מבקש את האירוע הבא - זה תפקיד Advance/EnterNode.
        public ResolveResult Resolve(ChoiceSide side)
        {
            var applied = new List<ResourceDelta>();
            var choice = _current != null ? _current.GetChoice(side) : null;
            if (choice == null) return new ResolveResult(applied, _status);

            _state.Turn++;   // one decision applied (spec 7.5) - drives the MaxTurns win condition

            string brokenResource = null;
            ResourceEdge brokenEdge = ResourceEdge.Min;

            foreach (var eff in choice.Effects)
            {
                var def = _resources.Find(eff.ResourceId);
                int cur = _state.GetResource(eff.ResourceId);
                int next = def != null ? ResourceRules.Clamp(cur + eff.Delta, def.min, def.max) : cur + eff.Delta;
                _state.Resources[eff.ResourceId] = next;
                applied.Add(new ResourceDelta(eff.ResourceId, next - cur)); // השינוי בפועל אחרי clamp, לא הנומינלי

                if (def != null && brokenResource == null && ResourceRules.IsBroken(def, next, out brokenEdge))
                    brokenResource = def.id;
            }

            foreach (var kv in choice.SetFlags)
                _state.Flags[kv.Key] = kv.Value;

            _pendingNext = choice.Next;

            if (brokenResource != null)
            {
                _status = GameStatus.GameOver;
                _lastGameOver = new GameOverInfo(GameOverReason.ResourceBroken, EndingText(brokenResource, brokenEdge));
            }

            var result = new ResolveResult(applied, _status);
            OnResolved?.Invoke(result);
            if (_status == GameStatus.GameOver) OnGameOver?.Invoke(_lastGameOver);
            return result;
        }

        // מבקש את האירוע הבא ומקדם את Current (Reigns: נקרא מיד אחרי Resolve).
        // next מפורש גובר על בחירת ה-source (ספק 12.4).
        public EventNode Advance()
        {
            if (_status == GameStatus.GameOver) return null;

            // Win condition (spec 7.5): reached the run length -> survival = a win, with a
            // branching climax based on flags.
            if (_story.MaxTurns > 0 && _state.Turn >= _story.MaxTurns) { EndSurvived(); return null; }

            EventNode next;
            if (!string.IsNullOrEmpty(_pendingNext))
            {
                next = _source.EnterNode(_state, _pendingNext);
            }
            else
            {
                if (!_source.HasNext(_state)) { EndNoMoreEvents(); return null; }
                next = _source.NextEvent(_state);
            }

            _pendingNext = null;
            if (next == null) { EndNoMoreEvents(); return null; }

            _current = next;
            _state.CurrentNodeId = next.Id;
            CheckGoal();   // מסע: הגעה ליעד = ניצחון (no-op ב-Reigns, Deck אינו IGoalAwareSource)
            return next;
        }

        // כניסה מפורשת לצומת (מסע / re-entry של שמירה, ספק 12.4).
        public EventNode EnterNode(string nodeId)
        {
            var next = _source.EnterNode(_state, nodeId);
            if (next == null) { EndNoMoreEvents(); return null; }
            _current = next;
            _state.CurrentNodeId = next.Id;
            _pendingNext = null;
            // מסע: הגעה ליעד = ניצחון; אחרת מבוי-סתום (צומת בלי המשך שאינו היעד) -> סיום מבוקר (M1/§10.7).
            if (!CheckGoal() && !_source.HasNext(_state)) EndNoMoreEvents();
            return next;
        }

        // ניצחון-מסע (ספק 7.5.6): המקור מצהיר שהגענו ליעד -> סיום מבוקר עם reason=ReachedGoal.
        private bool CheckGoal()
        {
            if (_status == GameStatus.GameOver) return true;
            if (_source is IGoalAwareSource g && g.IsAtGoal(_state))
            {
                _status = GameStatus.GameOver;
                _lastGameOver = new GameOverInfo(GameOverReason.ReachedGoal, GoalEndingText());
                OnGameOver?.Invoke(_lastGameOver);
                return true;
            }
            return false;
        }

        private string GoalEndingText()
        {
            foreach (var e in _story.Endings)
                if (e.ReachedGoal) return e.Text;
            return "You reached your destination.";
        }

        private void EndNoMoreEvents()
        {
            _status = GameStatus.GameOver;
            _lastGameOver = new GameOverInfo(GameOverReason.NoMoreEvents, FallbackEndingText());
            OnGameOver?.Invoke(_lastGameOver);
        }

        // Survival to MaxTurns = a win (spec 7.5). The climax branches: the first flag ending whose
        // flag state matches, otherwise the fallback. So choices made along the run change the ending
        // with no per-game code (data-driven).
        private void EndSurvived()
        {
            _status = GameStatus.GameOver;
            _lastGameOver = new GameOverInfo(GameOverReason.Survived, SurvivalEndingText());
            OnGameOver?.Invoke(_lastGameOver);
        }

        private string SurvivalEndingText()
        {
            foreach (var e in _story.Endings)
                if (!e.Fallback && !e.ReachedGoal && e.Flag != null && _state.GetFlag(e.Flag) == e.FlagIs)
                    return e.Text;
            return FallbackEndingText();
        }

        private string EndingText(string resourceId, ResourceEdge edge)
        {
            foreach (var e in _story.Endings)
                if (!e.Fallback && e.ResourceId == resourceId && e.Edge == edge)
                    return e.Text;
            return FallbackEndingText();
        }

        private string FallbackEndingText()
        {
            foreach (var e in _story.Endings)
                if (e.Fallback) return e.Text;
            // last-resort אגנוסטי-משחק - מגיעים לכאן רק אם התוכן לא הגדיר ending fallback
            // (ה-StoryValidator מתריע על כך). מחרוזת ניטרלית, לא תלוית-שפה של משחק מסוים.
            return "The story has reached its end.";
        }
    }
}
