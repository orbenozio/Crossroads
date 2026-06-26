using NUnit.Framework;
using UnityEngine;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // המיפוי טיפוסי-Engine -> view DTOs (ספק 12.3 / 14.2). הלב: קדימות displayName (J8).
    public sealed class ViewMapperTests
    {
        private static ResourceSet OneResource(string id, string displayName)
        {
            var set = ScriptableObject.CreateInstance<ResourceSet>();
            set.resources = new[]
            {
                new ResourceDef { id = id, displayName = displayName, min = 0, max = 10, start = 5, breakOn = BreakOn.Both, dangerBand = 1 }
            };
            return set;
        }

        private static GameState StateWith(string id, int value)
        {
            var s = new GameState();
            s.Resources[id] = value;
            return s;
        }

        // J8 / §14.2: override ב-Theme גובר על ה-displayName שב-ResourceDef.
        [Test]
        public void DisplayName_ThemeOverride_Wins()
        {
            var res = OneResource("money", "Money");
            var theme = ScriptableObject.CreateInstance<Theme>();
            theme.resourceLabels.Add(new Theme.ResourceLabel { id = "money", label = "Cash" });

            var views = ViewMapper.BuildResourceViews(StateWith("money", 5), res, theme);

            Assert.AreEqual(1, views.Count);
            Assert.AreEqual("Cash", views[0].DisplayName);
        }

        [Test]
        public void DisplayName_FallsBackToResourceDef_WhenNoOverride()
        {
            var res = OneResource("money", "Money");
            var views = ViewMapper.BuildResourceViews(StateWith("money", 5), res, null);
            Assert.AreEqual("Money", views[0].DisplayName);
        }

        [Test]
        public void BuildResourceViews_CarriesValueAndDanger()
        {
            var res = OneResource("hp", "HP"); // dangerBand 1, breakOn Both
            var views = ViewMapper.BuildResourceViews(StateWith("hp", 0), res, null);
            Assert.AreEqual(0, views[0].Value);
            Assert.AreEqual(DangerLevel.WillBreak, views[0].Danger); // 0 = שבירה
        }

        [Test]
        public void FormatDeltas_UsesDisplayName_AndSign()
        {
            var res = OneResource("money", "Money");
            var s = ViewMapper.FormatDeltas(new[] { new ResourceDelta("money", -2) }, res, null);
            Assert.AreEqual("Money -2", s);
        }

        [Test]
        public void FormatDeltas_ThemeOverride_AndSkipsZero()
        {
            var res = OneResource("money", "Money");
            var theme = ScriptableObject.CreateInstance<Theme>();
            theme.resourceLabels.Add(new Theme.ResourceLabel { id = "money", label = "Cash" });
            var s = ViewMapper.FormatDeltas(
                new[] { new ResourceDelta("money", 3), new ResourceDelta("ignored", 0) }, res, theme);
            Assert.AreEqual("Cash +3", s, "theme override used, '+' sign on positive, zero delta skipped");
        }

        [Test]
        public void BuildNodeView_MapsBodyAndChoiceLabels()
        {
            var node = new EventNode { Id = "n", Body = "hello", Speaker = "x" };
            node.Choices.Add(new Choice { Side = ChoiceSide.Left, Label = "L" });
            node.Choices.Add(new Choice { Side = ChoiceSide.Right, Label = "R" });

            var v = ViewMapper.BuildNodeView(node);

            Assert.AreEqual("hello", v.Body);
            Assert.AreEqual("L", v.LeftLabel);
            Assert.AreEqual("R", v.RightLabel);
        }
    }
}
