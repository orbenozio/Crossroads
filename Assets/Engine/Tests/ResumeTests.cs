using NUnit.Framework;
using Crossroads.Engine;

namespace Crossroads.Engine.Tests
{
    // המשך ריצה מ-GameState טעון (J4/M5). נבדק ברמת ה-Engine דרך round-trip של SaveSystem
    // (Serialize/Deserialize בלבד, בלי דיסק) - כך שהבדיקות דטרמיניסטיות ולא נוגעות בקבצים.
    public sealed class ResumeTests
    {
        // story A,B: כל בחירה מורידה e ב-1. start A.
        private static StoryData TwoCardStory() =>
            TestData.Story("A", TestData.Node("A", ("e", -1)), TestData.Node("B", ("e", -1)));

        [Test]
        public void Resume_RestoresResourcesAndCurrentNode()
        {
            var story = TwoCardStory();
            var res = TestData.Resources(("e", 3));
            var deck = new Deck(story);

            var fresh = new EventEngine(story, res, deck, 1);
            fresh.Resolve(ChoiceSide.Left);   // e: 3 -> 2
            fresh.Advance();                  // current: A -> B
            Assert.AreEqual("B", fresh.Current.Id);

            // שמירה -> טעינה (round-trip דרך JSON, כמו בלולאה האמיתית)
            var loaded = SaveSystem.Deserialize(SaveSystem.Serialize(fresh.State));
            var resumed = EventEngine.Resume(story, res, new Deck(story), loaded);

            Assert.IsNotNull(resumed);
            Assert.AreEqual("B", resumed.Current.Id, "must re-enter the saved node");
            Assert.AreEqual(2, resumed.State.GetResource("e"), "resources must carry over, not reset to start");
            Assert.AreEqual(GameStatus.Running, resumed.Status);
        }

        [Test]
        public void Resume_ContinuesLoop_AfterRestore()
        {
            var story = TwoCardStory();
            var res = TestData.Resources(("e", 3));

            var fresh = new EventEngine(story, res, new Deck(story), 1);
            fresh.Resolve(ChoiceSide.Left);
            fresh.Advance();

            var resumed = EventEngine.Resume(story, res, new Deck(story),
                SaveSystem.Deserialize(SaveSystem.Serialize(fresh.State)));

            resumed.Resolve(ChoiceSide.Left); // e: 2 -> 1
            resumed.Advance();
            Assert.AreEqual(1, resumed.State.GetResource("e"));
            Assert.AreEqual(GameStatus.Running, resumed.Status);
            Assert.IsNotNull(resumed.Current);
        }

        [Test]
        public void Resume_BackfillsNewResource_FromStart()
        {
            var story = TwoCardStory();

            // שמירה נוצרה כשהיה רק משאב e
            var fresh = new EventEngine(story, TestData.Resources(("e", 3)), new Deck(story), 1);
            fresh.Resolve(ChoiceSide.Left);   // e: 3 -> 2
            fresh.Advance();
            var loaded = SaveSystem.Deserialize(SaveSystem.Serialize(fresh.State));
            Assert.AreEqual(0, loaded.GetResource("m"), "saved state has no 'm' yet");

            // התוכן גדל: נוסף מד m עם start=5. resume חייב לאתחל אותו ל-start, לא ל-0.
            var resNew = TestData.Resources(("e", 3), ("m", 5));
            var resumed = EventEngine.Resume(story, resNew, new Deck(story), loaded);

            Assert.IsNotNull(resumed);
            Assert.AreEqual(2, resumed.State.GetResource("e"), "existing resource preserved");
            Assert.AreEqual(5, resumed.State.GetResource("m"), "new resource backfilled from start, not 0");
        }

        [Test]
        public void Resume_NullState_ReturnsNull()
        {
            var story = TwoCardStory();
            // הקורא (GameBootstrap) נופל חזרה לריצה טרייה כש-Resume מחזיר null (אין שמירה).
            Assert.IsNull(EventEngine.Resume(story, TestData.Resources(("e", 3)), new Deck(story), null));
        }

        [Test]
        public void Resume_UnknownNode_ReturnsNull()
        {
            var story = TwoCardStory();
            var ghost = new GameState { SchemaVersion = 1, CurrentNodeId = "does_not_exist" };
            ghost.Resources["e"] = 2;

            // צומת שמור שכבר לא קיים בתוכן (תוכן השתנה) -> null, לא קריסה (ספק 10.7).
            Assert.IsNull(EventEngine.Resume(story, TestData.Resources(("e", 3)), new Deck(story), ghost));
        }
    }
}
