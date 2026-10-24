// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        private readonly Dictionary<Shader, bool> reviewedNonToonSources = new Dictionary<Shader, bool>();

        private void TransformUvTextures()
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            TransformProfileTextures(renderers);
            // This deliberately small support matrix needs no shader decoder and no lossy re-encoding.
            if (unknownDriver || root.GetComponentsInChildren<ParticleSystemRenderer>(true).Length != 0 || MaterialUvAnimated())
            { report.Note("实验 UV：内置 Unlit 网格路线因未知驱动、粒子或材质动画保留；Shader适配结果另列"); return; }
            var rotated = new Dictionary<Texture2D, Texture2D>();
            foreach (var group in renderers.Where(r => RenderMesh(r) != null).GroupBy(RenderMesh))
            {
                var mesh = group.Key;
                var materials = group.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
                bool simpleNonToon = materials.Length == 1 && group.All(r => r.sharedMaterials.Length == 1) &&
                    mesh.subMeshCount == 1 && SimpleNonToonMaterial(materials[0]);
                if (!owned.Contains(mesh) || !mesh.isReadable || !mesh.HasVertexAttribute(VertexAttribute.TexCoord0) || mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0) != 2 ||
                    mesh.GetVertexAttributeFormat(VertexAttribute.TexCoord0) != VertexAttributeFormat.Float32 ||
                    group.Any(r => settings.ExcludeUv.Contains(r) || r.HasPropertyBlock() || r.GetComponent<Cloth>() != null || r is MeshRenderer mr && mr.additionalVertexStreams != null) ||
                    materials.Length == 0 || !simpleNonToon && materials.Any(m => !UvMaterial(m)) ||
                    renderers.Any(r => RenderMesh(r) != mesh && r.sharedMaterials.Any(materials.Contains)))
                { report.Note("实验 UV：仅支持独立可读 UV0 网格、独立材质、内置 Unlit 或默认单贴图 NonToon；手动排除或其他契约保留"); continue; }
                var uv = mesh.uv;
                if (uv.Length != mesh.vertexCount || uv.Any(v => !Finite(new Vector3(v.x, v.y, 0)))) continue;
                var replacementUv = uv.Select(v => Vector2.one - v).ToArray();
                if (replacementUv.Any(v => !Finite(new Vector3(v.x, v.y, 0)))) continue;
                if (simpleNonToon && materials.Select(m => (Texture2D)m.mainTexture).Where(t => !rotated.ContainsKey(t))
                    .Distinct().Sum(TexturePayloadBytes) > 256L * 1024 * 1024 - report.UvTextureBytes)
                { report.Note("实验 UV：NonToon新增纹理数据达到256 MiB上限，保留网格"); continue; }
                var pending = new Dictionary<Texture2D, Texture2D>();
                try
                {
                    foreach (var material in materials)
                    {
                        var texture = (Texture2D)material.mainTexture;
                        if (!rotated.ContainsKey(texture) && !pending.ContainsKey(texture))
                            pending[texture] = simpleNonToon ? RotateProfileTexture(texture) : RotateTexture(texture);
                    }
                }
                catch (Exception e)
                {
                    foreach (var texture in pending.Values) Object.DestroyImmediate(texture);
                    report.Note("实验 UV：无法无损转换贴图，保留该网格：" + e.GetType().Name); continue;
                }
                foreach (var pair in pending)
                { Own(pair.Value); rotated[pair.Key] = pair.Value; }
                mesh.uv = replacementUv;
                foreach (var material in materials)
                {
                    var texture = (Texture2D)material.mainTexture;
                    var scale = material.mainTextureScale; var offset = material.mainTextureOffset;
                    material.mainTexture = rotated[texture];
                    if (!simpleNonToon) material.mainTextureOffset = Vector2.one - scale - offset;
                }
                report.TransformedUvMeshes++;
                if (simpleNonToon)
                {
                    report.TransformedUvMaterials++;
                    report.TransformedUvTextures += pending.Count;
                    report.UvTextureBytes += pending.Keys.Sum(TexturePayloadBytes);
                    foreach (var original in pending.Keys)
                    {
                        if (TryBlockFormat(original.format, out var block)) report.LosslessCompressedUvTextures++;
                        if (original.format == TextureFormat.BC7) report.RecompressedUvTextures++;
                    }
                    report.Note("实验 UV：默认单贴图 NonToon 网格 UV0 与基础贴图已同步旋转");
                }
            }
        }

        private bool SimpleNonToonMaterial(Material material)
        {
            if (material == null || !owned.Contains(material) || material.shader == null) return false;
            var shader = material.shader;
            foreach (var pair in sharedAssets)
                if (pair.Value == shader && pair.Key is Shader original) { shader = original; break; }
            if (shader.name != "NonToon" ||
                AssetDatabase.GetAssetPath(shader).Replace('\\', '/') != "Packages/jp.lilxyzw.nontoon/Shaders/NonToon.scshader" ||
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(AssetDatabase.GetAssetPath(shader))?.version != "0.3.0" ||
                !ReviewedNonToonSource(shader)) return false;
            var texture = material.GetTexture("_BaseTexture") as Texture2D;
            if (texture == null || material.mainTexture != texture || !ProfileTextureSupported(texture) ||
                material.GetTextureScale("_BaseTexture") != Vector2.one || material.GetTextureOffset("_BaseTexture") != Vector2.zero) return false;
            var defaults = new Material(shader);
            try
            {
                if (!material.shaderKeywords.SequenceEqual(defaults.shaderKeywords) || material.renderQueue != defaults.renderQueue) return false;
                for (int i = 0; i < shader.GetPropertyCount(); i++)
                {
                    string name = shader.GetPropertyName(i);
                    if (name == "_BaseTexture") continue;
                    switch (shader.GetPropertyType(i))
                    {
                        case ShaderPropertyType.Texture:
                            if (material.GetTexture(name) != defaults.GetTexture(name)) return false;
                            break;
                        case ShaderPropertyType.Color:
                            if (material.GetColor(name) != defaults.GetColor(name)) return false;
                            break;
                        case ShaderPropertyType.Vector:
                            if (material.GetVector(name) != defaults.GetVector(name)) return false;
                            break;
                        case ShaderPropertyType.Int:
                            if (material.GetInteger(name) != defaults.GetInteger(name)) return false;
                            break;
                        default:
                            if (material.GetFloat(name) != defaults.GetFloat(name)) return false;
                            break;
                    }
                }
                return true;
            }
            finally { Object.DestroyImmediate(defaults); }
        }

        private bool ReviewedNonToonSource(Shader shader)
        {
            if (reviewedNonToonSources.TryGetValue(shader, out var approved)) return approved;
            var source = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(shader))
                .OfType<TextAsset>().SingleOrDefault(a => a.name == "Shader Source");
            if (source == null) return reviewedNonToonSources[shader] = false;
            using (var sha = SHA256.Create())
            {
                var digest = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(source.text))).Replace("-", "").ToLowerInvariant();
                return reviewedNonToonSources[shader] = digest == "c61fb228fddd56184bfde30e810c91d0e0f2b194a61fbe1de4896ecd45f2f3d5";
            }
        }

        private static Mesh RenderMesh(Renderer renderer) => renderer is SkinnedMeshRenderer skin ? skin.sharedMesh :
            renderer is MeshRenderer && renderer.GetComponent<MeshFilter>() != null ? renderer.GetComponent<MeshFilter>().sharedMesh : null;

        private bool UvMaterial(Material material)
        {
            if (material == null || !owned.Contains(material) || material.shader == null ||
                !(material.shader.name == "Unlit/Texture" || material.shader.name == "Unlit/Transparent" || material.shader.name == "Unlit/Transparent Cutout")) return false;
            string path = AssetDatabase.GetAssetPath(material.shader);
            if (path != "Resources/unity_builtin_extra" && path != "Library/unity default resources") return false;
            var texture = material.mainTexture as Texture2D;
            if (texture == null || !texture.isReadable || BytesPerPixel(texture.format) == 0 ||
                texture.wrapModeU == TextureWrapMode.MirrorOnce || texture.wrapModeV == TextureWrapMode.MirrorOnce) return false;
            var scale = material.mainTextureScale; var offset = material.mainTextureOffset;
            return Finite(new Vector3(scale.x, scale.y, 0)) && Finite(new Vector3(offset.x, offset.y, 0)) &&
                Finite(new Vector3(1 - scale.x - offset.x, 1 - scale.y - offset.y, 0));
        }

        private bool MaterialUvAnimated()
        {
            foreach (var site in clips)
            {
                if (AnimationUtility.GetCurveBindings(site.Clip).Any(b => b.propertyName.StartsWith("material.", StringComparison.Ordinal) ||
                    b.propertyName.StartsWith("m_Materials", StringComparison.Ordinal))) return true;
                if (AnimationUtility.GetObjectReferenceCurveBindings(site.Clip).Any(b => b.propertyName.StartsWith("m_Materials", StringComparison.Ordinal) ||
                    b.propertyName.StartsWith("material.", StringComparison.Ordinal))) return true;
            }
            return false;
        }

        private static int BytesPerPixel(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.RGBA32: case TextureFormat.ARGB32: case TextureFormat.BGRA32: return 4;
                case TextureFormat.RGB24: return 3;
                case TextureFormat.RGBAHalf: return 8;
                case TextureFormat.RGBAFloat: return 16;
                default: return 0;
            }
        }

        private Texture2D RotateTexture(Texture2D source)
        {
            int stride = BytesPerPixel(source.format);
            var copy = new Texture2D(source.width, source.height, source.format, source.mipmapCount,
                !GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat));
            try
            {
                if (copy.graphicsFormat != source.graphicsFormat) throw new InvalidOperationException("Texture graphics format changed");
                for (int mip = 0; mip < source.mipmapCount; mip++)
                {
                    var input = source.GetPixelData<byte>(mip).ToArray();
                    int pixels = Math.Max(1, source.width >> mip) * Math.Max(1, source.height >> mip);
                    if (input.Length != pixels * stride) throw new InvalidOperationException("Unexpected raw mip layout");
                    var output = new byte[input.Length];
                    for (int i = 0; i < pixels; i++) Buffer.BlockCopy(input, i * stride, output, (pixels - 1 - i) * stride, stride);
                    copy.SetPixelData(output, mip);
                }
                copy.Apply(false, false);
                ConfigureRotatedTexture(source, copy);
                return copy;
            }
            catch { Object.DestroyImmediate(copy); throw; }
        }
    }
}
