using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // המסע האמיתי (RefugeeRoad) מקצה-לקצה דרך אותו EventEngine + MapGraph: טוען story.json + map.json
    // האמיתיים מהדיסק, מנווט לאורך המפה עד היעד, ומאמת ניצחון - בלי UI (מסכי-מפה הם LATER §9.6).
    public sealed class RefugeeRoadContentTests
    {
        private const string Dir = "Assets/Games/RefugeeRoad/Content/";

        [Test]
        public void RefugeeRoad_FullJourney_ReachesHaven_AndWins()
        {
            var storyAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Dir + "story.json");
            var mapAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Dir + "map.json");
            var resources = AssetDatabase.LoadAssetAtPath<ResourceSet>(Dir + "resources.asset");
            Assert.IsNotNull(storyAsset, "RefugeeRoad story.json missing");
            Assert.IsNotNull(mapAsset, "RefugeeRoad map.json missing");
            Assert.IsNotNull(resources, "RefugeeRoad resources.asset missing");

            var story = StoryLoader.Parse(storyAsset.text);
            var map = MapLoader.Parse(mapAsset.text);
            Assert.IsEmpty(System.Linq.Enumerable.Where(StoryValidator.Validate(story, resources),
                i => i.Severity == IssueSeverity.Error), "RefugeeRoad content must validate clean");

            var engine = new EventEngine(story, resources, new MapGraph(story, map), 1);
            GameOverInfo? over = null;
            engine.OnGameOver += i => over = i;

            Assert.AreEqual("border", engine.Current.Id, "journey starts at the map start");

            // נתיב חי לאורך המפה: border -> checkpoint -> forest -> town -> haven, בבחירות ששומרות משאבים.
            engine.Resolve(ChoiceSide.Left); engine.EnterNode("checkpoint");
            engine.Resolve(ChoiceSide.Left); engine.EnterNode("forest");
            engine.Resolve(ChoiceSide.Left); engine.EnterNode("town");
            engine.Resolve(ChoiceSide.Left); engine.EnterNode("haven");

            Assert.AreEqual(GameStatus.GameOver, engine.Status);
            Assert.IsTrue(over.HasValue && over.Value.Reason == GameOverReason.ReachedGoal,
                "reaching haven is a victory");
            StringAssert.Contains("made it", over.Value.Text, "win shows the reachedGoal ending text");
        }
    }
}
