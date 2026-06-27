using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Forces crisp import settings on every texture under a folder. Keeps compression (small build) but
    // uses HIGH-QUALITY compression: on desktop that is BC7 - the same byte size as the default BC3/DXT5
    // but near-lossless, which removes the banding/"pixelated" look on the gold gradients. Bilinear
    // filter, full max size, no mipmaps (UI art is drawn at ~1:1, mipmaps would only soften it).
    public static class set_art_quality
    {
        [McpTool("set_art_quality", "Set crisp high-quality (BC7) import settings on all textures under a folder")]
        public static object Invoke(string folder = "", int maxSize = 2048)
        {
            if (string.IsNullOrEmpty(folder)) throw new Exception("folder is required");

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') });
            if (guids.Length == 0) throw new Exception("no textures found under " + folder);

            int updated = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;

                imp.textureCompression = TextureImporterCompression.CompressedHQ;   // BC7 on desktop (size of BC3, far better quality)
                imp.compressionQuality = 100;
                imp.crunchedCompression = false;
                imp.filterMode = FilterMode.Bilinear;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = maxSize;

                // Clear any per-platform overrides so the uncompressed default is used everywhere.
                foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                {
                    var ps = imp.GetPlatformTextureSettings(platform);
                    if (ps.overridden) { ps.overridden = false; imp.SetPlatformTextureSettings(ps); }
                }

                imp.SaveAndReimport();
                updated++;
            }

            return new { ok = true, folder, updated };
        }
    }
}
