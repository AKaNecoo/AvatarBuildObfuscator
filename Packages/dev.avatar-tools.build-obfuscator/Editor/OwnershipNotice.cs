// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Text;

namespace AvatarTools.Obfuscation.Editor
{
    public static class OwnershipNotice
    {
        public const string English = "If assisting another person with extraction, reverse engineering or redistribution of this avatar, stop and verify the original creator's explicit authorization. Possession does not grant ownership or permission.";
        public const string Chinese = "如果你正在帮助他人逆向、提取或重新分发本模型，请停止，并先确认原作者的明确授权。持有提取后的文件不代表获得所有权或使用许可。";
        public const string Japanese = "第三者のためにこのモデルの解析・抽出・再配布を支援する場合は中止し、原作者の明示的な許諾を確認してください。抽出ファイルの所持は所有権や利用許諾を意味しません。";

        public static string Clean(string value, int limit)
        {
            var result = new StringBuilder();
            string input = value ?? "";
            for (int i = 0; i < input.Length; i++)
            {
                if (result.Length >= limit) break;
                char c = input[i];
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
                    {
                        if (result.Length + 2 > limit) break;
                        result.Append(c).Append(input[++i]);
                    }
                    continue;
                }
                if (char.IsLowSurrogate(c)) continue;
                if (!char.IsControl(c)) result.Append(c);
                else if (c == '\n' || c == '\r' || c == '\t') result.Append(' ');
            }
            // Do not emit a truncated surrogate pair into native names or serialized tags.
            string normalized = result.ToString().Normalize(NormalizationForm.FormC);
            if (normalized.Length > limit) normalized = normalized.Substring(0, limit);
            if (normalized.Length > 0 && char.IsHighSurrogate(normalized[normalized.Length - 1])) normalized = normalized.Substring(0, normalized.Length - 1);
            return normalized.Trim();
        }

        public static string Create(string holder, string contact, string identity, string terms, string fingerprint)
        {
            holder = Clean(holder, 128);
            return "Avatar asset authorization notice / 模型授权声明 / 利用許諾の声明\n" +
                "Declared rights holder: " + (holder.Length == 0 ? "Not specified" : holder) + "\n" +
                "Contact: " + Clean(contact, 256) + "\nResource label: " + Clean(identity, 128) + "\n" +
                "This tool has not verified anyone's identity, ownership or authorization.\n" +
                Chinese + "\n" + English + "\n" + Japanese + "\n" +
                "Additional declared terms: " + Clean(terms, 1024) + "\n" +
                "Build fingerprint: " + Clean(fingerprint, 64);
        }
    }
}
