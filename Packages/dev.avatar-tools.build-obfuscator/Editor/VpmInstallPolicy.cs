// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace AvatarTools.Obfuscation.Editor
{
    // Unity-independent: an arbitrary local/development package must not be classified
    // as VPM merely because this project also has a VPM manifest.
    internal static class VpmInstallPolicy
    {
        internal const string ListingUrl = "https://akanecoo.github.io/AvatarBuildObfuscator/index.json";
        internal const string GuideUrl = "https://akanecoo.github.io/AvatarBuildObfuscator/";

        [DataContract] private sealed class LockFile
        { [DataMember(Name = "locked")] public Dictionary<string, LockEntry> Locked { get; set; } }
        [DataContract] private sealed class LockEntry
        { [DataMember(Name = "version")] public string Version { get; set; } }

        internal static bool IsManaged(string source, string installedPath, string projectPath, string installedVersion, string manifestJson)
        {
            if (source != "Embedded" || !UpdatePolicy.IsVersion(installedVersion) ||
                string.IsNullOrEmpty(installedPath) || string.IsNullOrEmpty(projectPath) ||
                string.IsNullOrEmpty(manifestJson) || manifestJson.Length > 1024 * 1024) return false;
            try
            {
                var comparison = Path.DirectorySeparatorChar == (char)92 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                var expected = Path.GetFullPath(Path.Combine(projectPath, "Packages", UpdatePolicy.PackageName));
                if (!string.Equals(Path.GetFullPath(installedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), expected, comparison)) return false;
                var serializer = new DataContractJsonSerializer(typeof(LockFile), new DataContractJsonSerializerSettings
                { UseSimpleDictionaryFormat = true, MaxItemsInObjectGraph = 10000 });
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(manifestJson)))
                {
                    var parsed = serializer.ReadObject(stream) as LockFile;
                    return parsed?.Locked != null && parsed.Locked.TryGetValue(UpdatePolicy.PackageName, out var entry) &&
                        entry != null && entry.Version == installedVersion;
                }
            }
            catch (SerializationException) { return false; }
            catch (ArgumentException) { return false; }
            catch (IOException) { return false; }
            catch (InvalidOperationException) { return false; }
        }
    }
}
