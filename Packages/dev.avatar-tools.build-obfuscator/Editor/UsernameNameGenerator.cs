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
                key = sha.ComputeHash(Encoding.UTF8.GetBytes("AvatarBuildObfuscator/names/v1\0" + normalized));
        }

        public static string NormalizeUserName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("请在 Avatar Build Obfuscator 组件中填写你自己的用户名。", nameof(value));
            return value.Trim().Normalize(NormalizationForm.FormC);
        }

        public string Next()
        {
            var value = counter;
            checked { counter++; }
            var input = new byte[8];
            for (int i = 7; i >= 0; i--) { input[i] = (byte)value; value >>= 8; }
            byte[] hash;
            using (var hmac = new HMACSHA256(key)) hash = hmac.ComputeHash(input);
            // 128 bits, represented with ASCII capital I and lowercase l. The visible
            // username is sanitized separately; the complete normalized input seeds HMAC.
            var result = new StringBuilder(prefix, prefix.Length + 128);
            for (int i = 0; i < 16; i++)
                for (int bit = 7; bit >= 0; bit--) result.Append((hash[i] & (1 << bit)) == 0 ? 'I' : 'l');
            return result.ToString();
        }

        public static bool IsGeneratedName(string value)
        {
            if (value == null || value.Length < 130 || value[value.Length - 129] != '_') return false;
            for (int i = value.Length - 128; i < value.Length; i++)
                if (value[i] != 'I' && value[i] != 'l') return false;
            return true;
        }
    }
}
