// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed class ObfuscationPreset : ScriptableObject
    {
        [Serializable] public sealed class Option { public string property; public bool enabled; }
        public ProtectionMode mode;
        public List<Option> options = new List<Option>();
        private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "randomizeEachBuild", "auditBuild", "pruneEmptyBlendShapes", "embedOwnershipNotice", "embedBuildFingerprint",
            "experimentalUvTransform", "mergeSynchronizedBlendShapes", "renameAssets", "freezeUnusedBlendShapes",
            "renameDynamicBlendShapes", "allowMmdObfuscation", "shuffleBlendShapeOrder", "renameGameObjects", "renameMmdObjectNames", "shuffleGameObjectOrder",
            "renameParameters", "renameAnimatorInternals", "renameBones", "reorderMeshVertices", "reorderBonePalette"
        };

        public void Capture(AvatarBuildObfuscator component)
        {
            mode = component.mode; options.Clear(); var serialized = new SerializedObject(component);
            foreach (var name in Allowed) options.Add(new Option { property = name, enabled = serialized.FindProperty(name).boolValue });
        }

        public void Apply(AvatarBuildObfuscator component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (mode != ProtectionMode.Conservative && mode != ProtectionMode.Aggressive) throw new InvalidOperationException("Invalid preset mode");
            Undo.RecordObject(component, "Apply Avatar Build Obfuscator preset");
            var serialized = new SerializedObject(component); serialized.FindProperty("mode").enumValueIndex = (int)mode;
            foreach (var option in options)
                if (option != null && Allowed.Contains(option.property)) serialized.FindProperty(option.property).boolValue = option.enabled;
            serialized.ApplyModifiedProperties();
        }

        internal static void Save(AvatarBuildObfuscator component)
        {
            var path = EditorUtility.SaveFilePanelInProject("Avatar Build Obfuscator", "ObfuscationPreset", "asset", "");
            if (string.IsNullOrEmpty(path)) return;
            var preset = CreateInstance<ObfuscationPreset>(); preset.Capture(component); AssetDatabase.CreateAsset(preset, path); EditorGUIUtility.PingObject(preset);
        }
    }
}
