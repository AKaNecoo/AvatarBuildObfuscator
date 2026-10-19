using System;
using System.Globalization;
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
        private ulong counter;

        public UsernameNameGenerator(string userName)
        {
            var normalized = NormalizeUserName(userName);
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
            var result = new StringBuilder("o_", 34);
            for (int i = 0; i < 16; i++) result.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }

        public static bool IsGeneratedName(string value)
        {
            if (value == null || value.Length != 34 || !value.StartsWith("o_", StringComparison.Ordinal)) return false;
            for (int i = 2; i < value.Length; i++)
                if (!(value[i] >= '0' && value[i] <= '9' || value[i] >= 'a' && value[i] <= 'f')) return false;
            return true;
        }
    }
}
