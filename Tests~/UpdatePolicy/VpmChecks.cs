// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.IO;
using AvatarTools.Obfuscation.Editor;

internal static class VpmChecks
{
    private static int passed;
    private static void Check(bool value, string reason)
    { if (!value) throw new Exception(reason); passed++; }

    internal static int Run()
    {
        passed = 0;
        var project = Path.GetFullPath("PolicyFixture");
        var installed = Path.Combine(project, "Packages", UpdatePolicy.PackageName);
        string json = "{\"locked\":{\"" + UpdatePolicy.PackageName + "\":{\"version\":\"0.8.0-beta.1\",\"dependencies\":{}}}}";
        Check(VpmInstallPolicy.IsManaged("Embedded", installed, project, "0.8.0-beta.1", json), "Valid VPM installation rejected");
        Check(VpmInstallPolicy.IsManaged("Embedded", installed + Path.DirectorySeparatorChar, project, "0.8.0-beta.1", json), "Trailing separator rejected");
        foreach (var source in new[] { "Git", "Local", "Registry", "BuiltIn", "", null })
            Check(!VpmInstallPolicy.IsManaged(source, installed, project, "0.8.0-beta.1", json), "Non-VPM source classified as managed");
        foreach (var path in new[] { Path.Combine(project, "Library", "PackageCache", UpdatePolicy.PackageName),
            Path.Combine(project, "Packages", UpdatePolicy.PackageName + "-copy"), Path.Combine(project, "Other", UpdatePolicy.PackageName), "", null })
            Check(!VpmInstallPolicy.IsManaged("Embedded", path, project, "0.8.0-beta.1", json), "Unrelated path classified as VPM");
        foreach (var text in new[] { "", "{}", "null", "not JSON", "{\"locked\":{}}", "{\"locked\":[]}",
            json.Replace("0.8.0-beta.1", "0.7.0"), json.Replace(UpdatePolicy.PackageName, "other.package"), new string(' ', 1024 * 1024 + 1) })
            Check(!VpmInstallPolicy.IsManaged("Embedded", installed, project, "0.8.0-beta.1", text), "Invalid/unrelated lock accepted");
        Check(!VpmInstallPolicy.IsManaged("Embedded", installed, project, "invalid", json), "Invalid installed SemVer accepted");
        Check(!VpmInstallPolicy.IsManaged("Embedded", installed, "", "0.8.0-beta.1", json), "Missing project accepted");
        Check(VpmInstallPolicy.ListingUrl.StartsWith("https://akanecoo.github.io/AvatarBuildObfuscator/", StringComparison.Ordinal), "Wrong VPM host");
        return passed;
    }
}
