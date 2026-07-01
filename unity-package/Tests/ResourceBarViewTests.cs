using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // משוב ה-delta בזמן swipe (ספק 10.3): מד מושפע מציג את השינוי הצפוי, וניקוי מחזיר לבסיס.
    // נבדק על MonoBehaviour אמיתי (GameObject חי) - בודק את מיפוי ה-delta לתווית, לא רק לוגיקה.
    public sealed class ResourceBarViewTests
    {
        private static GameState StateWith(params (string id, int v)[] vals)
        {
            var s = new GameState();
            foreach (var p in vals) s.Resources[p.id] = p.v;
            return s;
        }

        [Test]
        public void ShowPreview_AppendsDelta_AndClearRestores()
        {
            var go = new GameObject("bars");
            try
            {
                var bars = go.AddComponent<ResourceBarView>();
                var res = TestData.Resources(("sleep", 6), ("money", 6));
                var views = ViewMapper.BuildResourceViews(StateWith(("sleep", 6), ("money", 6)), res, null);
                bars.Bind(views);

                var sleepLabel = go.transform.Find("Meters/Bar_sleep/Label").GetComponent<TMP_Text>();
                Assert.AreEqual("sleep 6", sleepLabel.text, "base label before preview");

                bars.ShowPreview(new[] { new ResourceDelta("sleep", -2), new ResourceDelta("money", 1) });
                StringAssert.Contains("-2", sleepLabel.text, "affected meter shows its pending delta");
                StringAssert.StartsWith("sleep 6", sleepLabel.text, "base label kept, delta appended");

                bars.ClearPreview();
                Assert.AreEqual("sleep 6", sleepLabel.text, "clear restores the base label");
            }
            finally { Object.DestroyImmediate(go); }
        }

        private static Sprite Sprite1x1()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false, false);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        [Test]
        public void MeterIcon_UsesThemeSizeAndTint()
        {
            var go = new GameObject("bars");
            var theme = ScriptableObject.CreateInstance<Theme>();
            var icon = Sprite1x1();
            var frameArt = Sprite1x1();
            try
            {
                // A theme that supplies a meter icon + frame art, with overridden sizes and tint.
                theme.resourceLabels.Add(new Theme.ResourceLabel { id = "sleep", icon = icon });
                theme.meterFrame = frameArt;
                theme.meter = new Theme.MeterStyle
                {
                    iconSize = new OptionalFloat(64f),
                    frameSize = new OptionalFloat(100f),
                    iconTint = new OptionalColor(new Color(1f, 0f, 0f, 1f)),
                };

                var bars = go.AddComponent<ResourceBarView>();
                var res = TestData.Resources(("sleep", 6));
                bars.SetTheme(theme);
                bars.Bind(ViewMapper.BuildResourceViews(StateWith(("sleep", 6)), res, theme));

                var iconImg = go.transform.Find("Meters/Bar_sleep/Icon").GetComponent<Image>();
                Assert.IsTrue(iconImg.gameObject.activeSelf, "meter icon shown when the theme supplies one");
                Assert.AreEqual(new Vector2(64f, 64f), iconImg.rectTransform.sizeDelta, "icon sized from the theme");
                Assert.AreEqual(new Color(1f, 0f, 0f, 1f), iconImg.color, "icon tinted from the theme");

                var frame = go.transform.Find("Meters/Bar_sleep/Frame").GetComponent<Image>();
                Assert.AreEqual(new Vector2(100f, 100f), frame.rectTransform.sizeDelta, "frame sized from the theme");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(theme);
                Object.DestroyImmediate(icon.texture); Object.DestroyImmediate(icon);
                Object.DestroyImmediate(frameArt.texture); Object.DestroyImmediate(frameArt);
            }
        }

        [Test]
        public void ShowPreview_ZeroDelta_NoSuffix()
        {
            var go = new GameObject("bars");
            try
            {
                var bars = go.AddComponent<ResourceBarView>();
                var res = TestData.Resources(("sleep", 6));
                bars.Bind(ViewMapper.BuildResourceViews(StateWith(("sleep", 6)), res, null));

                bars.ShowPreview(new[] { new ResourceDelta("sleep", 0) });
                var label = go.transform.Find("Meters/Bar_sleep/Label").GetComponent<TMP_Text>();
                Assert.AreEqual("sleep 6", label.text, "a zero delta adds no suffix");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
