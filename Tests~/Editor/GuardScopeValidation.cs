// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System.IO;
using System.Linq;
using AvatarTools.Obfuscation.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        public static string RunScopes(string output)
        {
            results = new Results();
            Run("No overrides keep the original mode", () => {
                var root = Root(); var r = Renderer(root, "Body", Mesh());
                var s = new GuardSettings(root.GetComponent<AvatarBuildObfuscator>());
                new GuardProcessor(root, s); Check(!s.HasSubtreeOverrides && s.ModeFor(r) == ProtectionMode.Conservative, "Default changed");
            });
            Run("Nearest configured ancestor wins and siblings inherit default", () => {
                var root = Root(); var a = Renderer(root, "Clothes", Mesh()); var b = Renderer(a.gameObject, "Accessory", Mesh());
                var body = Renderer(root, "Body", Mesh()); var m = root.GetComponent<AvatarBuildObfuscator>();
                m.subtreeModes = new[] { Scope(a.transform, ProtectionMode.Aggressive), Scope(b.transform, ProtectionMode.Conservative) };
                var s = new GuardSettings(m); new GuardProcessor(root, s);
                Check(s.ModeFor(a) == ProtectionMode.Aggressive && s.ModeFor(b) == ProtectionMode.Conservative && s.ModeFor(body) == ProtectionMode.Conservative, "Inheritance broken");
            });
            Run("Duplicate subtree entries use the last explicit mode", () => {
                var root = Root(); var a = Renderer(root, "Clothes", Mesh()); var m = root.GetComponent<AvatarBuildObfuscator>();
                m.subtreeModes = new[] { Scope(a.transform, ProtectionMode.Conservative), Scope(a.transform, ProtectionMode.Aggressive) };
                var s = new GuardSettings(m); var p = new GuardProcessor(root, s);
                Check(s.ModeFor(a) == ProtectionMode.Aggressive && p.Run().Notes.Any(n => n.Contains("重复配置")), "Duplicate not reported/resolved");
            });
            Run("Outside-avatar roots and invalid enum values are ignored", () => {
                var root = Root(); var outside = Track(new GameObject("OtherAvatar")); var m = root.GetComponent<AvatarBuildObfuscator>();
                m.subtreeModes = new[] { Scope(outside.transform, ProtectionMode.Aggressive), Scope(root.transform, (ProtectionMode)9), null, Scope(null, ProtectionMode.Aggressive) };
                var s = new GuardSettings(m); new GuardProcessor(root, s); Check(!s.HasSubtreeOverrides && s.GlobalMode == m.mode, "Invalid root affected policy");
            });
            Run("Root override covers the entire model and its globals", () => {
                var root = Root(); var a = Renderer(root, "Clothes", Mesh()); var m = root.GetComponent<AvatarBuildObfuscator>();
                m.subtreeModes = new[] { Scope(root.transform, ProtectionMode.Aggressive) };
                var s = new GuardSettings(m); new GuardProcessor(root, s);
                Check(s.ModeFor(a) == ProtectionMode.Aggressive && s.GlobalMode == ProtectionMode.Aggressive, "Root override not respected");
            });
            Run("Conservative subtree keeps shared global parameter policy conservative", () => {
                var root = Root(); var a = Renderer(root, "Body", Mesh()); var m = root.GetComponent<AvatarBuildObfuscator>(); m.mode = ProtectionMode.Aggressive;
                m.subtreeModes = new[] { Scope(a.transform, ProtectionMode.Conservative) };
                var s = new GuardSettings(m); new GuardProcessor(root, s); Check(s.GlobalMode == ProtectionMode.Conservative, "Global policy ignored conservative subtree");
            });
            Run("Original subtree policy survives optimizer reparenting", () => {
                var root = Root(); var a = Renderer(root, "Clothes", Mesh()); var b = Renderer(a.gameObject, "Piece", Mesh());
                var m = root.GetComponent<AvatarBuildObfuscator>(); m.subtreeModes = new[] { Scope(a.transform, ProtectionMode.Aggressive) };
                var s = new GuardSettings(m); b.transform.SetParent(root.transform, false); new GuardProcessor(root, s);
                Check(s.ModeFor(b) == ProtectionMode.Aggressive && s.ModeFor(b.transform) == ProtectionMode.Aggressive, "Reparent lost origin mode");
            });
            Run("Generated descendants inherit an explicit surviving override", () => {
                var root = Root(); var a = Renderer(root, "Clothes", Mesh()); var m = root.GetComponent<AvatarBuildObfuscator>();
                m.subtreeModes = new[] { Scope(a.transform, ProtectionMode.Aggressive) }; var s = new GuardSettings(m);
                var added = Renderer(a.gameObject, "NewPart", Mesh()); new GuardProcessor(root, s);
                Check(s.ModeFor(added) == ProtectionMode.Aggressive, "Generated descendant lost inheritance");
            });
            Run("Generated objects without explicit lineage use conservative global policy", () => {
                var root = Root(); var a = Renderer(root, "Body", Mesh()); var m = root.GetComponent<AvatarBuildObfuscator>(); m.mode = ProtectionMode.Aggressive;
                m.subtreeModes = new[] { Scope(a.transform, ProtectionMode.Conservative) }; var s = new GuardSettings(m);
                var added = Renderer(root, "Generated", Mesh()); new GuardProcessor(root, s);
                Check(s.ModeFor(added) == ProtectionMode.Conservative, "Unknown provenance became aggressive");
            });
            Run("Ambiguous many-to-one conservative geometry retains conservative policy", () => {
                var root = Root(); var body = Renderer(root, "Body", Mesh()); var cloth = Renderer(root, "Clothes", Mesh());
                var m = root.GetComponent<AvatarBuildObfuscator>(); m.mode = ProtectionMode.Aggressive;
                m.subtreeModes = new[] { Scope(body.transform, ProtectionMode.Conservative) }; var s = new GuardSettings(m);
                Object.DestroyImmediate(body.gameObject); cloth.sharedMesh = Mesh("Merged"); new GuardProcessor(root, s);
                Check(s.ModeFor(cloth) == ProtectionMode.Conservative, "Conservative geometry promoted after merge");
            });
            Run("Unchanged aggressive mesh is not downgraded by unrelated removed body", () => {
                var root = Root(); var body = Renderer(root, "Body", Mesh()); var cloth = Renderer(root, "Clothes", Mesh());
                var m = root.GetComponent<AvatarBuildObfuscator>(); m.mode = ProtectionMode.Aggressive;
                m.subtreeModes = new[] { Scope(body.transform, ProtectionMode.Conservative) }; var s = new GuardSettings(m);
                Object.DestroyImmediate(body.gameObject); new GuardProcessor(root, s);
                Check(s.ModeFor(cloth) == ProtectionMode.Aggressive, "Unrelated unchanged mesh downgraded");
            });
            Run("Two subtree modes bake and retain compatibility shapes independently", () => {
                var root = Root(); var mesh = Mesh("smile"); var before = EditorJsonUtility.ToJson(mesh);
                var body = Renderer(root, "Body", mesh); var cloth = Renderer(root, "Clothes", mesh);
                root.GetComponent<AvatarBuildObfuscator>().subtreeModes = new[] { Scope(cloth.transform, ProtectionMode.Aggressive) };
                var report = Process(root);
                Check(body.sharedMesh.blendShapeCount == 1 && body.sharedMesh.GetBlendShapeName(0) == "smile", "Conservative shape changed");
                Check(cloth.sharedMesh.blendShapeCount == 0 && report.FrozenShapes == 1 && report.SubtreeOverrides == 1, "Aggressive shape not baked");
                Check(EditorJsonUtility.ToJson(mesh) == before && report.ConservativeScopeRenderers == 1 && report.AggressiveScopeRenderers == 1, "Source or report changed");
            });
            Run("Shared custom-shader mesh retains vertex order for conservative users", SharedScopeVertices);
            Run("Presets preserve subtree object references and modes", () => {
                var root = Root(); var a = Renderer(root, "Body", Mesh()); var m = root.GetComponent<AvatarBuildObfuscator>();
                m.subtreeModes = new[] { Scope(a.transform, ProtectionMode.Conservative) }; var preset = Track(ScriptableObject.CreateInstance<ObfuscationPreset>());
                preset.mode = ProtectionMode.Aggressive; preset.Apply(m);
                Check(m.subtreeModes.Length == 1 && m.subtreeModes[0].root == a.transform && m.subtreeModes[0].mode == ProtectionMode.Conservative, "Preset overwrote local scope");
            });
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(results, true)); return JsonUtility.ToJson(results, true);
        }

        private static SubtreeModeOverride Scope(Transform root, ProtectionMode mode) => new SubtreeModeOverride { root = root, mode = mode };

        private static void SharedScopeVertices()
        {
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.reorderMeshVertices = true;
            var a = new GameObject("Body"); a.transform.SetParent(root.transform, false);
            var b = new GameObject("Clothes"); b.transform.SetParent(root.transform, false);
            var mesh = Mesh(); var source = mesh.vertices.ToArray();
            var mat = Track(new Material(Shader.Find("Sprites/Default")));
            foreach (var go in new[] { a, b }) { go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat; }
            marker.subtreeModes = new[] { Scope(a.transform, ProtectionMode.Conservative) };
            var report = Process(root, ProtectionMode.Aggressive);
            Check(report.ReorderedMeshes == 0 && mesh.vertices.SequenceEqual(source), "Shared conservative resource lost protection");
        }
    }
}
