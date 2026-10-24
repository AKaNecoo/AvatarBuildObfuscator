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
        // Opt-in integration tests: requires the exact upstream shader packages in TESTING.md.
        public static string RunShaders(string output)
        {
            results = new Results();
            foreach (string name in new[] { "lilToon", "Hidden/lilToonOutline", "Hidden/lilToonCutout", "Hidden/lilToonTransparent",
                "Hidden/lilToonCutoutOutline", "Hidden/lilToonTransparentOutline" })
            {
                string shader = name;
                Run("lilToon sampling, masks and source isolation: " + name, () => ProfileRendering(shader, "_MainTex"));
            }
            Run("Poiyomi independent main sampling", () => ProfileRendering(".poiyomi/Poiyomi Toon", "_MainTex"));
            Run("NonToon default single texture preserves rendered sampling", () => NonToonSimpleRendering(false));
            Run("NonToon compressed base texture preserves rendered sampling", () => NonToonSimpleRendering(true));
            Run("NonToon non-default texture remains unchanged", NonToonPreserved);
            Run("UnlitWF independent second layer", () => ProfileRendering("UnlitWF/WF_UnToon_Opaque", "_TX2_MainTex"));
            Run("lilToon unreadable compressed material renders identically", () => ProfileRendering("lilToon", "_MainTex", true));
            Run("Poiyomi unreadable compressed material renders identically", () => ProfileRendering(".poiyomi/Poiyomi Toon", "_MainTex", true));
            Run("Poiyomi locked material retains texture coordinates", () => PoiyomiPreserved("_ShaderOptimizerEnabled"));
            Run("Poiyomi stochastic sampling retains texture coordinates", () => PoiyomiPreserved("_MainTexStochastic"));
            foreach (var format in new[] { TextureFormat.RGBA32, TextureFormat.ARGB32, TextureFormat.BGRA32, TextureFormat.RGB24,
                TextureFormat.DXT1, TextureFormat.DXT5, TextureFormat.BC4, TextureFormat.BC5, TextureFormat.BC7 })
                foreach (bool linear in new[] { false, true })
                {
                    var captured = format; bool space = linear;
                    Run("Unreadable rotation preserves format/mips or safely retains BC7: " + format + " linear=" + linear, () => ProfileMips(captured, space));
                }
            foreach (string reason in new[] { "excluded", "property-block", "animated", "sharing-excluded", "mirror-once", "scroll", "parallax", "emission-mask" })
            {
                string captured = reason;
                Run("lilToon preserves unsafe contract: " + reason, () => ProfilePreserve(captured));
            }
            Run("Unrelated particles and material animation do not block a safe material", ProfileUnrelated);
            Run("Copied shader resolves back to original profile", ProfileCopiedShader);
            Run("BRP-inactive lilToon aliases normalize to main without changing rendering", ProfileAliases);
            Run("Color-only material animation retains UV compensation", ProfileColorAnimation);
            Run("Material-slot swaps retain both rendered material contracts", ProfileMaterialSwap);
            Run("Absent stock-lilToon deformation uniforms do not block sampling", ProfileAbsentUniforms);
            Run("Color-only property blocks retain pixels and original block", () => ProfileBlocks("color"));
            Run("Unrelated editor property blocks do not prevent conversion", () => ProfileBlocks("unrelated"));
            Run("Indexed texture-coordinate property blocks preserve sampling", () => ProfileBlocks("indexed-uv"));
            Run("Animated lilToon light clamps retain sampled appearance", ProfileLightAnimation);
            Run("lilToon shadow LUT keeps its lookup pixels", ProfileShadowLut);
            Run("Animated lilToon scalar features retain sampled appearance", ProfileScalarAnimation);
            foreach (string property in new[] { "_UseGlitter", "_UseParallax", "_ShadowColorType", "_UseEmission" })
            {
                string captured = property;
                Run("Sampling-changing scalar curve is retained: " + property, () => ProfileUnsafeScalar(captured));
            }
            Run("Scalar lighting property block remains intact", () => ProfileBlocks("scalar"));
            Run("Owned streaming build copy decodes without source importer changes", () => ProfileStreaming(true));
            Run("Unowned streaming texture is retained without residency changes", () => ProfileStreaming(false));
            foreach (bool profile in new[] { false, true })
                foreach (bool mips in new[] { false, true })
                {
                    bool adapter = profile, chain = mips;
                    Run("Generated streaming/priority: profile=" + profile + " mips=" + mips, () => RotationStreaming(adapter, chain));
                }
            Run("Incompatible compressed mip blocks retain the complete material", ProfileCompressedLayout);
            Run("Texture storage guard rejects expansion or changed dimensions", ProfileStorageGuard);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(results, true));
            return JsonUtility.ToJson(results, true);
        }

        private static Tuple<GameObject, SkinnedMeshRenderer, Material, Texture2D> ProfileFixture(string shaderName, string property)
        {
            var fixture = UvFixture(); var marker = fixture.Item1.GetComponent<AvatarBuildObfuscator>();
            marker.renameAssets = false;
            var shader = Shader.Find(shaderName);
            Check(shader != null && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "Required shader missing or unsupported: " + shaderName);
            var material = Track(new Material(shader)); fixture.Item2.sharedMaterial = material;
            material.SetTexture(property, fixture.Item4);
            material.SetTextureScale(property, new Vector2(1.25f, -.75f)); material.SetTextureOffset(property, new Vector2(.13f, .27f));
            if (shaderName.Contains("lilToon"))
            {
                material.SetFloat("_Cull", 0); material.SetFloat("_AsUnlit", .5f);
                foreach (string slot in new[] { "_MainColorAdjustMask", "_ShadowStrengthMask", "_ShadowBlurMask", "_ShadowColorTex", "_RimColorTex",
                    "_Main2ndBlendMask", "_Bump2ndScaleMask", "_AlphaMask", "_MatCapBlendMask" })
                {
                    material.SetTexture(slot, fixture.Item4);
                    material.SetTextureScale(slot, new Vector2(.8f, 1.3f)); material.SetTextureOffset(slot, new Vector2(.2f, -.4f));
                }
                material.SetFloat("_UseShadow", 1); material.SetFloat("_UseRim", 1); material.SetFloat("_UseEmission", 1);
                material.SetFloat("_AlphaMaskMode", 1); material.SetFloat("_Cutoff", .01f);
                var normal = Track(new Texture2D(8, 8, TextureFormat.RGBA32, true, true));
                normal.SetPixels32(Enumerable.Range(0, 64).Select(i => new Color32((byte)(118 + i % 20), (byte)(118 + i / 4), 255, 255)).ToArray());
                normal.Apply(); material.SetTexture("_BumpMap", normal); material.SetFloat("_UseBumpMap", 1);
                material.SetTextureScale("_BumpMap", new Vector2(.6f, 1.7f)); material.SetTextureOffset("_BumpMap", new Vector2(.2f, -.1f));
                material.SetColor("_EmissionColor", new Color(.12f, .15f, .08f, 1));
                material.SetFloat("_UseMatCap", 1); material.SetFloat("_MatCapBlend", .2f);
                material.SetTexture("_OutlineTex", fixture.Item4); material.SetFloat("_OutlineWidth", .01f);
                material.SetTexture("_BaseMap", fixture.Item4); material.SetTexture("_BaseColorMap", fixture.Item4);
            }
            if (shaderName.StartsWith("UnlitWF/")) { material.SetFloat("_TX2_Enable", 1); material.EnableKeyword("_TX2_ENABLE"); material.SetFloat("_CullMode", 0); }
            return Tuple.Create(fixture.Item1, fixture.Item2, material, fixture.Item4);
        }

        private static void ProfileRendering(string shader, string property, bool compressed = false)
        {
            var fixture = ProfileFixture(shader, property); var material = fixture.Item3;
            if (compressed) { EditorUtility.CompressTexture(fixture.Item4, TextureFormat.DXT5, TextureCompressionQuality.Normal); fixture.Item4.Apply(false, true); }
            string beforeMaterial = EditorJsonUtility.ToJson(material);
            var beforeUv = fixture.Item2.sharedMesh.uv;
            var before = UvPixels(fixture.Item1);
            material.SetTexture(property, Texture2D.whiteTexture);
            var white = UvPixels(fixture.Item1);
            material.SetTexture(property, fixture.Item4);
            Check(PixelDifference(before, white) > 10, "Test did not exercise the selected texture layer");
            var report = Process(fixture.Item1);
            var after = UvPixels(fixture.Item1);
            Check(report.TransformedUvMaterials == 1 && report.TransformedUvTextures >= 1,
                "Material not transformed: " + string.Join("; ", report.Notes));
            Check(fixture.Item2.sharedMaterial.GetTexture(property) != fixture.Item4, "Original pixels remain assigned");
            Check(fixture.Item2.sharedMesh.uv.SequenceEqual(beforeUv), "Material adapter changed mesh UV");
            Check(EditorJsonUtility.ToJson(material) == beforeMaterial, "Source material changed");
            int max = PixelDifference(before, after);
            Check(max <= 3 && before.Any(c => c.r != before[0].r), "Shader render changed/blank: " + max);
        }

        private static int PixelDifference(Color32[] before, Color32[] after)
            => before.Zip(after, (a, b) => new[] { Math.Abs(a.r - b.r), Math.Abs(a.g - b.g), Math.Abs(a.b - b.b), Math.Abs(a.a - b.a) }.Max()).Max();

        private static void ProfileMips(TextureFormat format, bool linear)
        {
            bool compressed = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsCompressedFormat(format);
            var source = Track(new Texture2D(32, 16, compressed ? TextureFormat.RGBA32 : format, true, linear));
            for (int mip = 0; mip < source.mipmapCount; mip++)
            {
                int size = Math.Max(1, source.width >> mip) * Math.Max(1, source.height >> mip);
                int width = Math.Max(1, source.width >> mip);
                for (int i = 0; i < size; i++) source.SetPixel(i % width, i / width, new Color32((byte)(i * 17 + mip * 31),
                    (byte)(i * 11 + 37), (byte)(i * 3 + mip * 41), (byte)(100 + i % 155)), mip);
            }
            source.Apply(false, false);
            if (compressed) EditorUtility.CompressTexture(source, format, TextureCompressionQuality.Normal);
            source.Apply(false, true);
            string sourceJson = EditorJsonUtility.ToJson(source);
            var expected = Enumerable.Range(0, source.mipmapCount).Select(mip => ReadProfileMip(source, mip)).ToArray();
            var root = Root(); var processor = new GuardProcessor(root, new GuardSettings(root.GetComponent<AvatarBuildObfuscator>()), x => Track(x));
            var method = typeof(GuardProcessor).GetMethod("RotateProfileTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Texture2D copy;
            try { copy = Track((Texture2D)method.Invoke(processor, new object[] { source })); }
            catch (System.Reflection.TargetInvocationException e) when (format == TextureFormat.BC7 &&
                e.InnerException is InvalidOperationException && e.InnerException.Message.Contains("1/255 quality limit"))
            {
                Check(EditorJsonUtility.ToJson(source) == sourceJson, "Rejected BC7 changed source metadata");
                for (int mip = 0; mip < source.mipmapCount; mip++)
                    Check(PixelDifference(expected[mip], ReadProfileMip(source, mip)) == 0, "Rejected BC7 changed source pixels");
                return;
            }
            Check(!source.isReadable && !copy.isReadable && copy.mipmapCount == source.mipmapCount &&
                copy.format == source.format && copy.graphicsFormat == source.graphicsFormat, "Rotation changed format/mips/readability");
            Check(copy.streamingMipmaps && EditorJsonUtility.ToJson(source) == sourceJson, "Generated streaming or source isolation failed");
            for (int mip = 0; mip < source.mipmapCount; mip++)
            {
                var decoded = ReadProfileMip(copy, mip).Reverse().ToArray();
                Check(PixelDifference(expected[mip], decoded) <= 1, "GPU mip sampling differs: " + mip);
            }
        }

        private static Color32[] ReadProfileMip(Texture2D source, int mip)
        {
            bool srgb = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
            var reader = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Packages/dev.avatar-tools.build-obfuscator/Editor/TextureMip.shader"));
            int width = Math.Max(1, source.width >> mip), height = Math.Max(1, source.height >> mip);
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, srgb ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear);
            var cpu = new Texture2D(width, height, TextureFormat.RGBA32, false, !srgb);
            var previous = RenderTexture.active; bool previousSrgb = GL.sRGBWrite;
            try
            {
                reader.SetFloat("_Mip", mip); GL.sRGBWrite = srgb; Graphics.Blit(source, rt, reader);
                RenderTexture.active = rt; cpu.ReadPixels(new Rect(0, 0, width, height), 0, 0, false); cpu.Apply(false, false);
                return cpu.GetPixels32();
            }
            finally { RenderTexture.active = previous; GL.sRGBWrite = previousSrgb; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(cpu); Object.DestroyImmediate(reader); }
        }

        private static void ProfilePreserve(string reason)
        {
            var f = ProfileFixture("lilToon", "_MainTex");
            if (reason == "excluded") f.Item1.GetComponent<AvatarBuildObfuscator>().excludeUvRenderers = new Renderer[] { f.Item2 };
            if (reason == "property-block") { var block = new MaterialPropertyBlock(); block.SetFloat("_UseParallax", 1); f.Item2.SetPropertyBlock(block); }
            if (reason == "mirror-once") f.Item4.wrapMode = TextureWrapMode.MirrorOnce;
            if (reason == "scroll") f.Item3.SetVector("_MainTex_ScrollRotate", new Vector4(.1f, 0, 0, 0));
            if (reason == "parallax") f.Item3.SetFloat("_UseParallax", 1);
            if (reason == "emission-mask") f.Item3.SetTexture("_EmissionBlendMask", f.Item4);
            if (reason == "sharing-excluded")
            {
                var other = Renderer(f.Item1, "Excluded", Mesh()); other.sharedMaterial = f.Item3;
                f.Item1.GetComponent<AvatarBuildObfuscator>().excludeUvRenderers = new Renderer[] { other };
            }
            if (reason == "animated")
            {
                var clip = Track(new AnimationClip()); AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "material._MainTex_ST.x"), AnimationCurve.Linear(0, 0, 1, 1));
                Controller(f.Item1, clip);
            }
            var scale = f.Item3.GetTextureScale("_MainTex"); var offset = f.Item3.GetTextureOffset("_MainTex");
            var report = Process(f.Item1);
            Check(report.TransformedUvMaterials == 0 && f.Item2.sharedMaterial.GetTextureScale("_MainTex") == scale &&
                f.Item2.sharedMaterial.GetTextureOffset("_MainTex") == offset, "Unsafe material changed: " + reason);
        }

        private static void ProfileUnrelated()
        {
            var f = ProfileFixture("lilToon", "_MainTex");
            var other = Renderer(f.Item1, "Other", Mesh()); other.sharedMaterial = Track(new Material(Shader.Find("Unlit/Color")));
            var particles = new GameObject("Particle"); particles.transform.SetParent(f.Item1.transform); particles.AddComponent<ParticleSystem>();
            var clip = Track(new AnimationClip()); AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("Other", typeof(SkinnedMeshRenderer), "material._Color.r"), AnimationCurve.Linear(0, 0, 1, 1));
            Controller(f.Item1, clip);
            Check(Process(f.Item1).TransformedUvMaterials == 1, "Unrelated user blocked material profile");
        }

        private static void ProfileCopiedShader()
        {
            var f = ProfileFixture("lilToon", "_MainTex"); f.Item1.GetComponent<AvatarBuildObfuscator>().renameAssets = true;
            Check(Process(f.Item1).TransformedUvMaterials == 1 && f.Item2.sharedMaterial.shader != f.Item3.shader, "Copied shader profile lost");
        }

        private static void NonToonSimpleRendering(bool compressed)
        {
            var f = UvFixture(); f.Item1.GetComponent<AvatarBuildObfuscator>().renameAssets = compressed;
            var shader = Shader.Find("NonToon"); Check(shader != null && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "NonToon shader unavailable");
            var material = Track(new Material(shader)); material.SetTexture("_BaseTexture", f.Item4); f.Item2.sharedMaterial = material;
            if (compressed) { EditorUtility.CompressTexture(f.Item4, TextureFormat.DXT5, TextureCompressionQuality.Normal); f.Item4.Apply(false, true); }
            var beforeMaterial = EditorJsonUtility.ToJson(material);
            var uv = f.Item2.sharedMesh.uv; var before = UvPixels(f.Item1);
            material.SetTexture("_BaseTexture", Texture2D.whiteTexture);
            var white = UvPixels(f.Item1); material.SetTexture("_BaseTexture", f.Item4);
            Check(PixelDifference(before, white) > 10, "NonToon fixture did not sample base texture; diff=" + PixelDifference(before, white) +
                " pixels=" + before.Count(c => c.r != 0 || c.g != 0 || c.b != 0) + " shaderError=" + ShaderUtil.ShaderHasError(shader));
            var report = Process(f.Item1); var after = UvPixels(f.Item1);
            Check(report.TransformedUvMeshes == 1 && report.TransformedUvMaterials == 1 && report.TransformedUvTextures == 1,
                "NonToon default material was not converted: " + string.Join("; ", report.Notes));
            Check(f.Item2.sharedMaterial.GetTexture("_BaseTexture") != f.Item4 &&
                uv.Zip(f.Item2.sharedMesh.uv, (a, b) => Vector2.Distance(Vector2.one - a, b)).Max() < 1e-7f,
                "NonToon texture/mesh UV pair was not isolated and rotated");
            Check(EditorJsonUtility.ToJson(material) == beforeMaterial, "NonToon source material changed");
            Check(PixelDifference(before, after) <= 3 && before.Any(c => c.r != before[0].r), "NonToon render changed or blank");
        }

        private static void NonToonPreserved()
        {
            var f = UvFixture(); f.Item1.GetComponent<AvatarBuildObfuscator>().renameAssets = false;
            var shader = Shader.Find("NonToon"); Check(shader != null, "NonToon is required for this preservation test");
            f.Item3.shader = shader; f.Item3.SetTexture("_jp_lilxyzw_nontoon_details_Detail0Texture", f.Item4);
            var uv = f.Item2.sharedMesh.uv;
            var report = Process(f.Item1);
            Check(report.TransformedUvMaterials == 0 && report.TransformedUvMeshes == 0 && f.Item2.sharedMesh.uv.SequenceEqual(uv),
                "Unvalidated NonToon adapter changed sampling");
            Check(report.Notes.Any(n => n.Contains("NonToon") && n.Contains("其他契约保留")), "Missing explicit NonToon preservation reason");
        }

        private static void PoiyomiPreserved(string property)
        {
            var f = ProfileFixture(".poiyomi/Poiyomi Toon", "_MainTex"); f.Item3.SetFloat(property, 1);
            var scale = f.Item3.GetTextureScale("_MainTex"); var offset = f.Item3.GetTextureOffset("_MainTex");
            var report = Process(f.Item1);
            Check(report.TransformedUvMaterials == 0 && f.Item2.sharedMaterial.GetTextureScale("_MainTex") == scale &&
                f.Item2.sharedMaterial.GetTextureOffset("_MainTex") == offset, "Poiyomi unsafe coordinates changed");
        }

        private static void ProfileAliases()
        {
            var f = ProfileFixture("lilToon", "_MainTex"); f.Item3.SetTexture("_BaseMap", Texture2D.blackTexture);
            var before = UvPixels(f.Item1); var report = Process(f.Item1); var after = UvPixels(f.Item1);
            Check(report.TransformedUvMaterials == 1 && PixelDifference(before, after) <= 3, "Alias normalization changed BRP rendering");
            Check(f.Item2.sharedMaterial.GetTexture("_BaseMap") == f.Item2.sharedMaterial.GetTexture("_MainTex") &&
                f.Item3.GetTexture("_BaseMap") == Texture2D.blackTexture, "Main alias/source isolation lost");
        }

        private static void ProfileColorAnimation()
        {
            var f = ProfileFixture("lilToon", "_MainTex"); var clip = Track(new AnimationClip());
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "material._Color.r"),
                AnimationCurve.Linear(0, 0, 1, 1)); Controller(f.Item1, clip);
            Check(Process(f.Item1).TransformedUvMaterials == 1, "Color-only curves incorrectly block coordinate compensation");
        }

        private static void ProfileMaterialSwap()
        {
            var f = ProfileFixture("lilToon", "_MainTex"); var clip = Track(new AnimationClip());
            var other = Track(new Material(Shader.Find("Unlit/Texture"))); other.mainTexture = f.Item4;
            var binding = EditorCurveBinding.PPtrCurve("Body", typeof(SkinnedMeshRenderer), "m_Materials.Array.data[0]");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, new[] {
                new ObjectReferenceKeyframe { time = 0, value = f.Item3 }, new ObjectReferenceKeyframe { time = 1, value = other } });
            Controller(f.Item1, clip);
            var first = UvPixels(f.Item1); f.Item2.sharedMaterial = other; var second = UvPixels(f.Item1); f.Item2.sharedMaterial = f.Item3;
            var report = Process(f.Item1); Check(report.TransformedUvMaterials == 1, "Material swap blocked profile");
            var copy = f.Item1.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
            var curve = AnimationUtility.GetObjectReferenceCurve(copy, AnimationUtility.GetObjectReferenceCurveBindings(copy).Single());
            f.Item2.sharedMaterial = (Material)curve[0].value; Check(PixelDifference(first, UvPixels(f.Item1)) <= 3, "First swap material changed sampling");
            f.Item2.sharedMaterial = (Material)curve[1].value; Check(PixelDifference(second, UvPixels(f.Item1)) <= 3, "Second swap material changed sampling");
            Check(clip != copy && other.mainTexture == f.Item4 && f.Item3.GetTexture("_MainTex") == f.Item4, "Swap source changed");
        }

        private static void ProfileAbsentUniforms()
        {
            var f = ProfileFixture("lilToon", "_MainTex"); var clip = Track(new AnimationClip());
            foreach (string property in new[] { "_CustomGravitySize", "_DPSOrificeChannel", "_Envelope", "_UseMain2ndTex" })
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "material." + property),
                    AnimationCurve.Linear(0, 0, 1, 1));
            Controller(f.Item1, clip); var before = UvPixels(f.Item1);
            Check(Process(f.Item1).TransformedUvMaterials == 1 && PixelDifference(before, UvPixels(f.Item1)) <= 3, "Unrelated stock uniforms blocked/changed rendering");
        }

        private static void ProfileBlocks(string scenario)
        {
            var f = ProfileFixture("lilToon", "_MainTex"); var block = new MaterialPropertyBlock();
            if (scenario == "color") block.SetColor("_Color", new Color(.6f, .8f, .7f, 1));
            if (scenario == "unrelated") block.SetFloat("_ABOEditorBookkeepingFixture", 123);
            if (scenario == "scalar") { block.SetFloat("_UseBacklight", 1); block.SetFloat("_AsUnlit", .35f); }
            if (scenario == "indexed-uv") { block.SetVector("_MainTex_ST", new Vector4(1, 1, .2f, .4f)); f.Item2.SetPropertyBlock(block, 0); }
            else f.Item2.SetPropertyBlock(block);
            var before = UvPixels(f.Item1); var report = Process(f.Item1); var after = UvPixels(f.Item1);
            Check(report.TransformedUvMaterials == (scenario == "indexed-uv" ? 0 : 1) && PixelDifference(before, after) <= 3,
                "Property-block contract was not respected: " + scenario);
            var read = new MaterialPropertyBlock();
            if (scenario == "indexed-uv") f.Item2.GetPropertyBlock(read, 0); else f.Item2.GetPropertyBlock(read);
            Check(scenario == "scalar" ? read.GetFloat("_UseBacklight") == 1 && read.GetFloat("_AsUnlit") == .35f :
                scenario == "color" ? read.GetColor("_Color") == new Color(.6f, .8f, .7f, 1) : scenario == "unrelated" ?
                read.GetFloat("_ABOEditorBookkeepingFixture") == 123 : read.GetVector("_MainTex_ST") == new Vector4(1, 1, .2f, .4f), "Original property block changed");
        }

        private static void ProfileLightAnimation()
        {
            foreach (float light in new[] { 0f, .3f, .8f })
            {
                var f = ProfileFixture("lilToon", "_MainTex"); var clip = Track(new AnimationClip());
                f.Item3.SetFloat("_LightMinLimit", light); f.Item3.SetFloat("_LightMaxLimit", 1.2f); f.Item3.SetFloat("_AsUnlit", light);
                foreach (string property in new[] { "_LightMinLimit", "_LightMaxLimit", "_AsUnlit" })
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "material." + property),
                        AnimationCurve.Linear(0, 0, 1, 1));
                Controller(f.Item1, clip); var before = UvPixels(f.Item1);
                Check(Process(f.Item1).TransformedUvMaterials == 1 && PixelDifference(before, UvPixels(f.Item1)) <= 3, "Light clamp changed texture sampling");
            }
        }

        private static void ProfileShadowLut()
        {
            var f = ProfileFixture("lilToon", "_MainTex");
            var lut = Track(new Texture2D(256, 16, TextureFormat.RGBA32, false, false));
            lut.SetPixels32(Enumerable.Range(0, 4096).Select(i => new Color32((byte)(i % 256), (byte)(i / 16 % 256), (byte)(255 - i % 256), 255)).ToArray());
            lut.Apply(); f.Item3.SetTexture("_ShadowColorTex", lut); f.Item3.SetFloat("_ShadowColorType", 1);
            var before = UvPixels(f.Item1); var report = Process(f.Item1); var after = UvPixels(f.Item1);
            var retained = (Texture2D)f.Item2.sharedMaterial.GetTexture("_ShadowColorTex");
            Check(report.TransformedUvMaterials == 1 && PixelDifference(before, after) <= 3 &&
                retained.GetRawTextureData<byte>().SequenceEqual(lut.GetRawTextureData<byte>()), "Shadow LUT was rotated or rendering changed");
        }

        private static void ProfileScalarAnimation()
        {
            foreach (float weight in new[] { 0f, .4f, 1f })
            {
                var f = ProfileFixture("lilToon", "_MainTex"); var clip = Track(new AnimationClip());
                foreach (string property in new[] { "_UseBacklight", "_UseShadow", "_UseReflection", "_UseRim", "_UseBumpMap", "_UseMatCap" })
                {
                    f.Item3.SetFloat(property, weight);
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "material." + property),
                        AnimationCurve.Linear(0, 0, 1, 1));
                }
                Controller(f.Item1, clip); var before = UvPixels(f.Item1);
                Check(Process(f.Item1).TransformedUvMaterials == 1 && PixelDifference(before, UvPixels(f.Item1)) <= 3,
                    "Scalar feature sampling changed at " + weight);
            }
        }

        private static void ProfileUnsafeScalar(string property)
        {
            var f = ProfileFixture("lilToon", "_MainTex"); var clip = Track(new AnimationClip());
            f.Item3.SetFloat(property, 0);
            if (property == "_UseEmission") f.Item3.SetTexture("_EmissionBlendMask", f.Item4);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "material." + property),
                AnimationCurve.Linear(0, 0, 1, 1)); Controller(f.Item1, clip);
            Check(Process(f.Item1).TransformedUvMaterials == 0, "Animated coordinate/compile branch was transformed: " + property);
        }

        private static void ProfileStreaming(bool ownedCopy)
        {
            Directory.CreateDirectory(Folder + "/Fixtures");
            string path = Folder + "/Fixtures/Streaming_" + Guid.NewGuid().ToString("N") + ".png";
            var f = ProfileFixture("lilToon", "_MainTex");
            File.WriteAllBytes(path, f.Item4.EncodeToPNG()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = false; importer.mipmapEnabled = true; importer.streamingMipmaps = true;
            importer.textureCompression = TextureImporterCompression.Compressed; importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Check(source.streamingMipmaps && !source.isReadable && source.loadedMipmapLevel == 0, "Streaming fixture not fully resident");
            var sourceBytes = File.ReadAllBytes(path); var sourceMeta = File.ReadAllBytes(path + ".meta");
            int request = source.requestedMipmapLevel;
            var expected = Enumerable.Range(0, source.mipmapCount).Select(m => ReadProfileMip(source, m)).ToArray();
            f.Item3.SetTexture("_MainTex", source);
            f.Item1.GetComponent<AvatarBuildObfuscator>().renameAssets = ownedCopy;
            var processor = new GuardProcessor(f.Item1, new GuardSettings(f.Item1.GetComponent<AvatarBuildObfuscator>()), x => Track(x));
            string folder = Folder + "/Fixtures/StreamingCopy_" + Guid.NewGuid().ToString("N");
            Texture2D imported = null; int importedRequest = -1;
            if (ownedCopy)
            {
                Directory.CreateDirectory(folder); AssetDatabase.Refresh(); processor.PrepareImportedAssets(folder);
                imported = Directory.GetFiles(folder, "*.png").Select(p => AssetDatabase.LoadAssetAtPath<Texture2D>(p.Replace('\\', '/')))
                    .Single(t => t.streamingMipmaps);
                importedRequest = imported.requestedMipmapLevel;
            }
            var report = processor.Run(); var output = (Texture2D)f.Item2.sharedMaterial.GetTexture("_MainTex");
            Check(report.TransformedUvMaterials == (ownedCopy ? 1 : 0), "Streaming ownership policy failed: " + string.Join("; ", report.Notes));
            if (ownedCopy)
            {
                    Check(output != source && output.mipmapCount == source.mipmapCount && output.streamingMipmaps && output.format == source.format &&
                    imported.requestedMipmapLevel == importedRequest, "Build mip chain/request was not preserved");
                for (int mip = 0; mip < source.mipmapCount; mip++)
                    Check(PixelDifference(expected[mip], ReadProfileMip(output, mip).Reverse().ToArray()) <= 1, "Streaming decode changed mip " + mip);
            }
            Check(source.requestedMipmapLevel == request && source.streamingMipmaps && !source.isReadable &&
                sourceBytes.SequenceEqual(File.ReadAllBytes(path)) && sourceMeta.SequenceEqual(File.ReadAllBytes(path + ".meta")),
                "Source streaming request, pixels or importer changed");
        }

        private static void RotationStreaming(bool profile, bool mips)
        {
            var f = profile ? ProfileFixture("lilToon", "_MainTex") : UvFixture();
            var texture = Track(new Texture2D(8, 8, TextureFormat.RGBA32, mips, false));
            texture.SetPixels32(f.Item4.GetPixels32()); texture.Apply();
            using (var so = new SerializedObject(texture))
            {
                so.FindProperty("m_StreamingMipmaps").boolValue = false;
                so.FindProperty("m_StreamingMipmapsPriority").intValue = 5;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            f.Item3.mainTexture = texture;
            string before = EditorJsonUtility.ToJson(texture);
            var report = Process(f.Item1); var output = f.Item2.sharedMaterial.mainTexture as Texture2D;
            Check(output != null && output != texture && (profile ? report.TransformedUvMaterials : report.TransformedUvMeshes) > 0,
                "Expected rotation did not run");
            using (var so = new SerializedObject(output))
                Check(output.streamingMipmaps == mips && so.FindProperty("m_StreamingMipmapsPriority").intValue == 5,
                    "Generated streaming flag or priority was lost");
            Check(EditorJsonUtility.ToJson(texture) == before, "Source streaming settings changed");
        }

        private static void ProfileCompressedLayout()
        {
            var f = ProfileFixture("lilToon", "_MainTex");
            // Unity's compressor retains RGBA32 for unsupported NPOT inputs. Build
            // actual BC3 blocks so the 6x4 second mip exercises palette crossing.
            var texture = Track(new Texture2D(12, 8, TextureFormat.DXT5, true, false));
            for (int mip = 0; mip < texture.mipmapCount; mip++)
            {
                int width = Math.Max(1, texture.width >> mip), height = Math.Max(1, texture.height >> mip);
                texture.SetPixelData(new byte[((width + 3) / 4) * ((height + 3) / 4) * 16], mip);
            }
            texture.Apply(false, true);
            Check(texture.format == TextureFormat.DXT5 && texture.mipmapCount > 1, "Compressed rejection fixture was not compressed");
            f.Item3.mainTexture = texture; string before = EditorJsonUtility.ToJson(f.Item3);
            var scale = f.Item3.mainTextureScale; var offset = f.Item3.mainTextureOffset;
            Check(Process(f.Item1).TransformedUvMaterials == 0, "Incompatible block palettes were rotated");
            Check(f.Item2.sharedMaterial.mainTexture == texture && f.Item2.sharedMaterial.mainTextureScale == scale &&
                f.Item2.sharedMaterial.mainTextureOffset == offset && EditorJsonUtility.ToJson(f.Item3) == before,
                "Retained material texture or sampling changed");
        }

        private static void ProfileStorageGuard()
        {
            var source = Track(new Texture2D(8, 8, TextureFormat.RGBA32, true));
            source.Apply(); EditorUtility.CompressTexture(source, TextureFormat.DXT5, TextureCompressionQuality.Normal);
            var expanded = Track(new Texture2D(8, 8, TextureFormat.RGBA32, true));
            var cropped = Track(new Texture2D(8, 4, TextureFormat.DXT5, source.mipmapCount, false));
            var root = Root(); var processor = new GuardProcessor(root, new GuardSettings(root.GetComponent<AvatarBuildObfuscator>()), x => Track(x));
            var method = typeof(GuardProcessor).GetMethod("ConfigureRotatedTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var output in new[] { expanded, cropped })
            {
                bool rejected = false;
                try { method.Invoke(processor, new object[] { source, output }); }
                catch (System.Reflection.TargetInvocationException e) when (e.InnerException is InvalidOperationException) { rejected = true; }
                Check(rejected, "Format expansion or changed dimensions were accepted");
            }
        }
    }
}
