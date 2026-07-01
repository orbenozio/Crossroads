using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // The loading-progress marker is theme-driven, not a baked game-specific shape: a game's theme.loadingMarker
    // is honored; with none set the engine draws a plain neutral seal (never a crown).
    public sealed class LoadingScreenTests
    {
        private static Sprite Sprite1x1()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false, false);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Image HeadOf(GameObject go) =>
            go.transform.Find("LoadingScreen/Bar/Channel/Head").GetComponent<Image>();

        private static void ApplyBranding(LoadingScreen ls) =>
            typeof(LoadingScreen).GetMethod("ApplyBranding", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(ls, null);

        [Test]
        public void LoadingMarker_UsesThemeSpriteWhenSet()
        {
            var go = new GameObject("ls");
            var theme = ScriptableObject.CreateInstance<Theme>();
            var marker = Sprite1x1();
            try
            {
                var ls = go.AddComponent<LoadingScreen>();
                theme.loadingMarker = marker;
                ls.SetTheme(theme);
                ls.SetCaption("");   // triggers Ensure() -> builds the hierarchy in edit mode
                ApplyBranding(ls);

                Assert.AreSame(marker, HeadOf(go).sprite, "the game's loadingMarker is used as the progress marker");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(theme);
                Object.DestroyImmediate(marker.texture); Object.DestroyImmediate(marker);
            }
        }

        [Test]
        public void LoadingMarker_DefaultsToANeutralSeal_NotACustomSprite()
        {
            var go = new GameObject("ls");
            var theme = ScriptableObject.CreateInstance<Theme>();
            try
            {
                var ls = go.AddComponent<LoadingScreen>();
                ls.SetTheme(theme);   // no loadingMarker
                ls.SetCaption("");
                ApplyBranding(ls);

                var sprite = HeadOf(go).sprite;
                Assert.IsNotNull(sprite, "a procedural seal is drawn by default");
                Assert.AreEqual(sprite.rect.width, sprite.rect.height, "default marker is a square procedural disc");
                Assert.GreaterOrEqual(sprite.rect.width, 64f, "default marker is the high-res procedural disc, not tiny art");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(theme);
            }
        }
    }
}
