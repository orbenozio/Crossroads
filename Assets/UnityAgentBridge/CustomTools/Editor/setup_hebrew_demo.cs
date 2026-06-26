using System;
using UnityEditor;
using UnityEngine;
using Crossroads.UI;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // יוצר Theme עברי (RTL + תוויות-משאב בעברית) להדגמת §10.6. התוויות בעברית הן ליטרלים ב-C#
    // (קובץ-מקור UTF-8), כדי לעקוף בעיית-קידוד של עברית ב-args של ה-CLI.
    public static class setup_hebrew_demo
    {
        [McpTool("setup_hebrew_demo", "Create a Hebrew RTL Theme (energy=אנרגיה, calm=רוגע) at path")]
        public static object Invoke(string path = "Assets/Games/_Template/Content/theme_he.asset")
        {
            var theme = AssetDatabase.LoadAssetAtPath<Theme>(path);
            bool created = theme == null;
            if (created) theme = ScriptableObject.CreateInstance<Theme>();

            theme.rightToLeft = true;
            theme.resourceLabels.Clear();
            theme.resourceLabels.Add(new Theme.ResourceLabel { id = "energy", label = "אנרגיה" });
            theme.resourceLabels.Add(new Theme.ResourceLabel { id = "calm", label = "רוגע" });

            if (created) AssetDatabase.CreateAsset(theme, path);
            else EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            return new { ok = true, path, created, rtl = true, labels = theme.resourceLabels.Count };
        }
    }
}
