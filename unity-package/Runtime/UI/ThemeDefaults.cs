using System;
using UnityEngine;

namespace Crossroads.UI
{
    // Explicit "unset" wrappers (see THEMING.md). Unity deserializes a field missing from an older
    // theme.asset to the type default, so `set == false` cleanly means "inherit the engine default" while a
    // game that genuinely wants 0 / transparent sets `set = true` with that value. No magic zero.
    [Serializable]
    public struct OptionalFloat
    {
        public bool set;
        public float value;
        public OptionalFloat(float v) { set = true; value = v; }
        public float Or(float fallback) => set ? value : fallback;
    }

    [Serializable]
    public struct OptionalColor
    {
        public bool set;
        public Color value;
        public OptionalColor(Color v) { set = true; value = v; }
        public Color Or(Color fallback) => set ? value : fallback;
    }

    // The single home for every engine default literal (see THEMING.md). Accessors on Theme resolve
    // `optional.Or(ThemeDefaults.X)`; no default is duplicated in a consumer. The palette here is the engine's
    // NEUTRAL out-of-box identity - a strong art direction (e.g. the medieval look) ships as a skin asset that
    // overrides these, not as the default.
    public static class ThemeDefaults
    {
        // Neutral base palette.
        public static readonly Color Background = new Color(0.12f, 0.12f, 0.14f);
        public static readonly Color Card = new Color(0.20f, 0.20f, 0.24f);
        public static readonly Color Text = Color.white;
        public static readonly Color Accent = new Color(0.42f, 0.52f, 0.66f);   // neutral slate (was a strong blue)
        public static readonly Color Approaching = new Color(0.90f, 0.72f, 0.25f);
        public static readonly Color WillBreak = new Color(0.82f, 0.30f, 0.30f);

        // Speaker medallion.
        public const float PortraitSize = 250f;
        public const float RingThickness = 0.06f;
        public const float InnerFraction = 0.78f;   // how far the portrait tucks under an ornate speakerFrame

        // Meters.
        public const float MeterIconSize = 82f;
        public const float MeterFrameSize = 132f;
        public static readonly Color MeterIconTint = new Color(0.82f, 0.82f, 0.85f, 1f);   // neutral (was bronze)

        // Choice-selection feedback (neutral; the medieval skin sets warm gold via the tokens).
        public static readonly Color ChoiceHint = new Color(0.92f, 0.92f, 0.94f, 1f);   // resting choice-label color
        public static readonly Color ChoiceGlow = new Color(0.80f, 0.85f, 0.95f, 1f);   // active glow-halo
        public static readonly Color WarmPlaque = new Color(0.98f, 0.98f, 1.0f, 1f);    // faint lift on the active plaque
        public static readonly Color DimPlaque = new Color(0.5f, 0.48f, 0.48f, 1f);     // the non-active plaque recedes

        // A slightly-muted variant of a text color (textMuted fallback).
        public static Color Muted(Color text) => new Color(text.r, text.g, text.b, text.a * 0.7f);

        // Legacy HUD plate fill derived from the background (kept as the fallback when hudPlate is unset, so a
        // theme that does not set it looks exactly as before). A game with a light background sets hudPlate.
        public static Color HudPlate(Color background) =>
            new Color(background.r * 1.4f + 0.04f, background.g * 1.4f + 0.04f, background.b * 1.5f + 0.05f, 0.72f);
    }
}
