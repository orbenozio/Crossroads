using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // יוצר TMP_FontAsset דינמי מ-TTF (לעברית). מצב Dynamic = הגליפים מרוסטרים על-פי-דרישה מה-TTF,
    // כך שאין צורך לאפות מראש את טווח-העברית. החומר והאטלס נשמרים כ-sub-assets כדי שיתמידו.
    public static class create_hebrew_font
    {
        [McpTool("create_hebrew_font", "Create a dynamic TMP_FontAsset from a TTF (Hebrew-capable) at savePath")]
        public static object Invoke(string ttfPath = "", string savePath = "")
        {
            var src = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (src == null) throw new Exception("source TTF not found at " + ttfPath);

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(savePath);
            if (existing != null) return new { ok = true, savePath, font = existing.name, created = false };

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                src, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
            if (fontAsset == null) throw new Exception("CreateFontAsset returned null");

            fontAsset.name = Path.GetFileNameWithoutExtension(savePath);
            AssetDatabase.CreateAsset(fontAsset, savePath);

            // לשמור material + atlas textures כ-sub-assets של ה-font asset.
            if (fontAsset.material != null)
            {
                fontAsset.material.name = fontAsset.name + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }
            if (fontAsset.atlasTextures != null)
                foreach (var tex in fontAsset.atlasTextures)
                    if (tex != null)
                    {
                        tex.name = fontAsset.name + " Atlas";
                        AssetDatabase.AddObjectToAsset(tex, fontAsset);
                    }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(savePath);

            return new { ok = true, savePath, font = fontAsset.name, created = true };
        }
    }
}
