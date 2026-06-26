using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;
using UnityAgentBridge.Editor;
using Crossroads.Engine;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Wire a game's GameBootstrap into the active scene and preview its first card. Generic over the
    // concrete bootstrap (resolved by full type name) + content paths, so it serves any clone, not just
    // _Template. The generic bridge tools build the Canvas/Card (create_canvas, create_gameobject, ...);
    // this tool does only what they cannot: author object/asset reference fields via SerializedObject.
    // Expects Canvas + Canvas/Card + Canvas/Card/Label to already exist in the active scene.
    public static class wire_game_scene
    {
        [McpTool("wire_game_scene", "Add+wire a game's GameBootstrap (by type) + UI components into the active scene; preview card 1")]
        public static object Invoke(string bootstrapType = "", string storyPath = "", string resourcesPath = "", string themePath = "",
            string title = "", string intro = "", bool showOpening = false, bool showMenu = false)
        {
            if (string.IsNullOrEmpty(bootstrapType)) throw new Exception("bootstrapType is required (full type name)");

            var bootType = ResolveType(bootstrapType);
            if (bootType == null) throw new Exception($"type not found: {bootstrapType}");

            var story = AssetDatabase.LoadAssetAtPath<TextAsset>(storyPath);
            var resources = AssetDatabase.LoadAssetAtPath<ResourceSet>(resourcesPath);
            if (story == null) throw new Exception("story TextAsset not found at " + storyPath);
            if (resources == null) throw new Exception("ResourceSet not found at " + resourcesPath);
            var theme = string.IsNullOrEmpty(themePath) ? null : AssetDatabase.LoadAssetAtPath<Theme>(themePath);

            var canvas = GameObject.Find("Canvas");
            var card = GameObject.Find("Canvas/Card");
            if (canvas == null || card == null)
                throw new Exception("expected Canvas + Canvas/Card in the active scene (build them via create_canvas + create_gameobject first)");

            UIFonts.RightToLeft = theme != null && theme.rightToLeft;   // Hebrew/RTL from the Theme (for display + screenshot)

            // Rebuild old procedural UI (legacy Text) as TMP - matters when migrating existing scenes.
            CleanOldUi(canvas);
            EnsureBackground(canvas, theme);   // full-screen themed backdrop behind the card
            // body is TMP (RTL); drop an old set_text Label if present.
            var legacy = card.transform.Find("Label");
            if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            var body = EnsureTmpBody(card);
            // Bands: portrait + speaker name at the top, choice hints at the bottom, body in the middle.
            var bodyRt = (RectTransform)body.transform;
            bodyRt.anchorMin = new Vector2(0.08f, 0.16f); bodyRt.anchorMax = new Vector2(0.92f, 0.67f);
            bodyRt.offsetMin = Vector2.zero; bodyRt.offsetMax = Vector2.zero;
            var speakerIcon = MakeCardImage(card, "SpeakerIcon", new Vector2(0.33f, 0.745f), new Vector2(0.67f, 0.975f));
            var speaker = MakeCardLabel(card, "Speaker", new Vector2(0.08f, 0.685f), new Vector2(0.92f, 0.74f), 26, TextAlignmentOptions.Center);
            var choiceLeft = MakeCardLabel(card, "ChoiceLeft", new Vector2(0.06f, 0.03f), new Vector2(0.48f, 0.14f), 22, TextAlignmentOptions.Left);
            var choiceRight = MakeCardLabel(card, "ChoiceRight", new Vector2(0.52f, 0.03f), new Vector2(0.94f, 0.14f), 22, TextAlignmentOptions.Right);

            // CardView on Card, wired to the TMP body + portrait + speaker/choice labels + the card Image.
            var cardView = card.GetComponent<CardView>() ?? card.AddComponent<CardView>();
            var cvSo = new SerializedObject(cardView);
            cvSo.FindProperty("bodyText").objectReferenceValue = body;
            cvSo.FindProperty("speakerLabel").objectReferenceValue = speaker;
            cvSo.FindProperty("speakerIcon").objectReferenceValue = speakerIcon;
            cvSo.FindProperty("leftLabel").objectReferenceValue = choiceLeft;
            cvSo.FindProperty("rightLabel").objectReferenceValue = choiceRight;
            cvSo.FindProperty("cardBackground").objectReferenceValue = card.GetComponent<Image>();
            cvSo.ApplyModifiedProperties();

            var bar = canvas.GetComponent<ResourceBarView>() ?? canvas.AddComponent<ResourceBarView>();
            var swipe = canvas.GetComponent<SwipeInput>() ?? canvas.AddComponent<SwipeInput>();
            var end = canvas.GetComponent<EndScreen>() ?? canvas.AddComponent<EndScreen>();
            var overlay = canvas.GetComponent<MessageOverlay>() ?? canvas.AddComponent<MessageOverlay>();
            var menu = canvas.GetComponent<MenuOverlay>() ?? canvas.AddComponent<MenuOverlay>();
            var pause = canvas.GetComponent<PauseButton>() ?? canvas.AddComponent<PauseButton>();
            var audioDir = canvas.GetComponent<AudioDirector>() ?? canvas.AddComponent<AudioDirector>();

            // Game object + the concrete bootstrap (by type), fully wired.
            var gameGo = GameObject.Find("Game") ?? new GameObject("Game");
            var boot = gameGo.GetComponent(bootType) ?? gameGo.AddComponent(bootType);
            var bSo = new SerializedObject(boot);
            bSo.FindProperty("storyJson").objectReferenceValue = story;
            bSo.FindProperty("resources").objectReferenceValue = resources;
            if (theme != null) bSo.FindProperty("theme").objectReferenceValue = theme;
            bSo.FindProperty("cardView").objectReferenceValue = cardView;
            bSo.FindProperty("resourceBar").objectReferenceValue = bar;
            bSo.FindProperty("swipeInput").objectReferenceValue = swipe;
            bSo.FindProperty("endScreen").objectReferenceValue = end;
            bSo.FindProperty("messageOverlay").objectReferenceValue = overlay;
            // menu/pauseButton are new optional fields - present on Reigns bootstraps, absent on the journey one.
            var menuProp = bSo.FindProperty("menu");
            if (menuProp != null) menuProp.objectReferenceValue = menu;
            var pauseProp = bSo.FindProperty("pauseButton");
            if (pauseProp != null) pauseProp.objectReferenceValue = pause;
            var audioProp = bSo.FindProperty("audioDirector");
            if (audioProp != null) audioProp.objectReferenceValue = audioDir;
            if (!string.IsNullOrEmpty(title)) bSo.FindProperty("title").stringValue = title;
            if (!string.IsNullOrEmpty(intro)) bSo.FindProperty("intro").stringValue = intro;
            bSo.ApplyModifiedProperties();

            // Edit-mode preview: run the engine once so a screenshot shows real content driving the UI.
            var storyData = StoryLoader.Parse(story.text);
            var issues = StoryValidator.Validate(storyData, resources);
            var engine = new EventEngine(storyData, resources, new Deck(storyData), 12345);
            cardView.Bind(ViewMapper.BuildNodeView(engine.Current), theme);
            bar.SetTheme(theme);
            bar.Bind(ViewMapper.BuildResourceViews(engine.State, resources, theme));

            // showOpening: render the simple opening overlay over the card (for a screenshot).
            // showMenu: render the real main menu (Continue/New Game/Quit) over the card (for a screenshot).
            string t = title;
            string introText = intro;
            if (string.IsNullOrEmpty(t)) t = new SerializedObject(boot).FindProperty("title").stringValue;
            if (string.IsNullOrEmpty(introText)) introText = new SerializedObject(boot).FindProperty("intro").stringValue;
            if (showMenu)
            {
                menu.SetTheme(theme);
                menu.Show(t, introText, new[]
                {
                    new MenuOverlay.MenuItem("Continue", null, true),
                    new MenuOverlay.MenuItem("New Game", null),
                    new MenuOverlay.MenuItem("Quit", null)
                }, true);   // useLogo: preview the title wordmark when the theme has one
            }
            else if (showOpening) overlay.Show(t, introText, "Start", null);
            else { overlay.Hide(); menu.Hide(); }
            pause.SetVisible(!showOpening && !showMenu);

            EditorUtility.SetDirty(cardView);
            EditorUtility.SetDirty(bar);
            EditorUtility.SetDirty(overlay);
            EditorUtility.SetDirty(boot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            return new
            {
                ok = true,
                bootstrap = bootType.FullName,
                validationIssues = issues.Count,
                firstNode = engine.Current != null ? engine.Current.Id : null,
                firstBody = engine.Current != null ? engine.Current.Body : null,
                meters = resources.resources.Length
            };
        }

        // Removes old procedural panels from the Canvas so they rebuild as TMP (legacy Text migration)
        // and so re-wiring is idempotent for the menu/HUD objects.
        internal static void CleanOldUi(GameObject canvas)
        {
            foreach (var name in new[] { "Meters", "MetersBg", "EndScreen", "MessageOverlay", "MenuOverlay", "PauseButton", "MapView" })
            {
                var t = canvas.transform.Find(name);
                if (t != null) UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
        }

        // Full-screen themed backdrop behind the card (first sibling). Created/recolored each wire.
        internal static void EnsureBackground(GameObject canvas, Theme theme)
        {
            var found = canvas.transform.Find("Background");
            Image img;
            if (found != null) img = found.GetComponent<Image>();
            else
            {
                var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(canvas.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                img = go.GetComponent<Image>();
                img.raycastTarget = false;
            }
            img.color = theme != null ? theme.background : new Color(0.12f, 0.12f, 0.14f);
            img.transform.SetAsFirstSibling();   // behind the card and HUD
        }

        // Creates an Image on the card (speaker portrait), preserving aspect; starts hidden until bound.
        internal static Image MakeCardImage(GameObject card, string name, Vector2 aMin, Vector2 aMax)
        {
            var found = card.transform.Find(name);
            if (found != null) UnityEngine.Object.DestroyImmediate(found.gameObject);
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(card.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = Color.white;
            go.SetActive(false);
            return img;
        }

        // Creates a TMP label on the card (speaker badge / choice hint), RTL per UIFonts.
        internal static TMP_Text MakeCardLabel(GameObject card, string name, Vector2 aMin, Vector2 aMax, int size, TextAlignmentOptions align)
        {
            var found = card.transform.Find(name);
            if (found != null) UnityEngine.Object.DestroyImmediate(found.gameObject);
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(card.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = size; t.alignment = align;
            t.color = Color.white; t.raycastTarget = false;
            t.enableWordWrapping = false;
            UIFonts.Apply(t);
            return t;
        }

        // יוצר/מוצא TMP body על הקלף (RTL לפי UIFonts.RightToLeft שכבר הוגדר מה-Theme).
        internal static TMP_Text EnsureTmpBody(GameObject card)
        {
            var found = card.transform.Find("Body");
            TextMeshProUGUI body;
            if (found != null) body = found.GetComponent<TextMeshProUGUI>();
            else
            {
                var go = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(card.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(0.08f, 0.1f); rt.anchorMax = new Vector2(0.92f, 0.9f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                body = go.GetComponent<TextMeshProUGUI>();
                body.fontSize = 34; body.alignment = TextAlignmentOptions.Center;
                body.color = Color.white; body.raycastTarget = false;
            }
            UIFonts.Apply(body);
            return body;
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
