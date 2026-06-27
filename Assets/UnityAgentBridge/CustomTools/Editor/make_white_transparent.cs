using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Cuts a flat near-white background out of a PNG to transparency (for art exported without alpha,
    // e.g. ChatGPT flattening "transparent" to white). Near-white = all of R,G,B above the threshold;
    // gold/colored pixels (low blue) are kept, so the plate/ornaments survive.
    public static class make_white_transparent
    {
        [McpTool("make_white_transparent", "Make near-white pixels transparent in a PNG (path, threshold 0-255)")]
        public static object Invoke(string path = "", int threshold = 240)
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("path is required");
            if (!File.Exists(path)) throw new Exception("file not found: " + path);

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path))) throw new Exception("could not decode PNG");

            var px = tex.GetPixels32();
            int changed = 0;
            for (int i = 0; i < px.Length; i++)
            {
                Color32 c = px[i];
                if (c.r > threshold && c.g > threshold && c.b > threshold) { c.a = 0; px[i] = c; changed++; }
            }
            tex.SetPixels32(px);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            return new { ok = true, path, threshold, total = px.Length, changed };
        }
    }
}
