using NUnit.Framework;
using UnityEngine;
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
