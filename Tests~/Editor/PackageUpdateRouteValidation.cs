// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AvatarTools.Obfuscation.Validation
{
    [InitializeOnLoad]
    public static class PackageUpdateRouteValidation
    {
        private const string Key = "ABO.Validation.UpdateRoute";
        private static double nextTick, settledAfter;
        private static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
        [Serializable] public sealed class Result
        {
            public string stage, route, expectedSource, expectedCommit, expectedVersion, startedUtc, fromVersion, toVersion, source, channel;
            public string username, mode, language, scriptGuid;
            public string sourceMarker;
            public bool originalAutomaticInstall, automaticWasEnabled, installerDeleted, settingsPreserved;
            public List<string> errors = new List<string>();
        }
        private static Type Service => Type.GetType("AvatarTools.Obfuscation.Editor.PackageUpdates, dev.avatar-tools.build-obfuscator.editor", true);
        private static object Property(string name) { return Service.GetProperty(name, Flags).GetValue(null, null); }
        private static object Settings => Property("Settings");
        private static AvatarBuildObfuscator Marker(Result value)
        {
            GlobalObjectId id;
            if (!GlobalObjectId.TryParse(value.sourceMarker, out id)) throw new Exception("Invalid source component identity");
            var marker = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as AvatarBuildObfuscator;
            if (marker == null) throw new Exception("Source component no longer exists");
            return marker;
        }
        static PackageUpdateRouteValidation()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, stack, type) =>
            {
                var value = Read();
                if (value == null || value.stage != "waiting" || !(type == LogType.Error || type == LogType.Assert || type == LogType.Exception)) return;
                value.errors.Add(message); Write(value);
            };
        }
        public static void Arm(string output, string route, string source, string commit, string version, int sourceInstanceId = 0)
        {
            var active = Read();
            if (active != null && active.stage == "waiting") throw new Exception("Route test already active");
            settledAfter = 0;
            SessionState.SetString(Key, output);
            var marker = sourceInstanceId != 0 ? EditorUtility.InstanceIDToObject(sourceInstanceId) as AvatarBuildObfuscator :
                UnityEngine.Object.FindObjectsOfType<AvatarBuildObfuscator>(true)
                    .Where(c => c.gameObject.scene.IsValid() && !EditorSceneManager.IsPreviewScene(c.gameObject.scene)).Single();
            if (marker == null || EditorUtility.IsPersistent(marker)) throw new Exception("Select a scene component to validate");
            var identity = GlobalObjectId.GetGlobalObjectIdSlow(marker);
            if (GlobalObjectId.GlobalObjectIdentifierToObjectSlow(identity) != marker)
                throw new Exception("Source component requires a stable saved-scene identity");
            Write(new Result { stage = "waiting", route = route, expectedSource = source, expectedCommit = commit, expectedVersion = version,
                startedUtc = DateTime.UtcNow.ToString("O"), fromVersion = (string)Property("CurrentVersion"),
                username = marker.userName, mode = marker.mode.ToString(), language = marker.inspectorLanguage.ToString(),
                sourceMarker = identity.ToString(),
                originalAutomaticInstall = (bool)Settings.GetType().GetField("automaticInstall").GetValue(Settings) });
        }
        private static Result Read()
        {
            var path = SessionState.GetString(Key, "");
            return File.Exists(path) ? JsonUtility.FromJson<Result>(File.ReadAllText(path)) : null;
        }
        private static void Write(Result value)
        {
            string path = SessionState.GetString(Key, "");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(value, true));
        }
        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextTick) return;
            nextTick = EditorApplication.timeSinceStartup + .5;
            var value = Read();
            if (value == null || value.stage != "waiting") return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { settledAfter = 0; return; }
            if ((string)Property("CurrentVersion") != value.expectedVersion || (bool)Property("Working"))
            {
                settledAfter = 0;
                if (DateTime.UtcNow - DateTime.Parse(value.startedUtc).ToUniversalTime() < TimeSpan.FromMinutes(4)) return;
                value.errors.Add("Update route timed out");
            }
            else
            {
                if (settledAfter == 0) { settledAfter = EditorApplication.timeSinceStartup + 3; return; }
                if (EditorApplication.timeSinceStartup < settledAfter) return;
            }
            var marker = Marker(value);
            value.toVersion = (string)Property("CurrentVersion");
            value.source = (bool)Property("Legacy") ? "Assets" : "Git";
            value.channel = (string)Property("Channel");
            value.scriptGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(marker)));
            value.settingsPreserved = marker.userName == value.username && marker.mode.ToString() == value.mode && marker.inspectorLanguage.ToString() == value.language;
            value.installerDeleted = !File.Exists((string)Property("ArchivePath"));
            var commit = (string)Settings.GetType().GetField("managedCommit").GetValue(Settings);
            value.automaticWasEnabled = (bool)Settings.GetType().GetField("automaticInstall").GetValue(Settings);
            value.stage = value.toVersion == value.expectedVersion && value.source == value.expectedSource && commit == value.expectedCommit &&
                value.settingsPreserved && value.installerDeleted && value.channel == "beta" && value.errors.Count == 0 &&
                (value.route.IndexOf("automatic", StringComparison.OrdinalIgnoreCase) < 0 || value.automaticWasEnabled) &&
                value.scriptGuid == "7c5e3c2dfef2cf4469c50c0ba239aac9" ? "passed" : "failed";
            Settings.GetType().GetField("automaticInstall").SetValue(Settings, value.originalAutomaticInstall);
            Settings.GetType().GetMethod("Persist").Invoke(Settings, null);
            Write(value);
        }
    }
}
