using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Sets the Unity built-in splash to a single full-bleed title image (the baked-in-title poster),
    // through the PlayerSettings.SplashScreen API so Unity serializes it itself (an external text edit
    // to ProjectSettings.asset gets clobbered while the Editor runs). Imports the image as a crisp
    // Sprite (BC7, no mipmaps), clears any separate logo overlay so the wordmark is not doubled, and
    // tries to hide the Made-with-Unity logo (only possible on a Pro/Plus license; reported back).
    public static class set_splash
    {
        [McpTool("set_splash", "Set the Unity splash to a full title image. path = image asset path; removeLogos clears the separate logo overlay; showUnityLogo=false needs Unity Pro/Plus.")]
        public static object Invoke(string path = "", bool removeLogos = true, bool showUnityLogo = false)
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("path is required (e.g. Assets/Games/NewbornKing/Art/splash.png)");

            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) throw new Exception("not a texture asset: " + path);

            // Import as a crisp Sprite so it can be assigned as the splash background.
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;   // one full-image Sprite (not Multiple/sliced -> no sub-sprite to load)
            imp.textureCompression = TextureImporterCompression.CompressedHQ;   // BC7 on desktop, near-lossless
            imp.compressionQuality = 100;
            imp.crunchedCompression = false;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            // LoadAssetAtPath<Sprite> can return null synchronously right after a reimport; fall back to
            // scanning the asset's sub-objects for the generated Sprite.
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is Sprite s) { sprite = s; break; }
            }
            if (sprite == null) throw new Exception("could not load Sprite at " + path);

            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.background = sprite;          // landscape fallback
            PlayerSettings.SplashScreen.backgroundPortrait = sprite;  // portrait (this game's orientation)
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.09f, 0.08f, 0.16f, 1f); // letterbox bars
            PlayerSettings.SplashScreen.blurBackgroundImage = false;   // the poster IS the content - do not blur it
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Static;   // no dolly zoom (would crop the title text)

            if (removeLogos)
                PlayerSettings.SplashScreen.logos = new PlayerSettings.SplashScreenLogo[0];

            bool unityLogoApplied = true;
            try { PlayerSettings.SplashScreen.showUnityLogo = showUnityLogo; }
            catch { unityLogoApplied = false; }

            // Clear the legacy VR splash texture (it pointed at the separate wordmark logo).
            PlayerSettings.virtualRealitySplashScreen = null;

            AssetDatabase.SaveAssets();
            return new
            {
                ok = true,
                path,
                width = sprite.texture != null ? sprite.texture.width : 0,
                height = sprite.texture != null ? sprite.texture.height : 0,
                logosCleared = removeLogos,
                requestedShowUnityLogo = showUnityLogo,
                actualShowUnityLogo = PlayerSettings.SplashScreen.showUnityLogo,
                unityLogoApplied
            };
        }
    }
}
