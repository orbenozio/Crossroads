using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Assigns an audio file to a Theme audio slot (field=music|swipeSfx|clickSfx). Unity auto-imports
    // mp3/wav/ogg as AudioClip. Keeps audio data-driven: the theme owns it, so cloning swaps it (J8).
    public static class set_theme_audio
    {
        [McpTool("set_theme_audio", "Assign an audio clip to a Theme slot (field=music|musicMenu|swipeSfx|cardSfx|clickSfx)")]
        public static object Invoke(string themePath = "", string clipPath = "", string field = "music")
        {
            if (string.IsNullOrEmpty(themePath)) throw new Exception("themePath is required");
            if (string.IsNullOrEmpty(clipPath)) throw new Exception("clipPath is required");

            var theme = AssetDatabase.LoadAssetAtPath<Theme>(themePath);
            if (theme == null) throw new Exception("Theme not found at " + themePath);

            AssetDatabase.ImportAsset(clipPath, ImportAssetOptions.ForceSynchronousImport);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) throw new Exception("could not load an AudioClip from " + clipPath + " (is it inside Assets/?)");

            switch (field.ToLowerInvariant())
            {
                case "musicmenu": theme.musicMenu = clip; break;
                case "swipesfx":  theme.swipeSfx = clip; break;
                case "cardsfx":   theme.cardSfx = clip; break;
                case "clicksfx":  theme.clickSfx = clip; break;
                default:          theme.music = clip; field = "music"; break;
            }

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return new { ok = true, themePath, clipPath, field, clip = clip.name, seconds = clip.length };
        }
    }
}
