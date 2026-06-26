using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.Engine;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Edit-mode swipe demo so each step is screenshot-able (the real SwipeInput path only runs in
    // play mode, and screenshots cannot be taken mid-play). Drives the SAME CardView/ResourceBarView/
    // EndScreen + EventEngine the live path uses. Engine kept static across calls.
    // action=start (bind first card) | drag (tilt the card + preview deltas) | commit (Resolve+Advance).
    // Content is parameterized so it serves any game (defaults to _Template); theme drives labels (J8).
    public static class demo_swipe
    {
        private static EventEngine _engine;
        private static ResourceSet _res;
        private static Theme _theme;
        private static string _endText = "The story has reached its end.";

        [McpTool("demo_swipe", "Edit-mode swipe demo: action=start|drag|commit, side, amount, storyPath, resourcesPath, themePath")]
        public static object Invoke(string action = "start", string side = "right", float amount = 0.6f,
            string storyPath = "Assets/Games/_Template/Content/story.json",
            string resourcesPath = "Assets/Games/_Template/Content/resources.asset",
            string themePath = "")
        {
            var card = GameObject.Find("Canvas/Card")?.GetComponent<CardView>();
            var bars = GameObject.Find("Canvas")?.GetComponent<ResourceBarView>();
            var end = GameObject.Find("Canvas")?.GetComponent<EndScreen>();
            if (card == null) throw new Exception("CardView not found - wire the scene first");
            var s = side == "left" ? ChoiceSide.Left : ChoiceSide.Right;

            if (action == "start" || _engine == null)
            {
                var storyAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(storyPath);
                _res = AssetDatabase.LoadAssetAtPath<ResourceSet>(resourcesPath);
                _theme = string.IsNullOrEmpty(themePath) ? null : AssetDatabase.LoadAssetAtPath<Theme>(themePath);
                if (storyAsset == null || _res == null) throw new Exception("story/resources not found at given paths");
                var story = StoryLoader.Parse(storyAsset.text);
                _engine = new EventEngine(story, _res, new Deck(story), 12345);
                _engine.OnGameOver += info => _endText = info.Text;
                if (bars != null) bars.SetTheme(_theme);
                card.Bind(ViewMapper.BuildNodeView(_engine.Current), _theme);
            }

            if (bars != null) bars.Bind(ViewMapper.BuildResourceViews(_engine.State, _res, _theme));

            if (action == "drag")
            {
                card.ApplyDrag(s, amount);
                if (_engine.Status == GameStatus.Running)
                {
                    var deltas = _engine.Preview(s).Deltas;          // preview deltas (spec 10.3)
                    if (bars != null) bars.ShowPreview(deltas);      // on the meters
                    card.ShowPreviewDeltas(ViewMapper.FormatDeltas(deltas, _res, _theme), s); // and on the card corner
                }
            }
            else if (action == "commit")
            {
                _engine.Resolve(s);
                if (_engine.Status == GameStatus.Running) _engine.Advance();
                card.Bind(ViewMapper.BuildNodeView(_engine.Current), _theme); // Bind resets the drag transform
                if (bars != null) bars.Bind(ViewMapper.BuildResourceViews(_engine.State, _res, _theme));
            }

            if (end != null)
            {
                if (_engine.Status == GameStatus.GameOver) end.Show(_endText, null);
                else end.Hide();
            }

            EditorUtility.SetDirty(card);
            if (bars != null) EditorUtility.SetDirty(bars);
            return new
            {
                ok = true,
                action,
                node = _engine.Current != null ? _engine.Current.Id : null,
                resources = ResourcesSnapshot(),
                status = _engine.Status.ToString()
            };
        }

        private static string ResourcesSnapshot()
        {
            if (_engine == null || _res == null) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var d in _res.resources)
                sb.Append(d.id).Append('=').Append(_engine.State.GetResource(d.id)).Append(' ');
            return sb.ToString().Trim();
        }
    }
}
