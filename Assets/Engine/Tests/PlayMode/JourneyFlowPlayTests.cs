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
    // אינטגרציית פורמט-המסע ב-PlayMode (ספק 7.5): התזמון המובחן מפה<->קלף דרך RefugeeRoad GameBootstrap.
    // opening -> Start -> קלף; commit -> מפה מופיעה; בחירת-צומת -> קלף הבא. בונה rig תוכניתית.
    public sealed class JourneyFlowPlayTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown() { if (_root != null) Object.Destroy(_root); }

        [UnityTest]
        public IEnumerator Journey_Opening_Card_Map_Navigation()
        {
            BuildRig(out var swipe, out var label, out var overlay, out var mapView);

            yield return null; yield return null;
            Assert.IsTrue(overlay.IsShown, "journey opens on the opening screen");

            // Start -> הקלף הראשון (border).
            overlay.transform.Find("MessageOverlay/Button").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsFalse(mapView.IsShown, "card is shown, not the map, right after Start");
            Assert.IsFalse(string.IsNullOrEmpty(label.text), "the first node's card renders");
            string firstBody = label.text;

            // commit -> חוזרים למפה.
            swipe.Commit(ChoiceSide.Left);
            yield return null;
            Assert.IsTrue(mapView.IsShown, "after resolving the node, the map appears (spec 7.5)");

            // בחירת השכן הבא (checkpoint) -> קלף הבא, מפה נסגרת.
            var node = mapView.transform.Find("MapView/MapNode_checkpoint");
            Assert.IsNotNull(node, "the reachable neighbor is on the map");
            node.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.IsFalse(mapView.IsShown, "selecting a node returns to the card");
            Assert.AreNotEqual(firstBody, label.text, "the card now shows the next node");
        }

        private void BuildRig(out SwipeInput swipe, out TMP_Text bodyLabel, out MessageOverlay overlay, out MapView mapView)
        {
            _root = new GameObject("JourneyTestRoot");

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
            var end = canvasGo.AddComponent<EndScreen>();
            overlay = canvasGo.AddComponent<MessageOverlay>();
            mapView = canvasGo.AddComponent<MapView>();

            // תוכן RefugeeRoad אמיתי מהדיסק + ResourceSet תואם (fuel/food/health/hope).
            string dir = Path.Combine(Application.dataPath, "Games/RefugeeRoad/Content/");
            var storyAsset = new TextAsset(File.ReadAllText(dir + "story.json"));
            var mapAsset = new TextAsset(File.ReadAllText(dir + "map.json"));
            var resources = ScriptableObject.CreateInstance<ResourceSet>();
            resources.resources = new[]
            {
                Def("fuel"), Def("food"), Def("health"), Def("hope")
            };

            var boot = _root.AddComponent<Crossroads.Game.RefugeeRoad.GameBootstrap>();
            SetPrivate(boot, "storyJson", storyAsset);
            SetPrivate(boot, "mapJson", mapAsset);
            SetPrivate(boot, "resources", resources);
            SetPrivate(boot, "cardView", cardView);
            SetPrivate(boot, "resourceBar", bars);
            SetPrivate(boot, "swipeInput", swipe);
            SetPrivate(boot, "endScreen", end);
            SetPrivate(boot, "messageOverlay", overlay);
            SetPrivate(boot, "mapView", mapView);
        }

        private static ResourceDef Def(string id) =>
            new ResourceDef { id = id, displayName = id, min = 0, max = 10, start = 6, breakOn = BreakOn.Min, dangerBand = 2 };

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"private field '{field}' not found on {target.GetType().Name}");
            f.SetValue(target, value);
        }
    }
}
