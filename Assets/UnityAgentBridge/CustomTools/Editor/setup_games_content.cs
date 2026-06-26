using UnityEngine;
using UnityAgentBridge.Editor;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Parameterless setup that creates the ScriptableObject content assets games need. It calls the
    // generic create_resource_set / create_theme tools IN-PROCESS (real C# args), which sidesteps the
    // call_tool arg-forwarding bug (see docs/bridge-wishlist.md - args arrive empty over call_tool).
    // Run once via the bridge; idempotent (the generic tools find-or-create).
    public static class setup_games_content
    {
        [McpTool("setup_games_content", "Create content ScriptableObjects: _Template theme + NewbornKing resources/theme")]
        public static object Invoke()
        {
            // _Template: a theme.asset (palette only) so the template has a wired Theme.
            var templateTheme = create_theme.Invoke("Assets/Games/_Template/Content/theme.asset");

            // NewbornKing: 4 meters. sleep breaks on Both (collapse / withdrawal), the rest on Min.
            var nbkResources = create_resource_set.Invoke(
                "Assets/Games/NewbornKing/Content/resources.asset",
                "sleep:Sleep:0:10:6:Both:2|sanity:Sanity:0:10:6:Min:2|money:Money:0:10:6:Min:2|baby:Baby:0:10:6:Min:2");

            // NewbornKing theme: override 'Baby' -> 'Heir' to demonstrate J8 (Theme label > ResourceDef).
            var nbkTheme = create_theme.Invoke(
                "Assets/Games/NewbornKing/Content/theme.asset",
                "baby=Heir");

            return new { ok = true, templateTheme, nbkResources, nbkTheme };
        }
    }
}
