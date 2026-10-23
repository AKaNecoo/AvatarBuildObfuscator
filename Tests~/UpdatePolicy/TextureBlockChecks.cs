// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Linq;
using AvatarTools.Obfuscation.Editor;

internal static class TextureBlockChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var dimensions = new[] { new[] { 1, 1 }, new[] { 2, 1 }, new[] { 1, 2 }, new[] { 2, 2 },
            new[] { 3, 3 }, new[] { 4, 4 }, new[] { 8, 4 }, new[] { 4, 8 }, new[] { 16, 8 }, new[] { 12, 8 } };
        foreach (TextureBlockFormat format in Enum.GetValues(typeof(TextureBlockFormat)))
            foreach (var size in dimensions)
            {
                int width = size[0], height = size[1];
                int stride = format == TextureBlockFormat.Dxt1 || format == TextureBlockFormat.Bc4 ? 8 : 16;
                var random = new Random(121 + (int)format * 31 + width * 3 + height);
                var source = new byte[(width + 3) / 4 * ((height + 3) / 4) * stride];
                random.NextBytes(source);
                var original = (byte[])source.Clone();
                var rotated = TextureBlockRotation.RotateMip(source, width, height, format);
                string label = format + " " + width + "x" + height;
                check(source.SequenceEqual(original), "Input bytes unchanged: " + label);
                check(rotated.Length == source.Length, "Compressed size unchanged: " + label);
                check(TextureBlockRotation.RotateMip(rotated, width, height, format).SequenceEqual(source), "Two rotations restore every byte: " + label);
                // This decoder independently identifies the endpoint palette and the
                // decoded selector at each visible texel, including partial tail mips.
                bool pixels = true;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        pixels &= Texel(source, width, x, y, format) == Texel(rotated, width, width - 1 - x, height - 1 - y, format);
                check(pixels, "Decoded endpoint/selector identity: " + label);
            }
        foreach (var size in new[] { new[] { 0, 4 }, new[] { -1, 4 }, new[] { 6, 4 }, new[] { 8, 5 } })
        {
            bool rejected = false;
            try { TextureBlockRotation.RotateMip(new byte[128], size[0], size[1], TextureBlockFormat.Dxt1); }
            catch (ArgumentException) { rejected = true; }
            check(rejected, "Reject block crossing with incompatible palettes");
        }
        bool badLength = false;
        try { TextureBlockRotation.RotateMip(new byte[7], 4, 4, TextureBlockFormat.Dxt1); }
        catch (ArgumentException) { badLength = true; }
        check(badLength, "Reject truncated compressed block");
        bool badFormat = false;
        try { TextureBlockRotation.RotateMip(new byte[8], 4, 4, (TextureBlockFormat)255); }
        catch (ArgumentOutOfRangeException) { badFormat = true; }
        check(badFormat, "Reject unknown compressed block format");
    }

    private static string Texel(byte[] data, int width, int x, int y, TextureBlockFormat format)
    {
        int stride = format == TextureBlockFormat.Dxt1 || format == TextureBlockFormat.Bc4 ? 8 : 16;
        int start = ((y / 4) * ((width + 3) / 4) + x / 4) * stride;
        int pixel = y % 4 * 4 + x % 4;
        switch (format)
        {
            case TextureBlockFormat.Dxt1: return Color(data, start, pixel);
            case TextureBlockFormat.Dxt5: return Alpha(data, start, pixel) + "/" + Color(data, start + 8, pixel);
            case TextureBlockFormat.Bc4: return Alpha(data, start, pixel);
            case TextureBlockFormat.Bc5: return Alpha(data, start, pixel) + "/" + Alpha(data, start + 8, pixel);
            default: throw new Exception("Unexpected test format");
        }
    }

    private static string Color(byte[] data, int start, int pixel)
        => BitConverter.ToString(data, start, 4) + ":" + ((data[start + 4 + pixel / 4] >> (pixel % 4 * 2)) & 3);

    private static string Alpha(byte[] data, int start, int pixel)
    {
        int bit = pixel * 3, value = 0;
        for (int i = 0; i < 3; i++)
            value |= ((data[start + 2 + (bit + i) / 8] >> ((bit + i) % 8)) & 1) << i;
        return data[start] + "," + data[start + 1] + ":" + value;
    }
}
