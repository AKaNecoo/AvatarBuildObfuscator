using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Validation
{
    // Opt-in integration checks against Unity's loaded updater, not a copy of its logic.
    public static class PackageUpdateValidation
    {
        [Serializable] private sealed class Result { public string version; public int passed; }
        private static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        public static string Run(string output)
        {
            var type = Type.GetType("AvatarTools.Obfuscation.Editor.PackageUpdates, dev.avatar-tools.build-obfuscator.editor", true);
            var settings = (UnityEngine.Object)type.GetProperty("Settings", Static).GetValue(null, null);
            var persist = settings.GetType().GetMethod("Persist");
            var pending = settings.GetType().GetField("importingArchive");
            var path = (string)type.GetProperty("ArchivePath", Static).GetValue(null, null);
            if ((bool)type.GetProperty("Working", Static).GetValue(null, null) || File.Exists(path))
                throw new InvalidOperationException("Do not interrupt an active real update");
            var original = EditorJsonUtility.ToJson(settings);
            var originalStatus = type.GetProperty("Status", Static).GetValue(null, null);
            var originalError = type.GetProperty("Error", Static).GetValue(null, null);
            var result = new Result { version = (string)type.GetProperty("CurrentVersion", Static).GetValue(null, null) };
            Action<bool> check = valid => { if (!valid) throw new Exception("Update integration assertion " + result.passed); result.passed++; };
            try
            {
                check(!(bool)settings.GetType().GetField("automaticInstall").GetValue(settings));
                var runs = type.GetNestedType("Runs", BindingFlags.NonPublic);
                var parsed = JsonUtility.FromJson("{\"workflow_runs\":[{\"event\":\"push\"}]}", runs);
                var values = (Array)runs.GetField("workflow_runs").GetValue(parsed);
                check((string)values.GetValue(0).GetType().GetField("event").GetValue(values.GetValue(0)) == "push");
                using ((IDisposable)type.GetMethod("DuringBuild", Static).Invoke(null, null))
                    check((bool)type.GetProperty("EditorBusy", Static).GetValue(null, null));
                if ((bool)type.GetProperty("Legacy", Static).GetValue(null, null))
                {
                    var manifestType = type.GetNestedType("Manifest", BindingFlags.NonPublic);
                    var stale = Activator.CreateInstance(manifestType);
                    manifestType.GetField("version").SetValue(stale, "0.0.0");
                    type.GetField("legacyManifest", Static).SetValue(null, stale);
                    type.GetField("legacyRead", Static).SetValue(null, true);
                    type.GetMethod("OnImported", Static).Invoke(null, new object[] { "manual-version-only-update" });
                    check((string)type.GetProperty("CurrentVersion", Static).GetValue(null, null) != "0.0.0");
                }
                foreach (var outcome in new[] { "completed", "failed", "cancelled" })
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.WriteAllText(path, "temporary archive fixture");
                    pending.SetValue(settings, true);
                    if (outcome == "completed") type.GetMethod("OnImported", Static).Invoke(null, new object[] { "pending" });
                    else type.GetMethod("Fail", Static).Invoke(null, new object[] { "update.failed", outcome });
                    check(!File.Exists(path));
                    check(!(bool)pending.GetValue(settings));
                }
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(result, true));
                return JsonUtility.ToJson(result);
            }
            finally
            {
                type.GetMethod("CleanupArchive", Static).Invoke(null, null);
                EditorJsonUtility.FromJsonOverwrite(original, settings); persist.Invoke(settings, null);
                type.GetProperty("Status", Static).SetValue(null, originalStatus, null);
                type.GetProperty("Error", Static).SetValue(null, originalError, null);
            }
        }
    }
}
