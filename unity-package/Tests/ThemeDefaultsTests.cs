using NUnit.Framework;
using UnityEngine;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // The theme resolves every visual default through accessors that inherit the engine default (ThemeDefaults)
    // when a field is unset. A fresh instance has every OptionalFloat/OptionalColor with set == false - which is
    // exactly how an OLDER theme.asset (missing these fields entirely) deserializes - so this both verifies the
    // defaults AND stands in for the migration path the "unset = inherit" convention exists to protect.
    public sealed class ThemeDefaultsTests
    {
        private static Sprite Sprite1x1()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false, false);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        [Test]
        public void UnsetFields_ResolveToEngineDefaults()
        {
            var t = ScriptableObject.CreateInstance<Theme>();
            try
            {
                // Medallion + meter metrics.
                Assert.AreEqual(ThemeDefaults.PortraitSize, t.MedallionSize, "medallion size default");
                Assert.AreEqual(ThemeDefaults.RingThickness, t.MedallionRingThickness, 1e-5f, "ring thickness default");
                Assert.AreEqual(ThemeDefaults.InnerFraction, t.MedallionInnerFraction, 1e-5f, "inner fraction default");
                Assert.AreEqual(ThemeDefaults.MeterIconSize, t.MeterIconSize, "meter icon size default");
                Assert.AreEqual(ThemeDefaults.MeterFrameSize, t.MeterFrameSize, "meter frame size default");
                Assert.AreEqual(ThemeDefaults.MeterIconTint, t.MeterIconTint, "meter icon tint default");

                // Palette roles fall back to their base role.
                Assert.AreEqual(t.accent, t.Ring, "ring falls back to accent");
                Assert.AreEqual(t.accent, t.Divider, "divider falls back to accent");
                Assert.AreEqual(t.accent, t.MedallionRingColor, "medallion ring color falls back through ring -> accent");
                Assert.AreEqual(ThemeDefaults.ChoiceHint, t.ChoiceHint, "choice hint default");
                Assert.AreEqual(ThemeDefaults.ChoiceGlow, t.ChoiceGlow, "choice glow default");
                Assert.AreEqual(ThemeDefaults.HudPlate(t.background), t.HudPlate, "hud plate derived from background");
                Assert.AreEqual(ThemeDefaults.PlaqueFill, t.PlaqueFill, "plaque fill default");
                Assert.AreEqual(ThemeDefaults.PlaqueEdge, t.PlaqueEdge, "plaque edge default (neutral, not bronze)");
            }
            finally { Object.DestroyImmediate(t); }
        }

        [Test]
        public void SetFields_Override()
        {
            var t = ScriptableObject.CreateInstance<Theme>();
            try
            {
                t.medallion = new Theme.MedallionStyle
                {
                    size = new OptionalFloat(180f),
                    ringThickness = new OptionalFloat(0.03f),
                    innerFraction = new OptionalFloat(0.6f),
                    ringColor = new OptionalColor(new Color(0f, 0.5f, 1f, 1f)),
                };
                t.meter = new Theme.MeterStyle
                {
                    iconSize = new OptionalFloat(64f),
                    frameSize = new OptionalFloat(100f),
                    iconTint = new OptionalColor(new Color(1f, 0f, 0f, 1f)),
                };
                t.divider = new OptionalColor(new Color(0f, 1f, 0f, 1f));
                t.plaqueEdge = new OptionalColor(new Color(0.55f, 0.45f, 0.26f, 0.95f));   // medieval bronze

                Assert.AreEqual(180f, t.MedallionSize);
                Assert.AreEqual(0.03f, t.MedallionRingThickness, 1e-5f);
                Assert.AreEqual(0.6f, t.MedallionInnerFraction, 1e-5f);
                Assert.AreEqual(new Color(0f, 0.5f, 1f, 1f), t.MedallionRingColor, "explicit ring color overrides the role fallback");
                Assert.AreEqual(64f, t.MeterIconSize);
                Assert.AreEqual(100f, t.MeterFrameSize);
                Assert.AreEqual(new Color(1f, 0f, 0f, 1f), t.MeterIconTint);
                Assert.AreEqual(new Color(0f, 1f, 0f, 1f), t.Divider, "explicit divider overrides accent");
                Assert.AreEqual(new Color(0.55f, 0.45f, 0.26f, 0.95f), t.PlaqueEdge, "explicit plaque edge overrides the neutral default");
            }
            finally { Object.DestroyImmediate(t); }
        }

        [Test]
        public void OptionalColor_TransparentIsAValidExplicitValue()
        {
            // Unlike the old "alpha 0 = unset" trap, an explicit fully-transparent value is honored.
            var t = ScriptableObject.CreateInstance<Theme>();
            try
            {
                t.meter = new Theme.MeterStyle { iconTint = new OptionalColor(new Color(0f, 0f, 0f, 0f)) };
                Assert.AreEqual(new Color(0f, 0f, 0f, 0f), t.MeterIconTint, "an explicit transparent tint is not treated as unset");
            }
            finally { Object.DestroyImmediate(t); }
        }

        [Test]
        public void GameplayArt_FallsBackToKeyArt()
        {
            var t = ScriptableObject.CreateInstance<Theme>();
            var key = Sprite1x1();
            var gameplay = Sprite1x1();
            try
            {
                Assert.IsNull(t.GetGameplayArt(), "no art at all -> null");
                t.keyArt = key;
                Assert.AreSame(key, t.GetGameplayArt(), "falls back to keyArt when gameplayArt is unset");
                t.gameplayArt = gameplay;
                Assert.AreSame(gameplay, t.GetGameplayArt(), "dedicated gameplayArt wins when set");
            }
            finally
            {
                Object.DestroyImmediate(t);
                Object.DestroyImmediate(key.texture); Object.DestroyImmediate(key);
                Object.DestroyImmediate(gameplay.texture); Object.DestroyImmediate(gameplay);
            }
        }
    }
}
