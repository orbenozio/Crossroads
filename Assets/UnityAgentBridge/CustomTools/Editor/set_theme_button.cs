using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Imports a button plate as a 9-sliced Sprite (ornate ends stay, plain middle stretches to any width)
    // and assigns it to Theme.buttonSprite. The border (in source pixels) defines the non-stretching caps.
    public static class set_theme_button
    {
        [McpTool("set_theme_button", "Import a 9-sliced button plate and assign it to Theme.buttonSprite")]
        public static object Invoke(string themePath = "", string spritePath = "",
            int left = 380, int right = 380, int top = 120, int bottom = 120)
        {
            if (string.IsNullOrEmpty(themePath)) throw new Exception("themePath is required");
            if (string.IsNullOrEmpty(spritePath)) throw new Exception("spritePath is required");

            var theme = AssetDatabase.LoadAssetAtPath<Theme>(themePath);
            if (theme == null) throw new Exception("Theme not found at " + themePath);

            var imp = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (imp == null) throw new Exception("no texture importer at " + spritePath);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = new Vector4(left, bottom, right, top);   // Unity order: L, B, R, T
            imp.SaveAndReimport();
            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
                foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(spritePath))
                    if (o is Sprite sp) { sprite = sp; break; }
            if (sprite == null) throw new Exception("could not load a Sprite from " + spritePath);

            theme.buttonSprite = sprite;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return new { ok = true, themePath, spritePath, border = new[] { left, bottom, right, top }, sprite = sprite.name };
        }
    }
}
