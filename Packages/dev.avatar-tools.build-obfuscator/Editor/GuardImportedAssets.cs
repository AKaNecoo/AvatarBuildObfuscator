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
    public sealed partial class GuardProcessor
    {
        private string importedAssetFolder;
        private bool sdkResourceCompletion;
        private ScriptableObject sdkResourceContainer;
        private readonly Dictionary<Object, Transform> copiedAnimationRoots = new Dictionary<Object, Transform>();

        /// <summary>
        /// Run before opening NDMF's serialization scope: asset imports must be allowed
        /// to complete. CopyAsset preserves compressed/imported data and importer settings
        /// without changing Read/Write, decoding textures or touching the source files.
        /// </summary>
        public void PrepareImportedAssets(string generatedFolder)
        {
            if (string.IsNullOrEmpty(generatedFolder)) return;
            generatedFolder = generatedFolder.Replace('\\', '/').TrimEnd('/');
            // NDMF's normal SDK/Play pipeline saves under its package's __Generated
            // tree. Manual/test builds can instead choose an Assets folder. Accept
            // both, without permitting arbitrary package sources or traversal.
            const string ndmfGenerated = "Packages/nadena.dev.ndmf/__Generated";
            bool allowed = generatedFolder.StartsWith("Assets/", StringComparison.Ordinal) ||
                generatedFolder == ndmfGenerated || generatedFolder.StartsWith(ndmfGenerated + "/", StringComparison.Ordinal);
            if (!allowed || generatedFolder.Split('/').Any(segment => segment == "." || segment == ".."))
                throw new ArgumentException("Generated assets must be inside Assets or NDMF's __Generated tree");
            if (!AssetDatabase.IsValidFolder(generatedFolder)) return;
            importedAssetFolder = generatedFolder;
            if (!settings.RenameAssets) return;
            var sources = WalkReferencedAssets().Where(x => x is Texture || x is AudioClip).ToArray();
            var pending = new List<Tuple<Object, string, string>>();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // CopyAsset normally refreshes and imports for every single file. Hundreds
            // of avatar textures/audio then repeatedly enter import/postprocessor work.
            // Queue copies as one batch and load the new objects only after imports.
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < sources.Length; i++)
                {
                    var asset = sources[i];
                    if (owned.Contains(asset) || sharedAssets.ContainsKey(asset)) continue;
                    if (asset is RenderTexture || !AssetDatabase.IsMainAsset(asset)) continue;
                    string path = AssetDatabase.GetAssetPath(asset);
                    var importer = AssetImporter.GetAtPath(path);
                    if (!(importer is TextureImporter || importer is AudioImporter)) continue;
                    if (EditorUtility.DisplayCancelableProgressBar("Avatar Build Obfuscator",
                        "复制构建资源 " + (i + 1) + "/" + sources.Length + "（完成后统一导入）", (float)i / sources.Length))
                        throw new OperationCanceledException("用户取消了构建资源复制");
                    string name = NewName();
                    string destination = ImportedAssetPath(generatedFolder, name, Path.GetExtension(path));
                    if (destination == null)
                    {
                        // Keep the source reference when even a compact independent copy
                        // cannot fit. Do not retry through Instantiate later in the pass.
                        sharedAssets[asset] = asset;
                        report.Note("构建输出目录过长，保留无法安全复制的贴图/音频；请缩短工程路径或 Avatar 根对象名称");
                        continue;
                    }
                    if (!AssetDatabase.CopyAsset(path, destination))
                    { report.Note("导入资源无法复制，保留原资源：" + asset.GetType().Name); continue; }
                    pending.Add(Tuple.Create(asset, destination, name));
                }
            }
            finally
            {
                // Always release import suspension, including user cancellation/errors.
                try { AssetDatabase.StopAssetEditing(); }
                finally { EditorUtility.ClearProgressBar(); }
            }
            // In an Editor callback StopAssetEditing can defer discovery until the
            // next refresh. Complete that single batch before loading replacements.
            if (pending.Count > 0) AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var item in pending)
            {
                var asset = item.Item1; var destination = item.Item2;
                var copy = AssetDatabase.LoadAssetAtPath(destination, asset.GetType());
                if (copy == null) throw new InvalidOperationException("新复制资源无法导入：" + destination);
                if (asset is Texture2D a && copy is Texture2D b &&
                    (a.width != b.width || a.height != b.height || a.format != b.format || a.mipmapCount != b.mipmapCount ||
                     a.isReadable != b.isReadable || a.graphicsFormat != b.graphicsFormat))
                    throw new InvalidOperationException("复制贴图改变了导入格式：" + AssetDatabase.GetAssetPath(asset));
                // The native audio importer still rejects Windows paths >= 260 chars.
                // Disk names may be compact; the build object's name retains the
                // complete username prefix and 128-bit homoglyph name used by other assets.
                copy.name = item.Item3;
                sharedAssets[asset] = copy;
                nadena.dev.ndmf.ObjectRegistry.TryRegisterReplacedObject(nadena.dev.ndmf.ObjectRegistry.GetReference(asset), copy);
                Own(copy); report.RenamedAssets++;
                if (asset is Texture) report.ImportedTextureCopies++;
            }
            watch.Stop();
            report.Note("批量复制/导入 " + pending.Count + " 个贴图/音频，用时 " + watch.Elapsed.TotalSeconds.ToString("F2") + " 秒");
        }

        private static string ImportedAssetPath(string folder, string name, string extension)
        {
            // Leave room below MAX_PATH for importer sidecars and unique-path suffixes.
            // Apply this budget on all hosts so the generated project stays portable.
            const int maxPath = 240, uniqueReserve = 8, minSuffix = 32; // 64 bits in the four-symbol alphabet.
            int budget = maxPath - uniqueReserve - Path.GetFullPath(folder + "/").Length - extension.Length;
            if (name.Length > budget)
            {
                int separator = name.Length - UsernameNameGenerator.SuffixLength - 1;
                int prefixLength = Math.Min(separator, budget - minSuffix - 1);
                if (prefixLength < 1) return null;
                int suffixLength = Math.Min(UsernameNameGenerator.SuffixLength, budget - prefixLength - 1);
                name = name.Substring(0, prefixLength) + "_" + name.Substring(separator + 1, suffixLength);
            }
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + extension);
            return Path.GetFullPath(path).Length <= maxPath ? path : null;
        }

        private IEnumerable<Object> WalkReferencedAssets()
            => BuildMetadataInspection.Dependencies(root);

        // SDK callbacks and late NDMF passes can add resources after the main pass.
        // Revisit only asset references, retaining the existing path/parameter maps;
        // do not run geometry, shape, UV or hierarchy transformations a second time.
        internal void FinishSdkBuild()
        {
            if (settings.RenameAssets || settings.RenameAnimatorInternals)
            {
                sdkResourceCompletion = true;
                try
                {
                    if (settings.AuditBuild) CaptureAuditSources();
                    PrepareImportedAssets(importedAssetFolder);
                    RewriteComponentAssets();
                    // An owned menu/material may have received a new source reference.
                    // Never write into unowned source objects reached through the graph.
                    foreach (var asset in WalkReferencedAssets().Where(owned.Contains).ToArray())
                    {
                        if (asset is Material material) RewriteMaterialReferences(material, material, root.transform);
                        else if (!(asset is Texture || asset is Mesh || asset is Avatar || asset is AudioClip || asset is Shader))
                            RewriteOwnedAssetReferences(asset);
                    }
                    EmbedBuildMetadata(false);
                    // Thousands of subassets can share one generated file. Save each
                    // dirty file once, rather than serializing that file for every child.
                    var persisted = owned.Concat(new Object[] { sdkResourceContainer })
                        .Where(a => a != null && EditorUtility.IsPersistent(a));
                    foreach (var group in persisted.GroupBy(AssetDatabase.GetAssetPath))
                    {
                        var dirty = group.FirstOrDefault(EditorUtility.IsDirty);
                        if (dirty != null) AssetDatabase.SaveAssetIfDirty(dirty);
                    }
                }
                finally { sdkResourceCompletion = false; }
            }
            if (settings.AuditBuild) AuditOutput("sdk-preprocess");
        }

        private void RewriteOwnedAssetReferences(Object asset)
        {
            using (var serialized = new SerializedObject(asset))
            {
                var p = serialized.GetIterator(); bool changed = false;
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference || p.propertyPath == "m_Script") continue;
                    var source = p.objectReferenceValue;
                    if (source == null || source is GameObject || source is Component || source is MonoScript) continue;
                    var replacement = CloneAsset(source, copiedAnimationRoots.TryGetValue(asset, out var animatorRoot) ? animatorRoot : root.transform);
                    if (replacement != source) { p.objectReferenceValue = replacement; changed = true; }
                }
                if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void SaveSdkResource(Object value)
        {
            if (EditorUtility.IsPersistent(value)) return;
            if (string.IsNullOrEmpty(importedAssetFolder)) { saveAsset?.Invoke(value); return; }
            if (sdkResourceContainer == null)
            {
                // The NDMF saver has finished by this callback. Use a new, independent
                // container in its generated folder instead of reopening that saver.
                sdkResourceContainer = ScriptableObject.CreateInstance<nadena.dev.ndmf.runtime.SubAssetContainer>();
                sdkResourceContainer.name = NewName();
                AssetDatabase.CreateAsset(sdkResourceContainer, AssetDatabase.GenerateUniqueAssetPath(
                    importedAssetFolder + "/abo-final-" + report.BuildFingerprint + ".asset"));
            }
            AssetDatabase.AddObjectToAsset(value, sdkResourceContainer);
        }

        private Shader CopyShader(Shader source)
        {
            string path = AssetDatabase.GetAssetPath(source);
            if (!path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) || !File.Exists(path) ||
                source.name.StartsWith("VRChat/Mobile/", StringComparison.Ordinal) || source.name.StartsWith("Hidden/Internal", StringComparison.Ordinal) ||
                source.name == "NadeVision/Depth Aware Nade")
            { report.Note("内置/移动白名单/非源码 Shader 保留契约名称与实现"); return source; }
            string code = File.ReadAllText(path);
            var declaration = Regex.Match(code, "\\bShader\\s*\"[^\"]+\"");
            if (!declaration.Success) return source;
            string name = NewName();
            code = code.Substring(0, declaration.Index) + "Shader \"" + name + "\"" + code.Substring(declaration.Index + declaration.Length);
            var directory = Path.GetDirectoryName(path).Replace('\\', '/');
            code = Regex.Replace(code, "(#\\s*(?:include|include_with_pragmas)\\s*\")([^\"]+)(\")", match =>
            {
                var include = match.Groups[2].Value;
                if (include.StartsWith("Assets/", StringComparison.Ordinal) || include.StartsWith("Packages/", StringComparison.Ordinal)) return match.Value;
                var resolved = directory + "/" + include;
                return File.Exists(resolved) ? match.Groups[1].Value + resolved + match.Groups[3].Value : match.Value;
            });
            if (EditorUtility.DisplayCancelableProgressBar("Avatar Build Obfuscator",
                "创建 Shader 副本（仅此构建）：" + source.name, .5f))
            { EditorUtility.ClearProgressBar(); throw new OperationCanceledException("用户取消了 Shader 副本创建"); }
            var watch = System.Diagnostics.Stopwatch.StartNew();
            Shader shader;
            try { shader = ShaderUtil.CreateShaderAsset(code, true); }
            finally { EditorUtility.ClearProgressBar(); }
            report.Note("Shader 副本创建用时 " + watch.Elapsed.TotalSeconds.ToString("F2") + " 秒：" + source.name);
            if (ShaderUtil.ShaderHasError(shader))
            { Object.DestroyImmediate(shader); throw new InvalidOperationException("复制 Shader 编译失败，停止本次构建：" + source.name); }
            return shader;
        }
    }
}
