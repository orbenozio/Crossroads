using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Imports every PNG under a folder as a Sprite (crisp HQ settings) and registers it in a Theme's
    // endingArt list, keyed by the file name (without extension) - which matches the story ending's
    // "image" key. Idempotent: rebuilds the list from the folder each run.
    public static class set_ending_art
    {
        [McpTool("set_ending_art", "Import end-state PNGs under a folder as sprites and wire them into a Theme.endingArt list (key = filename). Arg-free defaults to NewbornKing.")]
        public static object Invoke(
            string folder = "Assets/Games/NewbornKing/Art/Endings",
            string themePath = "Assets/Games/NewbornKing/Content/theme.asset",
            int maxSize = 1024)
        {
            var theme = AssetDatabase.LoadAssetAtPath<Theme>(themePath);
            if (theme == null) throw new Exception("theme not found at " + themePath);

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') });
            if (guids.Length == 0) throw new Exception("no textures under " + folder);

            theme.endingArt = new List<Theme.EndingArt>();
            var added = new List<string>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp != null)
                {
                    // Sprite type + crisp anti-aliased import (matches set_art_quality), so the dimmed
                    // backdrop downsamples smoothly and resolves to ASTC HQ on Android.
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.textureCompression = TextureImporterCompression.CompressedHQ;
                    imp.compressionQuality = 100;
                    imp.crunchedCompression = false;
                    imp.mipmapEnabled = true;
                    imp.filterMode = FilterMode.Trilinear;
                    imp.maxTextureSize = maxSize;
                    foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                    {
                        var ps = imp.GetPlatformTextureSettings(platform);
                        if (ps.overridden) { ps.overridden = false; imp.SetPlatformTextureSettings(ps); }
                    }
                    imp.SaveAndReimport();
                }

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) continue;
                string key = Path.GetFileNameWithoutExtension(path);
                theme.endingArt.Add(new Theme.EndingArt { key = key, art = sprite });
                added.Add(key);
            }

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return new { ok = true, themePath, count = added.Count, keys = added.ToArray() };
        }
    }
}
