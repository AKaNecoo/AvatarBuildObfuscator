// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        private readonly Dictionary<SkinnedMeshRenderer, HashSet<string>> removedShapeCurves = new Dictionary<SkinnedMeshRenderer, HashSet<string>>();
        private readonly Dictionary<SkinnedMeshRenderer, HashSet<string>> fixedShapeNames = new Dictionary<SkinnedMeshRenderer, HashSet<string>>();

        private void CollectFixedShapeNames()
        {
            foreach (var site in clips.Where(s => IsProxy(s.Clip)))
                foreach (var binding in AnimationUtility.GetCurveBindings(site.Clip))
                {
                    if (!binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)) continue;
                    foreach (var renderer in ResolveRenderers(site.Root, binding.path))
                    {
                        if (!fixedShapeNames.TryGetValue(renderer, out var names)) fixedShapeNames[renderer] = names = new HashSet<string>(StringComparer.Ordinal);
                        names.Add(binding.propertyName.Substring(11));
                    }
                }
        }

        private bool ProtectedShape(SkinnedMeshRenderer renderer, string name) => settings.PreserveShapes.Contains(name) ||
            settings.ModeFor(renderer) == ProtectionMode.Conservative && CompatibilityShape(name) ||
            fixedShapeNames.TryGetValue(renderer, out var fixedNames) && fixedNames.Contains(name) ||
            nativeShapes.TryGetValue(renderer, out var native) && native.Contains(name);

        private void PlanShapeReductions(SkinnedMeshRenderer renderer, Mesh mesh, string[] shapeNames, float[] weights,
            out bool[] empty, out Dictionary<int, int> merges)
        {
            empty = new bool[shapeNames.Length]; merges = new Dictionary<int, int>();
            if (unknownDriver || (!settings.PruneEmptyShapes && !settings.MergeShapes)) return;
            var dv = new Vector3[mesh.vertexCount]; var dn = new Vector3[mesh.vertexCount]; var dt = new Vector3[mesh.vertexCount];
            if (settings.PruneEmptyShapes)
                for (int i = 0; i < shapeNames.Length; i++)
                {
                    if (ProtectedShape(renderer, shapeNames[i]) || float.IsNaN(weights[i]) || float.IsInfinity(weights[i])) continue;
                    bool zero = mesh.GetBlendShapeFrameCount(i) > 0;
                    for (int f = 0; zero && f < mesh.GetBlendShapeFrameCount(i); f++)
                    {
                        mesh.GetBlendShapeFrameVertices(i, f, dv, dn, dt);
                        for (int v = 0; zero && v < dv.Length; v++) zero = ExactZero(dv[v]) && ExactZero(dn[v]) && ExactZero(dt[v]);
                    }
                    empty[i] = zero;
                }
            if (!empty.Any(x => x) && !settings.MergeShapes) return;
            var blocked = new HashSet<string>(StringComparer.Ordinal);
            var signatures = ShapeDriveSignatures(renderer, blocked);
            for (int i = 0; i < empty.Length; i++) if (blocked.Contains(shapeNames[i])) empty[i] = false;
            if (!settings.MergeShapes) return;
            var taken = new HashSet<int>();
            for (int a = 0; a < shapeNames.Length; a++)
            {
                if (taken.Contains(a) || empty[a] || ProtectedShape(renderer, shapeNames[a]) || blocked.Contains(shapeNames[a]) ||
                    !OrdinaryMergeShape(mesh, a, weights[a]) || !signatures.TryGetValue(shapeNames[a], out var signature)) continue;
                for (int b = a + 1; b < shapeNames.Length; b++)
                {
                    if (taken.Contains(b) || empty[b] || ProtectedShape(renderer, shapeNames[b]) || blocked.Contains(shapeNames[b]) ||
                        weights[a] != weights[b] || !OrdinaryMergeShape(mesh, b, weights[b]) ||
                        !signatures.TryGetValue(shapeNames[b], out var other) || signature != other) continue;
                    merges[a] = b; taken.Add(a); taken.Add(b); break;
                }
            }
        }

        private static bool ExactZero(Vector3 v) => v.x == 0f && v.y == 0f && v.z == 0f;
        private static bool OrdinaryMergeShape(Mesh mesh, int index, float weight) => weight >= 0 && weight <= 100 &&
            mesh.GetBlendShapeFrameCount(index) == 1 && mesh.GetBlendShapeFrameWeight(index, 0) == 100f;

        private Dictionary<string, string> ShapeDriveSignatures(SkinnedMeshRenderer renderer, HashSet<string> blocked)
        {
            var signatures = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
            for (int siteIndex = 0; siteIndex < clips.Count; siteIndex++)
            {
                var site = clips[siteIndex];
                foreach (var binding in AnimationUtility.GetCurveBindings(site.Clip))
                {
                    if (!binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)) continue;
                    var targets = ResolveRenderers(site.Root, binding.path).ToArray();
                    if (!targets.Contains(renderer)) continue;
                    string name = binding.propertyName.Substring(11);
                    if (targets.Length != 1 || IsProxy(site.Clip)) { blocked.Add(name); continue; }
                    var curve = AnimationUtility.GetEditorCurve(site.Clip, binding);
                    if (curve == null) { blocked.Add(name); continue; }
                    if (!signatures.TryGetValue(name, out var text)) signatures[name] = text = new StringBuilder();
                    text.Append(siteIndex).Append(':').Append(CurveFingerprint(curve)).Append(';');
                }
            }
            return signatures.ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.Ordinal);
        }

        private static string CurveFingerprint(AnimationCurve curve)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var sha = SHA256.Create())
            {
                writer.Write((int)curve.preWrapMode); writer.Write((int)curve.postWrapMode);
                foreach (var key in curve.keys)
                {
                    writer.Write(key.time); writer.Write(key.value); writer.Write(key.inTangent); writer.Write(key.outTangent);
                    writer.Write(key.inWeight); writer.Write(key.outWeight); writer.Write((int)key.weightedMode);
                }
                writer.Flush(); return Convert.ToBase64String(sha.ComputeHash(stream.ToArray()));
            }
        }

        private bool RemoveShapeBinding(EditorCurveBinding binding, Transform relativeRoot)
        {
            if (!binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)) return false;
            var targets = ResolveRenderers(relativeRoot, binding.path).ToArray();
            return targets.Length == 1 && removedShapeCurves.TryGetValue(targets[0], out var removed) && removed.Contains(binding.propertyName.Substring(11));
        }
    }
}
