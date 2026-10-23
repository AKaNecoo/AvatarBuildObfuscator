// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        private enum UvCompensation { None, Primary, Nested }
        private sealed class UvSlot
        {
            public string Property;
            public Texture2D Texture;
            public UvCompensation Compensation;
            public Vector2 Scale, Offset;
        }

        // Each adapter is a sampling contract for a reviewed upstream package version.
        // Mesh UVs/tangents and unrelated projection maps are left untouched by this route.
        private void TransformProfileTextures(Renderer[] renderers)
        {
            if (GraphicsSettings.currentRenderPipeline != null || unknownDriver && !settings.HasSubtreeOverrides && settings.Mode == ProtectionMode.Conservative)
            { report.Note("实验 UV：Shader适配仅支持内置渲染管线；保守模式保留未知驱动"); return; }
            const long budget = 256L * 1024 * 1024;
            long bytes = 0;
            var rotated = new Dictionary<Texture2D, Texture2D>();
            var meshes = new HashSet<Mesh>();
            foreach (var material in renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct())
            {
                if (!owned.Contains(material)) continue;
                var users = renderers.Where(r => r.sharedMaterials.Contains(material)).ToArray();
                if (unknownDriver && users.Any(r => settings.ModeFor(r) == ProtectionMode.Conservative))
                { report.Note("实验 UV：共享材质包含保守子树且存在未知驱动，保留"); continue; }
                string family, reason;
                List<UvSlot> plan;
                try { plan = ProfilePlan(material, out family, out reason); }
                catch (InvalidOperationException)
                { report.Note("实验 UV：不兼容的Shader纹理类型，保留材质"); continue; }
                if (family == null) continue;
                if (plan == null)
                { report.Note("实验 UV：" + family + "保留：" + reason); continue; }
                if (users.Any(r => settings.ExcludeUv.Contains(r) || r is SkinnedMeshRenderer skin && settings.Exclusions.Contains(skin) || ProfilePropertyBlock(r, material) ||
                    r is ParticleSystemRenderer || r.GetComponent<Cloth>() != null || MaterialAnimated(r, material, family)))
                { report.Note("实验 UV：" + family + "材质存在排除、属性块、Cloth、粒子或材质动画，保留所有共享使用者"); continue; }
                var textures = plan.Where(s => s.Texture != null).Select(s => s.Texture).Distinct().ToArray();
                if (textures.Length == 0) continue;
                if (textures.Any(t => !ProfileTextureSupported(t)) || plan.Any(s => !Finite(new Vector3(s.Scale.x, s.Scale.y, 0)) ||
                    !Finite(new Vector3(s.Offset.x, s.Offset.y, 0))))
                { report.Note("实验 UV：" + family + "包含未支持格式、非自有或未完整加载的流送贴图、MirrorOnce或非有限坐标，保留"); continue; }
                long additional = textures.Where(t => !rotated.ContainsKey(t)).Sum(TexturePayloadBytes);
                if (additional > budget - bytes)
                { report.Note("实验 UV：构建新增纹理数据达到256 MiB上限，保留后续材质"); continue; }
                var pending = new Dictionary<Texture2D, Texture2D>();
                try
                {
                    foreach (var texture in textures)
                        if (!rotated.ContainsKey(texture)) pending[texture] = RotateProfileTexture(texture);
                }
                catch (Exception error)
                {
                    foreach (var copy in pending.Values) Object.DestroyImmediate(copy);
                    string failureReason = error.Message.StartsWith("BC7 recompression exceeds", StringComparison.Ordinal)
                        ? "BC7逐Mip画质阈值1/255未通过" : error.GetType().Name;
                    report.Note("实验 UV：" + family + "逐Mip旋转、格式或画质检查失败，完整保留该材质：" + failureReason); continue;
                }
                foreach (var pair in pending)
                {
                    Own(pair.Value); rotated[pair.Key] = pair.Value;
                    report.TransformedUvTextures++;
                    if (TryBlockFormat(pair.Key.format, out var block)) report.LosslessCompressedUvTextures++;
                    else if (!pair.Key.isReadable || BytesPerPixel(pair.Key.format) == 0) report.DecodedUvTextures++;
                    if (pair.Key.format == TextureFormat.BC7) report.RecompressedUvTextures++;
                }
                bytes += additional;
                foreach (var slot in plan)
                {
                    if (slot.Texture != null) material.SetTexture(slot.Property, rotated[slot.Texture]);
                    if (slot.Compensation != UvCompensation.None)
                    {
                        material.SetTextureScale(slot.Property, slot.Scale);
                        material.SetTextureOffset(slot.Property, slot.Offset);
                    }
                }
                report.TransformedUvMaterials++;
                foreach (var renderer in users) { var mesh = RenderMesh(renderer); if (mesh != null) meshes.Add(mesh); }
                report.Note("实验 UV：已转换" + family + "材质采样及关联贴图；未修改网格UV、未添加运行时解密");
            }
            report.TransformedUvMeshes += meshes.Count;
            report.UvTextureBytes += bytes;
        }

        private List<UvSlot> ProfilePlan(Material material, out string family, out string reason)
        {
            family = null; reason = null;
            var shader = material.shader;
            foreach (var pair in sharedAssets)
                if (pair.Value == shader && pair.Key is Shader original) { shader = original; break; }
            if (shader == null) return null;
            string path = AssetDatabase.GetAssetPath(shader);
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
            if (package == null) return null;
            var plan = new List<UvSlot>();
            if (package.name == "jp.lilxyzw.liltoon")
            {
                family = "lilToon";
                string name = shader.name;
                var supported = new[] { "lilToon", "Hidden/lilToonCutout", "Hidden/lilToonTransparent", "Hidden/lilToonOutline",
                    "Hidden/lilToonCutoutOutline", "Hidden/lilToonTransparentOutline" };
                if (package.version != "2.4.1" || !supported.Contains(name))
                { reason = "未验证版本或变体（含Lite/Fur/Gem/自定义分支）"; return null; }
                if (VectorActive(material, "_MainTex_ScrollRotate") || VectorActive(material, "_OutlineTex_ScrollRotate") ||
                    Active(material, "_UseParallax", "_UsePOM", "_UseAudioLink", "_UseGlitter") ||
                    VectorXActive(material, "_DissolveParams", "_Main2ndDissolveParams", "_Main3rdDissolveParams") ||
                    VectorActive(material, "_EmissionBlendMask_ScrollRotate") || VectorActive(material, "_Emission2ndBlendMask_ScrollRotate"))
                { reason = "滚动/旋转、视差、AudioLink、Glitter或溶解契约"; return null; }
                if (Active(material, "_UseEmission") && material.GetTexture("_EmissionBlendMask") != null ||
                    Active(material, "_UseEmission2nd") && material.GetTexture("_Emission2ndBlendMask") != null)
                { reason = "发光遮罩坐标受lilToon全局编译优化切换影响"; return null; }
                AddSlot(plan, material, "_MainTex", UvCompensation.Primary);
                AddSlot(plan, material, "_OutlineTex", UvCompensation.Primary);
                foreach (string property in new[] { "_MainColorAdjustMask", "_Main2ndBlendMask", "_Main3rdBlendMask", "_ShadowStrengthMask",
                    "_ShadowBlurMask", "_ShadowBorderMask", "_ShadowColorTex", "_Shadow2ndColorTex", "_Shadow3rdColorTex", "_RimShadeMask" })
                    if (!(material.GetFloat("_ShadowColorType") == 1 && property.EndsWith("ColorTex", StringComparison.Ordinal)))
                        AddSlot(plan, material, property, UvCompensation.None);
                foreach (string property in new[] { "_AlphaMask", "_BumpMap", "_Bump2ndScaleMask", "_AnisotropyTangentMap",
                    "_AnisotropyScaleMask", "_AnisotropyShiftNoiseMask", "_BacklightColorTex", "_SmoothnessTex", "_MetallicGlossMap",
                    "_ReflectionColorTex", "_MatCapBumpMap", "_MatCapBlendMask", "_MatCap2ndBumpMap", "_MatCap2ndBlendMask",
                    "_RimColorTex" })
                    AddSlot(plan, material, property, UvCompensation.Nested);
                // These BRP-inactive compatibility aliases are not sampled by the reviewed includes.
                // Upstream optimizers can replace MainTex without updating them. Normalize only the
                // build copy to avoid retaining an untouched copy of the original pixels.
                var main = plan.Single(s => s.Property == "_MainTex");
                foreach (string alias in new[] { "_BaseMap", "_BaseColorMap" })
                {
                    if (material.HasProperty(alias)) plan.Add(new UvSlot { Property = alias, Texture = main.Texture,
                        Compensation = UvCompensation.Primary, Scale = main.Scale, Offset = main.Offset });
                }
            }
            else if (package.name == "com.poiyomi.toon")
            {
                family = "Poiyomi";
                if (package.version != "10.0.27" || !path.Replace('\\', '/').Contains("/Shaders/10.0/Toon/Poiyomi Toon.shader") ||
                    Active(material, "_ShaderOptimizerEnabled"))
                { reason = "未验证版本、非标准或已锁定Shader"; return null; }
                if (VectorActive(material, "_MainTexPan") || Active(material, "_MainTexStochastic", "_MainPixelMode", "_VideoEffectsEnable", "_ScreenSpaceMesh"))
                { reason = "主贴图滚动、随机采样、像素化、视频或屏幕空间契约"; return null; }
                AddSlot(plan, material, "_MainTex", UvCompensation.Primary);
            }
            else if (package.name == "jp.lilxyzw.nontoon")
            {
                family = "NonToon";
                reason = "基础贴图直接使用原始UV0；ShaderCore生成变体的渲染验收尚未完成";
                return null;
            }
            else if (package.name == "jp.whiteflare.unlitwf")
            {
                family = "UnlitWF（二层/背面贴图）";
                if (package.version != "2.14.0" || shader.name != "UnlitWF/WF_UnToon_Opaque")
                { reason = "未验证版本或变体"; return null; }
                // Main ST also drives procedural effects and vertex outline width: retain it.
                AddSlot(plan, material, "_TX2_MainTex", UvCompensation.Primary);
                AddSlot(plan, material, "_BKT_BackTex", UvCompensation.Primary);
            }
            return family == null ? null : plan;
        }

        private static void AddSlot(List<UvSlot> plan, Material material, string property, UvCompensation compensation)
        {
            if (!material.HasProperty(property)) return;
            var texture = material.GetTexture(property);
            // An incompatible non-2D texture makes the plan fail the support check as a whole.
            if (texture != null && !(texture is Texture2D)) throw new InvalidOperationException("Unexpected non-2D profile texture");
            var scale = material.GetTextureScale(property); var offset = material.GetTextureOffset(property);
            plan.Add(new UvSlot { Property = property, Texture = texture as Texture2D, Compensation = compensation,
                Scale = compensation == UvCompensation.Primary ? -scale : scale,
                Offset = compensation == UvCompensation.Primary ? Vector2.one - offset :
                    compensation == UvCompensation.Nested ? Vector2.one - scale - offset : offset });
        }

        private static bool Active(Material material, params string[] properties)
            => properties.Any(p => material.HasProperty(p) && material.GetFloat(p) != 0);
        private static bool VectorActive(Material material, string property)
            => material.HasProperty(property) && material.GetVector(property) != Vector4.zero;
        private static bool VectorXActive(Material material, params string[] properties)
            => properties.Any(p => material.HasProperty(p) && material.GetVector(p).x != 0);

        private bool ProfilePropertyBlock(Renderer renderer, Material material)
        {
            if (!renderer.HasPropertyBlock()) return false;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            if (ProfileBlockChangesSampling(block, material)) return true;
            var materials = renderer.sharedMaterials;
            for (int slot = 0; slot < materials.Length; slot++)
            {
                if (materials[slot] != material) continue;
                block.Clear(); renderer.GetPropertyBlock(block, slot);
                if (ProfileBlockChangesSampling(block, material)) return true;
            }
            return false;
        }

        private bool ProfileBlockChangesSampling(MaterialPropertyBlock block, Material material)
        {
            if (block.isEmpty) return false;
            var shader = material.shader;
            string family, reason;
            ProfilePlan(material, out family, out reason);
            for (int index = 0; index < shader.GetPropertyCount(); index++)
            {
                string property = shader.GetPropertyName(index);
                var type = shader.GetPropertyType(index);
                if (family == "lilToon" && LilScalarSafe(material, property, type)) continue;
                if (type != ShaderPropertyType.Color && block.HasProperty(property) || type == ShaderPropertyType.Texture &&
                    (block.HasProperty(property + "_ST") || block.HasProperty(property + "_TexelSize")))
                { report.Note("实验 UV：保留材质属性块覆盖 " + property); return true; }
            }
            // Only reviewed stock profiles reach this method. Color-only values and unrelated
            // editor bookkeeping do not change their sampling. Never write the property block.
            return false;
        }

        private static bool LilScalarSafe(Material material, string property, ShaderPropertyType type)
        {
            if (type != ShaderPropertyType.Float && type != ShaderPropertyType.Range && type != ShaderPropertyType.Int) return false;
            // These controls can invalidate the reviewed texture-coordinate/compile-branch
            // contract. Other scalar lighting/blending/feature controls use compensated samples.
            if (new[] { "_UseParallax", "_UsePOM", "_UseAudioLink", "_UseGlitter", "_ShadowColorType" }.Contains(property)) return false;
            if (property == "_UseEmission" && material.GetTexture("_EmissionBlendMask") != null ||
                property == "_UseEmission2nd" && material.GetTexture("_Emission2ndBlendMask") != null) return false;
            return true;
        }

        private bool MaterialAnimated(Renderer renderer, Material material, string family)
        {
            foreach (var site in clips)
            {
                var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(site.Clip);
                var bindings = AnimationUtility.GetCurveBindings(site.Clip).Concat(objectBindings);
                foreach (var binding in bindings)
                {
                    if (!binding.propertyName.StartsWith("material.", StringComparison.Ordinal) &&
                        !binding.propertyName.StartsWith("m_Materials", StringComparison.Ordinal)) continue;
                    // Swapping an entire material is safe for this material-only route: each
                    // material keeps a complete sampling contract, and raw mesh UV is unchanged.
                    if (objectBindings.Contains(binding) && binding.propertyName.StartsWith("m_Materials.Array.data[", StringComparison.Ordinal) &&
                        binding.propertyName.EndsWith("]", StringComparison.Ordinal) &&
                        AnimationUtility.GetObjectReferenceCurve(site.Clip, binding).All(k => k.value == null || k.value is Material)) continue;
                    if (binding.propertyName.StartsWith("material.", StringComparison.Ordinal))
                    {
                        string property = binding.propertyName.Substring(9).Split('.')[0];
                        int index = material.shader.FindPropertyIndex(property);
                        if (index >= 0 && material.shader.GetPropertyType(index) == ShaderPropertyType.Color) continue;
                        if (family == "lilToon")
                        {
                            if (index >= 0 && LilScalarSafe(material, property, material.shader.GetPropertyType(index))) continue;
                            // Reviewed stock lilToon 2.4.1 sources contain none of these external
                            // deformation uniforms. Do not generalize this to arbitrary uniforms:
                            // real sampling inputs such as MainTex_ST are not ShaderLab properties.
                            if (index < 0 && new[] { "_CustomGravitySize", "_DPSOrificeChannel", "_Envelope", "_LeftHand", "_RightHand",
                                "_LeftFoot", "_RightFoot", "_HeadEnabled", "_LimbVisibility", "_Strength", "_HeadContact" }.Contains(property)) continue;
                        }
                    }
                    string path = MapPath(binding.path, site.Root);
                    var target = path.Length == 0 ? site.Root : site.Root.Find(path);
                    if (target == renderer.transform)
                    { report.Note("实验 UV：保留材质动画属性 " + binding.propertyName); return true; }
                }
            }
            return false;
        }
    }
}
