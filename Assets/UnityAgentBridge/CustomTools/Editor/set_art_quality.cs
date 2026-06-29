using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Forces consistent, anti-aliased import settings on every texture under a folder. Keeps compression
    // (small build) but uses HIGH-QUALITY compression (BC7 on desktop / ASTC on mobile - near-lossless).
    // By default generates mipmaps + trilinear filtering so any sprite drawn smaller than its native size
    // (icons, portraits, the logo, plates) downsamples smoothly instead of aliasing into jaggies/shimmer.
    // maxSize lets each folder be right-sized to its on-screen size. Per-platform overrides are cleared so
    // every texture resolves to the same default format (ASTC on the Android build).
    public static class set_art_quality
    {
        [McpTool("set_art_quality", "Set consistent crisp import settings on textures under a folder. mipmaps=true (default) -> mipmaps + trilinear (anti-aliased downscale). compressed=true -> BC7/ASTC HQ; compressed=false -> Uncompressed RGBA32 (no banding, bigger build).")]
        public static object Invoke(string folder = "", int maxSize = 2048, bool compressed = true, bool mipmaps = true)
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

                // Uncompressed = RGBA32, zero block artifacts (the definitive "is it the compression?" test).
                // CompressedHQ = BC7 on desktop / ASTC on mobile - small, but can band on smooth dark gradients.
                imp.textureCompression = compressed ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
                imp.compressionQuality = 100;
                imp.crunchedCompression = false;
                // Mipmaps + trilinear fix the aliasing/shimmer when art is shown below native size; harmless
                // (base mip only) when shown at or above native. Disable only for art that must match source 1:1.
                imp.mipmapEnabled = mipmaps;
                imp.filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear;
                imp.maxTextureSize = maxSize;

                // Clear any per-platform overrides so the same HQ default format is used everywhere (ASTC on Android).
                foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                {
                    var ps = imp.GetPlatformTextureSettings(platform);
                    if (ps.overridden) { ps.overridden = false; imp.SetPlatformTextureSettings(ps); }
                }

                imp.SaveAndReimport();
                updated++;
            }

            return new { ok = true, folder, updated, mipmaps,
                compression = compressed ? "CompressedHQ (BC7/ASTC)" : "Uncompressed (RGBA32)" };
        }
    }
}
