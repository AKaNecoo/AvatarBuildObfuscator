// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
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
        public static string RunDiagnostics(string output)
        {
            results = new Results();
            Run("Bone palettes preserve several posed meshes and source data", () => BonePalette("ordinary"));
            Run("Bone palettes work independently of resource renaming", () => BonePalette("no-renaming"));
            Run("Shared mesh palettes update every renderer", () => BonePalette("sharing"));
            Run("One manual exclusion preserves the shared palette", () => BonePalette("manual"));
            Run("Incomplete bone arrays preserve their original palette", () => BonePalette("incomplete"));
            Run("Conservative custom-shader palettes remain intact", () => BonePalette("custom"));
            Run("Aggressive custom-shader palettes can be remapped", () => BonePalette("aggressive"));
            Run("Manual vertex-order preservation works on shared meshes", VertexPreservation);
            Run("Manual UV exclusion retains coordinates and pixels", () => UvUnsupported("manual"));
            foreach (string shader in new[] { "Unlit/Transparent", "Unlit/Transparent Cutout" })
            {
                var name = shader;
                Run("UV rotation retains GPU output: " + name, () => UvRotation(TextureWrapMode.Clamp, name));
            }
            Run("Metadata scanner matches materials and hierarchy", MetadataScanner);
            Run("Metadata scanner rejects path-like fingerprints", InvalidFingerprint);
            Run("Preset apply cannot overwrite identity or object references", PresetIsolation);
            Run("Only the exact constant optimizer duration sentinel is classified", DurationPlaceholder);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(results, true)); return JsonUtility.ToJson(results, true);
        }

        private static void BonePalette(string scenario)
        {
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.renameGameObjects = false;
            marker.renameAssets = scenario != "no-renaming";
            var mesh = Mesh(); var renderer = Renderer(root, "Body", mesh);
            var bones = Enumerable.Range(0, 3).Select(i =>
            {
                var go = new GameObject("Bone" + i); go.transform.SetParent(root.transform, false); go.transform.localPosition = Vector3.up * (i * .2f); return go.transform;
            }).ToArray();
            renderer.bones = bones; renderer.rootBone = bones[0];
            mesh.bindposes = bones.Select(b => b.worldToLocalMatrix * renderer.transform.localToWorldMatrix).ToArray();
            mesh.boneWeights = new[] {
                new BoneWeight { boneIndex0=0, boneIndex1=1, weight0=.7f, weight1=.3f },
                new BoneWeight { boneIndex0=1, boneIndex1=2, weight0=.6f, weight1=.4f },
                new BoneWeight { boneIndex0=2, weight0=1f }
            };
            if (scenario == "custom" || scenario == "aggressive") renderer.sharedMaterial = Track(new Material(Shader.Find("Hidden/Internal-Colored")));
            SkinnedMeshRenderer second = null;
            if (scenario == "sharing" || scenario == "manual")
            {
                second = Renderer(root, "Second", mesh); second.bones = bones.ToArray(); second.rootBone = bones[0];
                if (scenario == "manual") marker.preserveBonePaletteRenderers = new[] { second };
            }
            if (scenario == "incomplete") renderer.bones = new[] { bones[0] };
            var originalWeights = mesh.boneWeights; var originalBinds = mesh.bindposes;
            Vector3[][] expected = null;
            if (scenario != "incomplete") expected = new[] { 0f, 11f, -23f }.Select(angle =>
            { bones[1].localRotation = Quaternion.Euler(0, 0, angle); return Baked(renderer).vertices; }).ToArray();
            bones[1].localRotation = Quaternion.identity;
            var report = Process(root, scenario == "aggressive" ? ProtectionMode.Aggressive : ProtectionMode.Conservative);
            bool retained = scenario == "manual" || scenario == "incomplete" || scenario == "custom";
            Check(report.ReorderedBonePalettes == (retained ? 0 : 1), report.Summary);
            Check(mesh.boneWeights.SequenceEqual(originalWeights) && mesh.bindposes.SequenceEqual(originalBinds), "Source skin data changed");
            if (retained) Check(renderer.bones.SequenceEqual(scenario == "incomplete" ? new[] { bones[0] } : bones), "Retained renderer changed");
            else
            {
                Check(!renderer.bones.SequenceEqual(bones) && renderer.sharedMesh != mesh, "No independent palette remap");
                int i = 0;
                foreach (var angle in new[] { 0f, 11f, -23f })
                { bones[1].localRotation = Quaternion.Euler(0, 0, angle); Check(Difference(expected[i++], Baked(renderer).vertices) < 1e-6f, "Posed geometry changed"); }
                if (second != null) Check(second.bones.SequenceEqual(renderer.bones) && second.sharedMesh == renderer.sharedMesh, "Shared renderer palette not updated");
            }
        }

        private static void VertexPreservation()
        {
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.reorderMeshVertices = true;
            var source = Mesh(); var a = Renderer(root, "Body", source); var b = Renderer(root, "Other", source);
            marker.preserveVertexOrderRenderers = new Renderer[] { b };
            var report = Process(root); Check(report.ReorderedMeshes == 0 && a.sharedMesh.vertices.SequenceEqual(source.vertices), "Shared preserved vertex order changed");
        }

        private static void MetadataScanner()
        {
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.embedOwnershipNotice = marker.embedBuildFingerprint = true;
            var r = Renderer(root, "Body", Mesh()); r.sharedMaterial = Track(new Material(Shader.Find("Unlit/Color")));
            var report = Process(root); var result = BuildMetadataInspection.Inspect(root);
            Check(result.materials == 1 && result.noticeMaterials == 1 && result.fingerprintMaterials == 1, "Material coverage incorrect");
            Check(result.hierarchyNotices == 3 && result.hierarchyFingerprints == 1 && result.fingerprints.Single() == report.BuildFingerprint, "Hierarchy metadata incorrect");
        }

        private static void InvalidFingerprint()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh()); r.sharedMaterial = Track(new Material(Shader.Find("Unlit/Color")));
            r.sharedMaterial.SetOverrideTag(GuardProcessor.FingerprintTag, "../../outside");
            var result = BuildMetadataInspection.Inspect(root); Check(result.fingerprints.Count == 0 && result.matchingReports.Count == 0, "Untrusted metadata became a path");
        }

        private static void PresetIsolation()
        {
            var a = Root().GetComponent<AvatarBuildObfuscator>(); a.userName = "Private identity"; a.rightsContact = "private@example.org";
            a.reorderBonePalette = false; a.experimentalUvTransform = true; a.renameMmdObjectNames = true;
            var preset = Track(ScriptableObject.CreateInstance<ObfuscationPreset>()); preset.Capture(a);
            Check(!EditorJsonUtility.ToJson(preset).Contains("private@example.org") && !EditorJsonUtility.ToJson(preset).Contains("Private identity"), "Preset leaked identity");
            preset.options.Add(new ObfuscationPreset.Option { property = "userName", enabled = true });
            var b = Root().GetComponent<AvatarBuildObfuscator>(); b.userName = "Other identity"; b.rightsContact = "other@example.org";
            preset.Apply(b);
            Check(b.userName == "Other identity" && b.rightsContact == "other@example.org" && !b.reorderBonePalette && b.experimentalUvTransform && b.renameMmdObjectNames, "Preset overwrote identity or failed to apply operations");
        }

        private static void DurationPlaceholder()
        {
            var clip = Track(new AnimationClip());
            var binding = EditorCurveBinding.FloatCurve("$AvatarOptimizerClipLengthDummy$", typeof(GameObject), "m_IsActive");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(1, 1, 1));
            Check(BuildComparison.IsDurationPlaceholder(clip, binding), "Known sentinel not recognized");
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0, 0, 1, 1));
            Check(!BuildComparison.IsDurationPlaceholder(clip, binding), "Nonconstant broken binding suppressed");
            var other = EditorCurveBinding.FloatCurve("ActuallyMissing", typeof(GameObject), "m_IsActive");
            AnimationUtility.SetEditorCurve(clip, other, AnimationCurve.Constant(1, 1, 1));
            Check(!BuildComparison.IsDurationPlaceholder(clip, other), "Unknown broken path suppressed");
        }
    }
}
