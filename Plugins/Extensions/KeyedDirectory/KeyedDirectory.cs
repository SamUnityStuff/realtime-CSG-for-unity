using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCSG.Plugins.Extensions.KeyedDirectory
{
    [CreateAssetMenu(menuName = "Editor/KeyedDirectory")]
    public class KeyedDirectory : ScriptableObject
    {
        
        private static Dictionary<string, string> dict = new();
        public static string GetDirectory(string key)
        {
            #if UNITY_EDITOR
            if (dict.TryGetValue(key, out string path))
            {
                return path;
            }
            
            string[] AssetsGUID = AssetDatabase.FindAssets($"t:{nameof(KeyedDirectory)}");
            for (int i = 0; i < AssetsGUID.Length; i++)
            {
                GUID g = new GUID(AssetsGUID[i]);
                KeyedDirectory asset = AssetDatabase.LoadAssetByGUID<KeyedDirectory>(g);
                if (asset == null)
                {
                    Debug.LogError("hi");
                }

                string assetName = asset.name;
                if (key == assetName)
                {
                    dict[key] = assetName;
                    string finalPath = AssetDatabase.GUIDToAssetPath(g);
                    finalPath = System.IO.Path.GetDirectoryName(finalPath) + System.IO.Path.DirectorySeparatorChar;
                    //Debug.Log($"found! {finalPath}");
                    return finalPath;
                    //break;
                }
            }

            Debug.LogError($"Could not find KeyedDirectory {key}");
            #else
            Debug.LogError($"Keyed Directory is editor-only! Looking for: {key}");
            #endif
            return null;
        }
    }
}