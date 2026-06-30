using NUnit.Framework;
using Crossroads.Engine;

namespace Crossroads.Engine.Tests
{
    // round-trip של שמירה (M5) דרך Serialize/Deserialize בלבד (בלי דיסק).
    public sealed class SaveSystemTests
    {
        [Test]
        public void Serialize_Then_Deserialize_RoundTrips()
        {
            var state = new GameState
            {
                SchemaVersion = SaveSystem.CurrentSchemaVersion,
                CurrentNodeId = "card_07",
                Rng = new RngState(123456789, 17)
            };
            state.Resources["sleep"] = 4;
            state.Resources["money"] = 2;
            state.Flags["has_nanny"] = true;

            string json = SaveSystem.Serialize(state);
            GameState loaded = SaveSystem.Deserialize(json);

            Assert.IsNotNull(loaded);
            Assert.AreEqual("card_07", loaded.CurrentNodeId);
            Assert.AreEqual(4, loaded.GetResource("sleep"));
            Assert.AreEqual(2, loaded.GetResource("money"));
            Assert.IsTrue(loaded.GetFlag("has_nanny"));
            Assert.AreEqual(17, loaded.Rng.DrawCount, "full RNG state must round-trip (M5)");
        }

        [Test]
        public void Deserialize_SchemaMismatch_ReturnsNull()
        {
            string badSchema = "{\"schemaVersion\": 999, \"currentNodeId\": \"x\"}";
            Assert.IsNull(SaveSystem.Deserialize(badSchema), "mismatched schema must reject cleanly (R4)");
        }

        [Test]
        public void Deserialize_Garbage_ReturnsNull()
        {
            Assert.IsNull(SaveSystem.Deserialize("not json"));
            Assert.IsNull(SaveSystem.Deserialize(""));
        }

        // כתיבה אטומית (temp+rename) וקריאה בפועל מהדיסק (ספק 15.3). מנוקה ב-finally כדי לא
        // להשאיר save.json. Load כשאין קובץ -> null (נתיב "אין שמירה" שה-bootstrap נשען עליו).
        [Test]
        public void Save_Then_Load_RoundTripsViaDisk()
        {
            try
            {
                SaveSystem.Delete();
                Assert.IsNull(SaveSystem.Load(), "no file yet -> null (fresh run)");

                var state = new GameState { SchemaVersion = SaveSystem.CurrentSchemaVersion, CurrentNodeId = "card_03" };
                state.Resources["energy"] = 5;
                SaveSystem.Save(state);

                var loaded = SaveSystem.Load();
                Assert.IsNotNull(loaded);
                Assert.AreEqual("card_03", loaded.CurrentNodeId);
                Assert.AreEqual(5, loaded.GetResource("energy"));

                SaveSystem.Delete();
                Assert.IsNull(SaveSystem.Load(), "after Delete -> null");
            }
            finally
            {
                SaveSystem.Delete();
            }
        }
    }
}
