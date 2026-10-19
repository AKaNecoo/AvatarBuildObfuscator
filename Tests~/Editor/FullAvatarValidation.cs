using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AvatarTools.Obfuscation;
using AvatarTools.Obfuscation.Editor;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Validation
{
    public static class FullAvatarValidation
    {
        [Serializable] public sealed class Metrics
        {
            public string mode, image;
            public double seconds, meanPixelDifference;
            public int objects, renderers, shapes, materials, textures, controllers, clips,
                obfuscatedMeshes, obfuscatedShapes, obfuscatedMaterials, obfuscatedTextures, obfuscatedClips,
                missingPaths, missingShapes, proxyBindings, markerComponents, humanoidBones, differentPixels;
            public bool humanoid;
            public int unityErrors;
            public List<string> bindingProblems = new List<string>();
            public List<string> parameterNames = new List<string>();
        }
        [Serializable] public sealed class Job
        {
            public string stage, scene, error;
            public List<Metrics> builds = new List<Metrics>();
        }
        private static Job job;
        private static GameObject source;
        private static Scene scene;
        private static string output;
        private static Camera camera;
        private static Color32[] baselinePixels;
        private static readonly List<GameObject> outputs = new List<GameObject>();

        public static string Start(int sourceId, string outputDirectory)
        {
            source = EditorUtility.InstanceIDToObject(sourceId) as GameObject;
            if (source == null || source.GetComponent<VRCAvatarDescriptor>() == null)
                throw new InvalidOperationException("Select an avatar root before starting validation");
            output = outputDirectory; Directory.CreateDirectory(output);
            outputs.Clear(); baselinePixels = null;
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var cameraObject = new GameObject("Validation Camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
            camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.17f, .17f, .17f, 1);
            camera.cullingMask = 1 << 31;
            var lightObject = new GameObject("Validation Light"); SceneManager.MoveGameObjectToScene(lightObject, scene);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1; light.cullingMask = 1 << 31; light.transform.rotation = Quaternion.Euler(30, -30, 0);
            job = new Job { stage = "prepared" }; Write();
            SceneManager.SetActiveScene(source.scene);
            return "Prepared. RunNext once for baseline, once for Conservative, once for Aggressive; then Finish.";
        }

        public static string RunNext()
        {
            if (job == null || job.builds.Count >= 3) throw new InvalidOperationException("Start a fresh validation first");
            int index = job.builds.Count;
            string mode = index == 0 ? "baseline" : index == 1 ? "conservative" : "aggressive";
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var nativeErrors = new List<string>();
            Application.LogCallback listener = (message, stack, type) => {
                if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception) nativeErrors.Add(message);
            };
            Application.logMessageReceived += listener;
            try
            {
                job.stage = "building-" + mode; Write();
                var clone = Object.Instantiate(source); SceneManager.MoveGameObjectToScene(clone, scene);
                clone.name = "Guard_" + mode; clone.SetActive(true);
                foreach (var transform in clone.GetComponentsInChildren<Transform>(true))
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject))
                        PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var component in clone.GetComponentsInChildren<AvatarBuildObfuscator>(true)) Object.DestroyImmediate(component);
                // An earlier, separately audited plugin must not affect the baseline or this package's evidence.
                foreach (var component in clone.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null && component.GetType().FullName == "HateRipper.AvatarObfuscator.AvatarObfuscator")
                        Object.DestroyImmediate(component);
                if (index > 0)
                {
                    var marker = clone.AddComponent<AvatarBuildObfuscator>();
                    marker.userName = "Validation User";
                    marker.mode = index == 1 ? ProtectionMode.Conservative : ProtectionMode.Aggressive;
                }
                using (new OverrideTemporaryDirectoryScope(GuardValidation.Folder + "/BuildGenerated"))
                    AvatarProcessor.ProcessAvatar(clone);
                var metrics = Inspect(clone); metrics.mode = mode;
                metrics.image = Path.Combine(output, mode + ".png");
                var pixels = Render(clone, metrics.image);
                if (baselinePixels == null) baselinePixels = pixels;
                else
                {
                    double difference = 0;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        var a = baselinePixels[i]; var b = pixels[i];
                        int d = Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b) + Math.Abs(a.a - b.a);
                        if (d > 0) metrics.differentPixels++;
                        difference += d;
                    }
                    metrics.meanPixelDifference = difference / (pixels.Length * 4.0);
                }
                metrics.unityErrors = nativeErrors.Count;
                if (nativeErrors.Count != 0) throw new InvalidOperationException("Unity logged errors/assertions: " + string.Join("; ", nativeErrors.Take(3)));
                watch.Stop(); metrics.seconds = watch.Elapsed.TotalSeconds;
                clone.SetActive(false); outputs.Add(clone); job.builds.Add(metrics); job.stage = mode + "-complete";
                Write(); return JsonUtility.ToJson(metrics);
            }
            catch (Exception e) { job.error = e.ToString(); job.stage = "failed"; Write(); throw; }
            finally { Application.logMessageReceived -= listener; }
        }

        public static string Finish()
        {
            if (job == null || job.builds.Count != 3 || job.error != null) throw new InvalidOperationException("All three builds must complete first");
            for (int i = 0; i < outputs.Count; i++)
            {
                outputs[i].transform.position = new Vector3((i - 1) * 1.5f, 0, 0);
                outputs[i].SetActive(true);
                var pipeline = outputs[i].GetComponent<VRC.Core.PipelineManager>();
                if (pipeline != null) pipeline.blueprintId = "";
            }
            camera.transform.position = new Vector3(0, 1, 6); camera.transform.LookAt(new Vector3(0, 1, 0));
            camera.enabled = false;
            job.scene = AssetDatabase.GenerateUniqueAssetPath(GuardValidation.Folder + "/BuildComparison.unity");
            if (!EditorSceneManager.SaveScene(scene, job.scene)) throw new IOException("Could not save comparison scene");
            SceneManager.SetActiveScene(source.scene); job.stage = "completed"; Write(); return JsonUtility.ToJson(job, true);
        }

        private static void Write() { File.WriteAllText(Path.Combine(output, "full-avatar-results.json"), JsonUtility.ToJson(job, true)); }

        public static Metrics Inspect(GameObject avatar)
        {
            var m = new Metrics();
            m.objects = avatar.GetComponentsInChildren<Transform>(true).Length;
            var renderers = avatar.GetComponentsInChildren<Renderer>(true); m.renderers = renderers.Length;
            foreach (var r in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r.sharedMesh == null) continue;
                m.shapes += r.sharedMesh.blendShapeCount;
                if (UsernameNameGenerator.IsGeneratedName(r.sharedMesh.name)) m.obfuscatedMeshes++;
                for (int i = 0; i < r.sharedMesh.blendShapeCount; i++)
                    if (UsernameNameGenerator.IsGeneratedName(r.sharedMesh.GetBlendShapeName(i))) m.obfuscatedShapes++;
            }
            var mats = renderers.SelectMany(r => r.sharedMaterials).Where(x => x != null).Distinct().ToArray();
            m.materials = mats.Length; m.obfuscatedMaterials = mats.Count(x => UsernameNameGenerator.IsGeneratedName(x.name));
            var textures = mats.SelectMany(x => x.GetTexturePropertyNames().Select(x.GetTexture)).Where(x => x != null).Distinct().ToArray();
            m.textures = textures.Length; m.obfuscatedTextures = textures.Count(x => UsernameNameGenerator.IsGeneratedName(x.name));
            var sites = new List<Tuple<RuntimeAnimatorController, Transform>>();
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            foreach (var layer in descriptor.baseAnimationLayers.Concat(descriptor.specialAnimationLayers))
                if (layer.animatorController != null) sites.Add(Tuple.Create(layer.animatorController, avatar.transform));
            foreach (var a in avatar.GetComponentsInChildren<Animator>(true))
                if (a.runtimeAnimatorController != null) sites.Add(Tuple.Create(a.runtimeAnimatorController, a.transform));
            var controllers = sites.Select(x => x.Item1).Distinct().ToArray(); m.controllers = controllers.Length;
            var clips = controllers.SelectMany(c => c.animationClips).Distinct().ToArray(); m.clips = clips.Length;
            m.obfuscatedClips = clips.Count(c => UsernameNameGenerator.IsGeneratedName(c.name));
            foreach (var controller in controllers.OfType<AnimatorController>())
                m.parameterNames.AddRange(controller.parameters.Select(p => p.name));
            m.parameterNames = m.parameterNames.Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            foreach (var site in sites)
            foreach (var clip in site.Item1.animationClips.Distinct())
            foreach (var b in AnimationUtility.GetCurveBindings(clip).Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip)))
            {
                if (clip.name.StartsWith("proxy_")) continue;
                var target = string.IsNullOrEmpty(b.path) ? site.Item2 : site.Item2.Find(b.path);
                if (target == null)
                {
                    if (b.path == "$AvatarOptimizerClipLengthDummy$") { m.proxyBindings++; continue; }
                    m.missingPaths++; if (m.bindingProblems.Count < 20) m.bindingProblems.Add(b.path + ":" + b.propertyName); continue;
                }
                if (b.propertyName.StartsWith("blendShape."))
                {
                    var r = target.GetComponent<SkinnedMeshRenderer>();
                    if (r == null || r.sharedMesh == null || r.sharedMesh.GetBlendShapeIndex(b.propertyName.Substring(11)) < 0)
                    { m.missingShapes++; if (m.bindingProblems.Count < 20) m.bindingProblems.Add(b.path + ":" + b.propertyName); }
                }
            }
            m.markerComponents = avatar.GetComponentsInChildren<AvatarBuildObfuscator>(true).Length;
            var animator = avatar.GetComponent<Animator>(); m.humanoid = animator != null && animator.avatar != null && animator.avatar.isHuman;
            if (m.humanoid)
                for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                    if (animator.GetBoneTransform((HumanBodyBones)i) != null) m.humanoidBones++;
            return m;
        }

        private static Color32[] Render(GameObject avatar, string path)
        {
            avatar.transform.position = new Vector3(1000, 0, 0);
            foreach (var t in avatar.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            var renderers = avatar.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
            var bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            camera.transform.position = bounds.center + Vector3.forward * Mathf.Max(2, bounds.extents.magnitude * 2.6f);
            camera.transform.LookAt(bounds.center); camera.nearClipPlane = .01f; camera.farClipPlane = 100; camera.fieldOfView = 35;
            var rt = RenderTexture.GetTemporary(512, 768, 24, RenderTextureFormat.ARGB32); var previous = RenderTexture.active;
            var cpu = new Texture2D(512, 768, TextureFormat.RGBA32, false);
            try { camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt; cpu.ReadPixels(new Rect(0, 0, 512, 768), 0, 0); cpu.Apply(); File.WriteAllBytes(path, cpu.EncodeToPNG()); return cpu.GetPixels32(); }
            finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(cpu); }
        }
    }
}
