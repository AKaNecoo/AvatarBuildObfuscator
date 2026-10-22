// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        private bool ProfileTextureSupported(Texture2D texture)
        {
            if (texture == null || texture.width < 1 || texture.height < 1 ||
                texture.mipmapCount > 1 && QualitySettings.globalTextureMipmapLimit != 0 ||
                texture.wrapModeU == TextureWrapMode.MirrorOnce || texture.wrapModeV == TextureWrapMode.MirrorOnce) return false;
            if (texture.isReadable && BytesPerPixel(texture.format) != 0) return true;
            // Streaming hints may only be applied to our own imported build copies. A
            // partially resident texture is retained; never read missing high-resolution mips.
            if (texture.streamingMipmaps && (!owned.Contains(texture) || texture.loadedMipmapLevel != 0)) return false;
            switch (texture.format)
            {
                case TextureFormat.RGBA32: case TextureFormat.ARGB32: case TextureFormat.BGRA32:
                case TextureFormat.RGB24: case TextureFormat.DXT1: case TextureFormat.DXT5:
                case TextureFormat.BC4: case TextureFormat.BC5: case TextureFormat.BC7:
                    return SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
                default: return false;
            }
        }

        private static long DecodedTextureBytes(Texture2D source)
        {
            long bytes = 0;
            int stride = source.isReadable ? BytesPerPixel(source.format) : 0;
            if (stride == 0) stride = 4;
            for (int mip = 0; mip < source.mipmapCount; mip++)
                bytes += (long)Math.Max(1, source.width >> mip) * Math.Max(1, source.height >> mip) * stride;
            return bytes;
        }

        private Texture2D RotateProfileTexture(Texture2D source)
        {
            if (source.isReadable && BytesPerPixel(source.format) != 0) return RotateTexture(source);
            bool streaming = source.streamingMipmaps;
            if (streaming && (!owned.Contains(source) || source.loadedMipmapLevel != 0))
                throw new InvalidOperationException("Streaming texture is not an owned, fully resident build copy");
            int previousRequest = source.requestedMipmapLevel;
            var shader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(UnityEditor.AssetDatabase.GUIDToAssetPath(
                "168f32a1dd4646c1b00682f424fe4b18"));
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("GPU mip readback shader unavailable");
            bool srgb = GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, source.mipmapCount, !srgb);
            Material reader = null;
            var previous = RenderTexture.active;
            bool previousSrgb = GL.sRGBWrite;
            try
            {
                reader = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                if (streaming) source.requestedMipmapLevel = 0;
                for (int mip = 0; mip < source.mipmapCount; mip++)
                {
                    if (streaming && source.loadedMipmapLevel != 0) throw new InvalidOperationException("Streaming mip residency changed");
                    int width = Math.Max(1, source.width >> mip), height = Math.Max(1, source.height >> mip);
                    RenderTexture rt = null;
                    Texture2D staging = null;
                    try
                    {
                        rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                            srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
                        reader.SetFloat("_Mip", mip);
                        GL.sRGBWrite = srgb;
                        Graphics.Blit(source, rt, reader);
                        RenderTexture.active = rt;
                        staging = new Texture2D(width, height, TextureFormat.RGBA32, false, !srgb);
                        staging.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                        var input = staging.GetRawTextureData<byte>();
                        var output = new byte[width * height * 4];
                        for (int pixel = 0; pixel < width * height; pixel++)
                            for (int channel = 0; channel < 4; channel++)
                                output[(width * height - 1 - pixel) * 4 + channel] = input[pixel * 4 + channel];
                        copy.SetPixelData(output, mip);
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        if (staging != null) Object.DestroyImmediate(staging);
                        if (rt != null) RenderTexture.ReleaseTemporary(rt);
                    }
                }
                copy.Apply(false, false);
                copy.name = settings.RenameAssets ? NewName() : source.name;
                copy.filterMode = source.filterMode; copy.anisoLevel = source.anisoLevel; copy.mipMapBias = source.mipMapBias;
                copy.wrapModeU = source.wrapModeU; copy.wrapModeV = source.wrapModeV; copy.wrapModeW = source.wrapModeW;
                return copy;
            }
            catch { Object.DestroyImmediate(copy); throw; }
            finally
            {
                RenderTexture.active = previous; GL.sRGBWrite = previousSrgb;
                if (reader != null) Object.DestroyImmediate(reader);
                if (streaming)
                {
                    if (previousRequest < 0) source.ClearRequestedMipmapLevel();
                    else source.requestedMipmapLevel = previousRequest;
                }
            }
        }
    }
}
