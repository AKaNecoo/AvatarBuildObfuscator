using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        private void ReorderMeshes()
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            // A custom shader may depend on SV_VertexID without a serialized reference.
            // Only the audited engine shaders qualify for Conservative permutation.
            bool auditedShaders = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).All(m =>
                (AssetDatabase.GetAssetPath(m.shader) == "Resources/unity_builtin_extra" ||
                 AssetDatabase.GetAssetPath(m.shader) == "Library/unity default resources") &&
                (m.shader.name == "Standard" || m.shader.name == "Unlit/Texture" || m.shader.name == "Unlit/Color"));
            if (settings.Mode == ProtectionMode.Conservative && !auditedShaders)
            { report.Note("顶点重排：保守模式保留未确认顶点 ID 依赖的自定义 Shader 网格"); return; }
            var clothMeshes = new HashSet<Mesh>(root.GetComponentsInChildren<Cloth>(true)
                .Select(c => c.GetComponent<SkinnedMeshRenderer>()).Where(r => r != null).Select(r => r.sharedMesh));
            foreach (var mesh in owned.OfType<Mesh>().ToArray())
            {
                if (mesh.vertexCount < 2) continue;
                if (!mesh.isReadable || clothMeshes.Contains(mesh) || renderers.OfType<MeshRenderer>().Any(r => r.additionalVertexStreams != null))
                { report.Note("顶点重排：不可读网格、Cloth 或额外顶点流保持顺序"); continue; }
                ReorderMesh(mesh);
                report.ReorderedMeshes++;
            }
        }

        private void ReorderMesh(Mesh mesh)
        {
            int count = mesh.vertexCount;
            var random = new System.Random(names.NextSeed());
            var order = Enumerable.Range(0, count).ToArray();
            for (int i = count - 1; i > 0; i--)
            { int j = random.Next(i + 1); int old = order[i]; order[i] = order[j]; order[j] = old; }
            var inverse = new int[count];
            for (int i = 0; i < count; i++) inverse[order[i]] = i;
            var perVertex = mesh.GetBonesPerVertex().ToArray();
            var weights = mesh.GetAllBoneWeights().ToArray();
            // Raw vertex streams retain every vertex attribute and its exact storage
            // format, including UV3-UV8, packed colors and non-Float32 attributes.
            var streams = new List<byte[]>();
            using (var data = Mesh.AcquireReadOnlyMeshData(mesh))
                for (int stream = 0; stream < mesh.vertexBufferCount; stream++) streams.Add(data[0].GetVertexData<byte>(stream).ToArray());
            for (int stream = 0; stream < streams.Count; stream++)
            {
                    var raw = streams[stream];
                    int stride = mesh.GetVertexBufferStride(stream);
                    var output = new byte[raw.Length];
                    for (int i = 0; i < count; i++) Buffer.BlockCopy(raw, order[i] * stride, output, i * stride, stride);
                    mesh.SetVertexBufferData(output, 0, 0, output.Length, stream, MeshUpdateFlags.DontRecalculateBounds);
            }
            var submeshes = Enumerable.Range(0, mesh.subMeshCount).Select(i => new
            { topology = mesh.GetTopology(i), indices = mesh.GetIndices(i), bounds = mesh.GetSubMesh(i).bounds }).ToArray();
            if (count > 65535 && mesh.indexFormat == IndexFormat.UInt16) mesh.indexFormat = IndexFormat.UInt32;
            mesh.subMeshCount = submeshes.Length;
            for (int i = 0; i < submeshes.Length; i++)
            {
                var sub = submeshes[i];
                mesh.SetIndices(sub.indices.Select(index => inverse[index]).ToArray(), sub.topology, i, false);
                var descriptor = mesh.GetSubMesh(i); descriptor.bounds = sub.bounds;
                mesh.SetSubMesh(i, descriptor, MeshUpdateFlags.DontRecalculateBounds);
            }
            if (perVertex.Length == count && weights.Length > 0)
            {
                var offsets = new int[count + 1];
                for (int i = 0; i < count; i++) offsets[i + 1] = offsets[i] + perVertex[i];
                var outputCounts = new NativeArray<byte>(count, Allocator.Temp);
                var outputWeights = new NativeArray<BoneWeight1>(weights.Length, Allocator.Temp);
                try
                {
                    int position = 0;
                    for (int i = 0; i < count; i++)
                    {
                        int original = order[i]; outputCounts[i] = perVertex[original];
                        for (int j = offsets[original]; j < offsets[original + 1]; j++) outputWeights[position++] = weights[j];
                    }
                    mesh.SetBoneWeights(outputCounts, outputWeights);
                }
                finally { outputCounts.Dispose(); outputWeights.Dispose(); }
            }
            var frames = new List<Tuple<string, float, Vector3[], Vector3[], Vector3[]>>();
            for (int shape = 0; shape < mesh.blendShapeCount; shape++)
                for (int frame = 0; frame < mesh.GetBlendShapeFrameCount(shape); frame++)
                {
                    var v = new Vector3[count]; var n = new Vector3[count]; var t = new Vector3[count];
                    mesh.GetBlendShapeFrameVertices(shape, frame, v, n, t);
                    frames.Add(Tuple.Create(mesh.GetBlendShapeName(shape), mesh.GetBlendShapeFrameWeight(shape, frame),
                        order.Select(i => v[i]).ToArray(), order.Select(i => n[i]).ToArray(), order.Select(i => t[i]).ToArray()));
                }
            mesh.ClearBlendShapes();
            foreach (var frame in frames) mesh.AddBlendShapeFrame(frame.Item1, frame.Item2, frame.Item3, frame.Item4, frame.Item5);
        }
    }
}
