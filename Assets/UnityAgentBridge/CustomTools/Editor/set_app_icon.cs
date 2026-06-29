using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Sets the application launcher icon from a single square texture: the cross-platform default icon
    // plus every Android icon kind (adaptive foreground+background, round, legacy), each filled with the
    // same art so any launcher mask (squircle/circle/square) shows it edge-to-edge. The texture is first
    // re-imported readable + uncompressed so PlayerSettings can copy its pixels.
    public static class set_app_icon
    {
        [McpTool("set_app_icon", "Set the app/launcher icon (cross-platform default + all Android kinds) from one square texture path.")]
        public static object Invoke(string path = "")
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("path is required (e.g. Assets/Games/NewbornKing/Art/Icon/icon-crown-shield.png)");

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) throw new Exception("not a texture: " + path);
            // Icons must be readable and uncrunched so PlayerSettings can sample them cleanly.
            imp.textureType = TextureImporterType.Default;
            imp.isReadable = true;
            imp.mipmapEnabled = false;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) throw new Exception("could not load texture at " + path);

            // Cross-platform default icon (Unity falls back to this for any platform without its own set).
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { tex });

            string androidResult;
            try
            {
                // Query the kinds generically (adaptive/round/legacy) so this never has to reference the
                // Android-module-only AndroidPlatformIconKind type, then fill every size + layer with the art.
                var android = NamedBuildTarget.Android;
                int kinds = 0;
                foreach (var kind in PlayerSettings.GetSupportedIconKindsForPlatform(BuildTargetGroup.Android))
                {
                    var icons = PlayerSettings.GetPlatformIcons(android, kind);
                    foreach (var icon in icons)
                        for (int layer = 0; layer < icon.maxLayerCount; layer++)
                            icon.SetTexture(tex, layer);
                    PlayerSettings.SetPlatformIcons(android, kind, icons);
                    kinds++;
                }
                androidResult = kinds + " icon kind(s) set";
            }
            catch (Exception e)
            {
                androidResult = "android icons skipped (" + e.Message + ") - default icon still applies";
            }

            AssetDatabase.SaveAssets();
            return new { ok = true, path, defaultIcon = true, android = androidResult };
        }
    }
}
