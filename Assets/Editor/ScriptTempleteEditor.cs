using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public class ScriptTemplateEditor : AssetModificationProcessor
    {
        private const string AuthorName = "NiShiqiang";
        private const string DateFormat = "yyyy/MM/dd HH:mm:ss";
        private const string Namespace = "Game";
        private const string ScriptExtension = ".cs";

        private static void OnWillCreateAsset(string path)
        {
            string processedPath = path.Replace(".meta", "");
            if (Path.GetExtension(processedPath) != ScriptExtension)
            {
                return;
            }

            if (File.Exists(processedPath))
            {
                string allText = File.ReadAllText(processedPath);
                allText = allText.Replace("#Name#", AuthorName);
                allText = allText.Replace("#CreateTime#", DateTime.Now.ToString(DateFormat));

                // 获取脚本的相对路径并生成新的命名空间，只保留Scripts下的第一个层级
                string relativePath = Path.GetDirectoryName(processedPath).Replace("Assets/Scripts/", "");
                string[] pathParts = relativePath.Split(Path.DirectorySeparatorChar);
                var newNamespace = Namespace;
                if(pathParts.Length >2)
                {
                    for (int i = 2; i < pathParts.Length; i++)
                    {
                        var part = pathParts[i];
                        newNamespace+= $".{part}";
                    }
                }
                allText = allText.Replace("#Namespace#", newNamespace);
                File.WriteAllText(processedPath, allText);
                AssetDatabase.Refresh();
            }
        }
    }
}