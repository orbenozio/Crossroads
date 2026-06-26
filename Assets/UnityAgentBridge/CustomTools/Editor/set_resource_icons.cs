using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Imports every "<resourceId>.png" in a folder as a Sprite and assigns it to that resource's HUD
    // icon on a Theme (Theme.ResourceLabel.icon), creating the entry if missing and preserving any
    // label override. Filenames must match the resource id (e.g. baby.png even when shown as "Heir").
    public static class set_resource_icons
    {
        [McpTool("set_resource_icons", "Assign every <resourceId>.png in a folder to Theme resource HUD icons")]
        public static object Invoke(string themePath = "", string folder = "")
        {
            if (string.IsNullOrEmpty(themePath)) throw new Exception("themePath is required");
            if (string.IsNullOrEmpty(folder)) throw new Exception("folder is required (e.g. Assets/Games/.../Art/Meters)");

            var theme = AssetDatabase.LoadAssetAtPath<Theme>(themePath);
            if (theme == null) throw new Exception("Theme not found at " + themePath);

            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder.TrimEnd('/') });
            if (guids.Length == 0) throw new Exception("no textures found under " + folder);

            int assigned = 0;
            var ids = new System.Collections.Generic.List<string>();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string resourceId = Path.GetFileNameWithoutExtension(path);

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

                Theme.ResourceLabel entry = theme.resourceLabels.Find(r => r.id == resourceId);
                if (entry == null)
                {
                    entry = new Theme.ResourceLabel { id = resourceId };
                    theme.resourceLabels.Add(entry);
                }
                entry.icon = sprite;
                assigned++;
                ids.Add(resourceId);
            }

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return new { ok = true, themePath, folder, assigned, resources = ids.ToArray() };
        }
    }
}
