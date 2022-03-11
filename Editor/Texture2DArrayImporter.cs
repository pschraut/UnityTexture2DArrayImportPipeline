//
// Texture2D Array Importer for Unity. Copyright (c) 2019-2021 Peter Schraut (www.console-dev.de). See LICENSE.md
// https://github.com/pschraut/UnityTexture2DArrayImportPipeline
//
#pragma warning disable IDE1006, IDE0017
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace Oddworm.EditorFramework
{
    [CanEditMultipleObjects]
    [HelpURL("https://docs.unity3d.com/Manual/SL-TextureArrays.html")]
    [ScriptedImporter(k_VersionNumber, Texture2DArrayImporter.kFileExtension)]
    public class Texture2DArrayImporter : ScriptedImporter
    {
        [Tooltip("Selects how the Texture behaves when tiled.")]
        [SerializeField]
        TextureWrapMode m_WrapMode = TextureWrapMode.Repeat;

        [Tooltip("Selects how the Texture is filtered when it gets stretched by 3D transformations.")]
        [SerializeField]
        FilterMode m_FilterMode = FilterMode.Bilinear;

        [Tooltip("Increases Texture quality when viewing the texture at a steep angle.\n0 = Disabled for all textures\n1 = Enabled for all textures in Quality Settings\n2..16 = Anisotropic filtering level")]
        [Range(0, 16)]
        [SerializeField]
        int m_AnisoLevel = 1;

        [SerializeField]
        bool m_IsReadable = false;

        [Tooltip("A list of textures that are added to the texture array.")]
        [SerializeField]
        List<Texture2D> m_Textures = new List<Texture2D>();

        public enum VerifyResult
        {
            Valid,
            Null,
            MasterNull,
            WidthMismatch,
            HeightMismatch,
            FormatMismatch,
            MipmapMismatch,
            SRGBTextureMismatch,
            NotAnAsset,
            MasterNotAnAsset,
        }

        /// <summary>
        /// Gets or sets the textures that are being used to create the texture array.
        /// </summary>
        public Texture2D[] textures
        {
            get { return m_Textures.ToArray(); }
            set
            {
                if (value == null)
                    throw new System.NotSupportedException("'textures' must not be set to 'null'. If you want to clear the textures array, set it to a zero-sized array instead.");

                for (var n=0; n< value.Length; ++n)
                {
                    if (value[n] == null)
                        throw new System.NotSupportedException(string.Format("The texture at array index '{0}' must not be 'null'.", n));

                    if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(value[n])))
                        throw new System.NotSupportedException(string.Format("The texture '{1}' at array index '{0}' does not exist on disk. Only texture assets can be added.", n, value[n].name));
                }

                m_Textures = new List<Texture2D>(value);
            }
        }

        /// <summary>
        /// Texture coordinate wrapping mode.
        /// </summary>
        public TextureWrapMode wrapMode
        {
            get { return m_WrapMode; }
            set { m_WrapMode = value; }
        }

        /// <summary>
        /// Filtering mode of the texture.
        /// </summary>
        public FilterMode filterMode
        {
            get { return m_FilterMode; }
            set { m_FilterMode = value; }
        }

        /// <summary>
        /// Anisotropic filtering level of the texture.
        /// </summary>
        public int anisoLevel
        {
            get { return m_AnisoLevel; }
            set { m_AnisoLevel = value; }
        }

        /// <summary>
        /// Set this to true if you want texture data to be readable from scripts.
        /// Set it to false to prevent scripts from reading texture data.
        /// </summary>
        public bool isReadable
        {
            get { return m_IsReadable; }
            set { m_IsReadable = value; }
        }

        /// <summary>
        /// The file extension used for Texture2DArray assets without leading dot.
        /// </summary>
        public const string kFileExtension = "texture2darray";

#if UNITY_2020_1_OR_NEWER
        const int k_VersionNumber = 202011;
#else
        const int k_VersionNumber = 201941;
#endif

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var width = 64;
            var height = 64;
            var mipmapEnabled = true;
            var textureFormat = TextureFormat.ARGB32;
            var srgbTexture = true;

            // Check if the input textures are valid to be used to build the texture array.
            var isValid = Verify(ctx, false);
            if (isValid)
            {
                // Use the texture assigned to the first slice as "master".
                // This means all other textures have to use same settings as the master texture.
                var sourceTexture = m_Textures[0];
                width = sourceTexture.width;
                height = sourceTexture.height;
                textureFormat = sourceTexture.format;

                var sourceTexturePath = AssetDatabase.GetAssetPath(sourceTexture);
                var textureImporter = (TextureImporter)AssetImporter.GetAtPath(sourceTexturePath);
                mipmapEnabled = textureImporter.mipmapEnabled;
                srgbTexture = textureImporter.sRGBTexture;
            }

            // Create the texture array.
            // When the texture array asset is being created, there are no input textures added yet,
            // thus we do Max(1, Count) to make sure to add at least 1 slice.
            var texture2DArray = new Texture2DArray(width, height, Mathf.Max(1, m_Textures.Count), textureFormat, mipmapEnabled, !srgbTexture);
            texture2DArray.wrapMode = m_WrapMode;
            texture2DArray.filterMode = m_FilterMode;
            texture2DArray.anisoLevel = m_AnisoLevel;

            if (isValid)
            {
                // If everything is valid, copy source textures over to the texture array.
                for (var n = 0; n < m_Textures.Count; ++n)
                {
                    var source = m_Textures[n];
                    Graphics.CopyTexture(source, 0, texture2DArray, n);
                }
            }
            else
            {
                // If there is any error, copy a magenta colored texture into every slice.
                // I was thinking to only make the invalid slice magenta, but then it's way less obvious that
                // something isn't right with the texture array. Thus I mark the entire texture array as broken.
                var errorTexture = new Texture2D(width, height, textureFormat, mipmapEnabled);
                try
                {
                    var errorPixels = errorTexture.GetPixels32();
                    for (var n = 0; n < errorPixels.Length; ++n)
                        errorPixels[n] = Color.magenta;
                    errorTexture.SetPixels32(errorPixels);
                    errorTexture.Apply();

                    for (var n = 0; n < texture2DArray.depth; ++n)
                        Graphics.CopyTexture(errorTexture, 0, texture2DArray, n);
                }
                finally
                {
                    DestroyImmediate(errorTexture);
                }
            }

            // Mark all input textures as dependency to the texture array.
            // This causes the texture array to get re-generated when any input texture changes or when the build target changed.
            for (var n = 0; n < m_Textures.Count; ++n)
            {
                var source = m_Textures[n];
                if (source != null)
                {
                    var path = AssetDatabase.GetAssetPath(source);
#if UNITY_2020_1_OR_NEWER
                    ctx.DependsOnArtifact(path);
#else
                    ctx.DependsOnSourceAsset(path);
#endif
                }
            }

#if !UNITY_2020_1_OR_NEWER
            // This value is not really used in this importer,
            // but getting the build target here will add a dependency to the current active buildtarget.
            // Because DependsOnArtifact does not exist in 2019.4, adding this dependency on top of the DependsOnSourceAsset
            // will force a re-import when the target platform changes in case it would have impacted any texture this importer depends on.
            var buildTarget = ctx.selectedBuildTarget;
#endif

            texture2DArray.Apply(false, !m_IsReadable);
            ctx.AddObjectToAsset("Texture2DArray", texture2DArray);
            ctx.SetMainObject(texture2DArray);

            if (!isValid)
            {
                // Run the verify step again, but this time we have the main object asset.
                // Console logs should ping the asset, but they don't in 2019.3 beta, bug?
                Verify(ctx, true);
            }
        }

        /// <summary>
        /// Checks if the asset is set up properly and all its dependencies are ok.
        /// </summary>
        /// <returns>
        /// Returns true if the asset can be imported, false otherwise.
        /// </returns>
        bool Verify(AssetImportContext ctx, bool logToConsole)
        {
            if (!SystemInfo.supports2DArrayTextures)
            {
                if (logToConsole)
                    ctx.LogImportError(string.Format("Import failed '{0}'. Your system does not support texture arrays.", ctx.assetPath), ctx.mainObject);

                return false;
            }

            if (m_Textures.Count > 0)
            {
                if (m_Textures[0] == null)
                {
                    if (logToConsole)
                        ctx.LogImportError(string.Format("Import failed '{0}'. The first element in the 'Textures' list must not be 'None'.", ctx.assetPath), ctx.mainObject);

                    return false;
                }
            }

            var result = m_Textures.Count > 0;
            for (var n = 0; n < m_Textures.Count; ++n)
            {
                var valid = Verify(n);
                if (valid != VerifyResult.Valid)
                {
                    result = false;
                    if (logToConsole)
                    {
                        var error = GetVerifyString(n);
                        if (!string.IsNullOrEmpty(error))
                        {
                            var msg = string.Format("Import failed '{0}'. {1}", ctx.assetPath, error);
                            ctx.LogImportError(msg, ctx.mainObject);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Verifies the entry in the importer at the specified slice.
        /// </summary>
        /// <param name="slice">The texture slice. Must be an index in the 'textures' array.</param>
        /// <returns>Returns the verify result.</returns>
        public VerifyResult Verify(int slice)
        {
            Texture2D master = (m_Textures.Count > 0) ? m_Textures[0] : null;
            Texture2D texture = (slice >= 0 && m_Textures.Count > slice) ? m_Textures[slice] : null;

            if (texture == null)
                return VerifyResult.Null;

            var textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            if (textureImporter == null)
                return VerifyResult.NotAnAsset;

            if (master == null)
                return VerifyResult.MasterNull;

            var masterImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(master)) as TextureImporter;
            if (masterImporter == null)
                return VerifyResult.MasterNotAnAsset;

            if (texture.width != master.width)
                return VerifyResult.WidthMismatch;

            if (texture.height != master.height)
                return VerifyResult.HeightMismatch;

            if (texture.format != master.format)
                return VerifyResult.FormatMismatch;

            if (texture.mipmapCount != master.mipmapCount)
                return VerifyResult.MipmapMismatch;

            if (textureImporter.sRGBTexture != masterImporter.sRGBTexture)
                return VerifyResult.SRGBTextureMismatch;

            return VerifyResult.Valid;
        }

        /// <summary>
        /// Verifies the entry in the importer at the specified slice.
        /// </summary>
        /// <param name="slice">The texture slice. Must be an index in the 'textures' array.</param>
        /// <returns>Returns a human readable string that specifies if anything is wrong, or an empty string if it is ok.</returns>
        public string GetVerifyString(int slice)
        {
            var result = Verify(slice);
            switch (result)
            {
                case VerifyResult.Valid:
                    {
                        return "";
                    }

                case VerifyResult.MasterNull:
                    {
                        return "The texture for slice 0 must not be 'None'.";
                    }

                case VerifyResult.Null:
                    {
                        return string.Format("The texture for slice {0} must not be 'None'.", slice);
                    }

                case VerifyResult.FormatMismatch:
                    {
                        var master = m_Textures[0];
                        var texture = m_Textures[slice];

                        return string.Format("Texture '{0}' uses '{1}' as format, but must be using '{2}' instead, because the texture for slice 0 '{3}' is using '{2}' too.",
                            texture.name, texture.format, master.format, master.name);
                    }

                case VerifyResult.MipmapMismatch:
                    {
                        var master = m_Textures[0];
                        var texture = m_Textures[slice];

                        return string.Format("Texture '{0}' has '{1}' mipmap(s), but must have '{2}' instead, because the texture for slice 0 '{3}' is having '{2}' mipmap(s). Please check if the 'Generate Mip Maps' setting for both textures is the same.",
                            texture.name, texture.mipmapCount, master.mipmapCount, master.name);
                    }

                case VerifyResult.SRGBTextureMismatch:
                    {
                        var master = m_Textures[0];
                        var texture = m_Textures[slice];

                        return string.Format("Texture '{0}' uses different 'sRGB' setting than slice 0 texture '{1}'.",
                            texture.name, master.name);
                    }

                case VerifyResult.WidthMismatch:
                case VerifyResult.HeightMismatch:
                    {
                        var master = m_Textures[0];
                        var texture = m_Textures[slice];

                        return string.Format("Texture '{0}' is {1}x{2} in size, but must be using the same size as the texture for slice 0 '{3}', which is {4}x{5}.",
                            texture.name, texture.width, texture.height, master.name, master.width, master.height);
                    }

                case VerifyResult.MasterNotAnAsset:
                case VerifyResult.NotAnAsset:
                    {
                        var texture = m_Textures[slice];

                        return string.Format("Texture '{0}' is not saved to disk. Only texture assets that exist on disk can be added to a Texture2DArray asset.",
                            texture.name);
                    }
            }

            return "Unhandled validation issue.";
        }

        [MenuItem("Assets/Create/Texture2D Array", priority = 310)]
        static void CreateTexture2DArrayMenuItem()
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
                directoryPath += "/";
            if (string.IsNullOrEmpty(directoryPath))
                directoryPath = "Assets/";

            var fileName = string.Format("New Texture2DArray.{0}", kFileExtension);
            directoryPath = AssetDatabase.GenerateUniqueAssetPath(directoryPath + fileName);
            ProjectWindowUtil.CreateAssetWithContent(directoryPath, "This file represents a Texture2DArray asset for Unity.\nYou need the 'Texture2DArray Import Pipeline' package available at https://github.com/pschraut/UnityTexture2DArrayImportPipeline to properly import this file in Unity.");
        }
    }
}
