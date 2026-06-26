using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using Crossroads.Engine;
using Crossroads.UI;

namespace Crossroads.Engine.Tests
{
    // ולידציה של תוכן כל המשחקים (M9 על דאטה אמיתית, גנרי). מגלה כל ResourceSet תחת Assets/Games,
    // מאתר את story.json לידו ומריץ את אותו StoryValidator שרץ בזמן-טעינה. מכסה אוטומטית כל קלון חדש.
    public sealed class GamesContentTests
    {
        [Test]
        public void All_Game_Contents_Validate_NoErrors()
        {
            var guids = AssetDatabase.FindAssets("t:ResourceSet", new[] { "Assets/Games" });
            int validated = 0;
            var report = new StringBuilder();

            foreach (var guid in guids)
            {
                string resPath = AssetDatabase.GUIDToAssetPath(guid);
                string dir = Path.GetDirectoryName(resPath).Replace('\\', '/');
                string storyPath = dir + "/story.json";
                var storyAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(storyPath);
                if (storyAsset == null) continue; // ResourceSet בלי story.json לידו - לא משחק, דלג

                var resources = AssetDatabase.LoadAssetAtPath<ResourceSet>(resPath);
                var story = StoryLoader.Parse(storyAsset.text);
                var errors = StoryValidator.Validate(story, resources)
                    .Where(i => i.Severity == IssueSeverity.Error).ToList();

                validated++;
                if (errors.Count > 0)
                {
                    report.AppendLine(storyPath + ":");
                    foreach (var e in errors) report.AppendLine("  " + e);
                }
            }

            Assert.IsTrue(report.Length == 0, "game content has validation errors:\n" + report);
            Assert.GreaterOrEqual(validated, 2, "expected at least two real games' content to validate");
        }
    }
}
