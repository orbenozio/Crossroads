using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Imports an image as a Sprite and assigns it to a Theme sprite slot (keyArt / logo / cardArt /
    // meterFrame). Keeps art data-driven: the theme owns it, so cloning swaps art via its theme (J8).
    public static class set_theme_art
    {
        [McpTool("set_theme_art", "Import an image as a Sprite into a Theme slot (field=keyArt|logo|cardArt|meterFrame|menuIcon|buttonSprite)")]
        public static object Invoke(string themePath = "", string spritePath = "", string field = "keyArt")
        {
            if (string.IsNullOrEmpty(themePath)) throw new Exception("themePath is required");
            if (string.IsNullOrEmpty(spritePath)) throw new Exception("spritePath is required");

            var theme = AssetDatabase.LoadAssetAtPath<Theme>(themePath);
            if (theme == null) throw new Exception("Theme not found at " + themePath);

            // Ensure the texture is imported as a single UI Sprite (synchronously, so the sub-asset exists).
            var importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer == null) throw new Exception("no texture importer at " + spritePath + " (is the image inside Assets/?)");
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
                foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(spritePath))
                    if (o is Sprite sp) { sprite = sp; break; }
            if (sprite == null) throw new Exception("could not load a Sprite from " + spritePath);

            switch (field.ToLowerInvariant())
            {
                case "logo":         theme.logo = sprite; break;
                case "cardart":      theme.cardArt = sprite; break;
                case "meterframe":   theme.meterFrame = sprite; break;
                case "menuicon":     theme.menuIcon = sprite; break;
                case "buttonsprite": theme.buttonSprite = sprite; break;
                default:             theme.keyArt = sprite; field = "keyArt"; break;
            }
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();

            return new { ok = true, themePath, spritePath, field, sprite = sprite.name };
        }
    }
}
