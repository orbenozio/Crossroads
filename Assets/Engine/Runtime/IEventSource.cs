namespace Crossroads.Engine
{
    // ההפשטה שמאפשרת מנוע אחד לשני הפורמטים (ספק 12.4, ליבת G2).
    // ה-EventEngine לא יודע אם הוא רץ ב-Reigns (Deck) או במסע (MapGraph).
    public interface IEventSource
    {
        // האירוע התקף הבא בהינתן המצב, או null אם אין.
        EventNode NextEvent(GameState state);

        // כניסה מפורשת לצומת מסוים (לא "הבא") - לבחירת-מפה ול-re-entry של שמירה.
        EventNode EnterNode(GameState state, string nodeId);

        // האם המקור מסוגל לספק אירוע נוסף כעת (לבדיקת מבוי-סתום, M1).
        // נגזר מהמצב המלא, כולל ה-bag האטום של מצב-המפה (ספק 12.5).
        bool HasNext(GameState state);
    }

    // יכולת אופציונלית: מקור שיש לו יעד (מסע). ה-EventEngine בודק אותה אחרי כל כניסה-לצומת
    // ומסיים בניצחון (ReachedGoal) בהגעה ליעד. Deck (Reigns) אינו מממש אותה - אין השפעה על Reigns.
    public interface IGoalAwareSource
    {
        bool IsAtGoal(GameState state);
    }
}
