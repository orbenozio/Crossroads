using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Sets sane mobile audio import settings (perf review). kind=music -> Streaming + Vorbis (the clip is
    // decoded on the fly, not held as raw PCM in RAM). kind=sfx -> Decompress On Load + PCM (tiny clips
    // play instantly with no decode latency). Applied to the default sample settings (all platforms).
    public static class set_audio_import
    {
        [McpTool("set_audio_import", "Set audio import for mobile: kind=music (Streaming+Vorbis) or sfx (DecompressOnLoad+PCM)")]
        public static object Invoke(string path = "", string kind = "music")
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("path is required");
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) throw new Exception("not an audio asset: " + path);

            var s = imp.defaultSampleSettings;
            if (kind.ToLowerInvariant() == "sfx")
            {
                s.loadType = AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.PCM;
            }
            else
            {
                s.loadType = AudioClipLoadType.Streaming;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
            }
            imp.defaultSampleSettings = s;
            imp.forceToMono = false;
            imp.SaveAndReimport();

            return new { ok = true, path, kind, loadType = s.loadType.ToString(), format = s.compressionFormat.ToString() };
        }
    }
}
