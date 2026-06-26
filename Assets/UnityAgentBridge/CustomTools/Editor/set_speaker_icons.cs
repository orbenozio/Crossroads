using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Imports every "<speakerId>.png" in a folder as a Sprite and assigns it to the matching
    // SpeakerStyle.icon on a Theme (creating the SpeakerStyle if missing, preserving any existing tint).
    // Keeps speaker art data-driven: the theme owns it, so a clone swaps portraits via its theme (J8).
    public static class set_speaker_icons
    {
        [McpTool("set_speaker_icons", "Assign every <speakerId>.png in a folder to Theme speaker icons")]
        public static object Invoke(string themePath = "", string folder = "")
        {
            if (string.IsNullOrEmpty(themePath)) throw new Exception("themePath is required");
            if (string.IsNullOrEmpty(folder)) throw new Exception("folder is required (e.g. Assets/Games/.../Art/Speakers)");

            var theme = AssetDatabase.LoadAssetAtPath<Theme>(themePath);
            if (theme == null) throw new Exception("Theme not found at " + themePath);

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') });
            if (guids.Length == 0) throw new Exception("no textures found under " + folder);

            int assigned = 0;
            var ids = new System.Collections.Generic.List<string>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string speakerId = Path.GetFileNameWithoutExtension(path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                        if (o is Sprite sp) { sprite = sp; break; }
                if (sprite == null) continue;

                var style = theme.GetSpeaker(speakerId);
                if (style == null)
                {
                    style = new Theme.SpeakerStyle { id = speakerId, tint = Color.white };
                    theme.speakers.Add(style);
                }
                style.icon = sprite;
                assigned++;
                ids.Add(speakerId);
            }

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return new { ok = true, themePath, folder, assigned, speakers = ids.ToArray() };
        }
    }
}
