using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Edit-mode: stage the branded LoadingScreen at mid-progress for a screenshot. Run() uses a coroutine
    // (no-op in edit mode), so this drives the private build + SetProgress directly via reflection. Arg-free.
    public static class nbk_loading
    {
        [McpTool("nbk_loading", "Edit-mode: stage the NewbornKing loading screen at ~62% for screenshotting. Arg-free.")]
        public static object Invoke()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) throw new Exception("Canvas not found - wire the scene first");
            var loading = canvas.GetComponent<LoadingScreen>();
            if (loading == null) throw new Exception("LoadingScreen component not found - run wire_loading first");

            var theme = NbkContent.LoadTheme();
            var t = typeof(LoadingScreen);
            const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

            t.GetMethod("Ensure", F)?.Invoke(loading, null);
            loading.SetTheme(theme);
            loading.SetCaption("Preparing the regency");
            t.GetMethod("ApplyBackdrop", F)?.Invoke(loading, null);
            t.GetMethod("SetProgress", F)?.Invoke(loading, new object[] { 0.62f });

            // Bring the staged panel to the front so nothing covers it in the capture.
            var panel = canvas.transform.Find("LoadingScreen");
            if (panel != null) panel.SetAsLastSibling();

            EditorUtility.SetDirty(loading);
            NbkContent.ClearStageDirty();   // staged-for-capture only; do not leave the scene dirty
            return new { ok = true, themeLoaded = theme != null, hasPanel = panel != null };
        }
    }
}
