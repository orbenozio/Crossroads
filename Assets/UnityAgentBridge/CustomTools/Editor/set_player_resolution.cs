using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Sets the Standalone (PC) player resolution + window mode through the PlayerSettings API, so the
    // change is written by Unity itself (an external text edit to ProjectSettings.asset gets clobbered
    // while the Editor is running). For a portrait, Android-first game the PC build is test-only: render
    // Windowed at a portrait size that fits any monitor, instead of borderless-fullscreen at the
    // monitor's native landscape resolution (which upscales the 1080x1920 portrait UI 2-4x -> soft).
    public static class set_player_resolution
    {
        [McpTool("set_player_resolution", "Set Standalone player resolution/window: width, height, fullscreenMode (windowed|exclusive|maximized|borderless), native (use the monitor's native resolution)")]
        public static object Invoke(int width = 540, int height = 960, string fullscreenMode = "windowed", bool native = false)
        {
            PlayerSettings.defaultScreenWidth = width;
            PlayerSettings.defaultScreenHeight = height;
            PlayerSettings.defaultIsNativeResolution = native;
            PlayerSettings.resizableWindow = false;   // a stretched/resized window re-introduces the upscale

            switch (fullscreenMode.ToLowerInvariant())
            {
                case "exclusive": PlayerSettings.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
                case "maximized": PlayerSettings.fullScreenMode = FullScreenMode.MaximizedWindow; break;
                case "borderless": PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow; break;
                default: PlayerSettings.fullScreenMode = FullScreenMode.Windowed; break;
            }

            AssetDatabase.SaveAssets();
            return new
            {
                ok = true,
                width = PlayerSettings.defaultScreenWidth,
                height = PlayerSettings.defaultScreenHeight,
                fullscreenMode = PlayerSettings.fullScreenMode.ToString(),
                native = PlayerSettings.defaultIsNativeResolution,
                resizable = PlayerSettings.resizableWindow
            };
        }
    }
}
