using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using TMPro;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Engine.PlayTests
{
    // אינטגרציה ב-PlayMode (מה ש-EditMode לא מכסה): מחזור-החיים של ה-MonoBehaviour-ים + SwipeInput,
    // דרך GameBootstrap האמיתי. מניע swipe.Commit ובודק מעבר-קלף + הגעה ל-game-over (מסך-סיום נדלק).
    // הרכבה תוכניתית כדי לא לתלות את הבדיקה בקובץ-סצנה/Build Settings.
    public sealed class GameLoopPlayTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            SaveSystem.Delete(); // לא להשאיר שמירה בין בדיקות
        }

        [UnityTest]
        public IEnumerator Swipe_Transitions_Card_And_Reaches_GameOver()
        {
            SaveSystem.Delete();
            BuildRig(withMenu: false, out var swipe, out var label, out var end, out _);

            yield return null; // GameBootstrap.Start רץ פריים אחד אחרי AddComponent
            yield return null;

            string firstCard = label.text;
            Assert.IsFalse(string.IsNullOrEmpty(firstCard), "first card should render its body text");

            swipe.Commit(ChoiceSide.Right);
            yield return null;
            Assert.AreNotEqual(firstCard, label.text, "committing a choice must advance to a different card");

            // ממשיכים ימינה עד game-over (האנרגיה יורדת ונשברת) - מקסימום הגנה מפני לולאה.
            bool gameOver = IsEndShown(end);
            for (int i = 0; i < 15 && !gameOver; i++)
            {
                swipe.Commit(ChoiceSide.Right);
                yield return null;
                gameOver = IsEndShown(end);
            }

            Assert.IsTrue(gameOver, "repeated commits must drain a resource and show the end screen");
        }

        // Main menu (spec 9.5): when a menu is wired, the run does not start until New Game is chosen.
        [UnityTest]
        public IEnumerator MainMenu_GatesRun_UntilNewGameClicked()
        {
            SaveSystem.Delete();
            BuildRig(withMenu: true, out _, out var label, out _, out var menu);

            yield return null;
            yield return null;

            Assert.IsTrue(menu.IsShown, "main menu is shown before the run starts");
            Assert.IsTrue(string.IsNullOrEmpty(label.text), "no card is rendered until New Game is clicked");

            // With no save, the menu is [New Game, Quit]; Button_0 = New Game -> StartRun -> first card.
            menu.transform.Find("MenuOverlay/Buttons/Button_0").GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.IsFalse(menu.IsShown, "New Game hides the main menu");
            Assert.IsFalse(string.IsNullOrEmpty(label.text), "the first card renders after New Game");
        }

        // ה-EndScreen בונה פאנל-ילד בשם "EndScreen" רק בקריאת Show; קיים+פעיל => game-over מוצג.
        private static bool IsEndShown(EndScreen end)
        {
            var panel = end.transform.Find("EndScreen");
            return panel != null && panel.gameObject.activeSelf;
        }

        // בונה את אותה הרכבה ש-GameBootstrap מצפה לה ומזריק שדות פרטיים ב-reflection.
        private void BuildRig(bool withMenu, out SwipeInput swipe, out TMP_Text bodyLabel, out EndScreen end, out MenuOverlay menu)
        {
            _root = new GameObject("PlayTestRoot");

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(_root.transform, false);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var cardGo = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(CardView));
            cardGo.transform.SetParent(canvasGo.transform, false);
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(cardGo.transform, false);
            bodyLabel = labelGo.GetComponent<TextMeshProUGUI>();
            UIFonts.Apply(bodyLabel);

            var cardView = cardGo.GetComponent<CardView>();
            SetPrivate(cardView, "bodyText", bodyLabel);
            SetPrivate(cardView, "cardBackground", cardGo.GetComponent<Image>());

            var bars = canvasGo.AddComponent<ResourceBarView>();
            swipe = canvasGo.AddComponent<SwipeInput>();
            end = canvasGo.AddComponent<EndScreen>();
            menu = withMenu ? canvasGo.AddComponent<MenuOverlay>() : null;

            // תוכן: story.json האמיתי של ה-_Template מהדיסק + ResourceSet תואם (energy/calm).
            string storyPath = Path.Combine(Application.dataPath, "Games/_Template/Content/story.json");
            var storyAsset = new TextAsset(File.ReadAllText(storyPath));
            var resources = ScriptableObject.CreateInstance<ResourceSet>();
            resources.resources = new[]
            {
                new ResourceDef { id = "energy", displayName = "Energy", min = 0, max = 10, start = 6, breakOn = BreakOn.Both, dangerBand = 2 },
                new ResourceDef { id = "calm",   displayName = "Calm",   min = 0, max = 10, start = 6, breakOn = BreakOn.Both, dangerBand = 2 }
            };

            var boot = _root.AddComponent<Crossroads.Game.Template.GameBootstrap>();
            SetPrivate(boot, "storyJson", storyAsset);
            SetPrivate(boot, "resources", resources);
            SetPrivate(boot, "cardView", cardView);
            SetPrivate(boot, "resourceBar", bars);
            SetPrivate(boot, "swipeInput", swipe);
            SetPrivate(boot, "endScreen", end);
            if (menu != null) SetPrivate(boot, "menu", menu);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"private field '{field}' not found on {target.GetType().Name}");
            f.SetValue(target, value);
        }
    }
}
