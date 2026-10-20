// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Security.Cryptography;
using System.Text;

namespace AvatarTools.Obfuscation.Editor
{
    /// <summary>
    /// A deterministic sequence of opaque names derived from a manually supplied username.
    /// Usernames are not passwords. This generator provides naming, not confidentiality.
    /// </summary>
    public sealed class UsernameNameGenerator
    {
        public const string Alphabet = "\u00CC\u00CD\u00CE\u00CF";
        public const int SuffixLength = 64;
        private readonly byte[] key;
        private readonly string prefix;
        private ulong counter;

        public UsernameNameGenerator(string userName)
        {
            var normalized = NormalizeUserName(userName);
            var safe = new StringBuilder();
            foreach (char c in normalized)
            {
                if (safe.Length >= 32) break;
                safe.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            }
            prefix = safe.ToString().Trim('_');
            if (prefix.Length == 0) prefix = "User";
            prefix += "_";
            using (var sha = SHA256.Create())
                key = sha.ComputeHash(Encoding.UTF8.GetBytes("AvatarBuildObfuscator/names/v2\0" + normalized));
        }

        public static string NormalizeUserName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("请在 Avatar Build Obfuscator 组件中填写你自己的用户名。", nameof(value));
            return value.Trim().Normalize(NormalizationForm.FormC);
        }

        public string Next()
        {
            var hash = NextHash();
            // Four distinct code points encode two bits each: 64 glyphs, 128 bits.
            var result = new StringBuilder(prefix, prefix.Length + SuffixLength);
            for (int i = 0; i < 16; i++)
                for (int shift = 6; shift >= 0; shift -= 2) result.Append(Alphabet[(hash[i] >> shift) & 3]);
            return result.ToString();
        }

        /// <summary>Permutation randomness must not depend on the visible alphabet.</summary>
        public int NextSeed()
        {
            var hash = NextHash();
            return (hash[0] & 0x7f) << 24 | hash[1] << 16 | hash[2] << 8 | hash[3];
        }

        private byte[] NextHash()
        {
            var value = counter;
            checked { counter++; }
            var input = new byte[8];
            for (int i = 7; i >= 0; i--) { input[i] = (byte)value; value >>= 8; }
            using (var hmac = new HMACSHA256(key)) return hmac.ComputeHash(input);
        }

        public static bool IsGeneratedName(string value)
        {
            if (value == null || value.Length < SuffixLength + 2 || value[value.Length - SuffixLength - 1] != '_') return false;
            for (int i = value.Length - SuffixLength; i < value.Length; i++)
                if (Alphabet.IndexOf(value[i]) < 0) return false;
            return true;
        }
    }
}
