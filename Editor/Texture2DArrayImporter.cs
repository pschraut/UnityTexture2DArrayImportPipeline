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
    [CanEditMultipleObjects]
    [HelpURL("https://docs.unity3d.com/Manual/SL-TextureArrays.html")]
    [ScriptedImporter(1, "tex2darray")]
    public partial class Texture2DArrayImporter : ScriptedImporter
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

        [Tooltip("A list of textures that are added to the texture array.")]
        [SerializeField]
        List<Texture2D> m_Textures = new List<Texture2D>();

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
        /// The file extension used for Texture2DArray assets without leading dot.
        /// </summary>
        public const string kFileExtension = "tex2darray";

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
                    ctx.DependsOnSourceAsset(path);
                }
            }
            
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
                var valid = Verify(this, n);
                if (valid != VerifyResult.Valid)
                {
                    result = false;
                    if (logToConsole)
                    {
                        var error = GetVerifyString(this, n);
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
    }
}
