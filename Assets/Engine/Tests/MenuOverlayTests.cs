using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // Generic menu overlay (spec 9.5): builds one button per item, clicking runs the action and hides,
    // and re-showing rebuilds the stack to match the new item list.
    public sealed class MenuOverlayTests
    {
        private static Button Btn(GameObject go, int i) =>
            go.transform.Find("MenuOverlay/Buttons/Button_" + i).GetComponent<Button>();

        private static string Label(GameObject go, int i) =>
            go.transform.Find("MenuOverlay/Buttons/Button_" + i + "/Label").GetComponent<TMP_Text>().text;

        [Test]
        public void Show_BuildsButtons_ClickInvokesAndHides()
        {
            var go = new GameObject("menu");
            try
            {
                var menu = go.AddComponent<MenuOverlay>();
                bool aClicked = false, bClicked = false;
                menu.Show("Title", "Body", new[]
                {
                    new MenuOverlay.MenuItem("Continue", () => aClicked = true, true),
                    new MenuOverlay.MenuItem("Quit", () => bClicked = true)
                });

                Assert.IsTrue(menu.IsShown);
                Assert.AreEqual("Title", go.transform.Find("MenuOverlay/Title").GetComponent<TMP_Text>().text);
                // Labels are upper-cased to match the end-screen buttons (one button family), keeping the
                // number-key prefix for accessibility.
                Assert.AreEqual("[1] CONTINUE", Label(go, 0), "buttons carry a number-key prefix for accessibility");
                Assert.AreEqual("[2] QUIT", Label(go, 1));

                Btn(go, 0).onClick.Invoke();
                Assert.IsTrue(aClicked, "clicking a button runs its action");
                Assert.IsFalse(bClicked);
                Assert.IsFalse(menu.IsShown, "selecting an item hides the menu");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Reshow_RebuildsButtonsToMatchItems()
        {
            var go = new GameObject("menu");
            try
            {
                var menu = go.AddComponent<MenuOverlay>();
                menu.Show("A", null, new[]
                {
                    new MenuOverlay.MenuItem("One", null),
                    new MenuOverlay.MenuItem("Two", null),
                    new MenuOverlay.MenuItem("Three", null)
                });
                var buttons = go.transform.Find("MenuOverlay/Buttons");
                Assert.AreEqual(3, buttons.childCount);

                menu.Show("B", null, new[] { new MenuOverlay.MenuItem("Only", null) });
                Assert.AreEqual(1, buttons.childCount, "re-showing rebuilds the stack to the new item count");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
