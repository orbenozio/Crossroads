using System;
using UnityEditor;
using TMPro;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Assigns a TMP_FontAsset to a Theme's tmpFont (the per-game UI font). Keeps fonts data-driven: the
    // theme owns its font, so an English game can use a Latin display font while a Hebrew game keeps the
    // Hebrew font (J8).
    public static class set_theme_font
    {
        [McpTool("set_theme_font", "Assign a TMP_FontAsset to Theme.tmpFont")]
        public static object Invoke(string themePath = "", string fontPath = "")
        {
            if (string.IsNullOrEmpty(themePath)) throw new Exception("themePath is required");
            if (string.IsNullOrEmpty(fontPath)) throw new Exception("fontPath is required");

            var theme = AssetDatabase.LoadAssetAtPath<Theme>(themePath);
            if (theme == null) throw new Exception("Theme not found at " + themePath);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (font == null) throw new Exception("TMP_FontAsset not found at " + fontPath);

            theme.tmpFont = font;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return new { ok = true, themePath, fontPath, font = font.name };
        }
    }
}
