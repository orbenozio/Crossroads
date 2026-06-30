using NUnit.Framework;
using Crossroads.Engine;

namespace Crossroads.Engine.Tests
{
    // לולאת ה-Engine המינימלית (Phase 0b exit): Resolve+Advance, preview pure, סיום בשבירת-משאב.
    public sealed class EngineLoopTests
    {
        private EventEngine NewEngine()
        {
            var a = TestData.Node("A", ("e", -1));
            var b = TestData.Node("B", ("e", -1));
            return new EventEngine(TestData.Story("A", a, b), TestData.Resources(("e", 3)), new Deck(TestData.Story("A", a, b)), 1);
        }

        [Test]
        public void Preview_IsPure_DoesNotMutateState()
        {
            var engine = NewEngine();
            int before = engine.State.GetResource("e");
            var preview = engine.Preview(ChoiceSide.Left);
            Assert.AreEqual(before, engine.State.GetResource("e"), "Preview must not mutate state");
            Assert.AreEqual(1, preview.Deltas.Count);
            Assert.AreEqual(-1, preview.Deltas[0].Delta);
        }

        [Test]
        public void Loop_ResolveThenAdvance_RunsAndChangesResource()
        {
            var engine = NewEngine();
            engine.Resolve(ChoiceSide.Left);
            Assert.AreEqual(2, engine.State.GetResource("e"));
            engine.Advance();
            Assert.AreEqual(GameStatus.Running, engine.Status);
            Assert.IsNotNull(engine.Current);
        }

        [Test]
        public void Resolve_AppliedDelta_IsClampedNotNominal()
        {
            // e start=2, min=0. בחירה -5 -> נחתך ל-0; ה-delta בפועל הוא -2, לא -5.
            var a = TestData.Node("A", ("e", -5));
            var story = TestData.Story("A", a);
            var engine = new EventEngine(story, TestData.Resources(("e", 2)), new Deck(story), 1);

            var result = engine.Resolve(ChoiceSide.Left);

            Assert.AreEqual(1, result.AppliedDeltas.Count);
            Assert.AreEqual(-2, result.AppliedDeltas[0].Delta, "applied delta must reflect clamping (2 -> 0), not the nominal -5");
            Assert.AreEqual(0, engine.State.GetResource("e"));
        }

        [Test]
        public void Resource_Break_EndsGame()
        {
            var a = TestData.Node("A", ("e", -3));
            var story = TestData.Story("A", a);
            var engine = new EventEngine(story, TestData.Resources(("e", 3)), new Deck(story), 1);

            bool gameOver = false;
            engine.OnGameOver += _ => gameOver = true;

            engine.Resolve(ChoiceSide.Left);   // e: 3 -> 0, נשבר (breakOn Both)
            Assert.AreEqual(GameStatus.GameOver, engine.Status);
            Assert.IsTrue(gameOver, "OnGameOver should fire on a broken resource");
        }
    }
}
