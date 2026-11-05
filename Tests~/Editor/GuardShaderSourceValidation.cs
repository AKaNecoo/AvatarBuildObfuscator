// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AvatarTools.Obfuscation.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        // Thry integration is opt-in: no compile-time dependency on that package.
        public static string RunShaderSources(string output)
        {
            results = new Results();
            Directory.CreateDirectory(Folder + "/Fixtures");
            AssetDatabase.Refresh();
            foreach (ProtectionMode mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            {
                var captured = mode;
                Run("Source-backed optimizer shader survives serialization: " + mode, () => ShaderSourceFile(captured, false));
                Run("Thry locks the serialized copied shader: " + mode, () => ShaderSourceFile(captured, true));
                Run("Poiyomi source copy remains lockable: " + mode, () => ShaderSourcePoiyomi(captured));
                Run("Late SDK shader remains source-backed and lockable: " + mode, () => ShaderSourceFile(captured, true, true));
            }
            Run("Source-dependent shader is safely retained without an import folder", ShaderSourceWithoutFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(results, true));
            return JsonUtility.ToJson(results, true);
        }

        private static Shader ShaderSourceFixture(out string path)
        {
            string folder = Folder + "/Fixtures/SourceShader_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(folder + "/Includes");
            File.WriteAllText(folder + "/Includes/lil_pipeline_brp.hlsl", "#include \"UnityCG.cginc\"\nfloat4 ABOFixturePosition(float4 p) { return UnityObjectToClipPos(p); }\n");
            path = folder + "/Source.shader";
            File.WriteAllText(path, "Shader \"ABO/SourceRegression_" + Guid.NewGuid().ToString("N") + "\" {\n" +
                "Properties { [ThryShaderOptimizerLockButton] _ShaderOptimizerEnabled(\"Lock\", Int) = 0 _Color(\"Color\", Color) = (1,1,1,1) }\n" +
                "SubShader { Pass { CGPROGRAM\n#pragma vertex vert\n#pragma fragment frag\n" +
                "#include \"Includes/lil_pipeline_brp.hlsl\"\nfloat4 _Color;\n" +
                "float4 vert(float4 v:POSITION):SV_POSITION { return ABOFixturePosition(v); }\nfloat4 frag():SV_Target { return _Color; }\nENDCG\n} } }\n");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Check(shader != null && !ShaderUtil.ShaderHasError(shader), "Invalid source shader fixture");
            return shader;
        }

        private static void ShaderSourceFile(ProtectionMode mode, bool lockWithThry, bool late = false)
        {
            var shader = ShaderSourceFixture(out var sourcePath);
            var sourceBytes = File.ReadAllBytes(sourcePath);
            var sourceMeta = File.ReadAllBytes(sourcePath + ".meta");
            var includePath = Path.GetDirectoryName(sourcePath) + "/Includes/lil_pipeline_brp.hlsl";
            var includeBytes = File.ReadAllBytes(includePath);
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode;
            marker.userName = "ABO_Source_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var renderer = Renderer(root, "Body", Mesh());
            var source = Track(new Material(shader)); source.SetColor("_Color", Color.magenta); renderer.sharedMaterial = source;
            var originalMaterial = EditorJsonUtility.ToJson(source);
            string folder = Path.GetDirectoryName(sourcePath).Replace('\\', '/') + "/Output";
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var container = ScriptableObject.CreateInstance<nadena.dev.ndmf.runtime.SubAssetContainer>();
            AssetDatabase.CreateAsset(container, folder + "/Container.asset");
            var processor = new GuardProcessor(root, new GuardSettings(marker), asset => {
                if (!EditorUtility.IsPersistent(asset)) AssetDatabase.AddObjectToAsset(asset, container);
            });
            if (late) renderer.sharedMaterial = null;
            processor.PrepareImportedAssets(folder);
            AssetDatabase.StartAssetEditing();
            try { processor.Run(); }
            finally { AssetDatabase.StopAssetEditing(); }
            if (late)
            {
                renderer.sharedMaterial = source;
                typeof(GuardProcessor).GetMethod("FinishSdkBuild", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(processor, null);
            }
            AssetDatabase.SaveAssetIfDirty(container);
            var material = renderer.sharedMaterial; var copiedShader = material.shader;
            string copiedPath = AssetDatabase.GetAssetPath(copiedShader);
            Check(copiedShader != shader && UsernameNameGenerator.IsGeneratedName(copiedShader.name), "Shader not renamed on an independent copy");
            Check(copiedPath.StartsWith(folder + "/", StringComparison.Ordinal) && copiedPath.EndsWith(".shader", StringComparison.Ordinal),
                "Optimizer receives a binary asset container instead of a readable .shader source: " + copiedPath);
            Check(AssetDatabase.IsMainAsset(copiedShader) && !ShaderUtil.ShaderHasError(copiedShader), "Invalid imported shader copy");
            string copiedCode = File.ReadAllText(copiedPath);
            Check(copiedCode.Contains("#include \"" + includePath.Replace('\\', '/') + "\""), "Relative include was not rebased");
            Check(material.GetColor("_Color") == Color.magenta, "Material values changed");
            string beforeRepeat = copiedPath;
            processor.PrepareImportedAssets(folder);
            Check(renderer.sharedMaterial.shader == copiedShader && AssetDatabase.GetAssetPath(copiedShader) == beforeRepeat,
                "Repeated preparation copied the owned source again");

            if (lockWithThry)
            {
                var optimizer = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Thry.ThryEditor.ShaderOptimizer")).FirstOrDefault(t => t != null);
                Check(optimizer != null, "ThryEditor is required for this opt-in integration case");
                var method = optimizer.GetMethod("SetLockedForAllMaterials", BindingFlags.Public | BindingFlags.Static);
                Check(method != null, "Unsupported Thry locking API");
                bool locked = (bool)method.Invoke(null, new object[] { new[] { material }, 1, false, false, false, null });
                Check(locked && material.shader != copiedShader && material.shader != null && !ShaderUtil.ShaderHasError(material.shader),
                    "Thry could not lock the saved copied shader");
            }
            Check(EditorJsonUtility.ToJson(source) == originalMaterial && source.shader == shader, "Source material changed");
            Check(sourceBytes.SequenceEqual(File.ReadAllBytes(sourcePath)) && sourceMeta.SequenceEqual(File.ReadAllBytes(sourcePath + ".meta")) &&
                includeBytes.SequenceEqual(File.ReadAllBytes(includePath)), "Source shader/include/importer bytes changed");
        }

        private static void ShaderSourceWithoutFolder()
        {
            var shader = ShaderSourceFixture(out var sourcePath);
            var bytes = File.ReadAllBytes(sourcePath);
            var root = Root(); var renderer = Renderer(root, "Body", Mesh());
            var material = Track(new Material(shader)); renderer.sharedMaterial = material;
            var report = Process(root, ProtectionMode.Aggressive);
            Check(renderer.sharedMaterial.shader == shader && bytes.SequenceEqual(File.ReadAllBytes(sourcePath)),
                "Source-dependent shader was moved to an unreadable container without an import folder");
            Check(report.Notes.Any(n => n.Contains("Shader") && n.Contains("源码")), "Source-retention reason missing");
        }

        private static void ShaderSourcePoiyomi(ProtectionMode mode)
        {
            var shader = Shader.Find(".poiyomi/Poiyomi Toon");
            Check(shader != null && !ShaderUtil.ShaderHasError(shader), "Poiyomi Toon is required for this opt-in integration case");
            var sourcePath = AssetDatabase.GetAssetPath(shader); var bytes = File.ReadAllBytes(sourcePath);
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode;
            marker.userName = "ABOPoi_" + Guid.NewGuid().ToString("N").Substring(0, 25); // Full 32-character prefix.
            var renderer = Renderer(root, "Body", Mesh());
            var source = Track(new Material(shader)); renderer.sharedMaterial = source;
            string before = EditorJsonUtility.ToJson(source);
            string folder = Folder + "/Fixtures/Poiyomi_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var container = ScriptableObject.CreateInstance<nadena.dev.ndmf.runtime.SubAssetContainer>();
            AssetDatabase.CreateAsset(container, folder + "/Container.asset");
            var processor = new GuardProcessor(root, new GuardSettings(marker), asset => {
                if (!EditorUtility.IsPersistent(asset)) AssetDatabase.AddObjectToAsset(asset, container);
            });
            processor.PrepareImportedAssets(folder); processor.Run(); AssetDatabase.SaveAssetIfDirty(container);
            var material = renderer.sharedMaterial; var copied = material.shader;
            string path = AssetDatabase.GetAssetPath(copied);
            Check(copied != shader && copied.name.StartsWith(marker.userName + "_", StringComparison.Ordinal) && path.EndsWith(".shader"),
                "Poiyomi did not receive an independent named source file");
            Check(Path.GetFileName(path).Length <= 50, "Disk stem duplicates the full shader name in the optimizer cache");
            var optimizer = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Thry.ThryEditor.ShaderOptimizer")).FirstOrDefault(t => t != null);
            Check(optimizer != null, "ThryEditor is required");
            bool locked = (bool)optimizer.GetMethod("SetLockedForAllMaterials", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { new[] { material }, 1, false, false, false, null });
            Check(locked && material.shader != copied && material.shader != null && !ShaderUtil.ShaderHasError(material.shader),
                "Poiyomi locking failed after obfuscation");
            Check(source.shader == shader && before == EditorJsonUtility.ToJson(source) && bytes.SequenceEqual(File.ReadAllBytes(sourcePath)),
                "Poiyomi source material or shader changed");
        }
    }
}
