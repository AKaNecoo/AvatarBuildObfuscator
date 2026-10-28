// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.IO;
using System.Linq;
using AvatarTools.Obfuscation;
using AvatarTools.Obfuscation.Editor;
using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        public static string RunMaterialReferences(string output)
        {
            Directory.CreateDirectory(Folder + "/Fixtures");
            AssetDatabase.Refresh();
            results = new Results();
            MaterialReferenceTests();
            string json = JsonUtility.ToJson(results, true);
            File.WriteAllText(output, json);
            return json;
        }

        private static void MaterialReferenceTests()
        {
            foreach (var mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            foreach (bool unreadable in new[] { false, true })
            foreach (bool rename in new[] { false, true })
            {
                var selectedMode = mode; bool selectedUnreadable = unreadable, selectedRename = rename;
                Run("Saved material retains generated textures " + mode + "/unreadable=" + unreadable + "/rename=" + rename,
                    () => SavedMaterialReferences(selectedMode, selectedUnreadable, selectedRename));
            }
        }

        private static void SavedMaterialReferences(ProtectionMode mode, bool unreadable, bool rename)
        {
            string suffix = Guid.NewGuid().ToString("N");
            string path = Folder + "/Fixtures/MaterialReferences_" + suffix + ".shader";
            File.WriteAllText(path, "Shader \"ABO/MaterialReferences" + suffix + "\" { Properties { " +
                "_MainTex(\"Main\",2D)=\"white\"{} _ReflectionColorTex(\"Reflection\",2D)=\"white\"{} " +
                "_EmptyTex(\"Empty\",2D)=\"white\"{} _MatCapBlendMask(\"MatCap\",2D)=\"white\"{} " +
                "_UseReflection(\"Reflection on\",Float)=1 _UseMatCap(\"MatCap on\",Float)=1 " +
                "_Color(\"Color\",Color)=(1,1,1,1) _Vector(\"Vector\",Vector)=(0,0,0,0) } " +
                "SubShader { Pass { Name \"MaskPass\" CGPROGRAM\n#pragma vertex vert\n#pragma fragment frag\n" +
                "#pragma multi_compile_local _ ABO_TEST\n#include \"UnityCG.cginc\"\n" +
                "float4 vert(float4 v:POSITION):SV_POSITION { return UnityObjectToClipPos(v); } " +
                "float4 frag():SV_Target { return float4(1,1,1,1); }\nENDCG } } }");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Check(shader != null && !ShaderUtil.ShaderHasError(shader), "Fixture shader failed to compile");
            var shaderBytes = File.ReadAllBytes(path);
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>();
            marker.mode = mode; marker.renameAssets = rename; marker.embedBuildFingerprint = true;
            var renderer = root.AddComponent<MeshRenderer>();
            var material = Track(new Material(shader) { name = "GeneratedMaterial", renderQueue = 2451 });
            var main = Track(new Texture2D(8, 8, TextureFormat.RGBA32, false));
            var mask = Track(new Texture2D(8, 8, TextureFormat.RGBA32, false));
            main.SetPixels(Enumerable.Repeat(Color.blue, 64).ToArray()); main.Apply();
            mask.SetPixels(Enumerable.Repeat(new Color(.2f, .4f, .6f, .8f), 64).ToArray()); mask.Apply(false, unreadable);
            material.SetTexture("_MatCapBlendMask", mask); material.SetTexture("_MainTex", main);
            material.SetTexture("_ReflectionColorTex", mask); material.SetTexture("_EmptyTex", null);
            material.SetTextureScale("_ReflectionColorTex", new Vector2(.37f, .62f));
            material.SetTextureOffset("_ReflectionColorTex", new Vector2(.13f, -.21f));
            material.SetTextureScale("_MatCapBlendMask", new Vector2(2, 3));
            material.SetTextureOffset("_MatCapBlendMask", new Vector2(.2f, .4f));
            material.SetColor("_Color", Color.cyan); material.SetVector("_Vector", new Vector4(.1f, .2f, .3f, .4f));
            material.EnableKeyword("ABO_TEST"); material.SetShaderPassEnabled("MaskPass", false);
            material.SetOverrideTag("ABOFixture", "Keep this tag"); renderer.sharedMaterial = material;
            string sourceJson = EditorJsonUtility.ToJson(material);
            var beforeMain = Pixels(main); var beforeMask = Pixels(mask);
            // Match NDMF's immediate asset saving without saving unrelated dirty project assets.
            var container = new Mesh { name = "MaterialReferenceContainer" };
            AssetDatabase.CreateAsset(container, Folder + "/Fixtures/MaterialReferences_" + suffix + ".asset");
            var processor = new GuardProcessor(root, new GuardSettings(marker), asset => {
                Track(asset); AssetDatabase.AddObjectToAsset(asset, container);
            });
            processor.Run();
            var output = renderer.sharedMaterial;
            Check(output != material && EditorUtility.IsPersistent(output), "Expected an independently saved material copy");
            Check(output.GetTexture("_ReflectionColorTex") != null && output.GetTexture("_MatCapBlendMask") != null,
                "Generated mask references were cleared on the saved material");
            Check(output.GetTexture("_MainTex") != null && output.GetTexture("_EmptyTex") == null, "Main/empty texture slots changed");
            Check(output.GetTexture("_ReflectionColorTex") == output.GetTexture("_MatCapBlendMask"), "Shared mask identity was split");
            Check(beforeMain.SequenceEqual(Pixels(output.GetTexture("_MainTex"))) &&
                beforeMask.SequenceEqual(Pixels(output.GetTexture("_ReflectionColorTex"))), "Material texture pixels changed");
            foreach (string property in new[] { "_ReflectionColorTex", "_MatCapBlendMask" })
                Check(output.GetTextureScale(property) == material.GetTextureScale(property) &&
                    output.GetTextureOffset(property) == material.GetTextureOffset(property), "Texture transform changed: " + property);
            Check(output.GetFloat("_UseReflection") == 1 && output.GetFloat("_UseMatCap") == 1 &&
                output.GetColor("_Color") == Color.cyan && output.GetVector("_Vector") == material.GetVector("_Vector"), "Material values changed");
            Check(output.renderQueue == 2451 && output.IsKeywordEnabled("ABO_TEST") && !output.GetShaderPassEnabled("MaskPass") &&
                output.GetTag("ABOFixture", false, "") == "Keep this tag", "Material rendering state changed");
            Check(EditorJsonUtility.ToJson(material) == sourceJson && beforeMain.SequenceEqual(Pixels(main)) &&
                beforeMask.SequenceEqual(Pixels(mask)) && !EditorUtility.IsPersistent(main) && !EditorUtility.IsPersistent(mask),
                "Source material/texture state changed");
            Check(shaderBytes.SequenceEqual(File.ReadAllBytes(path)), "Source shader file changed");
        }
    }
}
