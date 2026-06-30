using System.Collections.Generic;
using NUnit.Framework;
using Crossroads.Engine;

namespace Crossroads.Engine.Tests
{
    // בחירה משוקללת + RNG דטרמיניסטי (ספק 14.1/14.5, פריטי Phase 1 השמורים).
    public sealed class RngTests
    {
        private static EventNode NodeW(string id, int weight)
        {
            var n = TestData.Node(id, ("e", -1));
            n.Weight = weight;
            return n;
        }

        // מקדם N תורים ומחזיר את רצף ה-id של הקלפים שנבחרו.
        private static List<string> Drive(EventEngine e, int steps)
        {
            var seq = new List<string>();
            for (int i = 0; i < steps && e.Status == GameStatus.Running; i++)
            {
                e.Resolve(ChoiceSide.Left);
                if (e.Status == GameStatus.Running) e.Advance();
                seq.Add(e.Current != null ? e.Current.Id : "<none>");
            }
            return seq;
        }

        [Test]
        public void Rng_SameSeedAndDrawCount_SameSequence()
        {
            var a = new DeterministicRng(42);
            var b = new DeterministicRng(42);
            for (int i = 0; i < 8; i++) Assert.AreEqual(a.NextDouble(), b.NextDouble());
        }

        [Test]
        public void Rng_FastForward_MatchesSequential()
        {
            var seq = new DeterministicRng(7);
            seq.NextDouble(); seq.NextDouble(); seq.NextDouble();   // advance to drawCount 3
            double sequential = seq.NextDouble();                   // the 4th value

            var ff = new DeterministicRng(7, 3);                    // fast-forwarded straight to 3
            Assert.AreEqual(3, new DeterministicRng(7, 3).DrawCount);
            Assert.AreEqual(sequential, ff.NextDouble(), "fast-forward to N gives the same next value as N sequential draws");
        }

        [Test]
        public void Deck_SameSeed_ProducesSameCardSequence()
        {
            var story = TestData.Story("A", TestData.Node("A", ("e", -1)),
                TestData.Node("B", ("e", -1)), TestData.Node("C", ("e", -1)));

            var e1 = new EventEngine(story, TestData.Resources(("e", 9)), new Deck(story), 12345);
            var e2 = new EventEngine(story, TestData.Resources(("e", 9)), new Deck(story), 12345);

            Assert.AreEqual(Drive(e1, 7), Drive(e2, 7), "same seed -> identical card sequence (determinism)");
        }

        [Test]
        public void Deck_SaveResume_ContinuesIdenticalSequence()
        {
            var story = TestData.Story("A", TestData.Node("A", ("e", -1)),
                TestData.Node("B", ("e", -1)), TestData.Node("C", ("e", -1)));
            var res = TestData.Resources(("e", 9));

            var e1 = new EventEngine(story, res, new Deck(story), 999);
            Drive(e1, 3);                                   // advance partway
            var mid = SaveSystem.Deserialize(SaveSystem.Serialize(e1.State));

            var continuationDirect = Drive(e1, 4);          // keep going on the original
            var resumed = EventEngine.Resume(story, res, new Deck(story), mid);
            var continuationResumed = Drive(resumed, 4);    // continue on a fresh deck after fast-forward

            Assert.AreEqual(continuationDirect, continuationResumed,
                "RNG drawCount round-trips: a resumed run continues the exact same card sequence (M5)");
        }

        [Test]
        public void Deck_WeightedSelection_FavorsHigherWeight()
        {
            // מ-S התקפים הם [Heavy(9), Light(1)]. על פני הרבה זרעים, Heavy צריך להיבחר הרבה יותר.
            var story = TestData.Story("S", NodeW("S", 1), NodeW("Heavy", 9), NodeW("Light", 1));
            int heavy = 0, light = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var e = new EventEngine(story, TestData.Resources(("e", 9)), new Deck(story), seed);
                e.Resolve(ChoiceSide.Left);
                e.Advance();
                if (e.Current.Id == "Heavy") heavy++;
                else if (e.Current.Id == "Light") light++;
            }
            Assert.Greater(heavy, light * 2, $"weight 9 should be picked far more than weight 1 (heavy={heavy}, light={light})");
        }
    }
}
