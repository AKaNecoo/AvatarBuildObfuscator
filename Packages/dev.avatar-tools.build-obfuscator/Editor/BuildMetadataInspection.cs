// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Editor
{
    [Serializable]
    public sealed class BuildMetadataResult
    {
        public int materials, noticeMaterials, fingerprintMaterials, hierarchyNotices, hierarchyFingerprints, invalidFingerprints;
        public List<string> fingerprints = new List<string>();
        public List<string> matchingReports = new List<string>();
    }

    public static class BuildMetadataInspection
    {
        public static string ReportsFolder => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/AvatarBuildObfuscator/Reports");
        internal static Object[] Dependencies(GameObject avatar)
        {
            var visited = new HashSet<Object>();
            var queue = new Queue<Object>(avatar.GetComponentsInChildren<Component>(true).Where(c => c != null && !(c is Transform) && !(c is AvatarBuildObfuscator)));
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == null || !visited.Add(current)) continue;
                if (current is Texture || current is AudioClip || current is Mesh || current is Avatar || current is Shader) continue;
                if (!(current is Component || current is RuntimeAnimatorController || current is ScriptableObject || current is AnimationClip || current is Material ||
                    current is AnimatorStateMachine || current is AnimatorState || current is AnimatorTransitionBase || current is BlendTree)) continue;
                using (var serialized = new SerializedObject(current))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null &&
                            !(property.objectReferenceValue is GameObject) && !(property.objectReferenceValue is Component) && !(property.objectReferenceValue is MonoScript))
                            queue.Enqueue(property.objectReferenceValue);
                }
            }
            return visited.ToArray();
        }

        public static BuildMetadataResult Inspect(GameObject avatar, IEnumerable<Material> materials = null, bool matchLocalReports = true)
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            var result = new BuildMetadataResult();
            var all = (materials ?? Dependencies(avatar).OfType<Material>())
                .Where(m => m != null).Distinct().ToArray();
            result.materials = all.Length;
            foreach (var material in all)
            {
                if (!string.IsNullOrEmpty(material.GetTag(GuardProcessor.NoticeTag, false, ""))) result.noticeMaterials++;
                string value = material.GetTag(GuardProcessor.FingerprintTag, false, "");
                if (value.Length > 0) { result.fingerprintMaterials++; AddFingerprint(result, value); }
            }
            foreach (var t in avatar.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == OwnershipNotice.Chinese || t.name == OwnershipNotice.Japanese ||
                    t.name == "AUTHORIZATION_NOTICE: " + OwnershipNotice.English) result.hierarchyNotices++;
                const string prefix = "BUILD_FINGERPRINT_";
                if (t.name.StartsWith(prefix, StringComparison.Ordinal))
                { result.hierarchyFingerprints++; AddFingerprint(result, t.name.Substring(prefix.Length)); }
            }
            foreach (var fingerprint in matchLocalReports ? result.fingerprints : Enumerable.Empty<string>())
            {
                var path = Path.Combine(ReportsFolder, fingerprint + ".json");
                if (!File.Exists(path)) continue;
                try
                {
                    var report = JsonUtility.FromJson<GuardReport>(File.ReadAllText(path));
                    if (report != null && report.BuildFingerprint == fingerprint) result.matchingReports.Add(path);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException) { }
            }
            return result;
        }

        private static void AddFingerprint(BuildMetadataResult result, string value)
        {
            // Do not allow model metadata to become an arbitrary file path.
            if (Regex.IsMatch(value, "\\A[0-9a-f]{32}\\z") && !result.fingerprints.Contains(value)) result.fingerprints.Add(value);
            else if (!Regex.IsMatch(value, "\\A[0-9a-f]{32}\\z")) result.invalidFingerprints++;
        }
    }
}
