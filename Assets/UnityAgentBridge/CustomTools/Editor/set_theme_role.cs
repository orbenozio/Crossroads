using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Sets a Theme palette color: either a base role (background/card/text/accent/approaching/willBreak) or an
    // optional role override (ring/divider/choiceHint/choiceGlow/hudPlate/textMuted - unset falls back to a
    // base role per THEMING.md). Keeps the look data-driven; the generic bridge set_property cannot reach the
    // OptionalColor wrappers. Color is r,g,b,a in 0..1. Pass clear=true to unset an optional role (inherit).
    public static class set_theme_role
    {
        [McpTool("set_theme_role", "Set a Theme palette color role (role=background|card|text|accent|approaching|willBreak|ring|divider|choiceHint|choiceGlow|hudPlate|textMuted; r,g,b,a in 0..1; clear=true unsets an optional role)")]
        public static object Invoke(string themePath = "", string role = "", float r = 0f, float g = 0f, float b = 0f, float a = 1f, bool clear = false)
        {
            if (string.IsNullOrEmpty(themePath)) throw new Exception("themePath is required");
            if (string.IsNullOrEmpty(role)) throw new Exception("role is required");

            var theme = AssetDatabase.LoadAssetAtPath<Theme>(themePath);
            if (theme == null) throw new Exception("Theme not found at " + themePath);

            var c = new Color(r, g, b, a);
            var opt = clear ? default(OptionalColor) : new OptionalColor(c);
            bool optional = true;

            switch (role.ToLowerInvariant())
            {
                // Base palette roles (always present; clear resets to the field's neutral default is not
                // supported here - a base role is a plain Color, so just assign it).
                case "background":   theme.background = c; optional = false; break;
                case "card":         theme.card = c; optional = false; break;
                case "text":         theme.text = c; optional = false; break;
                case "accent":       theme.accent = c; optional = false; break;
                case "approaching":  theme.approaching = c; optional = false; break;
                case "willbreak":    theme.willBreak = c; optional = false; break;
                // Optional role overrides (unset -> fall back to a base role).
                case "textmuted":    theme.textMuted = opt; break;
                case "ring":         theme.ring = opt; break;
                case "divider":      theme.divider = opt; break;
                case "choicehint":   theme.choiceHint = opt; break;
                case "choiceglow":   theme.choiceGlow = opt; break;
                case "hudplate":     theme.hudPlate = opt; break;
                default: throw new Exception("unknown role: " + role);
            }
            if (!optional && clear) throw new Exception("clear is only valid for optional roles (a base role is always set)");

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return new { ok = true, themePath, role, cleared = clear && optional, color = new[] { r, g, b, a } };
        }
    }
}
