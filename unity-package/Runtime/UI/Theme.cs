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
        [Header("Palette (base roles)")]
        public Color background = new Color(0.12f, 0.12f, 0.14f);
        public Color card = new Color(0.20f, 0.20f, 0.24f);
        public Color text = Color.white;
        public Color accent = new Color(0.42f, 0.52f, 0.66f);       // primary buttons / highlights (neutral slate; a skin repaints)
        public Color approaching = new Color(0.90f, 0.72f, 0.25f);  // color is a secondary channel; danger is also marked !/!!
        public Color willBreak = new Color(0.82f, 0.30f, 0.30f);

        // Semantic role overrides (see THEMING.md) so `accent` is no longer overloaded onto the ring, divider,
        // choice hint, etc. Each is optional: unset -> falls back to a base role, so a theme that sets none
        // looks identical. A game sets only the roles it wants distinct.
        [Header("Palette (role overrides; unset = fall back to a base role)")]
        public OptionalColor textMuted;    // secondary text; unset -> a muted `text`
        public OptionalColor ring;         // speaker medallion ring / procedural frames; unset -> `accent`
        public OptionalColor divider;      // HUD separator line; unset -> `accent`
        public OptionalColor choiceHint;   // resting choice-label color; unset -> neutral (ThemeDefaults)
        public OptionalColor choiceGlow;   // active choice glow-halo; unset -> neutral (ThemeDefaults)
        public OptionalColor hudPlate;     // meter plate fill; unset -> derived from `background` (legacy formula)
        public OptionalColor plaqueFill;   // procedural stone-plaque fill; unset -> neutral dark (ThemeDefaults)
        public OptionalColor plaqueEdge;   // procedural stone-plaque engraved edge; unset -> neutral grey (medieval skin = bronze)

        [Header("Art")]
        public Sprite keyArt;      // optional key-art backdrop for the menu screens; null = flat panel
        public Sprite logo;        // optional title wordmark shown in place of the menu title text
        public Sprite cardArt;     // optional card background art; null = flat card color
        public Sprite meterFrame;  // optional decorative frame drawn behind each meter icon
        public Sprite speakerFrame; // optional ornate circular medallion frame around the speaker portrait; null = a plain procedural ring
        public Sprite buttonSprite; // optional 9-sliced button plate; null = flat colored buttons
        public Sprite menuIcon;    // optional icon for the pause/menu button; null = the built-in three bars
        public Sprite loadingArt;  // optional full-bleed poster for the branded loading screen; null = fall back to keyArt
        public Sprite gameplayArt; // optional backdrop for the gameplay screen; null = fall back to keyArt

        [Header("Audio")]
        public AudioClip music;     // looping gameplay track
        public AudioClip musicMenu; // looping track for the title / main menu (falls back to music if unset)
        public AudioClip swipeSfx;  // played when a choice is committed (card slides off)
        public AudioClip cardSfx;   // played when a new card appears
        public AudioClip clickSfx;  // played on UI button presses

        // Per-widget metrics, grouped into style structs (see THEMING.md) instead of flat fields so the Theme
        // root does not become a god-object as more games are onboarded. Every field is an OptionalFloat /
        // OptionalColor: unset -> the engine default in ThemeDefaults.
        [Header("Component styles")]
        public MedallionStyle medallion;   // speaker portrait medallion
        public MeterStyle meter;           // HUD meter icon + frame

        [Header("Typography")]
        public Font font;
        public TMP_FontAsset tmpFont;   // per-game UI font; null = the global default (UIFonts.Default)
        public bool rightToLeft;   // עברית/RTL: ה-bootstrap מעביר ל-UIFonts.RightToLeft לפני בניית ה-UI (§10.6)

        [Header("Overrides")]
        public List<ResourceLabel> resourceLabels = new List<ResourceLabel>();
        public List<SpeakerStyle> speakers = new List<SpeakerStyle>();
        public List<EndingArt> endingArt = new List<EndingArt>();   // per-ending backdrop, keyed by the story ending's image key

        // Resolved palette roles (unset override -> base-role fallback, per THEMING.md).
        public Color TextMuted => textMuted.Or(ThemeDefaults.Muted(text));
        public Color Ring => ring.Or(accent);
        public Color Divider => divider.Or(accent);
        public Color ChoiceHint => choiceHint.Or(ThemeDefaults.ChoiceHint);
        public Color ChoiceGlow => choiceGlow.Or(ThemeDefaults.ChoiceGlow);
        public Color HudPlate => hudPlate.Or(ThemeDefaults.HudPlate(background));
        public Color PlaqueFill => plaqueFill.Or(ThemeDefaults.PlaqueFill);
        public Color PlaqueEdge => plaqueEdge.Or(ThemeDefaults.PlaqueEdge);

        // Resolved medallion metrics (unset -> ThemeDefaults). Ring color defers to the `ring` role.
        public float MedallionSize => medallion.size.Or(ThemeDefaults.PortraitSize);
        public Color MedallionRingColor => medallion.ringColor.Or(Ring);
        public float MedallionRingThickness => medallion.ringThickness.Or(ThemeDefaults.RingThickness);
        public float MedallionInnerFraction => medallion.innerFraction.Or(ThemeDefaults.InnerFraction);

        // Resolved meter metrics (unset -> ThemeDefaults).
        public float MeterIconSize => meter.iconSize.Or(ThemeDefaults.MeterIconSize);
        public float MeterFrameSize => meter.frameSize.Or(ThemeDefaults.MeterFrameSize);
        public Color MeterIconTint => meter.iconTint.Or(ThemeDefaults.MeterIconTint);

        // Gameplay-screen backdrop: the dedicated gameplayArt when set, else the menu key art (J8 fallback).
        public Sprite GetGameplayArt() => gameplayArt != null ? gameplayArt : keyArt;

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

        // Speaker portrait medallion metrics. Every field optional -> unset inherits ThemeDefaults.
        [System.Serializable]
        public struct MedallionStyle
        {
            public OptionalFloat size;           // medallion diameter (default 250)
            public OptionalColor ringColor;      // procedural ring color when there is no speakerFrame (unset -> the `ring` role)
            public OptionalFloat ringThickness;  // ring band width as a fraction of the radius (default 0.06)
            public OptionalFloat innerFraction;  // how far the portrait tucks under an ornate speakerFrame (default 0.78)
        }

        // HUD meter icon + frame metrics. Every field optional -> unset inherits ThemeDefaults.
        [System.Serializable]
        public struct MeterStyle
        {
            public OptionalFloat iconSize;   // meter icon size (default 82)
            public OptionalFloat frameSize;  // meter frame size (default 132)
            public OptionalColor iconTint;   // tint applied to meter icon art (default neutral)
        }
    }
}
