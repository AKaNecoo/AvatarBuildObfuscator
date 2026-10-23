// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using AvatarTools.Obfuscation.Editor;

internal static class Program
{
    private static int passed;
    private const string Commit = "1234567890abcdef1234567890abcdef12345678";
    private static void Check(bool value, string name)
    { if (!value) throw new Exception(name); passed++; }
    public static void Main()
    {
        string[] ordered = { "0.3.0-alpha", "0.3.0-alpha.1", "0.3.0-alpha.beta", "0.3.0-beta", "0.3.0-beta.2", "0.3.0-beta.11", "0.3.0-rc.1", "0.3.0", "0.3.1", "1.0.0", "10.0.0" };
        for (int i = 1; i < ordered.Length; i++)
        { Check(UpdatePolicy.CompareVersions(ordered[i], ordered[i - 1]) > 0, "SemVer forward " + i); Check(UpdatePolicy.CompareVersions(ordered[i - 1], ordered[i]) < 0, "SemVer reverse " + i); }
        foreach (string bad in new[] { "", "v1.0.0", "01.0.0", "1.0", "1.0.0-beta.01", "1.0.0-", "1.0.0+", "../1.0.0", "1.0.0\n" })
            Check(!UpdatePolicy.IsVersion(bad), "Reject version " + bad);
        Check(UpdatePolicy.CompareVersions("1.0.0+build.2", "1.0.0+build.1") == 0, "Build metadata does not upgrade");
        Check(UpdatePolicy.Channel(UpdatePolicy.PackageName + "@" + UpdatePolicy.GitUrl + "#beta", Commit, "", "") == "beta", "Follow beta");
        Check(UpdatePolicy.Channel(UpdatePolicy.PackageName + "@" + UpdatePolicy.GitUrl + "#main", Commit, "", "") == "main", "Follow main");
        Check(UpdatePolicy.Channel(UpdatePolicy.PackageName + "@" + UpdatePolicy.GitUrl + "#" + Commit, Commit, "", "") == null, "Respect manual pin");
        Check(UpdatePolicy.Channel(UpdatePolicy.PackageName + "@" + UpdatePolicy.GitUrl + "#" + Commit, Commit, Commit, "beta") == "beta", "Managed pin retains channel");
        Check(UpdatePolicy.Channel(UpdatePolicy.PackageName + "@" + UpdatePolicy.GitUrl + "#" + Commit, Commit, Commit, "other") == null, "Reject arbitrary channel");
        Check(UpdatePolicy.Channel(UpdatePolicy.PackageName + "@file:/local", Commit, "", "") == null, "Respect development install");
        Check(UpdatePolicy.Channel(UpdatePolicy.PackageName + "@" + UpdatePolicy.GitUrl.Replace("AKaNecoo", "someone") + "#beta", Commit, "", "") == null, "Reject other repository");
        Check(UpdatePolicy.VerifiedRun("beta", Commit, "completed", "success", "push", UpdatePolicy.Repository, "beta"), "Accept CI push");
        Check(!UpdatePolicy.VerifiedRun("main", Commit, "completed", "success", "push", UpdatePolicy.Repository, "beta"), "Reject other channel");
        Check(!UpdatePolicy.VerifiedRun("beta", Commit, "completed", "success", "pull_request", UpdatePolicy.Repository, "beta"), "Reject PR candidate");
        Check(!UpdatePolicy.VerifiedRun("beta", Commit, "completed", "failure", "push", UpdatePolicy.Repository, "beta"), "Reject failed CI");
        Check(!UpdatePolicy.VerifiedRun("beta", Commit, "in_progress", null, "push", UpdatePolicy.Repository, "beta"), "Reject pending CI");
        Check(!UpdatePolicy.VerifiedRun("beta", "bad", "completed", "success", "push", UpdatePolicy.Repository, "beta"), "Reject invalid SHA");
        Check(!UpdatePolicy.VerifiedRun("beta", Commit, "completed", "success", "push", "other/repo", "beta"), "Reject fork CI");
        Check(UpdatePolicy.CanInstall("0.3.0-beta.3", "0.3.0", "old", Commit, "main"), "Promote prerelease");
        Check(!UpdatePolicy.CanInstall("0.3.0", "0.3.1-beta.1", "old", Commit, "main"), "Keep stable channel stable");
        Check(UpdatePolicy.CanInstall("0.3.0", "0.3.1-beta.1", "old", Commit, "beta"), "Beta opt in");
        Check(!UpdatePolicy.CanInstall("0.3.0", "0.2.0", "old", Commit, "main"), "Never downgrade");
        Check(!UpdatePolicy.CanInstall("0.3.0", "0.3.0", "old", Commit, "main"), "No reinstall on documentation-only commit");
        Check(!UpdatePolicy.CanInstall("0.3.0", "0.3.1", Commit, Commit, "main"), "No reinstall same SHA");
        Check(!UpdatePolicy.CanAutomaticallyInstall(false, false, true, Commit, ""), "Auto install defaults off");
        Check(!UpdatePolicy.CanAutomaticallyInstall(true, true, true, Commit, ""), "Wait for idle");
        Check(!UpdatePolicy.CanAutomaticallyInstall(true, false, true, Commit, Commit), "No retry loop");
        Check(!UpdatePolicy.CanAutomaticallyInstall(true, false, false, Commit, ""), "Require candidate");
        Check(UpdatePolicy.CanAutomaticallyInstall(true, false, true, Commit, ""), "Allow opted-in update");
        Check(UpdatePolicy.TrustedArchive("https://github.com/" + UpdatePolicy.Repository + "/releases/download/v0.3.0/AvatarBuildObfuscator-0.3.0.unitypackage", "0.3.0"), "Official package download");
        Check(!UpdatePolicy.TrustedArchive("https://example.com/file.unitypackage", "0.3.0"), "Reject arbitrary download");
        var bytes = System.Text.Encoding.UTF8.GetBytes("abc");
        const string digest = "sha256:ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
        Check(UpdatePolicy.ValidArchive(bytes, digest), "Archive SHA256 matches");
        Check(!UpdatePolicy.ValidArchive(new byte[0], digest), "Reject corrupt download");
        Check(!UpdatePolicy.ValidArchive(bytes, ""), "Reject absent digest");
        Console.WriteLine("Update policy: " + passed + " PASS");
        int beforeNames = passed;
        NamingChecks.Run(Check);
        Console.WriteLine("Naming: " + (passed - beforeNames) + " PASS");
        int beforeBlocks = passed;
        TextureBlockChecks.Run(Check);
        Console.WriteLine("Compressed texture blocks: " + (passed - beforeBlocks) + " PASS");
    }
}
