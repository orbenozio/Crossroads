using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // The card's theme-configurable portrait size, the medallion it builds on bind, and the pluggable
    // choice-feedback hook (a game replaces the selection effect without engine edits).
    public sealed class CardViewTests
    {
        private static void SetField(object o, string name, object val)
        {
            var f = o.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "field not found: " + name);
            f.SetValue(o, val);
        }

        private static Sprite Sprite1x1()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false, false);
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Image MakeChild(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent.transform, false);
            return go.GetComponent<Image>();
        }

        private static TMP_Text MakeLabel(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);
            return go.GetComponent<TextMeshProUGUI>();
        }

        [Test]
        public void Bind_BuildsMedallion_AtThemePortraitSize()
        {
            var card = new GameObject("Card", typeof(RectTransform));
            var theme = ScriptableObject.CreateInstance<Theme>();
            var portrait = Sprite1x1();
            try
            {
                var view = card.AddComponent<CardView>();
                var speakerIcon = MakeChild(card, "SpeakerIcon");
                SetField(view, "speakerIcon", speakerIcon);

                theme.speakers.Add(new Theme.SpeakerStyle { id = "king", icon = portrait });
                theme.medallion = new Theme.MedallionStyle { size = new OptionalFloat(180f) };

                view.Bind(new EventNodeView("Long live the king.", "king", "Left", "Right"), theme);

                Assert.IsTrue(speakerIcon.gameObject.activeSelf, "portrait medallion shown for a speaker with an icon");
                Assert.AreEqual(new Vector2(180f, 180f), speakerIcon.rectTransform.sizeDelta, "medallion sized from the theme");
                Assert.IsNotNull(speakerIcon.transform.Find("PortraitMask"), "inset circular mask built");
                Assert.IsNotNull(speakerIcon.transform.Find("Ring"), "medallion ring built");
            }
            finally
            {
                Object.DestroyImmediate(card);
                Object.DestroyImmediate(theme);
                Object.DestroyImmediate(portrait.texture); Object.DestroyImmediate(portrait);
            }
        }

        // Records calls so the test can assert CardView routed the effect here instead of the built-in one.
        private sealed class RecordingFeedback : CardChoiceFeedback
        {
            public int dragCalls, resetCalls;
            public override void ApplyDrag(CardView card, ChoiceSide side, float fraction) => dragCalls++;
            public override void Reset(CardView card) => resetCalls++;
        }

        [Test]
        public void CustomFeedback_ReplacesTheDefaultEffect()
        {
            var card = new GameObject("Card", typeof(RectTransform));
            var theme = ScriptableObject.CreateInstance<Theme>();
            try
            {
                var view = card.AddComponent<CardView>();
                var recorder = card.AddComponent<RecordingFeedback>();
                var leftBg = MakeChild(card, "ChoiceLeftBg");
                var rightBg = MakeChild(card, "ChoiceRightBg");
                SetField(view, "leftLabel", MakeLabel(card, "ChoiceLeft"));
                SetField(view, "rightLabel", MakeLabel(card, "ChoiceRight"));

                view.Bind(new EventNodeView("body", "narrator", "Left", "Right"), theme);
                Assert.AreSame(leftBg, view.LeftPlaque, "left plaque wired on bind");

                view.ApplyDrag(ChoiceSide.Left, 1f);
                Assert.Greater(recorder.dragCalls, 0, "the custom feedback received the drag");
                // The built-in effect would swell the active plaque to 1.09x; the custom one does nothing,
                // proving CardView delegated rather than running the default emphasis too.
                Assert.AreEqual(Vector3.one, leftBg.rectTransform.localScale, "default plaque-glow did not run");

                view.ResetDrag();
                Assert.Greater(recorder.resetCalls, 0, "reset routed to the custom feedback");
            }
            finally
            {
                Object.DestroyImmediate(card);
                Object.DestroyImmediate(theme);
            }
        }
    }
}
