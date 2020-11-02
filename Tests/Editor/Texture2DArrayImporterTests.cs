//
// Texture2D Array Importer for Unity. Copyright (c) 2019 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityTexture2DArrayImportPipeline
//
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Oddworm.EditorFramework.Tests
{
    class Texture2DArrayImporterTests
    {
        /// <summary>
        /// Creates a new Texture2DArray asset and returns the asset path.
        /// </summary>
        string BeginAssetTest()
        {
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/" + string.Format("Test_Texture2DArray.{0}", Texture2DArrayImporter.kFileExtension));
            System.IO.File.WriteAllText(path, "");
            AssetDatabase.Refresh();
            return path;
        }

        /// <summary>
        /// Deletes the asset specified by path.
        /// </summary>
        /// <param name="path">The path returned by BeginAssetTest().</param>
        void EndAssetTest(string path)
        {
            AssetDatabase.DeleteAsset(path);
        }

        [Test]
        public void DefaultSettings()
        {
            var path = BeginAssetTest();
            try
            {
                var importer = (Texture2DArrayImporter)AssetImporter.GetAtPath(path);

                Assert.AreEqual(1, importer.anisoLevel);
                Assert.AreEqual(FilterMode.Bilinear, importer.filterMode);
                Assert.AreEqual(TextureWrapMode.Repeat, importer.wrapMode);
                Assert.AreEqual(0, importer.textures.Length);
            }
            finally
            {
                EndAssetTest(path);
            }
        }

        [Test]
        public void ScriptingAPI_SetProperties()
        {
            var path = BeginAssetTest();
            try
            {
                var anisoLevel = 10;
                var filterMode = FilterMode.Trilinear;
                var wrapMode = TextureWrapMode.Mirror;

                var importer = (Texture2DArrayImporter)AssetImporter.GetAtPath(path);
                importer.anisoLevel = anisoLevel;
                importer.filterMode = filterMode;
                importer.wrapMode = wrapMode;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                // Reload importer
                importer = (Texture2DArrayImporter)AssetImporter.GetAtPath(path);

                Assert.AreEqual(anisoLevel, importer.anisoLevel);
                Assert.AreEqual(filterMode, importer.filterMode);
                Assert.AreEqual(wrapMode, importer.wrapMode);
                Assert.AreEqual(0, importer.textures.Length);
            }
            finally
            {
                EndAssetTest(path);
            }
        }


        [Test]
        public void ScriptingAPI_AddMemoryTexture()
        {
            var path = BeginAssetTest();
            try
            {
                System.Exception exception = null;
                var importer = (Texture2DArrayImporter)AssetImporter.GetAtPath(path);
                var texture = new Texture2D(64, 64, TextureFormat.RGB24, true);

                try
                {
                    importer.textures = new Texture2D[] { texture };
                }
                catch (System.Exception e)
                {
                    exception = e;
                }
                finally
                {
                    Texture2D.DestroyImmediate(texture);
                }

                Assert.IsTrue(exception is System.NotSupportedException);
            }
            finally
            {
                EndAssetTest(path);
            }
        }


        [Test]
        public void ScriptingAPI_AddNullTexture()
        {
            var path = BeginAssetTest();
            try
            {
                System.Exception exception = null;
                var importer = (Texture2DArrayImporter)AssetImporter.GetAtPath(path);

                try
                {
                    importer.textures = new Texture2D[] { null };
                }
                catch (System.Exception e)
                {
                    exception = e;
                }
                Assert.IsTrue(exception is System.NotSupportedException);
            }
            finally
            {
                EndAssetTest(path);
            }
        }

        [Test]
        public void ScriptingAPI_SetNullArray()
        {
            var path = BeginAssetTest();
            try
            {
                System.Exception exception = null;
                var importer = (Texture2DArrayImporter)AssetImporter.GetAtPath(path);

                try
                {
                    importer.textures = null;
                }
                catch (System.Exception e)
                {
                    exception = e;
                }
                Assert.IsTrue(exception is System.NotSupportedException);
            }
            finally
            {
                EndAssetTest(path);
            }
        }

        [Test]
        public void LoadTexture2DArray()
        {
            var path = BeginAssetTest();
            try
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path) as Texture2DArray;
                Assert.IsNotNull(asset);
            }
            finally
            {
                EndAssetTest(path);
            }
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        //[UnityTest]
        //public IEnumerator Texture2DArrayImporterTestWithEnumeratorPasses()
        //{
        //    // Use the Assert class to test conditions.
        //    // Use yield to skip a frame.
        //    yield return null;
        //}
    }
}
