using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Forces crisp import settings on every texture under a folder. Keeps compression (small build) but
    // uses HIGH-QUALITY compression (BC7 on desktop - same byte size as BC3, near-lossless). Generates
    // mipmaps and clamps maxSize so sprites shown much smaller than native (portraits, icons) downsample
    // smoothly instead of aliasing into a "pixelated"/noisy look. maxSize lets each folder be right-sized
    // to its on-screen size.
    public static class set_art_quality
    {
        [McpTool("set_art_quality", "Set crisp BC7 + mipmap import settings on textures under a folder (right-size via maxSize)")]
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
                imp.mipmapEnabled = false;   // full resolution, no mip softening - UI art must match the source
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
