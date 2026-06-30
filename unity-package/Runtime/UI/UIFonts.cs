using TMPro;
using UnityEngine;

namespace Crossroads.UI
{
    // ספק-פונט מרכזי ל-UI הפרוצדורלי (TextMeshPro). הפונט נטען פעם אחת מ-Resources (פונט עברי דינמי);
    // RightToLeft הוא דגל-ריצה גלובלי (משחק אחד רץ בכל רגע) שה-bootstrap מגדיר מה-Theme - כך אותו
    // קוד-UI משרת גם אנגלית (LTR) וגם עברית (RTL). נקודה אחת להחלת פונט+כיוון על כל רכיב TMP.
    public static class UIFonts
    {
        private static TMP_FontAsset _default;
        public static bool RightToLeft;   // מוגדר ע"י ה-bootstrap מה-Theme לפני בניית ה-UI
        public static TMP_FontAsset Current;   // per-game font from the Theme; falls back to Default when null

        public static TMP_FontAsset Default
        {
            get
            {
                if (_default == null) _default = Resources.Load<TMP_FontAsset>("HebrewUI SDF");
                if (_default == null) _default = TMP_Settings.defaultFontAsset;   // fallback
                return _default;
            }
        }

        // Sets the per-game font from a Theme. Kept here (UI layer) so the thin Game bootstrap never
        // has to reference the TMP assembly itself.
        public static void UseThemeFont(Theme theme) => Current = theme != null ? theme.tmpFont : null;

        // החלת פונט + כיוון-טקסט על רכיב TMP. נקראת אחרי יצירת כל טקסט פרוצדורלי.
        public static void Apply(TMP_Text t)
        {
            if (t == null) return;
            var f = Current != null ? Current : Default;
            if (f != null) t.font = f;
            t.isRightToLeftText = RightToLeft;
        }
    }
}
