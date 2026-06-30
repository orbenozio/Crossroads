using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Forces UPM to re-read Packages/manifest.json and resolve packages, then recompiles.
    // Needed after editing manifest.json externally - AssetDatabase.Refresh alone does NOT
    // re-resolve the package graph, so newly-added local (file:) packages are otherwise ignored.
    public static class resolve_packages
    {
        [McpTool("resolve_packages", "Force UPM to re-resolve the manifest (Client.Resolve) and recompile")]
        public static object Invoke()
        {
            Client.Resolve();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            return new { ok = true, resolved = true };
        }
    }
}
