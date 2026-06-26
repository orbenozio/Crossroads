using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityAgentBridge.Editor;
using Crossroads.Engine;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // כמו wire_game_scene אבל לפורמט המסע: מחווט bootstrap-מסע (story+map+resources) + MapView,
    // ומציג בתצוגה-מקדימה את הקלף או את המפה (showMap) ל-screenshot. Expects Canvas/Card/Label קיימים.
    public static class wire_journey_scene
    {
        [McpTool("wire_journey_scene", "Wire a journey GameBootstrap (story+map) + MapView into the scene; preview card or map")]
        public static object Invoke(string bootstrapType = "", string storyPath = "", string mapPath = "",
            string resourcesPath = "", string themePath = "", string title = "", string intro = "", bool showMap = false)
        {
            var bootType = ResolveType(bootstrapType) ?? throw new Exception("type not found: " + bootstrapType);
            var story = AssetDatabase.LoadAssetAtPath<TextAsset>(storyPath) ?? throw new Exception("story not found: " + storyPath);
            var mapAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(mapPath) ?? throw new Exception("map not found: " + mapPath);
            var resources = AssetDatabase.LoadAssetAtPath<ResourceSet>(resourcesPath) ?? throw new Exception("resources not found: " + resourcesPath);
            var theme = string.IsNullOrEmpty(themePath) ? null : AssetDatabase.LoadAssetAtPath<Theme>(themePath);

            var canvas = GameObject.Find("Canvas");
            var card = GameObject.Find("Canvas/Card");
            if (canvas == null || card == null)
                throw new Exception("expected Canvas + Canvas/Card in the active scene");

            UIFonts.RightToLeft = theme != null && theme.rightToLeft;
            wire_game_scene.CleanOldUi(canvas);   // מנקה UI פרוצדורלי ישן (legacy) למיגרציית TMP
            var legacy = card.transform.Find("Label");
            if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            var body = wire_game_scene.EnsureTmpBody(card);   // TMP body משותף (RTL לפי ה-Theme)

            var cardView = card.GetComponent<CardView>() ?? card.AddComponent<CardView>();
            var cvSo = new SerializedObject(cardView);
            cvSo.FindProperty("bodyText").objectReferenceValue = body;
            cvSo.FindProperty("cardBackground").objectReferenceValue = card.GetComponent<Image>();
            cvSo.ApplyModifiedProperties();

            var bar = canvas.GetComponent<ResourceBarView>() ?? canvas.AddComponent<ResourceBarView>();
            var swipe = canvas.GetComponent<SwipeInput>() ?? canvas.AddComponent<SwipeInput>();
            var end = canvas.GetComponent<EndScreen>() ?? canvas.AddComponent<EndScreen>();
            var overlay = canvas.GetComponent<MessageOverlay>() ?? canvas.AddComponent<MessageOverlay>();
            var mapView = canvas.GetComponent<MapView>() ?? canvas.AddComponent<MapView>();

            var gameGo = GameObject.Find("Game") ?? new GameObject("Game");
            var boot = gameGo.GetComponent(bootType) ?? gameGo.AddComponent(bootType);
            var bSo = new SerializedObject(boot);
            bSo.FindProperty("storyJson").objectReferenceValue = story;
            bSo.FindProperty("mapJson").objectReferenceValue = mapAsset;
            bSo.FindProperty("resources").objectReferenceValue = resources;
            if (theme != null) bSo.FindProperty("theme").objectReferenceValue = theme;
            bSo.FindProperty("cardView").objectReferenceValue = cardView;
            bSo.FindProperty("resourceBar").objectReferenceValue = bar;
            bSo.FindProperty("swipeInput").objectReferenceValue = swipe;
            bSo.FindProperty("endScreen").objectReferenceValue = end;
            bSo.FindProperty("messageOverlay").objectReferenceValue = overlay;
            bSo.FindProperty("mapView").objectReferenceValue = mapView;
            if (!string.IsNullOrEmpty(title)) bSo.FindProperty("title").stringValue = title;
            if (!string.IsNullOrEmpty(intro)) bSo.FindProperty("intro").stringValue = intro;
            bSo.ApplyModifiedProperties();

            // Edit preview: drive the engine once with the REAL MapGraph.
            var storyData = StoryLoader.Parse(story.text);
            var map = MapLoader.Parse(mapAsset.text);
            var source = new MapGraph(storyData, map);
            var engine = new EventEngine(storyData, resources, source, 12345);
            cardView.Bind(ViewMapper.BuildNodeView(engine.Current), theme);
            bar.SetTheme(theme);
            bar.Bind(ViewMapper.BuildResourceViews(engine.State, resources, theme));

            if (showMap) mapView.Bind(map, engine.Current.Id, source.NeighborsOf(engine.State));
            else mapView.Hide();
            overlay.Hide();

            EditorUtility.SetDirty(cardView); EditorUtility.SetDirty(bar);
            EditorUtility.SetDirty(mapView); EditorUtility.SetDirty(boot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            return new
            {
                ok = true,
                bootstrap = bootType.FullName,
                startNode = engine.Current != null ? engine.Current.Id : null,
                neighbors = string.Join(",", source.NeighborsOf(engine.State)),
                goal = map.GoalNodeId
            };
        }

        private static Type ResolveType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName, false);
                if (t != null) return t;
            }
            return null;
        }
    }
}
