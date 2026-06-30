using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // נגישות-מקלדת למפה (§11.4): SelectByIndex (נתיב מקשי-המספרים) בוחר את השכן ה-N בני-ההגעה.
    // נבדק על MonoBehaviour אמיתי; קלט המקלדת עצמו (Update) דק ומאומת ידנית ב-playmode.
    public sealed class MapViewTests
    {
        private static MapData LineMap()
        {
            var map = new MapData { StartNodeId = "a", GoalNodeId = "c" };
            map.Edges["a"] = new List<string> { "b", "c" };
            return map;
        }

        [Test]
        public void SelectByIndex_FiresOnSelect_ForTheNthReachable()
        {
            var go = new GameObject("map");
            try
            {
                var mv = go.AddComponent<MapView>();
                string selected = null;
                mv.OnSelect += id => selected = id;

                mv.Bind(LineMap(), "a", new List<string> { "b", "c" });   // shown, reachable [b,c]
                mv.SelectByIndex(1);                                      // key "2" -> second reachable
                Assert.AreEqual("c", selected);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SelectByIndex_OutOfRange_NoOp()
        {
            var go = new GameObject("map");
            try
            {
                var mv = go.AddComponent<MapView>();
                string selected = "untouched";
                mv.OnSelect += id => selected = id;

                mv.Bind(LineMap(), "a", new List<string> { "b" });
                mv.SelectByIndex(5);
                Assert.AreEqual("untouched", selected, "an out-of-range key selects nothing");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void SelectByIndex_WhenHidden_NoOp()
        {
            var go = new GameObject("map");
            try
            {
                var mv = go.AddComponent<MapView>();
                string selected = null;
                mv.OnSelect += id => selected = id;

                mv.Bind(LineMap(), "a", new List<string> { "b", "c" });
                mv.Hide();
                mv.SelectByIndex(0);
                Assert.IsNull(selected, "no selection while the map is hidden");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
