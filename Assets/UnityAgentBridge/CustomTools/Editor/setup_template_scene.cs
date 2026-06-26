using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityAgentBridge.Editor;
using Crossroads.Engine;
using Crossroads.UI;
using Crossroads.Game.Template;

namespace UnityAgentBridge.Editor.CustomTools
{
    // One-time scene wiring for the _Template scene: creates resources.asset (ScriptableObject),
    // adds + wires CardView / ResourceBarView / SwipeInput / GameBootstrap, and previews the first
    // card in edit mode. Done in C# because the generic bridge tools cannot author a ScriptableObject
    // or set object/asset reference fields (see docs/bridge-wishlist.md).
    public static class setup_template_scene
    {
        [McpTool("setup_template_scene", "Wire GameBootstrap + CardView into the active scene and preview the first card")]
        public static object Invoke()
        {
            // 1. resources.asset (calm + energy, matching _Template/Content/story.json)
            const string resPath = "Assets/Games/_Template/Content/resources.asset";
            var resources = AssetDatabase.LoadAssetAtPath<ResourceSet>(resPath);
            if (resources == null)
            {
                resources = ScriptableObject.CreateInstance<ResourceSet>();
                resources.resources = new[]
                {
                    new ResourceDef { id = "energy", displayName = "Energy", min = 0, max = 10, start = 6, breakOn = BreakOn.Both, dangerBand = 2 },
                    new ResourceDef { id = "calm",   displayName = "Calm",   min = 0, max = 10, start = 6, breakOn = BreakOn.Both, dangerBand = 2 }
                };
                AssetDatabase.CreateAsset(resources, resPath);
                AssetDatabase.SaveAssets();
            }

            var story = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Games/_Template/Content/story.json");
            if (story == null) throw new System.Exception("story.json not found at Assets/Games/_Template/Content/story.json");

            // 2. scene objects
            var canvas = GameObject.Find("Canvas");
            var card = GameObject.Find("Canvas/Card");
            var label = GameObject.Find("Canvas/Card/Label");
            if (canvas == null || card == null || label == null)
                throw new System.Exception("expected Canvas/Card/Label in the active scene (run build_reigns_frame first)");

            // 3. CardView on Card, wired to the existing Image + Label
            var cardView = card.GetComponent<CardView>();
            if (cardView == null) cardView = card.AddComponent<CardView>();
            var cvSo = new SerializedObject(cardView);
            cvSo.FindProperty("bodyText").objectReferenceValue = label.GetComponent<Text>();
            cvSo.FindProperty("cardBackground").objectReferenceValue = card.GetComponent<Image>();
            cvSo.ApplyModifiedProperties();

            var bar = canvas.GetComponent<ResourceBarView>();
            if (bar == null) bar = canvas.AddComponent<ResourceBarView>();
            var swipe = canvas.GetComponent<SwipeInput>();
            if (swipe == null) swipe = canvas.AddComponent<SwipeInput>();
            var end = canvas.GetComponent<EndScreen>();
            if (end == null) end = canvas.AddComponent<EndScreen>();

            // 4. GameBootstrap on a Game object, fully wired
            var gameGo = GameObject.Find("Game");
            if (gameGo == null) gameGo = new GameObject("Game");
            var boot = gameGo.GetComponent<GameBootstrap>();
            if (boot == null) boot = gameGo.AddComponent<GameBootstrap>();
            var bSo = new SerializedObject(boot);
            bSo.FindProperty("storyJson").objectReferenceValue = story;
            bSo.FindProperty("resources").objectReferenceValue = resources;
            bSo.FindProperty("cardView").objectReferenceValue = cardView;
            bSo.FindProperty("resourceBar").objectReferenceValue = bar;
            bSo.FindProperty("swipeInput").objectReferenceValue = swipe;
            bSo.FindProperty("endScreen").objectReferenceValue = end;
            bSo.ApplyModifiedProperties();

            // 5. edit-mode preview: run the real engine once and bind the first card (so a
            //    screenshot shows real story content driving the UI, without entering play mode)
            var storyData = StoryLoader.Parse(story.text);
            var issues = StoryValidator.Validate(storyData, resources);
            var engine = new EventEngine(storyData, resources, new Deck(storyData), 12345);
            cardView.Bind(ViewMapper.BuildNodeView(engine.Current), null);

            EditorUtility.SetDirty(cardView);
            EditorUtility.SetDirty(boot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            return new
            {
                ok = true,
                resourcesCreated = resources.resources.Length,
                validationIssues = issues.Count,
                firstNode = engine.Current != null ? engine.Current.Id : null,
                firstBody = engine.Current != null ? engine.Current.Body : null
            };
        }
    }
}
