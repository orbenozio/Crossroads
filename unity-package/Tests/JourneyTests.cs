using System.Collections.Generic;
using NUnit.Framework;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // G2 (ספק 2/12.4): אותו EventEngine מריץ פורמט מסע דרך MapGraph במקום Deck - בלי שינוי מנוע.
    // מוכיח: ניצחון בהגעה ליעד, הפסד בשבירת-משאב (אותה בדיקה), ו-save/resume של מסע דרך ה-bag.
    public sealed class JourneyTests
    {
        // story עם 3 צמתים + ending ניצחון; map: start -> mid -> goal.
        private static (StoryData story, MapData map) Line(int hpStart, out ResourceSet res)
        {
            var story = TestData.Story("start",
                TestData.Node("start", ("hp", -1)),
                TestData.Node("mid", ("hp", -1)),
                TestData.Node("goal", ("hp", -1)));
            story.Endings.Add(new Ending { ReachedGoal = true, Text = "You reached safe haven." });

            var map = new MapData { StartNodeId = "start", GoalNodeId = "goal" };
            map.Edges["start"] = new List<string> { "mid" };
            map.Edges["mid"] = new List<string> { "goal" };

            res = TestData.Resources(("hp", hpStart));
            return (story, map);
        }

        [Test]
        public void Journey_SameEngine_ReachesGoal_Wins()
        {
            var (story, map) = Line(9, out var res);
            var engine = new EventEngine(story, res, new MapGraph(story, map), 1);
            GameOverInfo? over = null;
            engine.OnGameOver += i => over = i;

            Assert.AreEqual("start", engine.Current.Id, "journey starts at the map start node");

            engine.Resolve(ChoiceSide.Left);            // node choice applies (hp 9->8)
            engine.EnterNode("mid");                    // travel along the map
            Assert.AreEqual("mid", engine.Current.Id);
            Assert.AreEqual(GameStatus.Running, engine.Status);

            engine.Resolve(ChoiceSide.Left);            // hp 8->7
            engine.EnterNode("goal");                   // reach the goal -> victory

            Assert.AreEqual(GameStatus.GameOver, engine.Status);
            Assert.IsTrue(over.HasValue);
            Assert.AreEqual(GameOverReason.ReachedGoal, over.Value.Reason, "reaching the goal is a WIN, not a loss");
            Assert.AreEqual("You reached safe haven.", over.Value.Text, "win uses the reachedGoal ending text");
        }

        [Test]
        public void Journey_ResourceBreak_IsLoss_NotWin()
        {
            var (story, map) = Line(1, out var res);     // hp starts at 1
            var engine = new EventEngine(story, res, new MapGraph(story, map), 1);
            GameOverInfo? over = null;
            engine.OnGameOver += i => over = i;

            engine.Resolve(ChoiceSide.Left);             // hp 1->0 -> breaks (same check as Reigns)

            Assert.AreEqual(GameStatus.GameOver, engine.Status);
            Assert.AreEqual(GameOverReason.ResourceBroken, over.Value.Reason, "a journey still loses on a broken resource");
        }

        [Test]
        public void Journey_SaveResume_RestoresMapPosition_AndStillWins()
        {
            var (story, map) = Line(9, out var res);
            var engine = new EventEngine(story, res, new MapGraph(story, map), 1);
            engine.Resolve(ChoiceSide.Left);
            engine.EnterNode("mid");                     // position is now "mid", stored in the opaque bag

            var loaded = SaveSystem.Deserialize(SaveSystem.Serialize(engine.State));
            Assert.IsNotNull(loaded);
            StringAssert.Contains("mid", loaded.MapState, "map position round-trips in the opaque MapState bag");

            // resume with a FRESH MapGraph - re-entry restores position via EnterNode (ספק 14.5).
            var resumed = EventEngine.Resume(story, res, new MapGraph(story, map), loaded);
            Assert.IsNotNull(resumed);
            Assert.AreEqual("mid", resumed.Current.Id, "resume re-enters the saved map node");

            GameOverInfo? over = null;
            resumed.OnGameOver += i => over = i;
            resumed.Resolve(ChoiceSide.Left);
            resumed.EnterNode("goal");
            Assert.AreEqual(GameOverReason.ReachedGoal, over.Value.Reason, "the resumed journey still wins at the goal");
        }

        [Test]
        public void MapLoader_Parses_Edges_And_Goal()
        {
            const string json = "{ \"startNodeId\":\"a\", \"goalNodeId\":\"c\", \"edges\": { \"a\":[\"b\"], \"b\":[\"c\"] } }";
            var map = MapLoader.Parse(json);
            Assert.AreEqual("a", map.StartNodeId);
            Assert.AreEqual("c", map.GoalNodeId);
            Assert.AreEqual("b", map.Neighbors("a")[0]);
            Assert.AreEqual("c", map.Neighbors("b")[0]);
            Assert.AreEqual(0, map.Neighbors("c").Count, "goal has no outgoing edges");
        }

        [Test]
        public void StoryLoader_Parses_ReachedGoal_Ending()
        {
            const string json = "{ \"startNodeId\":\"a\", \"nodes\":[], \"endings\":[ { \"when\":{\"reachedGoal\":true}, \"text\":\"home\" } ] }";
            var story = StoryLoader.Parse(json);
            Assert.AreEqual(1, story.Endings.Count);
            Assert.IsTrue(story.Endings[0].ReachedGoal);
            Assert.AreEqual("home", story.Endings[0].Text);
        }

        private static bool HasError(System.Collections.Generic.List<ValidationIssue> issues, string contains) =>
            issues.Exists(i => i.Severity == IssueSeverity.Error && i.Message.Contains(contains));

        [Test]
        public void MapValidator_GoodMap_NoErrors()
        {
            var (story, map) = Line(9, out _);
            Assert.IsFalse(MapValidator.Validate(story, map).Exists(i => i.Severity == IssueSeverity.Error));
        }

        [Test]
        public void MapValidator_EdgeToMissingNode_Error()
        {
            var (story, map) = Line(9, out _);
            map.Edges["mid"] = new List<string> { "ghost" };   // 'ghost' אינו צומת ב-story
            Assert.IsTrue(HasError(MapValidator.Validate(story, map), "ghost"));
        }

        [Test]
        public void MapValidator_UnreachableGoal_Error()
        {
            var (story, map) = Line(9, out _);
            map.Edges.Remove("mid");                           // אין דרך מ-mid ל-goal
            Assert.IsTrue(HasError(MapValidator.Validate(story, map), "not reachable"));
        }

        [Test]
        public void MapValidator_NoReachedGoalEnding_Warning()
        {
            var (story, map) = Line(9, out _);
            story.Endings.RemoveAll(e => e.ReachedGoal);
            Assert.IsTrue(MapValidator.Validate(story, map)
                .Exists(i => i.Severity == IssueSeverity.Warning && i.Message.Contains("reachedGoal")));
        }

        [Test]
        public void Journey_DeadEnd_EndsControlled_NotStuck()
        {
            var story = TestData.Story("start",
                TestData.Node("start", ("hp", -1)),
                TestData.Node("dead", ("hp", -1)),
                TestData.Node("goal", ("hp", -1)));
            story.Endings.Add(new Ending { ReachedGoal = true, Text = "win" });
            var map = new MapData { StartNodeId = "start", GoalNodeId = "goal" };
            map.Edges["start"] = new List<string> { "dead" };   // 'dead' has no outgoing edges, and isn't the goal

            var engine = new EventEngine(story, TestData.Resources(("hp", 9)), new MapGraph(story, map), 1);
            GameOverInfo? over = null;
            engine.OnGameOver += i => over = i;

            engine.Resolve(ChoiceSide.Left);
            engine.EnterNode("dead");                           // מבוי-סתום שאינו היעד

            Assert.AreEqual(GameStatus.GameOver, engine.Status);
            Assert.AreEqual(GameOverReason.NoMoreEvents, over.Value.Reason, "a journey dead-end ends controlled, not a stuck map");
        }
    }
}
