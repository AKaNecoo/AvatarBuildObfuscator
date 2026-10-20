// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace AvatarTools.Obfuscation.Editor
{
    // Kept independent of Unity so hosted CI can exercise the actual update policy.
    internal static class UpdatePolicy
    {
        internal const string PackageName = "dev.avatar-tools.build-obfuscator";
        internal const string Repository = "AKaNecoo/AvatarBuildObfuscator";
        internal const string GitUrl = "https://github.com/" + Repository + ".git?path=/Packages/" + PackageName;

        internal static bool IsCommit(string value)
        { return value != null && Regex.IsMatch(value, @"^[a-f0-9]{40}\z"); }

        internal static string Channel(string packageId, string hash, string managedCommit, string managedChannel)
        {
            var prefix = PackageName + "@" + GitUrl + "#";
            if (packageId == null || !packageId.StartsWith(prefix, StringComparison.Ordinal)) return null;
            var revision = packageId.Substring(prefix.Length);
            if (revision == "main" || revision == "beta") return revision;
            if (IsCommit(revision) && revision == hash && revision == managedCommit &&
                (managedChannel == "main" || managedChannel == "beta")) return managedChannel;
            return null;
        }

        internal static bool VerifiedRun(string branch, string commit, string status, string conclusion,
            string eventName, string repository, string expectedChannel)
        {
            return (expectedChannel == "main" || expectedChannel == "beta") && branch == expectedChannel &&
                IsCommit(commit) && status == "completed" && conclusion == "success" && eventName == "push" &&
                repository == Repository;
        }

        private static readonly Regex VersionPattern = new Regex(
            @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?\z");

        internal static bool IsVersion(string value)
        {
            var match = VersionPattern.Match(value ?? "");
            return match.Success && (match.Groups[4].Value.Length == 0 || match.Groups[4].Value.Split('.').All(p =>
                !p.All(char.IsDigit) || p.Length == 1 || p[0] != '0'));
        }

        private static int Numeric(string a, string b)
        { return a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b); }

        internal static int CompareVersions(string a, string b)
        {
            if (!IsVersion(a) || !IsVersion(b)) throw new ArgumentException("Invalid semantic version");
            var left = VersionPattern.Match(a); var right = VersionPattern.Match(b);
            for (int i = 1; i <= 3; i++)
            { int order = Numeric(left.Groups[i].Value, right.Groups[i].Value); if (order != 0) return order; }
            string preA = left.Groups[4].Value, preB = right.Groups[4].Value;
            if (preA == preB) return 0;
            if (preA.Length == 0) return 1;
            if (preB.Length == 0) return -1;
            var partsA = preA.Split('.'); var partsB = preB.Split('.');
            for (int i = 0; i < Math.Min(partsA.Length, partsB.Length); i++)
            {
                bool numberA = partsA[i].All(char.IsDigit), numberB = partsB[i].All(char.IsDigit);
                int order = numberA && numberB ? Numeric(partsA[i], partsB[i]) :
                    numberA != numberB ? (numberA ? -1 : 1) : string.CompareOrdinal(partsA[i], partsB[i]);
                if (order != 0) return order;
            }
            return partsA.Length.CompareTo(partsB.Length);
        }

        internal static bool CanInstall(string current, string candidate, string currentHash, string commit, string channel)
        {
            return IsCommit(commit) && currentHash != commit && IsVersion(current) && IsVersion(candidate) &&
                (channel == "main" || channel == "beta") && (channel != "main" || !candidate.Split('+')[0].Contains("-")) &&
                CompareVersions(candidate, current) > 0;
        }

        internal static bool CanAutomaticallyInstall(bool enabled, bool busy, bool available, string commit, string lastAttempt)
        { return enabled && !busy && available && IsCommit(commit) && commit != lastAttempt; }

        internal static bool TrustedArchive(string url, string version)
        { return IsVersion(version) && url == "https://github.com/" + Repository + "/releases/download/v" + version + "/AvatarBuildObfuscator-" + version + ".unitypackage"; }

        internal static bool ValidArchive(byte[] bytes, string digest)
        {
            if (bytes == null || digest == null || !Regex.IsMatch(digest, @"^sha256:[a-f0-9]{64}\z")) return false;
            using (var sha = SHA256.Create())
                return "sha256:" + BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() == digest;
        }
    }
}
