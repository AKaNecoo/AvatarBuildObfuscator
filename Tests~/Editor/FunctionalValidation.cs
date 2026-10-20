using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Validation
{
    /// <summary>Compares saved, reloaded build outputs. Never samples the source avatar.</summary>
    public static class FunctionalValidation
    {
        [Serializable] public sealed class Result
        {
            public string stage, error;
            public int clipPairs, poseSamples, texturePairs, materialPairs;
            public float maxPositionDelta, maxNormalDelta, maxBoneDelta;
            public int pixelDifferences, nativeErrors;
        }
        private sealed class Pair { public AnimationClip a, b, c; }
        private static Renderer[][] orderedRenderers;

        public static string Run(string resultsPath, string outputPath)
        {
            var job = JsonUtility.FromJson<FullAvatarValidation.Job>(File.ReadAllText(resultsPath));
            if (job.stage != "completed" || job.builds.Count != 3) throw new InvalidOperationException("Need three completed saved builds");
            var scene = SceneManager.GetSceneByPath(job.scene);
            if (!scene.IsValid() || !scene.isLoaded) scene = EditorSceneManager.OpenScene(job.scene, OpenSceneMode.Additive);
            var originals = job.builds.Select(m => scene.GetRootGameObjects().Single(g => g.name == m.rootName)).ToArray();
            var result = new Result { stage = "sampling" };
            var probes = new List<GameObject>(); var errors = new List<string>();
            var activeScene = SceneManager.GetActiveScene();
            var testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Application.LogCallback listener = (message, stack, type) => {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message);
            };
            Application.logMessageReceived += listener;
            try
            {
                if (AnimationMode.InAnimationMode()) throw new InvalidOperationException("Another animation preview is active");
                foreach (var original in originals)
                {
                    var clone = Object.Instantiate(original); SceneManager.MoveGameObjectToScene(clone, testScene);
                    clone.SetActive(true); probes.Add(clone);
                    var d = clone.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                    clone.GetComponent<Animator>().runtimeAnimatorController = d.baseAnimationLayers.First(l => l.animatorController != null).animatorController;
                }
                orderedRenderers = Enumerable.Range(0,3).Select(mode => job.builds[mode].rendererSites.Select(site => {
                    var t = string.IsNullOrEmpty(site.path) ? probes[mode].transform : probes[mode].transform.Find(site.path);
                    if (t == null) throw new InvalidOperationException("Saved renderer path missing");
                    var renderer = t.GetComponents<Renderer>()[site.index];
                    if (renderer.GetType().FullName != site.type) throw new InvalidOperationException("Renderer type changed");
                    return renderer;
                }).ToArray()).ToArray();
                if (orderedRenderers.Any(rs=>rs.Length!=orderedRenderers[0].Length)) throw new InvalidOperationException("Renderer count changed");
                CompareMaterials(probes, result);
                var pairs = ClipPairs(probes).GroupBy(p => p.a).Select(g => g.First()).ToArray();
                var selected = pairs.Where(p => p.a.humanMotion).Take(5)
                    .Concat(pairs.Where(p => AnimationUtility.GetCurveBindings(p.a).Any(b => b.propertyName.StartsWith("blendShape."))).Take(6))
                    .Concat(pairs.Where(p => AnimationUtility.GetCurveBindings(p.a).Any(b => b.type == typeof(Transform))).Take(4))
                    .GroupBy(p => p.a).Select(g => g.First()).ToArray();
                if (selected.Length == 0) throw new InvalidOperationException("No sampleable clips");
                foreach (var pair in selected)
                {
                    result.clipPairs++;
                    foreach (float normalized in new[] { 0f, .37f, .83f })
                    {
                        AnimationMode.StartAnimationMode();
                        try
                        {
                            AnimationMode.BeginSampling();
                            AnimationMode.SampleAnimationClip(probes[0], pair.a, pair.a.length * normalized);
                            AnimationMode.SampleAnimationClip(probes[1], pair.b, pair.b.length * normalized);
                            AnimationMode.SampleAnimationClip(probes[2], pair.c, pair.c.length * normalized);
                            AnimationMode.EndSampling();
                            CompareGeometry(probes, result); result.poseSamples++;
                        }
                        finally { AnimationMode.StopAnimationMode(); }
                    }
                }
                result.nativeErrors = errors.Count;
                if (errors.Count != 0) throw new InvalidOperationException(string.Join("; ", errors.Take(3)));
                if (result.maxPositionDelta > .0002f || result.maxBoneDelta > .0002f || result.maxNormalDelta > .002f || result.pixelDifferences != 0)
                    throw new InvalidOperationException("Sampled geometry, bones or textures differ beyond tolerance");
                result.stage = "passed";
            }
            catch (Exception e) { result.stage = "failed"; result.error = e.ToString(); result.nativeErrors = errors.Count; }
            finally
            {
                Application.logMessageReceived -= listener;
                foreach (var probe in probes) if (probe != null) Object.DestroyImmediate(probe);
                EditorSceneManager.CloseScene(testScene, true);
                if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
                File.WriteAllText(outputPath, JsonUtility.ToJson(result, true));
            }
            return JsonUtility.ToJson(result, true);
        }

        private static IEnumerable<Pair> ClipPairs(List<GameObject> probes)
        {
            var layers = probes.Select(p => {
                var d = p.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                return d.baseAnimationLayers.Concat(d.specialAnimationLayers).ToArray();
            }).ToArray();
            for (int i = 0; i < layers[0].Length; i++)
            {
                var a = layers[0][i].animatorController as AnimatorController;
                var b = layers[1][i].animatorController as AnimatorController;
                var c = layers[2][i].animatorController as AnimatorController;
                if (a == null || b == null || c == null) continue;
                if (a.layers.Length != b.layers.Length || a.layers.Length != c.layers.Length) throw new InvalidOperationException("Layer count changed");
                for (int j = 0; j < a.layers.Length; j++)
                    foreach (var pair in MachinePairs(a.layers[j].stateMachine, b.layers[j].stateMachine, c.layers[j].stateMachine)) yield return pair;
            }
        }

        private static IEnumerable<Pair> MachinePairs(AnimatorStateMachine a, AnimatorStateMachine b, AnimatorStateMachine c)
        {
            if (a.states.Length != b.states.Length || a.states.Length != c.states.Length ||
                a.stateMachines.Length != b.stateMachines.Length || a.stateMachines.Length != c.stateMachines.Length)
                throw new InvalidOperationException("State graph topology changed");
            for (int i = 0; i < a.states.Length; i++)
                foreach (var pair in MotionPairs(a.states[i].state.motion, b.states[i].state.motion, c.states[i].state.motion)) yield return pair;
            for (int i = 0; i < a.stateMachines.Length; i++)
                foreach (var pair in MachinePairs(a.stateMachines[i].stateMachine, b.stateMachines[i].stateMachine, c.stateMachines[i].stateMachine)) yield return pair;
        }

        private static IEnumerable<Pair> MotionPairs(Motion a, Motion b, Motion c)
        {
            if (a == null)
            { if (b != null || c != null) throw new InvalidOperationException("Unexpected new state motion"); yield break; }
            if (a is AnimationClip clip)
            {
                if (!(b is AnimationClip) || !(c is AnimationClip)) throw new InvalidOperationException("State motion lost");
                yield return new Pair { a = clip, b = (AnimationClip)b, c = (AnimationClip)c }; yield break;
            }
            var aa = a as BlendTree; var bb = b as BlendTree; var cc = c as BlendTree;
            if (aa == null || bb == null || cc == null || aa.children.Length != bb.children.Length || aa.children.Length != cc.children.Length)
                throw new InvalidOperationException("BlendTree topology changed");
            for (int i = 0; i < aa.children.Length; i++)
                foreach (var pair in MotionPairs(aa.children[i].motion, bb.children[i].motion, cc.children[i].motion)) yield return pair;
        }

        private static void CompareGeometry(List<GameObject> probes, Result result)
        {
            var renderers = orderedRenderers.Select(rs=>rs.OfType<SkinnedMeshRenderer>().ToArray()).ToArray();
            for (int i = 0; i < renderers[0].Length; i++)
            {
                var baked = new[] { new Mesh(), new Mesh(), new Mesh() };
                try
                {
                    for (int mode = 0; mode < 3; mode++) renderers[mode][i].BakeMesh(baked[mode]);
                    var indices = Enumerable.Range(0,3).Select(mode => renderers[mode][i].sharedMesh.triangles).ToArray();
                    var vertices = baked.Select(m => m.vertices).ToArray(); var normals = baked.Select(m => m.normals).ToArray();
                    var matrices = Enumerable.Range(0,3).Select(mode => probes[mode].transform.worldToLocalMatrix * renderers[mode][i].localToWorldMatrix).ToArray();
                    var normalMatrices = matrices.Select(m => m.inverse.transpose).ToArray();
                    for (int mode = 1; mode < 3; mode++)
                    {
                        if (indices[0].Length != indices[mode].Length) throw new InvalidOperationException("Mesh topology changed");
                        for (int k = 0; k < indices[0].Length; k++)
                        {
                            int a = indices[0][k], b = indices[mode][k];
                            result.maxPositionDelta = Mathf.Max(result.maxPositionDelta, Vector3.Distance(matrices[0].MultiplyPoint3x4(vertices[0][a]), matrices[mode].MultiplyPoint3x4(vertices[mode][b])));
                            if (normals[0].Length == vertices[0].Length && normals[mode].Length == vertices[mode].Length)
                                result.maxNormalDelta = Mathf.Max(result.maxNormalDelta, Vector3.Distance(normalMatrices[0].MultiplyVector(normals[0][a]).normalized, normalMatrices[mode].MultiplyVector(normals[mode][b]).normalized));
                        }
                    }
                }
                finally { foreach (var mesh in baked) Object.DestroyImmediate(mesh); }
            }
            var animators = probes.Select(p => p.GetComponent<Animator>()).ToArray();
            for (int bone = 0; bone < (int)HumanBodyBones.LastBone; bone++)
            {
                var baseline = animators[0].GetBoneTransform((HumanBodyBones)bone); if (baseline == null) continue;
                var position = probes[0].transform.InverseTransformPoint(baseline.position);
                for (int mode = 1; mode < 3; mode++)
                {
                    var target = animators[mode].GetBoneTransform((HumanBodyBones)bone);
                    if (target == null) throw new InvalidOperationException("Humanoid bone missing");
                    result.maxBoneDelta = Mathf.Max(result.maxBoneDelta, Vector3.Distance(position, probes[mode].transform.InverseTransformPoint(target.position)));
                }
            }
        }

        private static void CompareMaterials(List<GameObject> probes, Result result)
        {
            var renderers = orderedRenderers;
            var seen = new HashSet<string>();
            for (int i = 0; i < renderers[0].Length; i++)
                for (int slot = 0; slot < renderers[0][i].sharedMaterials.Length; slot++)
                {
                    var materials = Enumerable.Range(0,3).Select(mode => renderers[mode][i].sharedMaterials[slot]).ToArray();
                    if (materials.Any(m => m == null)) continue;
                    var key = string.Join(":", materials.Select(m => m.GetInstanceID())); if (!seen.Add(key)) continue;
                    result.materialPairs++;
                    for (int mode = 1; mode < 3; mode++)
                        foreach (var property in materials[0].GetTexturePropertyNames())
                        {
                            var source = materials[0].GetTexture(property) as Texture2D;
                            var target = materials[mode].GetTexture(property) as Texture2D;
                            if (source == null && target == null) continue;
                            if (source == null || target == null || source.width != target.width || source.height != target.height || source.graphicsFormat != target.graphicsFormat)
                                throw new InvalidOperationException("Texture metadata differs");
                            string textureKey = "tex:" + source.GetInstanceID() + ":" + target.GetInstanceID();
                            if (!seen.Add(textureKey)) continue;
                            var a = Pixels(source); var b = Pixels(target); result.texturePairs++;
                            result.pixelDifferences += a.Where((pixel,index) => !pixel.Equals(b[index])).Count();
                        }
                }
        }

        private static Color32[] Pixels(Texture source)
        {
            int width = Mathf.Min(256,source.width), height = Mathf.Min(256,source.height);
            var rt = RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active; var cpu = new Texture2D(width,height,TextureFormat.RGBA32,false);
            try { Graphics.Blit(source,rt); RenderTexture.active=rt; cpu.ReadPixels(new Rect(0,0,width,height),0,0);cpu.Apply();return cpu.GetPixels32(); }
            finally { RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(cpu); }
        }
    }
}
