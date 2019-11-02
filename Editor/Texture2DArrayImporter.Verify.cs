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
        /// Verifies the entry in the importer at the specified slice.
        /// </summary>
        /// <param name="importer">The Texture2DArrayImporter.</param>
        /// <param name="slice">The texture slice. Must be an index in the 'textures' array.</param>
        /// <returns>Returns the verify result.</returns>
        public static VerifyResult Verify(Texture2DArrayImporter importer, int slice)
        {
            Texture2D master = (importer.m_Textures.Count > 0) ? importer.m_Textures[0] : null;
            Texture2D texture = (slice >= 0 && importer.m_Textures.Count > slice) ? importer.m_Textures[slice] : null;

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
        /// <param name="importer">The Texture2DArrayImporter.</param>
        /// <param name="slice">The texture slice. Must be an index in the 'textures' array.</param>
        /// <returns>Returns a human readable string that specifies if anything is wrong, or an empty string if it is ok.</returns>
        public static string GetVerifyString(Texture2DArrayImporter importer, int slice)
        {
            var result = Verify(importer, slice);
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
                        var master = importer.m_Textures[0];
                        var texture = importer.m_Textures[slice];

                        return string.Format("Texture '{0}' uses '{1}' as format, but must be using '{2}' instead, because the texture for slice 0 '{3}' is using '{2}' too.",
                            texture.name, texture.format, master.format, master.name);
                    }

                case VerifyResult.MipmapMismatch:
                    {
                        var master = importer.m_Textures[0];
                        var texture = importer.m_Textures[slice];

                        return string.Format("Texture '{0}' has '{1}' mipmap(s), but must have '{2}' instead, because the texture for slice 0 '{3}' is having '{2}' mipmap(s). Please check if the 'Generate Mip Maps' setting for both textures is the same.",
                            texture.name, texture.mipmapCount, master.mipmapCount, master.name);
                    }

                case VerifyResult.SRGBTextureMismatch:
                    {
                        var master = importer.m_Textures[0];
                        var texture = importer.m_Textures[slice];

                        return string.Format("Texture '{0}' uses different 'sRGB' setting than slice 0 texture '{1}'.",
                            texture.name, master.name);
                    }

                case VerifyResult.WidthMismatch:
                case VerifyResult.HeightMismatch:
                    {
                        var master = importer.m_Textures[0];
                        var texture = importer.m_Textures[slice];

                        return string.Format("Texture '{0}' is {1}x{2} in size, but must be using the same size as the texture for slice 0 '{3}', which is {4}x{5}.",
                            texture.name, texture.width, texture.height, master.name, master.width, master.height);
                    }

                case VerifyResult.MasterNotAnAsset:
                case VerifyResult.NotAnAsset:
                    {
                        var texture = importer.m_Textures[slice];

                        return string.Format("Texture '{0}' is not saved to disk. Only texture assets that exist on disk can be added to a Texture2DArray asset.",
                            texture.name);
                    }
            }

            return "Unhandled validation issue.";
        }
    }
}
