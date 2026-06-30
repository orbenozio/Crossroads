using NUnit.Framework;
using Crossroads.Engine;

namespace Crossroads.Engine.Tests
{
    // חוקי-המשאב המשותפים (שבירה + סכנה) - לב הלוגיקה שמשמשת גם engine וגם UI.
    public sealed class ResourceRulesTests
    {
        private static ResourceDef Def(BreakOn b) =>
            new ResourceDef { id = "x", min = 0, max = 10, start = 5, breakOn = b, dangerBand = 2 };

        [Test]
        public void Clamp_Bounds()
        {
            Assert.AreEqual(0, ResourceRules.Clamp(-3, 0, 10));
            Assert.AreEqual(10, ResourceRules.Clamp(99, 0, 10));
            Assert.AreEqual(5, ResourceRules.Clamp(5, 0, 10));
        }

        [Test]
        public void IsBroken_Both_Edges()
        {
            var d = Def(BreakOn.Both);
            Assert.IsTrue(ResourceRules.IsBroken(d, 0, out var e1)); Assert.AreEqual(ResourceEdge.Min, e1);
            Assert.IsTrue(ResourceRules.IsBroken(d, 10, out var e2)); Assert.AreEqual(ResourceEdge.Max, e2);
            Assert.IsFalse(ResourceRules.IsBroken(d, 5, out _));
        }

        [Test]
        public void IsBroken_MinOnly_IgnoresMax()
        {
            var d = Def(BreakOn.Min);
            Assert.IsTrue(ResourceRules.IsBroken(d, 0, out _));
            Assert.IsFalse(ResourceRules.IsBroken(d, 10, out _));
        }

        [Test]
        public void Danger_ThreeLevels()
        {
            var d = Def(BreakOn.Both);
            Assert.AreEqual(DangerLevel.None, ResourceRules.DangerFor(d, 5));
            Assert.AreEqual(DangerLevel.Approaching, ResourceRules.DangerFor(d, 2));   // within band of min
            Assert.AreEqual(DangerLevel.Approaching, ResourceRules.DangerFor(d, 8));   // within band of max
            Assert.AreEqual(DangerLevel.WillBreak, ResourceRules.DangerFor(d, 0));
            Assert.AreEqual(DangerLevel.WillBreak, ResourceRules.DangerFor(d, 10));
        }

        [Test]
        public void Danger_MinOnly_NoMaxSideDanger()
        {
            var d = Def(BreakOn.Min);
            Assert.AreEqual(DangerLevel.None, ResourceRules.DangerFor(d, 9));          // near max but breakOn=Min
            Assert.AreEqual(DangerLevel.Approaching, ResourceRules.DangerFor(d, 1));
        }
    }
}
