using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.Engine;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Parameterized swipe-frame stager for building a gameplay GIF. Unlike the arg-free nbk_* tools, this
    // is driven via the CLI shim which CAN forward args. It deterministically advances the seeded engine
    // by `step` right-resolves, binds the resulting card, then applies a drag of `amount` (0..1) toward
    // `side` with the live meter + on-card delta preview - so a sequence of captures at rising `amount`
    // animates one card's swipe. Edit-mode only; drives the real UI; never saves the scene.
    public static class nbk_swipe
    {
        [McpTool("nbk_swipe", "Edit-mode: stage NewbornKing card #step (0-based) with a drag of `amount` (0..1) toward `side` (Right/Left) and the live preview, for capturing GIF frames. Drive via the shim with args.")]
        public static object Invoke(int step = 0, float amount = 0f, string side = "Right")
        {
            var card = GameObject.Find("Canvas/Card")?.GetComponent<CardView>();
            var bars = GameObject.Find("Canvas")?.GetComponent<ResourceBarView>();
            var end  = GameObject.Find("Canvas")?.GetComponent<EndScreen>();
            var menu = GameObject.Find("Canvas")?.GetComponent<MenuOverlay>();
            if (card == null) throw new Exception("CardView not found - wire the scene first");

            var choice = side != null && side.ToLowerInvariant().StartsWith("l") ? ChoiceSide.Left : ChoiceSide.Right;

            var engine = NbkContent.NewEngine(out var res, out var theme);
            NbkContent.PruneForeignBars(res);
            if (end != null) end.Hide();
            if (menu != null) menu.Hide();

            // Advance deterministically to the requested card (always resolve right to walk the same path).
            for (int i = 0; i < step && engine.Status == GameStatus.Running; i++)
            {
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

            // Drag + preview (skip the preview at rest so amount=0 shows a clean, untilted card).
            if (engine.Status == GameStatus.Running)
            {
                card.ApplyDrag(choice, Mathf.Clamp01(amount));
                if (amount > 0.001f)
                {
                    var deltas = engine.Preview(choice).Deltas;
                    if (bars != null) bars.ShowPreview(deltas);
                    card.ShowPreviewDeltas(ViewMapper.FormatDeltas(deltas, res, theme), choice);
                }
            }

            EditorUtility.SetDirty(card);
            if (bars != null) EditorUtility.SetDirty(bars);
            NbkContent.ClearStageDirty();
            return new { ok = true, step, amount, side = choice.ToString(), node = engine.Current?.Id, status = engine.Status.ToString() };
        }
    }
}
