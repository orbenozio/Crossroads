using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Crossroads.Engine;

namespace Crossroads.Engine.Tests
{
    // ולידציה של דאטה (J9 / M9, ספק 15.2). מכסה את שלושת סוגי-השגיאה + warning של flag יתום.
    public sealed class StoryValidatorTests
    {
        private static ResourceSet Res() => TestData.Resources(("energy", 6), ("calm", 6));

        private static EventNode TwoChoiceNode(string id)
        {
            var n = new EventNode { Id = id, Body = id };
            n.Choices.Add(new Choice { Side = ChoiceSide.Left, Label = "L" });
            n.Choices.Add(new Choice { Side = ChoiceSide.Right, Label = "R" });
            return n;
        }

        private static StoryData Story(EventNode n, string start = "a")
        {
            var s = new StoryData { StartNodeId = start };
            s.Nodes.Add(n);
            s.Endings.Add(new Ending { Fallback = true, Text = "end" });
            return s;
        }

        [Test]
        public void ValidStory_NoErrors()
        {
            var n = TwoChoiceNode("a");
            n.Choices[0].Effects.Add(new ResourceEffect { ResourceId = "energy", Delta = -1 });
            var issues = StoryValidator.Validate(Story(n), Res());
            Assert.IsFalse(issues.Exists(i => i.Severity == IssueSeverity.Error), Dump(issues));
        }

        [Test]
        public void CardWithoutTwoChoices_Error()
        {
            var n = new EventNode { Id = "a", Body = "a" };
            n.Choices.Add(new Choice { Side = ChoiceSide.Left, Label = "only" });
            var issues = StoryValidator.Validate(Story(n), Res());
            Assert.IsTrue(issues.Exists(i => i.Severity == IssueSeverity.Error && i.Message.Contains("2 choices")), Dump(issues));
        }

        [Test]
        public void EffectOnUndefinedResource_Error()
        {
            var n = TwoChoiceNode("a");
            n.Choices[0].Effects.Add(new ResourceEffect { ResourceId = "ghost", Delta = 1 });
            var issues = StoryValidator.Validate(Story(n), Res());
            Assert.IsTrue(issues.Exists(i => i.Severity == IssueSeverity.Error && i.Message.Contains("ghost")), Dump(issues));
        }

        [Test]
        public void FlagReadButNeverSet_Warning()
        {
            var n = TwoChoiceNode("a");
            n.AppearWhen = new Condition();
            n.AppearWhen.AllOf.Add(new ConditionClause { Kind = ConditionKind.Flag, Flag = "ghost_flag", FlagIs = true });
            var issues = StoryValidator.Validate(Story(n), Res());
            Assert.IsTrue(issues.Exists(i => i.Severity == IssueSeverity.Warning && i.Message.Contains("ghost_flag")), Dump(issues));
        }

        [Test]
        public void NoFallbackEnding_Warning()
        {
            var n = TwoChoiceNode("a");
            var s = new StoryData { StartNodeId = "a" };
            s.Nodes.Add(n);
            // אין endings כלל -> אין fallback
            var issues = StoryValidator.Validate(s, Res());
            Assert.IsTrue(issues.Exists(i => i.Severity == IssueSeverity.Warning && i.Message.Contains("fallback ending")), Dump(issues));
        }

        [Test]
        public void WithFallbackEnding_NoFallbackWarning()
        {
            var n = TwoChoiceNode("a");
            var issues = StoryValidator.Validate(Story(n), Res()); // Story() כבר מוסיף ending fallback
            Assert.IsFalse(issues.Exists(i => i.Message.Contains("fallback ending")), Dump(issues));
        }

        [Test]
        public void MissingStartNode_Error()
        {
            var issues = StoryValidator.Validate(Story(TwoChoiceNode("a"), start: "nope"), Res());
            Assert.IsTrue(issues.Exists(i => i.Severity == IssueSeverity.Error && i.Message.Contains("startNodeId")), Dump(issues));
        }

        private static string Dump(List<ValidationIssue> issues)
        {
            var sb = new StringBuilder();
            foreach (var i in issues) sb.AppendLine(i.ToString());
            return sb.ToString();
        }
    }
}
