using NUnit.Framework;
using Crossroads.Engine;

namespace Crossroads.Engine.Tests
{
    // The MaxTurns win condition (spec 7.5) + branching survival endings. Decisions with no resource
    // effect never break a meter, so the only terminal here is reaching the turn cap.
    public sealed class WinConditionTests
    {
        private static EventEngine Run(StoryData story, ResourceSet res, int turns, out GameOverInfo info)
        {
            var engine = new EventEngine(story, res, new Deck(story), 1);
            GameOverInfo captured = default;
            engine.OnGameOver += i => captured = i;
            for (int i = 0; i < turns && engine.Status == GameStatus.Running; i++)
            {
                engine.Resolve(ChoiceSide.Left);
                if (engine.Status == GameStatus.Running) engine.Advance();
            }
            info = captured;
            return engine;
        }

        [Test]
        public void Survives_To_MaxTurns_Wins_WithFallbackText()
        {
            var story = TestData.Story("A", TestData.Node("A", ("e", 0)), TestData.Node("B", ("e", 0)));
            story.MaxTurns = 3;

            var engine = Run(story, TestData.Resources(("e", 5)), 3, out var info);

            Assert.AreEqual(GameStatus.GameOver, engine.Status);
            Assert.AreEqual(GameOverReason.Survived, info.Reason);
            Assert.AreEqual(3, engine.State.Turn, "the win fires exactly at the turn cap");
            Assert.AreEqual("end", info.Text, "no matching flag -> fallback survival text");
        }

        [Test]
        public void Survival_Ending_Branches_On_Flag()
        {
            var a = TestData.Node("A", ("e", 0));
            a.Choices[0].SetFlags["crowned"] = true;   // left choice sets the flag
            var story = TestData.Story("A", a, TestData.Node("B", ("e", 0)));
            story.MaxTurns = 2;
            story.Endings.Insert(0, new Ending { Flag = "crowned", FlagIs = true, Text = "you took the crown" });

            var engine = Run(story, TestData.Resources(("e", 5)), 2, out var info);

            Assert.AreEqual(GameOverReason.Survived, info.Reason);
            Assert.AreEqual("you took the crown", info.Text, "a set flag selects its survival ending over the fallback");
        }

        [Test]
        public void NoMaxTurns_DoesNotEndOnTurnCount()
        {
            var story = TestData.Story("A", TestData.Node("A", ("e", 0)), TestData.Node("B", ("e", 0)));
            // MaxTurns defaults to 0 - unbounded; harmless decisions never end the run.
            var engine = Run(story, TestData.Resources(("e", 5)), 50, out _);
            Assert.AreEqual(GameStatus.Running, engine.Status, "with no turn cap and no breaks the run keeps going");
        }
    }
}
