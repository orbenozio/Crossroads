using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityAgentBridge.Editor;
using Crossroads.Engine;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Arg-free capture helpers for the NewbornKing scene. call_tool cannot forward args (bridge bug,
    // docs/bridge-wishlist.md item), so these take no parameters and hardcode NewbornKing content.
    // Edit-mode only: they drive the REAL CardView / ResourceBarView / MenuOverlay so a screenshot
    // reflects the live UI. They never save the scene.
    internal static class NbkContent
    {
        public const string Story = "Assets/Games/NewbornKing/Content/story.json";
        public const string Res    = "Assets/Games/NewbornKing/Content/resources.asset";
        public const string ThemeP = "Assets/Games/NewbornKing/Content/theme.asset";

        public static ResourceSet LoadRes() => AssetDatabase.LoadAssetAtPath<ResourceSet>(Res);
        public static Theme LoadTheme() => AssetDatabase.LoadAssetAtPath<Theme>(ThemeP);

        public static EventEngine NewEngine(out ResourceSet res, out Theme theme)
        {
            var storyAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Story);
            res = LoadRes();
            theme = LoadTheme();
            if (storyAsset == null || res == null) throw new Exception("NewbornKing content not found");
            var story = StoryLoader.Parse(storyAsset.text);
            return new EventEngine(story, res, new Deck(story), 12345);
        }

        // Destroy meter bars whose id is not in the current resource set (stale _Template energy/calm
        // bars baked into the scene by demo_swipe). Keeps the live HUD to exactly this game's meters.
        public static int PruneForeignBars(ResourceSet res)
        {
            var meters = GameObject.Find("Canvas/Meters");
            if (meters == null) return 0;
            var keep = new HashSet<string>();
            foreach (var d in res.resources) keep.Add("Bar_" + d.id);
            var doomed = new List<GameObject>();
            foreach (Transform t in meters.transform)
                if (!keep.Contains(t.name)) doomed.Add(t.gameObject);
            foreach (var go in doomed) UnityEngine.Object.DestroyImmediate(go);
            return doomed.Count;
        }

        // These tools dirty the scene only to stage a frame for a screenshot - never meant to be saved.
        // Clearing the dirty flag (the staged objects stay live for the capture) keeps the scene "clean"
        // so no modal "Save Scene?" dialog ever pops up to block the bridge (e.g. before run_tests).
        // EditorSceneManager.ClearSceneDirtiness is internal, so reach it by reflection (cached).
        private static MethodInfo _clearDirty;
        public static void ClearStageDirty()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid()) return;
            if (_clearDirty == null)
                _clearDirty = typeof(EditorSceneManager).GetMethod(
                    "ClearSceneDirtiness", BindingFlags.NonPublic | BindingFlags.Static);
            _clearDirty?.Invoke(null, new object[] { scene });
        }
    }

    // Stage a NewbornKing card that has a speaker portrait, with the meters showing a right-swipe
    // preview - the richest single frame to review (portrait framing, card layout, meter overlap).
    public static class nbk_card
    {
        [McpTool("nbk_card", "Edit-mode: stage a NewbornKing speaker card + meters with a right-swipe preview, for screenshotting. Arg-free (call_tool cannot pass args).")]
        public static object Invoke()
        {
            var card = GameObject.Find("Canvas/Card")?.GetComponent<CardView>();
            var bars = GameObject.Find("Canvas")?.GetComponent<ResourceBarView>();
            var end  = GameObject.Find("Canvas")?.GetComponent<EndScreen>();
            var menu = GameObject.Find("Canvas")?.GetComponent<MenuOverlay>();
            if (card == null) throw new Exception("CardView not found - wire the scene first");

            var engine = NbkContent.NewEngine(out var res, out var theme);
            int pruned = NbkContent.PruneForeignBars(res);
            if (end != null) end.Hide();
            if (menu != null) menu.Hide();   // a prior nbk_menu leaves the overlay on top, covering the card

            // Walk forward (always swipe right) until the current node has a speaker that owns a portrait.
            string speaker = null;
            for (int step = 0; step < 16 && engine.Status == GameStatus.Running; step++)
            {
                var v = ViewMapper.BuildNodeView(engine.Current);
                bool named = !string.IsNullOrEmpty(v.Speaker) && v.Speaker != "narrator";
                var sp = theme != null ? theme.GetSpeaker(v.Speaker) : null;
                if (named && sp != null && sp.icon != null) { speaker = v.Speaker; break; }
                engine.Resolve(ChoiceSide.Right);
                if (engine.Status == GameStatus.Running) engine.Advance();
            }

            var view = ViewMapper.BuildNodeView(engine.Current);
            card.Bind(view, theme);
            if (bars != null)
            {
                bars.SetTheme(theme);
                bars.Bind(ViewMapper.BuildResourceViews(engine.State, res, theme));
            }

            // Apply a mid-swipe preview so the capture shows the tilt + on-card delta + meter deltas.
            if (engine.Status == GameStatus.Running)
            {
                card.ApplyDrag(ChoiceSide.Right, 0.55f);
                var deltas = engine.Preview(ChoiceSide.Right).Deltas;
                if (bars != null) bars.ShowPreview(deltas);
                card.ShowPreviewDeltas(ViewMapper.FormatDeltas(deltas, res, theme), ChoiceSide.Right);
            }

            EditorUtility.SetDirty(card);
            if (bars != null) EditorUtility.SetDirty(bars);
            NbkContent.ClearStageDirty();   // staged-for-capture only; do not leave the scene dirty
            return new { ok = true, speaker = speaker ?? "(none found)", node = engine.Current?.Id, prunedBars = pruned };
        }
    }

    // Stage the main menu (title + intro + buttons) exactly as ShowMainMenu builds it, for a capture
    // of the menu's gold level, button contrast and intro-text size.
    public static class nbk_menu
    {
        [McpTool("nbk_menu", "Edit-mode: stage the NewbornKing main menu (title/intro/buttons) for screenshotting. Arg-free.")]
        public static object Invoke()
        {
            var menu = GameObject.Find("Canvas")?.GetComponent<MenuOverlay>();
            var card = GameObject.Find("Canvas/Card")?.GetComponent<CardView>();
            if (menu == null) throw new Exception("MenuOverlay not found - wire the scene first");

            var theme = NbkContent.LoadTheme();
            menu.SetTheme(theme);
            if (card != null) card.Bind(ViewMapper.BuildNodeView(null), theme);   // clear the card behind the menu

            var items = new List<MenuOverlay.MenuItem>
            {
                new MenuOverlay.MenuItem("Continue", () => {}, true),
                new MenuOverlay.MenuItem("New Game", () => {}),
                new MenuOverlay.MenuItem("Quit", () => {}),
            };
            menu.Show("The Newborn King",
                "The old king is dead. Guide the regency and the infant heir. Swipe to decide.",
                items, true);

            EditorUtility.SetDirty(menu);
            NbkContent.ClearStageDirty();   // staged-for-capture only; do not leave the scene dirty
            return new { ok = true, shown = menu.IsShown };
        }
    }

    // Stage the menu with three buttons forced into the idle / hover / pressed ColorTint states (by writing
    // each button's image to its own normal/highlighted/pressed color), so one screenshot shows all the
    // button states at once - they otherwise only appear during live pointer interaction.
    public static class nbk_btn_states
    {
        [McpTool("nbk_btn_states", "Edit-mode: stage the menu with 3 buttons forced to idle/hover/pressed states for screenshotting the button feedback. Arg-free.")]
        public static object Invoke()
        {
            var menu = GameObject.Find("Canvas")?.GetComponent<MenuOverlay>();
            var card = GameObject.Find("Canvas/Card")?.GetComponent<CardView>();
            if (menu == null) throw new Exception("MenuOverlay not found - wire the scene first");

            var theme = NbkContent.LoadTheme();
            menu.SetTheme(theme);
            if (card != null) card.Bind(ViewMapper.BuildNodeView(null), theme);

            menu.Show("Button States", "Idle / Hover / Pressed",
                new List<MenuOverlay.MenuItem>
                {
                    new MenuOverlay.MenuItem("Idle", () => {}, true),
                    new MenuOverlay.MenuItem("Hover", () => {}),
                    new MenuOverlay.MenuItem("Pressed", () => {}),
                });

            // Force each button's graphic to a different ColorTint state so the still frame shows all three.
            var buttons = GameObject.Find("Canvas/MenuOverlay/Buttons");
            if (buttons != null)
            {
                for (int i = 0; i < buttons.transform.childCount && i < 3; i++)
                {
                    var b = buttons.transform.GetChild(i).GetComponent<UnityEngine.UI.Button>();
                    if (b == null || b.targetGraphic == null) continue;
                    var cb = b.colors;
                    b.targetGraphic.color = i == 0 ? cb.normalColor : i == 1 ? cb.highlightedColor : cb.pressedColor;
                }
            }

            EditorUtility.SetDirty(menu);
            NbkContent.ClearStageDirty();
            return new { ok = true };
        }
    }

    // Stage the idle swipe-hint over a centered card for a capture. The hint is play-only (CardView.Update
    // drives it), so this reaches its private build+animate via reflection and steps the animation phase on
    // each call (puck-right -> mid -> puck-left) so repeated nbk_hint + capture shows the motion extremes.
    public static class nbk_hint
    {
        private static int _cursor;
        private static readonly float[] Phases = { 0.375f, 0.5f, 1.125f };   // right extreme, mid-swing, left extreme

        [McpTool("nbk_hint", "Edit-mode: stage the idle swipe-hint (touch-puck + chevrons) over a centered card for screenshotting. Cycles puck position on repeated calls. Arg-free.")]
        public static object Invoke()
        {
            var card = GameObject.Find("Canvas/Card")?.GetComponent<CardView>();
            var bars = GameObject.Find("Canvas")?.GetComponent<ResourceBarView>();
            var end  = GameObject.Find("Canvas")?.GetComponent<EndScreen>();
            var menu = GameObject.Find("Canvas")?.GetComponent<MenuOverlay>();
            if (card == null) throw new Exception("CardView not found - wire the scene first");

            var engine = NbkContent.NewEngine(out var res, out var theme);
            NbkContent.PruneForeignBars(res);
            if (end != null) end.Hide();
            if (menu != null) menu.Hide();

            // Drop any SwipeHint left by an earlier preview (it survives a domain reload as a scene child),
            // so EnsureSwipeHint rebuilds it fresh at the current code's position.
            var staleHint = card.transform.Find("SwipeHint");
            if (staleHint != null) UnityEngine.Object.DestroyImmediate(staleHint.gameObject);

            card.Bind(ViewMapper.BuildNodeView(engine.Current), theme);
            if (bars != null) { bars.SetTheme(theme); bars.Bind(ViewMapper.BuildResourceViews(engine.State, res, theme)); }

            const BindingFlags BF = BindingFlags.NonPublic | BindingFlags.Instance;
            var ensure  = typeof(CardView).GetMethod("EnsureSwipeHint", BF);
            var animate = typeof(CardView).GetMethod("AnimateHint", BF);
            var groupF  = typeof(CardView).GetField("_hintGroup", BF);
            ensure.Invoke(card, null);
            var grp = (CanvasGroup)groupF.GetValue(card);
            grp.gameObject.SetActive(true);
            float t = Phases[_cursor % Phases.Length];
            _cursor++;
            animate.Invoke(card, new object[] { t });

            NbkContent.ClearStageDirty();
            return new { ok = true, phase = t };
        }
    }

    // Stage the end / game-over screen for a capture. Arg-free, but each call advances to the NEXT story
    // ending (static cursor, wraps), so repeated nbk_end + capture cycles steps through all 12 end states
    // with their per-ending backdrop art - the way to review the new end-state images.
    public static class nbk_end
    {
        private static int _cursor;

        [McpTool("nbk_end", "Edit-mode: stage the NewbornKing end screen for the NEXT story ending (cycles through all endings on repeated calls, each with its own backdrop). Arg-free.")]
        public static object Invoke()
        {
            var end = GameObject.Find("Canvas")?.GetComponent<EndScreen>();
            var menu = GameObject.Find("Canvas")?.GetComponent<MenuOverlay>();
            var card = GameObject.Find("Canvas/Card")?.GetComponent<CardView>();
            if (end == null) throw new Exception("EndScreen not found - wire the scene first");

            var theme = NbkContent.LoadTheme();
            var storyAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(NbkContent.Story);
            if (storyAsset == null) throw new Exception("NewbornKing story not found");
            var story = StoryLoader.Parse(storyAsset.text);
            if (story.Endings.Count == 0) throw new Exception("story has no endings");

            int idx = _cursor % story.Endings.Count;
            _cursor++;
            var e = story.Endings[idx];

            // Destroy any panel left from an earlier build so Show rebuilds it with the current backdrop
            // logic (avoids a stale edit-mode panel surviving a domain reload).
            var stale = end.transform.Find("EndScreen");
            if (stale != null) UnityEngine.Object.DestroyImmediate(stale.gameObject);

            end.SetTheme(theme);
            if (menu != null) menu.Hide();
            if (card != null) card.Bind(ViewMapper.BuildNodeView(null), theme);   // clear the card behind the end screen
            end.Show(e.Text, e.Image, () => {}, () => {});

            EditorUtility.SetDirty(end);
            NbkContent.ClearStageDirty();   // staged-for-capture only; do not leave the scene dirty
            string label = e.Fallback ? "fallback" : e.Flag ?? (e.ResourceId != null ? e.ResourceId + ":" + e.Edge : "goal");
            return new { ok = true, index = idx, count = story.Endings.Count, ending = label, image = e.Image ?? "(none)" };
        }
    }
}
