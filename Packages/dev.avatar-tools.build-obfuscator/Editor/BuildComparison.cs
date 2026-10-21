// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Editor
{
    [Serializable]
    public sealed class BuildComparisonResult
    {
        public string createdUtc, status, error, mode;
        public int sourceFilesChecked, rendererPairs, geometrySamples, missingBindings, knownDurationPlaceholders, materialPairs;
        public float maximumPositionError, maximumNormalError;
        public bool sourceMemoryUnchanged, sourceFilesUnchanged;
        public List<string> problems = new List<string>();
        public string limits = "Samples initial geometry, arm rotations and available matching BlendShapes. Does not test Animator transitions, physics, network, external devices or VRChat client behavior.";
    }

    public sealed class BuildComparison : IDisposable
    {
        private sealed class Site
        {
            public Renderer renderer;
            public Dictionary<string, string> shapes = new Dictionary<string, string>();
        }
        private static BuildComparison active;
        private Dictionary<string, Site> capture;
        private readonly List<GameObject> copies = new List<GameObject>();
        private readonly List<Object> transient = new List<Object>();
        private readonly List<Dictionary<string, Site>> sites = new List<Dictionary<string, Site>>();
        private Scene scene;
        private string generatedPath;
        private bool mergedShapes;
        public GameObject Baseline => copies.Count > 0 ? copies[0] : null;
        public GameObject Obfuscated => copies.Count > 1 ? copies[1] : null;
        public BuildComparisonResult Result { get; private set; }

        internal static void Capture(GameObject root)
        {
            if (active == null) return;
            active.capture = new Dictionary<string, Site>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var indices = new List<int>();
                for (var t = renderer.transform; t != root.transform; t = t.parent) indices.Add(t.GetSiblingIndex());
                string key = AnimationUtility.CalculateTransformPath(renderer.transform, root.transform) + "|" + string.Join(".", indices) + "|" + renderer.GetType().FullName + "|" + Array.IndexOf(renderer.GetComponents<Renderer>(), renderer);
                var site = new Site { renderer = renderer };
                var mesh = RenderMesh(renderer);
                if (mesh != null) for (int i = 0; i < mesh.blendShapeCount; i++) site.shapes[mesh.GetBlendShapeName(i)] = mesh.GetBlendShapeName(i);
                active.capture.Add(key, site);
            }
        }

        internal static void RecordShapeNames(Dictionary<SkinnedMeshRenderer, Dictionary<string, string>> names, bool merged)
        {
            if (active?.capture == null) return;
            active.mergedShapes |= merged;
            foreach (var site in active.capture.Values)
                if (site.renderer is SkinnedMeshRenderer renderer && names.TryGetValue(renderer, out var map))
                    foreach (var pair in map) site.shapes[pair.Key] = pair.Value;
        }

        public BuildComparisonResult Run(GameObject source, ProtectionMode? mode = null)
        {
            if (active != null || copies.Count != 0) throw new InvalidOperationException("A comparison is already running or completed.");
            if (source == null || !source.scene.IsValid() || PrefabUtility.IsPartOfPrefabAsset(source) || source.GetComponent<VRCAvatarDescriptor>() == null ||
                source.GetComponent<AvatarBuildObfuscator>() == null || !source.GetComponent<AvatarBuildObfuscator>().enabled)
                throw new InvalidOperationException("Select a scene avatar root with an enabled Avatar Build Obfuscator.");
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Wait for the Editor to become idle in Edit mode.");
            Result = new BuildComparisonResult { createdUtc = DateTime.UtcNow.ToString("o"), status = "running", mode = (mode ?? source.GetComponent<AvatarBuildObfuscator>().mode).ToString() };
            var sourceMemory = Memory(source);
            bool dirty = source.scene.isDirty;
            var sourceFiles = SourceFiles(source);
            Result.sourceFilesChecked = sourceFiles.Count;
            scene = EditorSceneManager.NewPreviewScene();
            generatedPath = "Packages/nadena.dev.ndmf/__Generated/AvatarBuildObfuscatorDiagnostics_" + Guid.NewGuid().ToString("N");
            active = this;
            Application.LogCallback listener = (message, stack, type) =>
            {
                if ((type == LogType.Error || type == LogType.Assert || type == LogType.Exception) && Result.problems.Count < 200)
                    Result.problems.Add("Unity error observed during comparison: " + message);
            };
            Application.logMessageReceived += listener;
            try
            {
                using (PackageUpdates.DuringBuild())
                {
                    for (int i = 0; i < 2; i++)
                    {
                        EditorUtility.DisplayProgressBar("Avatar Build Obfuscator", i == 0 ? "Building baseline copy…" : "Building obfuscated copy…", i * .4f);
                        var copy = Object.Instantiate(source); copies.Add(copy);
                        SceneManager.MoveGameObjectToScene(copy, scene);
                        copy.name = source.name; copy.SetActive(true);
                        foreach (var t in copy.GetComponentsInChildren<Transform>(true))
                            if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                        if (i == 0) foreach (var marker in copy.GetComponentsInChildren<AvatarBuildObfuscator>(true)) Object.DestroyImmediate(marker);
                        else if (mode.HasValue) copy.GetComponent<AvatarBuildObfuscator>().mode = mode.Value;
                        capture = null;
                        using (new OverrideTemporaryDirectoryScope(generatedPath + "/" + i)) AvatarProcessor.ProcessAvatar(copy);
                        if (capture == null || capture.Values.Any(s => s.renderer == null)) throw new InvalidOperationException("Renderer capture is incomplete after preprocessing.");
                        sites.Add(capture);
                        CheckBindings(copy);
                        copy.SetActive(false);
                    }
                    CompareGeometry();
                }
                Result.status = Result.problems.Count == 0 ? "sample-checks-passed" : "differences-found";
            }
            catch (Exception e)
            {
                Result.status = "failed"; Result.error = e.ToString();
            }
            finally
            {
                Application.logMessageReceived -= listener;
                active = null;
                EditorUtility.ClearProgressBar();
                try
                {
                    Result.sourceMemoryUnchanged = source != null && dirty == source.scene.isDirty && sourceMemory == Memory(source);
                    Result.sourceFilesUnchanged = sourceFiles.All(p => File.Exists(p.Key) && Digest(p.Key) == p.Value);
                }
                catch (Exception e) { Result.problems.Add("Source verification incomplete: " + e.Message); }
                if (!Result.sourceMemoryUnchanged || !Result.sourceFilesUnchanged)
                { Result.problems.Add("Source memory or saved dependency files changed during the check. No automatic restore was attempted."); Result.status = "source-change-detected"; }
                var folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library/AvatarBuildObfuscator/Diagnostics");
                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "latest.json"), JsonUtility.ToJson(Result, true));
            }
            return Result;
        }

        private static string Memory(GameObject source) => string.Join("\n", source.GetComponentsInChildren<Component>(true).Where(c => c != null)
            .Select(c => c.GetInstanceID() + ":" + EditorJsonUtility.ToJson(c))) + "\n" + string.Join(";", source.GetComponentsInChildren<Transform>(true).Select(t => t.GetInstanceID() + ":" + t.GetSiblingIndex() + ":" + t.gameObject.activeSelf));

        private static Mesh RenderMesh(Renderer renderer) => renderer is SkinnedMeshRenderer skin ? skin.sharedMesh :
            renderer is MeshRenderer && renderer.GetComponent<MeshFilter>() != null ? renderer.GetComponent<MeshFilter>().sharedMesh : null;

        private static Dictionary<string, string> SourceFiles(GameObject source)
        {
            var paths = BuildMetadataInspection.Dependencies(source).Select(AssetDatabase.GetAssetPath).Append(source.scene.path)
                .Where(p => !string.IsNullOrEmpty(p) && (p.StartsWith("Assets/", StringComparison.Ordinal) || p.StartsWith("Packages/", StringComparison.Ordinal)))
                .SelectMany(p => new[] { p, p + ".meta" }).Where(File.Exists).Distinct().ToArray();
            return paths.ToDictionary(Path.GetFullPath, Digest, StringComparer.OrdinalIgnoreCase);
        }

        private static string Digest(string path)
        {
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return Convert.ToBase64String(sha.ComputeHash(stream));
        }

        private void CheckBindings(GameObject copy)
        {
            var dependencies = BuildMetadataInspection.Dependencies(copy);
            foreach (var mask in dependencies.OfType<AvatarMask>())
                for (int i = 0; i < mask.transformCount; i++)
                {
                    string path = mask.GetTransformPath(i);
                    if (!string.IsNullOrEmpty(path) && copy.transform.Find(path) == null &&
                        !copy.GetComponentsInChildren<Animator>(true).Any(a => a.transform.Find(path) != null))
                    { Result.missingBindings++; Result.problems.Add("Unresolved AvatarMask path: " + mask.name + " / " + path); }
                }
            foreach (var clip in dependencies.OfType<AnimationClip>().Distinct())
            {
                string path = AssetDatabase.GetAssetPath(clip);
                if (clip.name.StartsWith("proxy_", StringComparison.OrdinalIgnoreCase) || path.StartsWith("Packages/com.vrchat.", StringComparison.Ordinal) ||
                    path.IndexOf("/VRCSDK/", StringComparison.OrdinalIgnoreCase) >= 0 || path == "Resources/unity_builtin_extra" || path == "Library/unity default resources") continue;
                // Child Animator bindings are relative to that Animator, not always the avatar root.
                var roots = copy.GetComponentsInChildren<Animator>(true).Where(a => a.runtimeAnimatorController != null && a.runtimeAnimatorController.animationClips.Contains(clip))
                    .Select(a => a.gameObject).Append(copy).Distinct().ToArray();
                foreach (var binding in AnimationUtility.GetCurveBindings(clip).Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip)))
                {
                    if (IsDurationPlaceholder(clip, binding)) { Result.knownDurationPlaceholders++; continue; }
                    bool valid = roots.Any(r =>
                    {
                        var target = AnimationUtility.GetAnimatedObject(r, binding);
                        if (target == null) return false;
                        if (!binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)) return true;
                        return target is SkinnedMeshRenderer skin && skin.sharedMesh != null && skin.sharedMesh.GetBlendShapeIndex(binding.propertyName.Substring(11)) >= 0;
                    });
                    if (!valid) { Result.missingBindings++; Result.problems.Add("Unresolved animation binding: " + clip.name + " / " + binding.path + " / " + binding.propertyName); }
                }
            }
        }

        public static bool IsDurationPlaceholder(AnimationClip clip, EditorCurveBinding binding)
        {
            if (binding.path != "$AvatarOptimizerClipLengthDummy$" || binding.type != typeof(GameObject) || binding.propertyName != "m_IsActive" || binding.isPPtrCurve) return false;
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            return curve != null && curve.length > 0 && curve.keys.All(k => k.value == 1f && Mathf.Abs(k.time - clip.length) <= .000001f);
        }

        private void CompareGeometry()
        {
            if (!new HashSet<string>(sites[0].Keys).SetEquals(sites[1].Keys))
            { Result.problems.Add("Upstream renderer identities differ between the two builds; automatic geometry pairing is unavailable."); return; }
            foreach (var key in sites[0].Keys)
            {
                var a = sites[0][key]; var b = sites[1][key];
                var materialsA = a.renderer.sharedMaterials; var materialsB = b.renderer.sharedMaterials;
                if (materialsA.Length != materialsB.Length) Result.problems.Add("Material slot count changed: " + key);
                else for (int i = 0; i < materialsA.Length; i++)
                {
                    var left = materialsA[i]; var right = materialsB[i];
                    if ((left == null) != (right == null)) { Result.problems.Add("Material reference missing: " + key + " / " + i); continue; }
                    if (left == null) continue;
                    Result.materialPairs++;
                    if (left.shader != null && right.shader == null) Result.problems.Add("Shader reference missing: " + key + " / " + i);
                    var names = new HashSet<string>(right.GetTexturePropertyNames());
                    foreach (var property in left.GetTexturePropertyNames())
                        if (left.GetTexture(property) != null && (!names.Contains(property) || right.GetTexture(property) == null))
                            Result.problems.Add("Texture reference missing: " + key + " / " + property);
                }
                if (!(a.renderer is SkinnedMeshRenderer sa) || !(b.renderer is SkinnedMeshRenderer sb)) continue;
                if ((sa.sharedMesh == null) != (sb.sharedMesh == null)) Result.problems.Add("Mesh reference missing: " + key);
                if (sa.sharedMesh == null || sb.sharedMesh == null) continue;
                Result.rendererPairs++;
                CompareMesh(sa, sb, key);
                // Pose both independent skeletons identically, then restore them.
                var bonesA = copies[0].GetComponentsInChildren<Transform>(true);
                var bonesB = copies[1].GetComponentsInChildren<Transform>(true);
                var rotationsA = bonesA.ToDictionary(t => t, t => t.localRotation);
                var rotationsB = bonesB.ToDictionary(t => t, t => t.localRotation);
                try
                {
                    foreach (var root in copies)
                    {
                        var animator = root.GetComponent<Animator>();
                        if (animator == null || !animator.isHuman) continue;
                        var arm = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
                        if (arm != null) arm.localRotation *= Quaternion.Euler(0, 0, 13);
                    }
                    CompareMesh(sa, sb, key + " / arm pose");
                }
                finally
                {
                    foreach (var p in rotationsA) if (p.Key != null) p.Key.localRotation = p.Value;
                    foreach (var p in rotationsB) if (p.Key != null) p.Key.localRotation = p.Value;
                }
                foreach (var shape in (mergedShapes ? Enumerable.Empty<string>() : a.shapes.Keys.Where(b.shapes.ContainsKey).Take(64)))
                {
                    int ia = sa.sharedMesh.GetBlendShapeIndex(a.shapes[shape]); int ib = sb.sharedMesh.GetBlendShapeIndex(b.shapes[shape]);
                    if (ia < 0 || ib < 0) continue;
                    float wa = sa.GetBlendShapeWeight(ia), wb = sb.GetBlendShapeWeight(ib);
                    try
                    {
                        sa.SetBlendShapeWeight(ia, 37); sb.SetBlendShapeWeight(ib, 37); CompareMesh(sa, sb, key + " / " + shape);
                    }
                    finally { sa.SetBlendShapeWeight(ia, wa); sb.SetBlendShapeWeight(ib, wb); }
                    break;
                }
            }
            if (Result.rendererPairs == 0) Result.problems.Add("No comparable skinned renderers were sampled.");
        }

        private void CompareMesh(SkinnedMeshRenderer a, SkinnedMeshRenderer b, string label)
        {
            var ma = new Mesh(); var mb = new Mesh();
            try
            {
                a.BakeMesh(ma); b.BakeMesh(mb);
                var va = ma.vertices; var vb = mb.vertices; var na = ma.normals; var nb = mb.normals;
                var ia = ma.triangles; var ib = mb.triangles;
                if (ia.Length != ib.Length) { Result.problems.Add("Triangle count changed: " + label); return; }
                var toA = copies[0].transform.worldToLocalMatrix * a.transform.localToWorldMatrix;
                var toB = copies[1].transform.worldToLocalMatrix * b.transform.localToWorldMatrix;
                float error = 0, normalError = 0;
                bool invalid = false;
                var normalsA = toA.inverse.transpose; var normalsB = toB.inverse.transpose;
                for (int i = 0; i < ia.Length; i++)
                {
                    float distance = Vector3.Distance(toA.MultiplyPoint3x4(va[ia[i]]), toB.MultiplyPoint3x4(vb[ib[i]]));
                    invalid |= float.IsNaN(distance) || float.IsInfinity(distance); error = Mathf.Max(error, distance);
                    if (na.Length == va.Length && nb.Length == vb.Length)
                    {
                        float n = Vector3.Distance(normalsA.MultiplyVector(na[ia[i]]).normalized, normalsB.MultiplyVector(nb[ib[i]]).normalized);
                        invalid |= float.IsNaN(n) || float.IsInfinity(n); normalError = Mathf.Max(normalError, n);
                    }
                    else if (na.Length != nb.Length) invalid = true;
                }
                Result.maximumPositionError = Mathf.Max(Result.maximumPositionError, error); Result.geometrySamples++;
                Result.maximumNormalError = Mathf.Max(Result.maximumNormalError, normalError);
                if (invalid || error > .0002f || normalError > .002f) Result.problems.Add("Geometry differs: " + label + " / position " + error + " / normal " + normalError);
            }
            finally { Object.DestroyImmediate(ma); Object.DestroyImmediate(mb); }
        }

        public Texture2D Render(bool obfuscated)
        {
            var target = obfuscated ? Obfuscated : Baseline;
            if (target == null) return null;
            foreach (var copy in copies) copy.SetActive(copy == target);
            var bounds = new Bounds(target.transform.position, Vector3.zero);
            foreach (var renderer in target.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(renderer.bounds);
            var go = new GameObject("ABO diagnostic camera"); SceneManager.MoveGameObjectToScene(go, scene);
            var camera = go.AddComponent<Camera>(); camera.enabled = false; camera.scene = scene;
            camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
            var lightObject = new GameObject("ABO diagnostic light"); lightObject.transform.SetParent(go.transform, false);
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.17f, .17f, .17f, 1);
            camera.transform.position = bounds.center + Vector3.forward * Mathf.Max(1, bounds.extents.magnitude * 2.8f); camera.transform.LookAt(bounds.center);
            var animator = target.GetComponent<Animator>();
            if (animator != null && animator.isHuman)
            {
                var head = animator.GetBoneTransform(HumanBodyBones.Head);
                var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                if (head != null && foot != null)
                {
                    var center = (head.position + foot.position) * .5f;
                    float height = Vector3.Distance(head.position, foot.position);
                    var left = animator.GetBoneTransform(HumanBodyBones.LeftHand); var right = animator.GetBoneTransform(HumanBodyBones.RightHand);
                    camera.orthographic = true;
                    camera.orthographicSize = Mathf.Max(.5f, height * .75f, left != null && right != null ? Vector3.Distance(left.position, right.position) * .6f : 0);
                    camera.transform.position = center + Vector3.forward * Mathf.Max(2, height * 3); camera.transform.LookAt(center);
                }
            }
            camera.nearClipPlane = .01f; camera.farClipPlane = 100;
            var render = RenderTexture.GetTemporary(512, 512, 24); var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = render; camera.Render(); RenderTexture.active = render;
                var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false); texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); texture.Apply(); transient.Add(texture); return texture;
            }
            finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(render); Object.DestroyImmediate(go); target.SetActive(false); }
        }

        public void Dispose()
        {
            if (active == this) active = null;
            foreach (var item in transient) if (item != null) Object.DestroyImmediate(item); transient.Clear();
            foreach (var copy in copies) BuildAuditCompletion.Forget(copy);
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            copies.Clear(); sites.Clear();
            if (!string.IsNullOrEmpty(generatedPath) && generatedPath.StartsWith("Packages/nadena.dev.ndmf/__Generated/AvatarBuildObfuscatorDiagnostics_", StringComparison.Ordinal))
                AssetDatabase.DeleteAsset(generatedPath);
            generatedPath = null;
        }
    }
}
