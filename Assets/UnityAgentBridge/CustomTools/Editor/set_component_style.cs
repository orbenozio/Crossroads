using System;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.UI;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Sets a Theme component-style metric on the medallion or meter struct (see THEMING.md). Float fields take
    // `value`; color fields (medallion.ringColor / meter.iconTint) take r,g,b,a. Pass clear=true to unset a
    // field (inherit the engine default). The generic bridge set_property cannot reach these OptionalFloat /
    // OptionalColor wrappers nested in the style structs, hence this tool.
    public static class set_component_style
    {
        [McpTool("set_component_style", "Set a Theme component style (widget=medallion|meter; field: medallion=size|ringThickness|innerFraction|ringColor, meter=iconSize|frameSize|iconTint; float fields use value=, color fields use r,g,b,a; clear=true unsets)")]
        public static object Invoke(string themePath = "", string widget = "", string field = "",
            float value = 0f, float r = 0f, float g = 0f, float b = 0f, float a = 1f, bool clear = false)
        {
            if (string.IsNullOrEmpty(themePath)) throw new Exception("themePath is required");
            if (string.IsNullOrEmpty(widget)) throw new Exception("widget is required (medallion|meter)");
            if (string.IsNullOrEmpty(field)) throw new Exception("field is required");

            var theme = AssetDatabase.LoadAssetAtPath<Theme>(themePath);
            if (theme == null) throw new Exception("Theme not found at " + themePath);

            var f = clear ? default(OptionalFloat) : new OptionalFloat(value);
            var col = clear ? default(OptionalColor) : new OptionalColor(new Color(r, g, b, a));
            string w = widget.ToLowerInvariant();
            string fl = field.ToLowerInvariant();

            if (w == "medallion")
            {
                switch (fl)
                {
                    case "size":          theme.medallion.size = f; break;
                    case "ringthickness": theme.medallion.ringThickness = f; break;
                    case "innerfraction": theme.medallion.innerFraction = f; break;
                    case "ringcolor":     theme.medallion.ringColor = col; break;
                    default: throw new Exception("unknown medallion field: " + field);
                }
            }
            else if (w == "meter")
            {
                switch (fl)
                {
                    case "iconsize":  theme.meter.iconSize = f; break;
                    case "framesize": theme.meter.frameSize = f; break;
                    case "icontint":  theme.meter.iconTint = col; break;
                    default: throw new Exception("unknown meter field: " + field);
                }
            }
            else throw new Exception("unknown widget: " + widget + " (medallion|meter)");

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            return new { ok = true, themePath, widget = w, field = fl, cleared = clear };
        }
    }
}
