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
    }
}
