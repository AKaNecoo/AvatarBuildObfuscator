// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;

namespace AvatarTools.Obfuscation.Editor
{
    internal enum TextureBlockFormat { Dxt1, Dxt5, Bc4, Bc5 }

    /// <summary>Rotates BC1/3/4/5 selectors without decoding or changing endpoints.</summary>
    internal static class TextureBlockRotation
    {
        internal static bool SupportsSize(int width, int height)
            => width > 0 && height > 0 && (width <= 4 || width % 4 == 0) && (height <= 4 || height % 4 == 0);

        internal static byte[] RotateMip(byte[] source, int width, int height, TextureBlockFormat format)
        {
            if (!SupportsSize(width, height)) throw new ArgumentException("Partial blocks cannot exchange endpoint palettes");
            int stride;
            switch (format)
            {
                case TextureBlockFormat.Dxt1: case TextureBlockFormat.Bc4: stride = 8; break;
                case TextureBlockFormat.Dxt5: case TextureBlockFormat.Bc5: stride = 16; break;
                default: throw new ArgumentOutOfRangeException(nameof(format));
            }
            int columns = checked((width + 3) / 4), rows = checked((height + 3) / 4);
            int length = checked(checked(columns * rows) * stride);
            if (source == null || source.Length != length) throw new ArgumentException("Unexpected compressed mip layout");
            var output = new byte[length];
            int validWidth = Math.Min(4, width), validHeight = Math.Min(4, height);
            for (int block = 0; block < columns * rows; block++)
            {
                int destination = (columns * rows - 1 - block) * stride;
                Buffer.BlockCopy(source, block * stride, output, destination, stride);
                switch (format)
                {
                    case TextureBlockFormat.Dxt1:
                        RotateSelectors(output, destination + 4, 4, 2, validWidth, validHeight); break;
                    case TextureBlockFormat.Dxt5:
                        RotateSelectors(output, destination + 2, 6, 3, validWidth, validHeight);
                        RotateSelectors(output, destination + 12, 4, 2, validWidth, validHeight); break;
                    case TextureBlockFormat.Bc4:
                        RotateSelectors(output, destination + 2, 6, 3, validWidth, validHeight); break;
                    case TextureBlockFormat.Bc5:
                        RotateSelectors(output, destination + 2, 6, 3, validWidth, validHeight);
                        RotateSelectors(output, destination + 10, 6, 3, validWidth, validHeight); break;
                }
            }
            return output;
        }

        private static void RotateSelectors(byte[] data, int offset, int count, int bits, int width, int height)
        {
            ulong original = 0;
            for (int i = 0; i < count; i++) original |= (ulong)data[offset + i] << (8 * i);
            // Keep padding selectors untouched in the final 1/2/3-pixel mip blocks.
            ulong rotated = original, mask = (1UL << bits) - 1;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int destination = (y * 4 + x) * bits;
                    int source = ((height - 1 - y) * 4 + width - 1 - x) * bits;
                    rotated = (rotated & ~(mask << destination)) | (((original >> source) & mask) << destination);
                }
            for (int i = 0; i < count; i++) data[offset + i] = (byte)(rotated >> (8 * i));
        }
    }
}
