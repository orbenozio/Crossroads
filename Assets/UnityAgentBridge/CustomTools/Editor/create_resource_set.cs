using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityAgentBridge.Editor;
using Crossroads.Engine;

namespace UnityAgentBridge.Editor.CustomTools
{
    // Data-driven creation of a ResourceSet ScriptableObject (the generic bridge tools cannot author a
    // ScriptableObject - see docs/bridge-wishlist.md). One [McpTool] per class (the invoker binds by class).
    // defs format: "id:Display:min:max:start:breakOn:dangerBand|..."  breakOn = Min|Max|Both
    //   e.g. "sleep:Sleep:0:10:6:Both:2|sanity:Sanity:0:10:6:Both:2"
    public static class create_resource_set
    {
        [McpTool("create_resource_set", "Create a ResourceSet asset at path from a delimited defs string")]
        public static object Invoke(string path = "", string defs = "")
        {
            if (string.IsNullOrEmpty(path)) throw new Exception("path is required");
            if (string.IsNullOrEmpty(defs)) throw new Exception("defs is required");

            var list = new List<ResourceDef>();
            foreach (var raw in defs.Split('|'))
            {
                var p = raw.Split(':');
                if (p.Length != 7) throw new Exception($"bad def '{raw}' - expected id:Display:min:max:start:breakOn:dangerBand");
                list.Add(new ResourceDef
                {
                    id = p[0].Trim(),
                    displayName = p[1].Trim(),
                    min = int.Parse(p[2]),
                    max = int.Parse(p[3]),
                    start = int.Parse(p[4]),
                    breakOn = (BreakOn)Enum.Parse(typeof(BreakOn), p[5].Trim(), true),
                    dangerBand = int.Parse(p[6])
                });
            }

            var set = AssetDatabase.LoadAssetAtPath<ResourceSet>(path);
            bool created = set == null;
            if (created) set = ScriptableObject.CreateInstance<ResourceSet>();
            set.resources = list.ToArray();
            if (created) AssetDatabase.CreateAsset(set, path);
            else EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();

            return new { ok = true, path, created, resources = set.resources.Length };
        }
    }
}
