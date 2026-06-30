namespace Crossroads.Engine
{
    // חוקי-משאב משותפים (שבירה + סכנה). מקור-אמת יחיד שמשמש גם את EventEngine (game-over/preview)
    // וגם את שכבת ה-UI (מצב-סכנה על מד-משאב) - בלי כפילות לוגיקה.
    public static class ResourceRules
    {
        public static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        public static bool IsBroken(ResourceDef def, int value, out ResourceEdge edge)
        {
            edge = ResourceEdge.Min;
            bool atMin = value <= def.min;
            bool atMax = value >= def.max;
            switch (def.breakOn)
            {
                case BreakOn.Min:  edge = ResourceEdge.Min; return atMin;
                case BreakOn.Max:  edge = ResourceEdge.Max; return atMax;
                case BreakOn.Both: edge = atMin ? ResourceEdge.Min : ResourceEdge.Max; return atMin || atMax;
                default: return false;
            }
        }

        // שלוש דרגות (ספק 10.4 / 11.1): None / Approaching / WillBreak.
        public static DangerLevel DangerFor(ResourceDef def, int value)
        {
            if (IsBroken(def, value, out _)) return DangerLevel.WillBreak;
            bool nearMin = (def.breakOn != BreakOn.Max) && (value - def.min) <= def.dangerBand;
            bool nearMax = (def.breakOn != BreakOn.Min) && (def.max - value) <= def.dangerBand;
            return (nearMin || nearMax) ? DangerLevel.Approaching : DangerLevel.None;
        }
    }
}
