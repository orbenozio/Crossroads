using UnityEditor;
using TMPro;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // מייבא TMP Essential Resources (TMP_Settings, פונט-ברירת-מחדל, shaders) ללא דיאלוג - חד-פעמי.
    // נדרש לפני שימוש ב-TextMeshPro. אחרי הקריאה צריך refresh_assets ואז לבדוק שוב עם tmp_probe.
    public static class import_tmp_essentials
    {
        [McpTool("import_tmp_essentials", "Import TMP Essential Resources non-interactively")]
        public static object Invoke()
        {
            TMP_PackageResourceImporter.ImportResources(true, false, false);
            AssetDatabase.Refresh();
            return new { ok = true, requested = true };
        }
    }
}
