using System.Collections.Generic;

namespace Crossroads.Engine
{
    // מצב-RNG מלא (ספק 14.5) - seed לבדו לא מספיק לשחזור מנקודת-שמירה (M5).
    // ברירת-המחדל ל-slice: seed + drawCount, עם run-forward בטעינה.
    [System.Serializable]
    public struct RngState
    {
        public int Seed;
        public int DrawCount;
        public RngState(int seed, int drawCount) { Seed = seed; DrawCount = drawCount; }
    }

    // כל ה-state הרץ (ספק 12.3). היחיד ש-SaveSystem מסריאל. ה-UI לא מחזיק state.
    public sealed class GameState
    {
        public int SchemaVersion = 1;
        public string CurrentNodeId;
        public Dictionary<string, int> Resources = new Dictionary<string, int>();
        public Dictionary<string, bool> Flags = new Dictionary<string, bool>();
        public RngState Rng;

        // Number of decisions applied (spec 7.5). Drives the MaxTurns win condition. Older saves
        // (from before this field) load as 0 - backward compatible with no schema bump.
        public int Turn;

        // bag-מצב-מפה אטום (ספק 12.3/14.5) - ה-Engine לא מפרש אותו. null ב-Reigns.
        // ה-IEventSource של המסע (MapGraph) הוא שכותב/קורא אותו.
        public string MapState;

        public int GetResource(string id)
        {
            return Resources.TryGetValue(id, out int v) ? v : 0;
        }

        public bool GetFlag(string id)
        {
            return Flags.TryGetValue(id, out bool v) && v;
        }
    }
}
