using System.Linq;
using NUnit.Framework;
using UnityEditor.Compilation;
using Crossroads.Engine;

namespace Crossroads.Engine.Tests
{
    // מבחני-קבלה ארכיטקטוניים (ספק 15.6). אוכפים את הגבולות אוטומטית כך ש-M7 נשמר בהמשך.
    public sealed class ArchitectureTests
    {
        // J-ARCH-1 (M7): Crossroads.Engine לא מפנה ל-UI/Games. הגבול נאכף קומפילטורית.
        [Test]
        public void Engine_DoesNotReference_UI_Or_Games()
        {
            var asms = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            var engine = asms.FirstOrDefault(a => a.name == "Crossroads.Engine");
            Assert.IsNotNull(engine, "Crossroads.Engine assembly not found");

            var refNames = engine.assemblyReferences.Select(r => r.name).ToList();
            Assert.IsFalse(refNames.Contains("Crossroads.UI"),
                "Engine must not reference Crossroads.UI (M7)");
            Assert.IsFalse(refNames.Any(n => n.StartsWith("Crossroads.Game")),
                "Engine must not reference any Game assembly (M7)");
        }

        // J-ARCH-2 (M6/M7): כל המשחקים (כולל הקלונים) מפנים ל-Engine+UI ולא להפך (תלות חד-כיוונית),
        // ולאף משחק אין קוד Engine/UI משלו - רק ה-bootstrap הדק. גנרי: מכסה כל Crossroads.Game.* קיים.
        [Test]
        public void Games_HaveOnlyBootstrap_And_EngineUI_NeverReferenceGames()
        {
            var asms = CompilationPipeline.GetAssemblies(AssembliesType.Player);
            var games = asms.Where(a => a.name.StartsWith("Crossroads.Game.")).ToList();

            Assert.GreaterOrEqual(games.Count, 2,
                "at least two real games (clones) must exist to prove cloning - found: " +
                string.Join(", ", games.Select(g => g.name)));

            foreach (var g in games)
            {
                // המשחק תלוי ב-Engine+UI, לא הם בו.
                var refs = g.assemblyReferences.Select(r => r.name).ToList();
                Assert.Contains("Crossroads.Engine", refs, g.name + " must depend on Engine");
                Assert.Contains("Crossroads.UI", refs, g.name + " must depend on UI");

                // אין קוד משלו מלבד ה-bootstrap (אין העתקת לוגיקת Engine/UI לתוך המשחק).
                var srcs = g.sourceFiles.Select(f => f.Replace('\\', '/')).ToList();
                Assert.AreEqual(1, srcs.Count, g.name + " must contain exactly one source file: " + string.Join(", ", srcs));
                Assert.IsTrue(srcs[0].EndsWith("/GameBootstrap.cs"), g.name + "'s only code must be GameBootstrap.cs, got " + srcs[0]);
            }

            // אף משחק לא נמצא ב-references של Engine או UI.
            foreach (var name in new[] { "Crossroads.Engine", "Crossroads.UI" })
            {
                var layer = asms.FirstOrDefault(a => a.name == name);
                Assert.IsNotNull(layer, name + " assembly not found");
                Assert.IsFalse(layer.assemblyReferences.Any(r => r.name.StartsWith("Crossroads.Game")),
                    name + " must not reference any Game assembly (one-directional dependency, M7)");
            }
        }

        // J-ARCH-3 (G2): המנוע אגנוסטי למקור. source מזויף מוכיח מוכנות-מסע בלי לבנות מפה,
        // ובמפורש את ה-gap בין Resolve (החלה) ל-Advance (קידום) - ספק 12.4.
        [Test]
        public void Resolve_AppliesChoice_ButDoesNotAdvance_UntilAdvance()
        {
            var nodeA = TestData.Node("A", ("e", -1));
            var nodeB = TestData.Node("B", ("e", -1));
            var nodeC = TestData.Node("C", ("e", -1));
            var fake = new FakeSource { Enter = nodeA, Next = nodeC };  // NextEvent יחזיר C
            var engine = new EventEngine(TestData.Story("A", nodeA, nodeB, nodeC),
                TestData.Resources(("e", 5)), fake, seed: 1);

            Assert.AreEqual("A", engine.Current.Id);

            engine.Resolve(ChoiceSide.Left);
            Assert.AreEqual(4, engine.State.GetResource("e"), "Resolve must apply effects");
            Assert.AreEqual("A", engine.Current.Id, "Resolve must NOT advance Current (the gap)");

            engine.Advance();
            Assert.AreEqual("C", engine.Current.Id, "Advance pulls next from the source");
        }

        // ניתוב next מפורש גובר על בחירת ה-source (ספק 12.4).
        [Test]
        public void ExplicitNext_Overrides_SourceSelection()
        {
            var nodeA = TestData.NodeWithNext("A", next: "B");
            var nodeB = TestData.Node("B", ("e", -1));
            var nodeC = TestData.Node("C", ("e", -1));
            var fake = new FakeSource { Enter = nodeA, Next = nodeC, ById = { ["B"] = nodeB } };
            var engine = new EventEngine(TestData.Story("A", nodeA, nodeB, nodeC),
                TestData.Resources(("e", 5)), fake, seed: 1);

            engine.Resolve(ChoiceSide.Right);   // הבחירה הימנית מגדירה next=B
            engine.Advance();
            Assert.AreEqual("B", engine.Current.Id, "explicit next must override source.NextEvent (C)");
        }

        private sealed class FakeSource : IEventSource
        {
            public EventNode Enter;
            public EventNode Next;
            public System.Collections.Generic.Dictionary<string, EventNode> ById = new();
            public EventNode NextEvent(GameState s) => Next;
            public EventNode EnterNode(GameState s, string id) => ById.TryGetValue(id, out var n) ? n : Enter;
            public bool HasNext(GameState s) => true;
        }
    }
}
