namespace Crossroads.Engine
{
    // RNG דטרמיניסטי לבחירה-משוקללת (ספק 14.5). ה-state השמיר הוא seed + drawCount בלבד (RngState),
    // וזה מספיק לשחזור-מדויק מנקודת-שמירה (M5): ה-ctor עושה fast-forward של drawCount צעדים מהזרע,
    // כך ש-resume ממשיך את אותה סדרה בדיוק. xorshift32 - מהיר, דטרמיניסטי, חוצה-פלטפורמות (M4).
    public sealed class DeterministicRng
    {
        private uint _state;
        private int _drawCount;

        public DeterministicRng(int seed, int drawCount = 0)
        {
            _state = (uint)seed;
            if (_state == 0) _state = 0x9E3779B9;   // xorshift מתנוון מ-state אפס
            for (int i = 0; i < drawCount; i++) Step();  // fast-forward למיקום השמור (ספק 14.5)
            _drawCount = drawCount;
        }

        public int DrawCount => _drawCount;

        private uint Step()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        // [0,1). מקדם את ה-drawCount (כל משיכה נספרת לצורך השמירה).
        public double NextDouble()
        {
            _drawCount++;
            return (Step() & 0xFFFFFF) / (double)0x1000000;
        }

        public int NextInt(int maxExclusive) =>
            maxExclusive <= 1 ? 0 : (int)(NextDouble() * maxExclusive);
    }
}
