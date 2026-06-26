using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // מסך-פתיחה / שגיאת-דאטה (ספק 9.5). נבדק על MonoBehaviour אמיתי: כותרת/גוף, כפתור אופציונלי, הסתרה.
    public sealed class MessageOverlayTests
    {
        private static TMP_Text Find(GameObject go, string path) => go.transform.Find(path).GetComponent<TMP_Text>();

        [Test]
        public void Show_WithButton_SetsTexts_AndShowsButton()
        {
            var go = new GameObject("ov");
            try
            {
                var ov = go.AddComponent<MessageOverlay>();
                bool clicked = false;
                ov.Show("Crossroads", "Swipe to decide.", "Start", () => clicked = true);

                Assert.IsTrue(ov.IsShown);
                Assert.AreEqual("Crossroads", Find(go, "MessageOverlay/Title").text);
                Assert.AreEqual("Swipe to decide.", Find(go, "MessageOverlay/Body").text);

                var button = go.transform.Find("MessageOverlay/Button").gameObject;
                Assert.IsTrue(button.activeSelf, "button shows when a label is given");
                Assert.AreEqual("Start", Find(go, "MessageOverlay/Button/Label").text);

                button.GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(clicked, "clicking the button fires onButton");
                Assert.IsFalse(ov.IsShown, "clicking hides the overlay");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Show_NoButtonLabel_HidesButton()
        {
            var go = new GameObject("ov");
            try
            {
                var ov = go.AddComponent<MessageOverlay>();
                ov.Show("Data Error", "The story data could not be loaded.", null, null);

                Assert.IsTrue(ov.IsShown);
                var button = go.transform.Find("MessageOverlay/Button").gameObject;
                Assert.IsFalse(button.activeSelf, "no button on an error screen (no label)");

                ov.Hide();
                Assert.IsFalse(ov.IsShown);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
