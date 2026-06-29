using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Crossroads.UI
{
    // נושא ויזואלי מתחלף (ספק 12.1). שכבת-ההחלפה שמאפשרת J8: החלפת ה-Theme משנה מראה+תוויות
    // בלי שינוי קוד. נוצר ב-Editor דרך Create > Crossroads > Theme. placeholder פרוצדורלי בשלב הראשון.
    [CreateAssetMenu(fileName = "theme", menuName = "Crossroads/Theme", order = 2)]
    public sealed class Theme : ScriptableObject
    {
        [Header("Palette")]
        public Color background = new Color(0.12f, 0.12f, 0.14f);
        public Color card = new Color(0.20f, 0.20f, 0.24f);
        public Color text = Color.white;
        public Color accent = new Color(0.30f, 0.55f, 0.95f);       // primary buttons / highlights (menus, end screen)
        public Color approaching = new Color(0.95f, 0.75f, 0.20f);  // color is a secondary channel; danger is also marked !/!!
        public Color willBreak = new Color(0.85f, 0.25f, 0.25f);

        [Header("Art")]
        public Sprite keyArt;      // optional key-art backdrop for the menu screens; null = flat panel
        public Sprite logo;        // optional title wordmark shown in place of the menu title text
        public Sprite cardArt;     // optional card background art; null = flat card color
        public Sprite meterFrame;  // optional decorative frame drawn behind each meter icon
        public Sprite speakerFrame; // optional ornate circular medallion frame around the speaker portrait; null = a plain procedural ring
        public Sprite buttonSprite; // optional 9-sliced button plate; null = flat colored buttons
        public Sprite menuIcon;    // optional icon for the pause/menu button; null = the built-in three bars
        public Sprite loadingArt;  // optional full-bleed poster for the branded loading screen; null = fall back to keyArt

        [Header("Audio")]
        public AudioClip music;     // looping gameplay track
        public AudioClip musicMenu; // looping track for the title / main menu (falls back to music if unset)
        public AudioClip swipeSfx;  // played when a choice is committed (card slides off)
        public AudioClip cardSfx;   // played when a new card appears
        public AudioClip clickSfx;  // played on UI button presses

        [Header("Typography")]
        public Font font;
        public TMP_FontAsset tmpFont;   // per-game UI font; null = the global default (UIFonts.Default)
        public bool rightToLeft;   // עברית/RTL: ה-bootstrap מעביר ל-UIFonts.RightToLeft לפני בניית ה-UI (§10.6)

        [Header("Overrides")]
        public List<ResourceLabel> resourceLabels = new List<ResourceLabel>();
        public List<SpeakerStyle> speakers = new List<SpeakerStyle>();
        public List<EndingArt> endingArt = new List<EndingArt>();   // per-ending backdrop, keyed by the story ending's image key

        // Theme override > ResourceDef default (ספק 14.2). null = אין override.
        public string GetResourceLabelOverride(string resourceId)
        {
            foreach (var r in resourceLabels)
                if (r.id == resourceId && !string.IsNullOrEmpty(r.label)) return r.label;
            return null;
        }

        // Optional per-resource HUD icon (J8 - art lives in the theme). null = no icon (text only).
        public Sprite GetResourceIcon(string resourceId)
        {
            foreach (var r in resourceLabels)
                if (r.id == resourceId && r.icon != null) return r.icon;
            return null;
        }

        public SpeakerStyle GetSpeaker(string speakerId)
        {
            foreach (var s in speakers)
                if (s.id == speakerId) return s;
            return null;
        }

        // Per-ending backdrop art (J8 - lives in the theme). null = fall back to keyArt.
        public Sprite GetEndingArt(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var e in endingArt)
                if (e.key == key && e.art != null) return e.art;
            return null;
        }

        [System.Serializable]
        public sealed class ResourceLabel
        {
            public string id;
            public string label;
            public Sprite icon;   // optional HUD icon for this resource
        }

        [System.Serializable]
        public sealed class SpeakerStyle
        {
            public string id;
            public Color tint = Color.white;
            public Sprite icon;
        }

        [System.Serializable]
        public sealed class EndingArt
        {
            public string key;    // matches the story ending's "image" key
            public Sprite art;
        }
    }
}
