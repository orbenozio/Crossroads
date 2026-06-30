using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Crossroads.Engine
{
    // שמירה/טעינה של GameState (ספק 14.5). JSON דרך Json.NET, כתיבה אטומית ל-persistentDataPath.
    // Serialize/Deserialize מופרדים מ-I/O כדי לאפשר round-trip-test בלי דיסק (M5).
    public static class SaveSystem
    {
        public const int CurrentSchemaVersion = 1;
        private const string FileName = "save.json";

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static string Serialize(GameState state) => JsonConvert.SerializeObject(state, Settings);

        // טעינה בטוחה: דאטה פגום/לא-תואם-schema -> null (מטופל כ"אין שמירה", ספק 10.7) ולא זורק.
        public static GameState Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var state = JsonConvert.DeserializeObject<GameState>(json, Settings);
                if (state == null || state.SchemaVersion != CurrentSchemaVersion) return null;
                return state;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // כתיבה אטומית: temp ואז rename, כדי שקריסה באמצע לא תשאיר קובץ חצי (ספק 15.3).
        public static void Save(GameState state)
        {
            string path = SavePath;
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, Serialize(state));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static GameState Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                return Deserialize(File.ReadAllText(SavePath));
            }
            catch (IOException)
            {
                return null;
            }
        }

        public static void Delete()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); }
            catch (IOException) { }
        }
    }
}
