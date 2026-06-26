using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Data-driven creation of a Theme ScriptableObject. One [McpTool] per class (the invoker binds by class).
    // labels format (optional): "id=Label|id=Label" - Theme override > ResourceDef default (spec 14.2).
    public static class create_theme
    {
        [McpTool("create_theme", "Create a Theme asset at path with optional resource-label overrides (rtl=true for Hebrew)")]
        public static object Invoke(string path = "", string labels = "", bool rtl = false)
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("path is required");

            var theme = AssetDatabase.LoadAssetAtPath<Theme>(path);
            bool created = theme == null;
            if (created) theme = ScriptableObject.CreateInstance<Theme>();

            theme.rightToLeft = rtl;
            theme.resourceLabels.Clear();
            if (!string.IsNullOrEmpty(labels))
            {
                foreach (var raw in labels.Split('|'))
                {
                    var p = raw.Split('=');
                    if (p.Length != 2) throw new Exception($"bad label '{raw}' - expected id=Label");
                    theme.resourceLabels.Add(new Theme.ResourceLabel { id = p[0].Trim(), label = p[1].Trim() });
                }
            }

            if (created) AssetDatabase.CreateAsset(theme, path);
            else EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            return new { ok = true, path, created, labelOverrides = theme.resourceLabels.Count };
        }
    }
}
