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

        private void CaptureAuditSources() => CaptureAuditSources(WalkReferencedAssets());

        private Object[] CaptureAuditSnapshot(string stage)
        {
            if (!settings.AuditBuild) return null;
            Object[] assets = null;
            TimeStage(stage, () =>
            {
                assets = WalkReferencedAssets().ToArray();
                CaptureAuditSources(assets);
            });
            return assets;
        }

        private void CaptureAuditSources(IEnumerable<Object> assets)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) sourceObjectNames.Add(t.name);
            foreach (var asset in assets)
                if (!(asset is Component) && !owned.Contains(asset) && !sourceAssets.Contains(asset.GetInstanceID()) &&
                    EditorUtility.IsPersistent(asset) && !GeneratedPath(AssetDatabase.GetAssetPath(asset)))
                    sourceAssets.Add(asset.GetInstanceID());
        }

        private static bool GeneratedPath(string path) => path.StartsWith("Packages/nadena.dev.ndmf/__Generated/", StringComparison.Ordinal) ||
            path.StartsWith("Packages/com.vrcfury.temp/", StringComparison.Ordinal);

        private void EmbedBuildMetadata(bool includeHierarchy = true)
        {
            if (!settings.EmbedNotice && !settings.EmbedFingerprint) return;
            string fingerprint = settings.EmbedFingerprint ? report.BuildFingerprint : "";
            string notice = settings.EmbedNotice ? OwnershipNotice.Create(settings.RightsHolder, settings.RightsContact,
                settings.UserName, settings.UsageRestrictions, fingerprint) : "";
            // Every material in this set is independently owned. Tags are data, not shader instructions.
            foreach (var material in owned.OfType<Material>())
            {
                if (settings.EmbedNotice && material.GetTag(NoticeTag, false, "") != notice) material.SetOverrideTag(NoticeTag, notice);
                if (settings.EmbedFingerprint && material.GetTag(FingerprintTag, false, "") != fingerprint) material.SetOverrideTag(FingerprintTag, fingerprint);
            }
            if (!includeHierarchy) return;
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
            TimeStage(stage == "sdk-preprocess" ? "sdk-output-audit" : "output-audit", () => CollectOutputAudit(stage));
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

        private void CollectOutputAudit(string stage)
        {
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
            var identities = new List<(AuditEntry entry, Object source)>();
            void Audit(string category, Object asset, string path, string reason)
            {
                var entry = new AuditEntry { category = category, type = asset.GetType().Name, name = asset.name,
                    path = path, reason = reason, sourceGlobalId = "" };
                report.AuditEntries.Add(entry);
                try
                {
                    var source = nadena.dev.ndmf.ObjectRegistry.GetReference(asset).Object;
                    if (source != null) identities.Add((entry, source));
                    else entry.sourceGlobalId = SourceId(source);
                }
                catch (Exception) { }
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (metadataObjects.Contains(t.gameObject)) continue;
                if (sourceObjectNames.Contains(t.name) && !UsernameNameGenerator.IsGeneratedName(t.name))
                    Audit("retained-object-name", t, AnimationUtility.CalculateTransformPath(t, root.transform),
                        t == root.transform ? "Avatar root identity" : !settings.RenameHierarchy ? "Object renaming disabled" :
                        settings.PreserveObjects.Any(p => p != null && (t == p || t.IsChildOf(p) || p.IsChildOf(t))) ? "Explicit preserved subtree or ancestor" :
                        unknownDriver && settings.ModeFor(t) == ProtectionMode.Conservative ? "Conservative unknown-driver preservation" :
                        "SDK, humanoid or fixed-path contract; see build notes");
            }
            // Counts and entries describe the same unchanged output graph.
            foreach (var asset in outputAssets)
            {
                if (asset is Component) continue;
                if (sourceAssets.Contains(asset.GetInstanceID()))
                {
                    string path = AssetDatabase.GetAssetPath(asset);
                    Audit("source-resource-reference", asset, path, IsProxyAsset(asset, path) ? "SDK/built-in identity contract" :
                        !settings.RenameAssets ? "Resource naming disabled; source data retained" : asset is Shader ? "Fixed shader identity or unsupported shader source" :
                        "Unsupported, explicitly preserved or unavailable independent copy; see build notes");
                }
                else if (!(asset is Shader) && !UsernameNameGenerator.IsGeneratedName(asset.name) && !string.IsNullOrEmpty(asset.name))
                    Audit("retained-resource-name", asset, AssetDatabase.GetAssetPath(asset), "Renaming disabled or resource contract retained");
            }
            foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = r.sharedMesh;
                if (mesh == null) continue;
                string path = null;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string name = mesh.GetBlendShapeName(i);
                    if (UsernameNameGenerator.IsGeneratedName(name)) continue;
                    if (path == null) path = AnimationUtility.CalculateTransformPath(r.transform, root.transform);
                    report.AuditEntries.Add(new AuditEntry { category = "retained-shape-name", type = "BlendShape", name = name,
                        path = path, reason = "Preservation, SDK/world contract or unsupported shape layout" });
                }
            }
            foreach (var marker in root.GetComponentsInChildren<AvatarBuildObfuscator>(true))
                Audit("editor-marker", marker, "", "Editor-only marker has not yet been removed by the caller");
            if (identities.Count == 0) return;
            var sources = identities.Select(item => item.source).ToArray();
            var ids = new GlobalObjectId[sources.Length];
            try
            {
                GlobalObjectId.GetGlobalObjectIdsSlow(sources, ids);
                for (int i = 0; i < ids.Length; i++) identities[i].entry.sourceGlobalId = ids[i].ToString();
            }
            catch (Exception)
            {
                for (int i = 0; i < sources.Length; i++) identities[i].entry.sourceGlobalId = SourceId(sources[i]);
            }
        }

        private static string SourceId(Object asset)
        {
            try { return GlobalObjectId.GetGlobalObjectIdSlow(asset).ToString(); }
            catch (Exception) { return ""; }
        }

        private static bool IsProxyAsset(Object asset, string path) => asset is AnimationClip clip && IsProxy(clip) ||
            path == "Resources/unity_builtin_extra" || path == "Library/unity default resources" || path.StartsWith("Packages/com.vrchat.", StringComparison.Ordinal);
    }

    // A later SDK callback completes resources added after this plugin's NDMF pass.
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
                try { processor.FinishSdkBuild(); }
                catch (Exception e)
                {
                    Debug.LogException(new InvalidOperationException("[Avatar Build Obfuscator] SDK resource completion failed", e));
                    return false;
                }
            }
            return true;
        }
    }
}
