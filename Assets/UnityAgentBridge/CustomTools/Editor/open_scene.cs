using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Open a scene asset in the Editor, SILENTLY discarding unsaved changes in the current scene
    // (EditorSceneManager.OpenScene does not show the save dialog the way entering Play Mode does).
    // The bridge has new_scene + save_scene but no open_scene - this fills that gap (bridge-wishlist).
    // Use it to restore a known-clean scene before Play Mode runs so no "save modified scene?" dialog blocks.
    public static class open_scene
    {
        [McpTool("open_scene", "Open a scene asset (path=Assets/.../X.unity), discarding unsaved changes in the current scene")]
        public static object Invoke(string path = "")
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("path is required (e.g. Assets/Games/NewbornKing/Scenes/Game.unity)");
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) == null)
                throw new Exception("scene not found at " + path);

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single); // discards current unsaved changes
            return new { ok = true, path, name = scene.name, isDirty = scene.isDirty };
        }
    }
}
