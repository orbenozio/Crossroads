using UnityEditor;
using TMPro;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // בדיקת-זמינות חד-פעמית ל-TextMeshPro: האם זמין, האם ה-Essentials/settings מיובאים, ומה פונט-ברירת-המחדל.
    public static class tmp_probe
    {
        [McpTool("tmp_probe", "Report TextMeshPro availability + settings + default font")]
        public static object Invoke()
        {
            TMP_Settings settings = null;
            try { settings = TMP_Settings.instance; } catch { }
            TMP_FontAsset def = null;
            try { def = TMP_Settings.defaultFontAsset; } catch { }

            return new
            {
                ok = true,
                tmpAvailable = true,
                hasSettings = settings != null,
                defaultFont = def != null ? def.name : null
            };
        }
    }
}
