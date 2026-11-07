// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AvatarTools.Obfuscation.Editor;
using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        public static string RunShaderPerformance(string output)
        {
            results = new Results(); Directory.CreateDirectory(Folder + "/Fixtures"); AssetDatabase.Refresh();
            foreach (ProtectionMode mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            {
                var captured = mode;
                Run("Deferred shader names reuse the native lock cache: " + mode, () => ShaderCacheReuse(captured));
                Run("Skipped locking retains readable final sources: " + mode, () => ShaderCacheReuse(captured, false));
            }
            Run("Compiled Unlit source retains rendered pixels and properties", () => {
                var shader = ShaderSourceFixture(out var path);
                File.WriteAllText(path, File.ReadAllText(path).Replace("[ThryShaderOptimizerLockButton]", ""));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                CompiledShaderRendering(AssetDatabase.LoadAssetAtPath<Shader>(path));
            });
            Run("Compiled lilToon source retains rendered pixels and properties", () => CompiledShaderRendering(Shader.Find("lilToon")));
            Run("Compiled locked Poiyomi retains rendered pixels and properties", () => CompiledShaderRendering(
                Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(s => s != null && s.name.StartsWith("Hidden/Locked/.poiyomi/", StringComparison.Ordinal))));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(results, true)); return JsonUtility.ToJson(results, true);
        }

        private static void CompiledShaderRendering(Shader shader)
        {
            Check(shader != null && shader.isSupported, "Required shader is missing");
            var fixture = UvFixture(); var marker = fixture.Item1.GetComponent<AvatarBuildObfuscator>(); marker.experimentalUvTransform = false;
            var material = Track(new Material(shader)); fixture.Item2.sharedMaterial = material;
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", fixture.Item4);
            if (material.HasProperty("_AsUnlit")) material.SetFloat("_AsUnlit", 1);
            string sourceName = shader.name, sourceMaterial = EditorJsonUtility.ToJson(material);
            var before = UvPixels(fixture.Item1); Process(fixture.Item1);
            var copy = fixture.Item2.sharedMaterial.shader; var after = UvPixels(fixture.Item1);
            Check(copy != shader && UsernameNameGenerator.IsGeneratedName(copy.name), "Compiled copy lost its generated identity");
            Check(copy.GetPropertyCount() == shader.GetPropertyCount() && copy.passCount == shader.passCount && !ShaderUtil.ShaderHasError(copy), "Compiled shader contract changed");
            Check(before.Any(c => c.r != 0 || c.g != 0 || c.b != 0) && PixelDifference(before, after) == 0, "Compiled shader rendering changed or fixture was blank");
            Check(shader.name == sourceName && sourceMaterial == EditorJsonUtility.ToJson(material), "Source shader/material changed");
        }

        private static void ShaderCacheReuse(ProtectionMode mode, bool lockMaterial = true)
        {
            var shader = ShaderSourceFixture(out var sourcePath); var inputBytes = File.ReadAllBytes(sourcePath);
            var source = Track(new Material(shader)); source.SetColor("_Color", Color.magenta);
            string sourceJson = EditorJsonUtility.ToJson(source), previousLocked = null, previousOutput = null;
            var optimizer = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Thry.ThryEditor.ShaderOptimizer")).FirstOrDefault(t => t != null);
            Check(optimizer != null, "ThryEditor is required");
            for (int i = 0; i < 2; i++)
            {
                var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode;
                marker.randomizeEachBuild = true; marker.userName = "CacheUser";
                var renderer = Renderer(root, "Body", Mesh()); renderer.sharedMaterial = source;
                string folder = Path.GetDirectoryName(sourcePath).Replace('\\', '/') + "/Output" + i;
                Directory.CreateDirectory(folder); AssetDatabase.Refresh();
                var container = ScriptableObject.CreateInstance<nadena.dev.ndmf.runtime.SubAssetContainer>();
                AssetDatabase.CreateAsset(container, folder + "/Container.asset");
                var processor = new GuardProcessor(root, new GuardSettings(marker), value => {
                    if (!EditorUtility.IsPersistent(value)) AssetDatabase.AddObjectToAsset(value, container);
                });
                typeof(GuardProcessor).GetField("DeferUnlockedShaderNames", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(processor, true);
                processor.PrepareImportedAssets(folder); var report = processor.Run();
                Check(renderer.sharedMaterial != source && renderer.sharedMaterial.shader == shader, "Original source identity was lost before locking");
                Check(!Directory.GetFiles(folder, "*.shader").Any(), "Temporary random source invalidates the locker cache");
                if (lockMaterial)
                {
                    bool success = (bool)optimizer.GetMethod("SetLockedForAllMaterials", BindingFlags.Public | BindingFlags.Static)
                        .Invoke(null, new object[] { new[] { renderer.sharedMaterial }, 1, false, false, false, null });
                    Check(success, "Native locking failed");
                    string lockedPath = AssetDatabase.GetAssetPath(renderer.sharedMaterial.shader);
                    if (i != 0) Check(lockedPath == previousLocked, "Per-build names prevent reuse of the same optimized shader");
                    previousLocked = lockedPath;
                }
                typeof(GuardProcessor).GetMethod("FinishSdkBuild", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(processor, null);
                var outputShader = renderer.sharedMaterial.shader;
                Check(outputShader != shader && UsernameNameGenerator.IsGeneratedName(outputShader.name) && !ShaderUtil.ShaderHasError(outputShader), "Final shader identity/compilation invalid");
                if (!lockMaterial) Check(AssetDatabase.GetAssetPath(outputShader).EndsWith(".shader"), "Skipped locker lost readable final source");
                if (i != 0) Check(outputShader.name != previousOutput, "Final shader randomization was lost");
                previousOutput = outputShader.name;
                Check(report.Timings.Any(t => t.Stage == "imports") && report.Timings.All(t => t.Seconds >= 0), "Timing data missing or invalid");
            }
            Check(sourceJson == EditorJsonUtility.ToJson(source) && source.shader == shader && inputBytes.SequenceEqual(File.ReadAllBytes(sourcePath)), "Source material/shader changed");
        }
    }
}
