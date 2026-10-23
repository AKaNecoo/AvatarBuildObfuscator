// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        private static bool TryBlockFormat(TextureFormat format, out TextureBlockFormat block)
        {
            switch (format)
            {
                case TextureFormat.DXT1: block = TextureBlockFormat.Dxt1; return true;
                case TextureFormat.DXT5: block = TextureBlockFormat.Dxt5; return true;
                case TextureFormat.BC4: block = TextureBlockFormat.Bc4; return true;
                case TextureFormat.BC5: block = TextureBlockFormat.Bc5; return true;
                default: block = default; return false;
            }
        }

        private static long RgbaWorkingBytes(Texture2D source)
        {
            long bytes = 0;
            for (int mip = 0; mip < source.mipmapCount; mip++)
                bytes += (long)Math.Max(1, source.width >> mip) * Math.Max(1, source.height >> mip) * 4;
            return bytes;
        }

        private Texture2D RotateCompressedTexture(Texture2D source, TextureBlockFormat format)
        {
            bool streaming = source.streamingMipmaps;
            if (streaming && (!owned.Contains(source) || source.loadedMipmapLevel != 0))
                throw new InvalidOperationException("Streaming texture is not an owned, fully resident build copy");
            int previousRequest = source.requestedMipmapLevel;
            Texture2D readable = null, copy = null;
            try
            {
                if (streaming) source.requestedMipmapLevel = 0;
                var input = source;
                bool linear = !GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
                if (!source.isReadable)
                {
                    readable = new Texture2D(source.width, source.height, source.format, source.mipmapCount, linear);
                    Graphics.CopyTexture(source, readable);
                    readable.Apply(false, false);
                    input = readable;
                }
                copy = new Texture2D(source.width, source.height, source.format, source.mipmapCount, linear);
                for (int mip = 0; mip < source.mipmapCount; mip++)
                {
                    if (streaming && source.loadedMipmapLevel != 0) throw new InvalidOperationException("Streaming mip residency changed");
                    var data = input.GetPixelData<byte>(mip).ToArray();
                    copy.SetPixelData(TextureBlockRotation.RotateMip(data, Math.Max(1, source.width >> mip), Math.Max(1, source.height >> mip), format), mip);
                }
                copy.Apply(false, !source.isReadable);
                ConfigureRotatedTexture(source, copy);
                return copy;
            }
            catch { if (copy != null) Object.DestroyImmediate(copy); throw; }
            finally
            {
                if (readable != null) Object.DestroyImmediate(readable);
                if (streaming)
                {
                    if (previousRequest < 0) source.ClearRequestedMipmapLevel();
                    else source.requestedMipmapLevel = previousRequest;
                }
            }
        }

        private static void RecompressBc7(Texture2D source, Texture2D copy, List<byte[]> expected, Material reader)
        {
            EditorUtility.CompressTexture(copy, source.format, TextureCompressionQuality.Best);
            copy.Apply(false, false);
            if (copy.format != source.format || copy.mipmapCount != source.mipmapCount)
                throw new InvalidOperationException("Compression changed the format or mip chain");
            for (int mip = 0; mip < source.mipmapCount; mip++)
            {
                var actual = ReadRgbaMip(copy, mip, reader);
                if (actual.Length != expected[mip].Length) throw new InvalidOperationException("Compressed mip size changed");
                for (int i = 0; i < actual.Length; i++)
                    if (Math.Abs(actual[i] - expected[mip][i]) > 1)
                        throw new InvalidOperationException("BC7 recompression exceeds the per-channel 1/255 quality limit");
            }
        }

        private static byte[] ReadRgbaMip(Texture2D source, int mip, Material reader)
        {
            int width = Math.Max(1, source.width >> mip), height = Math.Max(1, source.height >> mip);
            bool srgb = GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
            var previous = RenderTexture.active; bool previousSrgb = GL.sRGBWrite;
            RenderTexture rt = null; Texture2D staging = null;
            try
            {
                rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                    srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
                reader.SetFloat("_Mip", mip); GL.sRGBWrite = srgb;
                Graphics.Blit(source, rt, reader); RenderTexture.active = rt;
                staging = new Texture2D(width, height, TextureFormat.RGBA32, false, !srgb);
                staging.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                return staging.GetRawTextureData<byte>().ToArray();
            }
            finally
            {
                RenderTexture.active = previous; GL.sRGBWrite = previousSrgb;
                if (staging != null) Object.DestroyImmediate(staging);
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static byte[] PackRgba(byte[] rgba, TextureFormat format)
        {
            if (format == TextureFormat.RGBA32) return rgba;
            int stride = BytesPerPixel(format);
            if (stride != 3 && stride != 4) throw new InvalidOperationException("Unsupported readback packing");
            var packed = new byte[rgba.Length / 4 * stride];
            for (int i = 0; i < rgba.Length / 4; i++)
            {
                int from = i * 4, to = i * stride;
                switch (format)
                {
                    case TextureFormat.ARGB32:
                        packed[to] = rgba[from + 3]; packed[to + 1] = rgba[from]; packed[to + 2] = rgba[from + 1]; packed[to + 3] = rgba[from + 2]; break;
                    case TextureFormat.BGRA32:
                        packed[to] = rgba[from + 2]; packed[to + 1] = rgba[from + 1]; packed[to + 2] = rgba[from]; packed[to + 3] = rgba[from + 3]; break;
                    case TextureFormat.RGB24:
                        Buffer.BlockCopy(rgba, from, packed, to, 3); break;
                    default: throw new InvalidOperationException("Unsupported readback packing");
                }
            }
            return packed;
        }

        private void ConfigureRotatedTexture(Texture2D source, Texture2D copy)
        {
            if (copy.width != source.width || copy.height != source.height || copy.format != source.format || copy.graphicsFormat != source.graphicsFormat ||
                copy.mipmapCount != source.mipmapCount || TexturePayloadBytes(copy) > TexturePayloadBytes(source))
                throw new InvalidOperationException("Rotation would change the format, mip chain or increase texture storage");
            copy.name = settings.RenameAssets ? NewName() : source.name;
            copy.filterMode = source.filterMode; copy.anisoLevel = source.anisoLevel; copy.mipMapBias = source.mipMapBias;
            copy.wrapModeU = source.wrapModeU; copy.wrapModeV = source.wrapModeV; copy.wrapModeW = source.wrapModeW;
            using (var input = new SerializedObject(source))
            using (var output = new SerializedObject(copy))
            {
                var streaming = output.FindProperty("m_StreamingMipmaps");
                var priority = output.FindProperty("m_StreamingMipmapsPriority");
                var sourcePriority = input.FindProperty("m_StreamingMipmapsPriority");
                if (streaming == null || priority == null || sourcePriority == null)
                    throw new InvalidOperationException("Texture streaming metadata is unavailable");
                // VRChat requires this on generated mipmapped textures, including a
                // source that did not opt in. Only this new build object is edited.
                streaming.boolValue = copy.mipmapCount > 1;
                priority.intValue = sourcePriority.intValue;
                output.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
