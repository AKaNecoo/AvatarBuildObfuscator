// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        private void ReorderBonePalettes()
        {
            if (unknownDriver && settings.Mode == ProtectionMode.Conservative)
            { report.Note("骨骼索引表：保守模式保留未知驱动"); return; }
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var group in renderers.OfType<SkinnedMeshRenderer>().Where(r => r.sharedMesh != null).GroupBy(r => r.sharedMesh))
            {
                var mesh = group.Key;
                if (!owned.Contains(mesh) || !mesh.isReadable || mesh.bindposes.Length < 2) continue;
                int count = mesh.bindposes.Length;
                if (group.Any(r => settings.PreserveBonePalette.Contains(r) || meshSwaps.Contains(r) || r.GetComponent<Cloth>() != null ||
                    r.bones.Length != count || r.bones.Any(b => b == null)) ||
                    renderers.Any(r => !(r is SkinnedMeshRenderer) && RenderMesh(r) == mesh))
                { report.Note("骨骼索引表：手动保留、Cloth、网格切换、共享契约或不完整骨骼表"); continue; }
                bool audited = group.SelectMany(r => r.sharedMaterials).Where(m => m != null).All(m => m.shader != null &&
                    (AssetDatabase.GetAssetPath(m.shader) == "Resources/unity_builtin_extra" || AssetDatabase.GetAssetPath(m.shader) == "Library/unity default resources") &&
                    (m.shader.name == "Standard" || m.shader.name == "Unlit/Texture" || m.shader.name == "Unlit/Color" ||
                     m.shader.name == "Unlit/Transparent" || m.shader.name == "Unlit/Transparent Cutout"));
                if (settings.Mode == ProtectionMode.Conservative && !audited)
                { report.Note("骨骼索引表：保守模式保留未验证的自定义 Shader 索引依赖"); continue; }
                byte[] perVertex;
                BoneWeight1[] weights;
                perVertex = mesh.GetBonesPerVertex().ToArray();
                weights = mesh.GetAllBoneWeights().ToArray();
                if (perVertex.Length != mesh.vertexCount || weights.Length == 0 || perVertex.Sum(v => (int)v) != weights.Length ||
                    weights.Any(w => w.boneIndex < 0 || w.boneIndex >= count || float.IsNaN(w.weight) || float.IsInfinity(w.weight) || w.weight < 0))
                { report.Note("骨骼索引表：蒙皮权重不完整或索引无效"); continue; }
                var order = ShuffledIndices(count);
                if (order.Select((old, index) => old == index).All(x => x)) order = order.Skip(1).Concat(order.Take(1)).ToArray();
                var inverse = new int[count];
                for (int i = 0; i < count; i++) inverse[order[i]] = i;
                var bindposes = mesh.bindposes;
                for (int i = 0; i < weights.Length; i++) weights[i].boneIndex = inverse[weights[i].boneIndex];
                using (var counts = new NativeArray<byte>(perVertex, Allocator.Temp))
                using (var remapped = new NativeArray<BoneWeight1>(weights, Allocator.Temp)) mesh.SetBoneWeights(counts, remapped);
                mesh.bindposes = order.Select(i => bindposes[i]).ToArray();
                foreach (var renderer in group)
                {
                    var bones = renderer.bones;
                    renderer.bones = order.Select(i => bones[i]).ToArray();
                }
                report.ReorderedBonePalettes++;
                report.RemappedBoneEntries += order.Where((old, index) => old != index).Count();
            }
        }
    }
}
