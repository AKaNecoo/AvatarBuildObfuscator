// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Editor
{
    [Serializable] public sealed class AuditEntry
    {
        public string category, type, name, path, reason, sourceGlobalId;
    }

    public sealed partial class GuardProcessor
    {
        public const string NoticeTag = "ABO.AuthorizationNotice", FingerprintTag = "ABO.BuildFingerprint";
        private readonly HashSet<int> sourceAssets = new HashSet<int>();
        private readonly HashSet<string> sourceObjectNames = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<GameObject> metadataObjects = new HashSet<GameObject>();

        private void CaptureAuditSources()
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) sourceObjectNames.Add(t.name);
            foreach (var asset in WalkReferencedAssets())
                if (!(asset is Component) && EditorUtility.IsPersistent(asset) && !GeneratedPath(AssetDatabase.GetAssetPath(asset)))
                    sourceAssets.Add(asset.GetInstanceID());
        }

        private static bool GeneratedPath(string path) => path.StartsWith("Packages/nadena.dev.ndmf/__Generated/", StringComparison.Ordinal) ||
            path.StartsWith("Packages/com.vrcfury.temp/", StringComparison.Ordinal);

        private void EmbedBuildMetadata()
        {
            if (!settings.EmbedNotice && !settings.EmbedFingerprint) return;
            string fingerprint = settings.EmbedFingerprint ? report.BuildFingerprint : "";
            string notice = settings.EmbedNotice ? OwnershipNotice.Create(settings.RightsHolder, settings.RightsContact,
                settings.UserName, settings.UsageRestrictions, fingerprint) : "";
            // Every material in this set is independently owned. Tags are data, not shader instructions.
            foreach (var material in owned.OfType<Material>())
            {
                if (settings.EmbedNotice) material.SetOverrideTag(NoticeTag, notice);
                if (settings.EmbedFingerprint) material.SetOverrideTag(FingerprintTag, fingerprint);
            }
            // Keep the readable notices out of the avatar's immediate child list.
            // This ordinary, independently named subtree contains no functional bindings.
            var container = AddMetadataObject(NewName(), root.transform).transform;
            if (settings.EmbedNotice)
            {
                AddMetadataObject("AUTHORIZATION_NOTICE: " + OwnershipNotice.English, container);
                AddMetadataObject(OwnershipNotice.Chinese, container);
                AddMetadataObject(OwnershipNotice.Japanese, container);
                if (!string.IsNullOrWhiteSpace(settings.RightsHolder))
                    AddMetadataObject("DECLARED_RIGHTS_HOLDER: " + SafeMetadataName(settings.RightsHolder), container);
            }
            if (settings.EmbedFingerprint) AddMetadataObject("BUILD_FINGERPRINT_" + fingerprint, container);
        }

        private static string SafeMetadataName(string value) => OwnershipNotice.Clean(value, 128).Replace('/', '_').Replace('\\', '_');
        private GameObject AddMetadataObject(string value, Transform parent)
        {
            var child = new GameObject(value);
            child.transform.SetParent(parent, false);
            metadataObjects.Add(child);
            return child;
        }

        public void AuditOutput(string stage)
        {
            if (root == null) return;
            var outputAssets = WalkReferencedAssets().ToArray();
            report.OutputObjects = root.GetComponentsInChildren<Transform>(true).Count(t => !metadataObjects.Contains(t.gameObject));
            report.OutputMaterials = outputAssets.OfType<Material>().Distinct().Count();
            report.OutputTextures = outputAssets.OfType<Texture>().Distinct().Count();
            report.OutputMeshes = outputAssets.OfType<Mesh>().Distinct().Count();
            var metadata = BuildMetadataInspection.Inspect(root, outputAssets.OfType<Material>(), false);
            report.NoticeMaterials = metadata.noticeMaterials;
            report.FingerprintMaterials = metadata.fingerprintMaterials;
            report.MetadataMismatches = metadata.invalidFingerprints + metadata.fingerprints.Count(f => f != report.BuildFingerprint);
            report.AuditStage = stage;
            report.AuditEntries.Clear();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (metadataObjects.Contains(t.gameObject)) continue;
                if (sourceObjectNames.Contains(t.name) && !UsernameNameGenerator.IsGeneratedName(t.name))
                    Audit("retained-object-name", t, AnimationUtility.CalculateTransformPath(t, root.transform),
                        t == root.transform ? "Avatar root identity" : !settings.RenameHierarchy ? "Object renaming disabled" :
                        settings.PreserveObjects.Any(p => p != null && (t == p || t.IsChildOf(p) || p.IsChildOf(t))) ? "Explicit preserved subtree or ancestor" :
                        unknownDriver && settings.Mode == ProtectionMode.Conservative ? "Conservative unknown-driver preservation" :
                        "SDK, humanoid or fixed-path contract; see build notes");
            }
            foreach (var asset in WalkReferencedAssets())
            {
                if (asset is Component) continue;
                string path = AssetDatabase.GetAssetPath(asset);
                if (sourceAssets.Contains(asset.GetInstanceID()))
                    Audit("source-resource-reference", asset, path, IsProxyAsset(asset, path) ? "SDK/built-in identity contract" :
                        !settings.RenameAssets ? "Resource naming disabled; source data retained" : asset is Shader ? "Fixed shader identity or unsupported shader source" :
                        "Unsupported, explicitly preserved or unavailable independent copy; see build notes");
                else if (!(asset is Shader) && !UsernameNameGenerator.IsGeneratedName(asset.name) && !string.IsNullOrEmpty(asset.name))
                    Audit("retained-resource-name", asset, path, "Renaming disabled or resource contract retained");
            }
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = r.sharedMesh;
                if (mesh == null) continue;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                    if (!UsernameNameGenerator.IsGeneratedName(mesh.GetBlendShapeName(i)))
                        report.AuditEntries.Add(new AuditEntry { category = "retained-shape-name", type = "BlendShape", name = mesh.GetBlendShapeName(i),
                            path = AnimationUtility.CalculateTransformPath(r.transform, root.transform), reason = "Preservation, SDK/world contract or unsupported shape layout" });
            }
            foreach (var marker in root.GetComponentsInChildren<AvatarBuildObfuscator>(true))
                Audit("editor-marker", marker, "", "Editor-only marker has not yet been removed by the caller");
            try
            {
                string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/AvatarBuildObfuscator/Reports");
                Directory.CreateDirectory(folder);
                string json = JsonUtility.ToJson(report, true);
                File.WriteAllText(Path.Combine(folder, "latest.json"), json);
                File.WriteAllText(Path.Combine(folder, report.BuildFingerprint + ".json"), json);
                foreach (var old in new DirectoryInfo(folder).GetFiles("*.json").Where(f => f.Name != "latest.json")
                    .OrderByDescending(f => f.LastWriteTimeUtc).Skip(12)) old.Delete();
            }
            catch (Exception e) { Debug.LogWarning("[Avatar Build Obfuscator] Local audit could not be written: " + e.Message); }
        }

        private void Audit(string category, Object asset, string path, string reason) => report.AuditEntries.Add(new AuditEntry
        { category = category, type = asset.GetType().Name, name = asset.name, path = path, reason = reason, sourceGlobalId = SourceId(asset) });

        private static string SourceId(Object asset)
        {
            try { return GlobalObjectId.GetGlobalObjectIdSlow(nadena.dev.ndmf.ObjectRegistry.GetReference(asset).Object).ToString(); }
            catch (Exception) { return ""; }
        }

        private static bool IsProxyAsset(Object asset, string path) => asset is AnimationClip clip && IsProxy(clip) ||
            path == "Resources/unity_builtin_extra" || path == "Library/unity default resources" || path.StartsWith("Packages/com.vrchat.", StringComparison.Ordinal);
    }

    // A later SDK callback catches changes made after this plugin's NDMF pass.
    // Manual NDMF builds still receive the explicitly labelled ndmf-pass report.
    public sealed class BuildAuditCompletion : IVRCSDKPreprocessAvatarCallback
    {
        private static readonly Dictionary<int, GuardProcessor> pending = new Dictionary<int, GuardProcessor>();
        public int callbackOrder => int.MaxValue;
        internal static void Forget(GameObject root) { if (root != null) pending.Remove(root.GetInstanceID()); }
        internal static void Register(GameObject root, GuardProcessor processor)
        {
            if (pending.Count >= 16) pending.Clear();
            pending[root.GetInstanceID()] = processor;
        }
        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            if (avatarGameObject != null && pending.TryGetValue(avatarGameObject.GetInstanceID(), out var processor))
            {
                pending.Remove(avatarGameObject.GetInstanceID());
                processor.AuditOutput("sdk-preprocess");
            }
            return true;
        }
    }
}
