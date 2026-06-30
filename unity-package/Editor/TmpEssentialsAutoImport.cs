using UnityEditor;
using UnityEngine;
using TMPro;

namespace Crossroads.Editor
{
    // A consumer project that pulls this engine needs TextMeshPro's Essential Resources (TMP_Settings,
    // default font, shaders) before any TMP text renders - otherwise the user has to run
    // "Window > TextMeshPro > Import TMP Essential Resources" by hand on first open. This imports them
    // once, automatically, when they're missing. Once present the guard short-circuits, so it never loops.
    [InitializeOnLoad]
    public static class TmpEssentialsAutoImport
    {
        static TmpEssentialsAutoImport()
        {
            // Defer past the initial asset load so Resources/AssetDatabase are ready.
            EditorApplication.delayCall += TryImport;
        }

        private static void TryImport()
        {
            // TMP keeps its settings at Resources/TMP Settings. Present == essentials already imported.
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;

            TMP_PackageResourceImporter.ImportResources(true, false, false);
            Debug.Log("[Crossroads] Imported TMP Essential Resources (first-time setup for this project).");
        }
    }
}
