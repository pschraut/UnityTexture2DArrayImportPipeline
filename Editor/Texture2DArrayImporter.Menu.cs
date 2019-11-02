//
// Texture2D Array Importer for Unity. Copyright (c) 2019 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityTexture2DArrayImportPipeline
//
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using System.IO;
using System.Collections.Generic;

namespace Oddworm.EditorFramework
{
    public partial class Texture2DArrayImporter
    {
        public const string kMenuItemPath = "Assets/Create/Texture2D Array";

        [MenuItem(kMenuItemPath, priority = 310)]
        static void DoMenuItem()
        {
            // https://forum.unity.com/threads/how-to-implement-create-new-asset.759662/
            string directoryPath = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                directoryPath = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(directoryPath) && File.Exists(directoryPath))
                {
                    directoryPath = Path.GetDirectoryName(directoryPath);
                    break;
                }
            }
            directoryPath = directoryPath.Replace("\\", "/");
            if (directoryPath.Length > 0 && directoryPath[directoryPath.Length - 1] != '/')
                directoryPath = directoryPath + "/";
            if (string.IsNullOrEmpty(directoryPath))
                directoryPath = "Assets/";

            var fileName = string.Format("New Texture2DArray.{0}", kFileExtension);
            directoryPath = AssetDatabase.GenerateUniqueAssetPath(directoryPath + fileName);
            ProjectWindowUtil.CreateAssetWithContent(directoryPath, "This file represents a Texture2DArray asset for Unity.\nYou need the 'Texture2DArray Import Pipeline' package available at https://github.com/pschraut/UnityTexture2DArrayImportPipeline to properly import this file in Unity.");
        }
    }
}
