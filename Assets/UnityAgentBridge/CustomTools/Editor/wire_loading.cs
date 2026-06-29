using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Focused wiring for the branded loading screen: adds a LoadingScreen component to the Canvas and
    // assigns it to the bootstrap's loadingScreen field, without rebuilding the rest of the scene (so the
    // hand-tuned card layout is left untouched). The panel itself is built procedurally at runtime.
    public static class wire_loading
    {
        [McpTool("wire_loading", "Add a LoadingScreen to the Canvas and wire it onto the game's GameBootstrap (loadingScreen field).")]
        public static object Invoke(string bootstrapType = "Crossroads.Game.NewbornKing.GameBootstrap", string loadingCaption = "")
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas == null) throw new Exception("Canvas not found in the active scene");
            var gameGo = GameObject.Find("Game");
            if (gameGo == null) throw new Exception("Game object (bootstrap) not found in the active scene");

            var bootType = ResolveType(bootstrapType);
            if (bootType == null) throw new Exception("type not found: " + bootstrapType);
            var boot = gameGo.GetComponent(bootType);
            if (boot == null) throw new Exception("bootstrap component not found on Game: " + bootstrapType);

            // Drop any stale baked panel so the runtime rebuilds a clean one.
            var stale = canvas.transform.Find("LoadingScreen");
            if (stale != null) UnityEngine.Object.DestroyImmediate(stale.gameObject);

            var loading = canvas.GetComponent<LoadingScreen>() ?? canvas.AddComponent<LoadingScreen>();

            var bSo = new SerializedObject(boot);
            var prop = bSo.FindProperty("loadingScreen");
            if (prop == null) throw new Exception("bootstrap has no loadingScreen field");
            prop.objectReferenceValue = loading;
            var capProp = bSo.FindProperty("loadingCaption");
            if (capProp != null && !string.IsNullOrEmpty(loadingCaption)) capProp.stringValue = loadingCaption;
            bSo.ApplyModifiedProperties();

            EditorUtility.SetDirty(boot);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            return new { ok = true, bootstrap = bootType.FullName, captionSet = !string.IsNullOrEmpty(loadingCaption) };
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
