using Crossroads.Engine;
using UnityEngine;

namespace Crossroads.UI
{
    // ה-DTO-ים שה-UI צורך (ספק 12.3). חסרי-ידע על תוכן ספציפי (NewbornKing) - תומך M7/M8.
    // ה-UI ממיר טיפוסי-Engine ל-views האלה (ViewMapper); אינו קורא את story.json ישירות.

    public readonly struct EventNodeView
    {
        public readonly string Body;        // טקסט המצב (J1)
        public readonly string Speaker;     // מזהה-דמות; ה-Theme ממפה למראה
        public readonly string LeftLabel;   // תווית בחירת-שמאל
        public readonly string RightLabel;  // תווית בחירת-ימין
        public EventNodeView(string body, string speaker, string leftLabel, string rightLabel)
        {
            Body = body; Speaker = speaker; LeftLabel = leftLabel; RightLabel = rightLabel;
        }
    }

    public readonly struct ResourceView
    {
        public readonly string Id;
        public readonly string DisplayName;  // כבר-מוכרע (Theme override > ResourceDef, ספק 14.2)
        public readonly int Value;
        public readonly int Min;
        public readonly int Max;
        public readonly DangerLevel Danger;
        public readonly Sprite Icon;   // optional HUD icon (Theme), null = text only
        public ResourceView(string id, string displayName, int value, int min, int max, DangerLevel danger)
            : this(id, displayName, value, min, max, danger, null) { }
        public ResourceView(string id, string displayName, int value, int min, int max, DangerLevel danger, Sprite icon)
        {
            Id = id; DisplayName = displayName; Value = value; Min = min; Max = max; Danger = danger; Icon = icon;
        }
    }
}
