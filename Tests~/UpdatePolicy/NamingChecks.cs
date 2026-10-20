// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Linq;
using AvatarTools.Obfuscation.Editor;

internal static class NamingChecks
{
    public static void Run(Action<bool, string> check)
    {
        var a = new UsernameNameGenerator("VectorUser");
        check(a.Next() == "VectorUser_\u00cd\u00cf\u00cd\u00cf\u00ce\u00cc\u00cd\u00cd\u00cd\u00cc\u00cc\u00cf\u00cd\u00cc\u00ce\u00cf\u00cc\u00ce\u00cc\u00cf\u00cd\u00cd\u00cc\u00cf\u00cc\u00cf\u00cd\u00ce\u00cf\u00ce\u00cc\u00cc\u00cf\u00cc\u00ce\u00cf\u00cf\u00cf\u00cf\u00ce\u00cc\u00ce\u00cf\u00cc\u00ce\u00ce\u00cd\u00cd\u00cf\u00cc\u00ce\u00cd\u00cd\u00cd\u00ce\u00cf\u00ce\u00cc\u00cf\u00cd\u00ce\u00cf\u00cf\u00cf", "Independent four-glyph HMAC name vector");
        check(a.NextSeed() == 1326088035, "Name and seed consume a consistent byte stream");
        check(new UsernameNameGenerator("VectorUser").NextSeed() == 2005222219, "Independent first seed vector");
        var names = Enumerable.Range(0, 2048).Select(i => a.Next()).ToArray();
        check(names.Distinct().Count() == names.Length, "Unique generated names");
        check(names.All(n => n.StartsWith("VectorUser_") && n.Length == 75), "Username plus 64-symbol suffix");
        check(names.All(n => n.Substring(11).All(c => "\u00cc\u00cd\u00ce\u00cf".Contains(c))), "Exact alphabet only");
        check(names.All(UsernameNameGenerator.IsGeneratedName), "All generated names recognized");
        check(!UsernameNameGenerator.IsGeneratedName("User_" + new string('I', 128)), "Legacy ASCII is not the new alphabet");
        check(!UsernameNameGenerator.IsGeneratedName("User_" + new string('Ì', 63) + "X"), "Reject malformed suffix");
        check(new UsernameNameGenerator(" café ").Next() == new UsernameNameGenerator("cafe\u0301").Next(), "NFC normalization");
        check(new UsernameNameGenerator(new string('禾', 200)).Next().Length == 97, "Bounded Unicode prefix");
        check(!new UsernameNameGenerator("x/y\\z:*?\"<>|").Next().Any(c => "/\\:*?\"<>|".Contains(c)), "Safe filename prefix");
        check(new UsernameNameGenerator("A").Next() != new UsernameNameGenerator("B").Next(), "Username affects name");
        check(new UsernameNameGenerator("A").NextSeed() != new UsernameNameGenerator("B").NextSeed(), "Username affects shuffle seed");
        bool rejected = false;
        try { new UsernameNameGenerator(" "); } catch (ArgumentException) { rejected = true; }
        check(rejected, "Reject missing username");
        var salt = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var s1 = new UsernameNameGenerator("VectorUser", salt); var s2 = new UsernameNameGenerator("VectorUser", salt);
        check(s1.Next() == s2.Next() && s1.NextSeed() == s2.NextSeed(), "Explicit build salt is reproducible");
        check(s1.BuildFingerprint() == s2.BuildFingerprint(), "Fingerprint does not consume name stream");
        check(s1.BuildFingerprint().Length == 32, "128-bit fingerprint identifier");
        check(s1.BuildFingerprint() != new UsernameNameGenerator("VectorUser").BuildFingerprint(), "Salt uses a separate generation domain");
        salt[0] = 200;
        check(s1.BuildFingerprint() != new UsernameNameGenerator("VectorUser", salt).BuildFingerprint(), "Salt input cannot mutate an existing generator");
        var randomA = new UsernameNameGenerator("VectorUser", UsernameNameGenerator.NewBuildSalt());
        var randomB = new UsernameNameGenerator("VectorUser", UsernameNameGenerator.NewBuildSalt());
        check(randomA.Next() != randomB.Next() && randomA.BuildFingerprint() != randomB.BuildFingerprint(), "Independent builds differ");
        rejected = false; try { new UsernameNameGenerator("VectorUser", new byte[1]); } catch (ArgumentException) { rejected = true; }
        check(rejected, "Weak build salts rejected");
        string notice = OwnershipNotice.Create("Example holder", "https://example.org/contact", "Avatar label", "No redistribution", "abcd");
        check(notice.Contains(OwnershipNotice.Chinese) && notice.Contains(OwnershipNotice.English) && notice.Contains(OwnershipNotice.Japanese), "Three-language authorization text");
        check(notice.Contains("has not verified") && notice.Contains("Declared rights holder: Example holder"), "Declaration does not impersonate verified ownership");
        check(OwnershipNotice.Create("", "", "Label", "", "").Contains("Not specified"), "Empty holder does not invent an author");
        check(OwnershipNotice.Clean("x\0\r\ny", 40) == "x  y", "Native metadata strips control characters");
        check(OwnershipNotice.Clean("x" + char.ConvertFromUtf32(0x1f600), 2) == "x", "Metadata truncation preserves surrogate boundaries");
        check(OwnershipNotice.Clean("\ud800x\udc00", 16) == "x", "Malformed metadata cannot abort the build");
    }
}
