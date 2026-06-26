using System.Collections.Generic;

namespace Crossroads.Engine
{
    // אירוע/קלף בודד (ספק 14.1). זהה לשני הפורמטים - מה שמשתנה הוא רק מי בוחר את הבא (ספק 14.3).
    public sealed class EventNode
    {
        public string Id;
        public string Speaker;          // מזהה-דמות; ה-Theme ממפה לאייקון/צבע
        public string Body;             // טקסט המצב (J1)
        public Condition AppearWhen;    // null = תמיד תקף
        public int Weight = 1;          // reserved ל-LATER; ב-NOW Deck בוחר לפי סדר (ספק 14.1)
        public List<Choice> Choices = new List<Choice>();

        // עזר: שליפת בחירה לפי צד. הולידציה מבטיחה בדיוק שתי בחירות עם side שונה (ספק 14.1).
        public Choice GetChoice(ChoiceSide side)
        {
            for (int i = 0; i < Choices.Count; i++)
            {
                if (Choices[i].Side == side) return Choices[i];
            }
            return null;
        }
    }
}
