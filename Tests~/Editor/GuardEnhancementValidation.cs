// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.IO;
using System.Linq;
using AvatarTools.Obfuscation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        public static string RunEnhanced(string output)
        {
            Directory.CreateDirectory(Folder + "/Fixtures"); AssetDatabase.Refresh();
            results = new Results();
            Run("New defaults enable audit/randomization/notice, keep experiments off", NewDefaults);
            Run("Random builds differ without mutating source settings", RandomBuilds);
            Run("Deterministic builds keep names and fingerprint repeatable", RepeatableBuilds);
            Run("Authorization tags isolate source material even with asset naming off", MetadataIsolation);
            Run("Disabling metadata adds no declaration or fingerprint objects", MetadataDisabled);
            Run("Fingerprint-only builds carry no authorization claim", FingerprintOnly);
            Run("Notice-only builds use one independent named metadata subtree", NoticeOnly);
            Run("Metadata subtree preserves original objects in both protection modes", MetadataSubtreeIsolation);
            Run("SDK completion audit sees later source references and names", LateAudit);
            Run("Animated zero-delta shapes and curves are removed with source isolation", () => EmptyShape(false, false));
            Run("Multiple zero-delta frames can be removed", () => EmptyShape(true, false));
            Run("Normal-only deltas remain meaningful", () => EmptyShape(false, true));
            Run("Tiny nonzero deltas are never approximated to zero", TinyShape);
            Run("Native viseme zero shapes retain their contracts", NativeEmptyShape);
            Run("Manual zero shapes and SDK proxy-bound shapes retain names", PreservedEmptyShapes);
            Run("Equal dynamic curves merge without changing sampled geometry", () => SynchronizedShapes("equal"));
            Run("Different initial weights prevent dynamic merging", () => SynchronizedShapes("weights"));
            Run("Different animation curves prevent dynamic merging", () => SynchronizedShapes("curves"));
            Run("A missing curve in another state prevents dynamic merging", () => SynchronizedShapes("missing"));
            Run("Native driven shapes never merge", () => SynchronizedShapes("native"));
            Run("Manually preserved shapes never merge", () => SynchronizedShapes("manual"));
            foreach (var wrap in new[] { TextureWrapMode.Repeat, TextureWrapMode.Clamp, TextureWrapMode.Mirror })
            {
                var mode = wrap;
                Run("UV texture rotation preserves GPU output and mips: " + wrap, () => UvRotation(mode));
            }
            Run("Custom shaders retain UV/texture data", () => UvUnsupported("shader"));
            Run("Unreadable textures retain UV/texture data", () => UvUnsupported("unreadable"));
            Run("Animated material coordinates prevent UV rotation", () => UvUnsupported("animation"));
            Run("Material sharing across different meshes prevents UV rotation", () => UvUnsupported("sharing"));
            Run("MirrorOnce sampling remains unchanged", () => UvUnsupported("mirror-once"));
            Run("Renderer property-block coordinates prevent UV rotation", () => UvUnsupported("property-block"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(results, true));
            return JsonUtility.ToJson(results, true);
        }

        private static void NewDefaults()
        {
            var marker = Track(new GameObject("Defaults")).AddComponent<AvatarBuildObfuscator>();
            Check(marker.randomizeEachBuild && marker.auditBuild && marker.pruneEmptyBlendShapes && marker.embedOwnershipNotice && marker.embedBuildFingerprint, "New protections default off");
            Check(!marker.experimentalUvTransform && !marker.mergeSynchronizedBlendShapes, "Experiments default on");
            Check(marker.rightsHolder == "" && marker.rightsContact == "", "Invented owner/contact");
        }

        private static void RandomBuilds()
        {
            var a = Root(); var b = Root();
            var ma = a.GetComponent<AvatarBuildObfuscator>(); var mb = b.GetComponent<AvatarBuildObfuscator>();
            ma.randomizeEachBuild = mb.randomizeEachBuild = true;
            var ra = Renderer(a, "Body", Mesh("A")); var rb = Renderer(b, "Body", Mesh("A"));
            string before = EditorJsonUtility.ToJson(ma);
            var ar = Process(a); var br = Process(b);
            Check(ra.sharedMesh.name != rb.sharedMesh.name && ar.BuildFingerprint != br.BuildFingerprint, "Build salt ignored");
            Check(EditorJsonUtility.ToJson(ma) == before, "Build wrote a seed into the source component");
        }

        private static void RepeatableBuilds()
        {
            var a = Root(); var b = Root(); var ra = Renderer(a, "Body", Mesh("A")); var rb = Renderer(b, "Body", Mesh("A"));
            var ar = Process(a); var br = Process(b);
            Check(ra.sharedMesh.name == rb.sharedMesh.name && ar.BuildFingerprint == br.BuildFingerprint, "Deterministic mode varies");
        }

        private static void MetadataIsolation()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh());
            var source = Track(new Material(Shader.Find("Unlit/Color")) { name = "Original material" }); r.sharedMaterial = source;
            var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.renameAssets = false;
            marker.embedOwnershipNotice = marker.embedBuildFingerprint = true;
            marker.rightsHolder = "Example holder"; marker.rightsContact = "https://example.org";
            marker.usageRestrictions = "No redistribution";
            var report = Process(root); var copy = r.sharedMaterial;
            Check(copy != source && source.GetTag(GuardProcessor.NoticeTag, false, "") == "", "Source material tagged");
            string notice = copy.GetTag(GuardProcessor.NoticeTag, false, "");
            Check(notice.Contains(OwnershipNotice.Chinese) && notice.Contains(OwnershipNotice.English) && notice.Contains(OwnershipNotice.Japanese), "Three-language declaration missing");
            Check(notice.Contains("Example holder") && notice.Contains("No redistribution") && notice.Contains("has not verified"), "Owner declaration misrepresented");
            Check(copy.GetTag(GuardProcessor.FingerprintTag, false, "") == report.BuildFingerprint, "Material fingerprint missing");
            Check(root.GetComponentsInChildren<Transform>(true).Any(t => t.name.Contains(report.BuildFingerprint)), "Hierarchy fingerprint missing");
            Check(!notice.Contains("Assets/") && !notice.Contains("Library/"), "Private source map leaked");
        }

        private static void MetadataDisabled()
        {
            var root = Root(); Renderer(root, "Body", Mesh()); Process(root);
            Check(root.GetComponentsInChildren<Transform>(true).Length == 2, "Unexpected metadata objects");
        }

        private static void FingerprintOnly()
        {
            var root = Root(); root.GetComponent<AvatarBuildObfuscator>().embedBuildFingerprint = true;
            Renderer(root, "Body", Mesh()); Process(root);
            var fingerprint = root.GetComponentsInChildren<Transform>(true).Single(t => t.name.StartsWith("BUILD_FINGERPRINT_", StringComparison.Ordinal));
            Check(root.GetComponentsInChildren<Transform>(true).Length == 4 && fingerprint.parent.parent == root.transform &&
                UsernameNameGenerator.IsGeneratedName(fingerprint.parent.name) &&
                !root.GetComponentsInChildren<Transform>(true).Any(t => t.name.Contains("AUTHORIZATION_NOTICE")), "Notice added despite being disabled");
        }

        private static void NoticeOnly()
        {
            var root = Root(); root.GetComponent<AvatarBuildObfuscator>().embedOwnershipNotice = true;
            Renderer(root, "Body", Mesh()); Process(root);
            var notice = root.GetComponentsInChildren<Transform>(true).Single(t => t.name == OwnershipNotice.Chinese);
            Check(root.GetComponentsInChildren<Transform>(true).Length == 6 && notice.parent.parent == root.transform &&
                notice.parent.childCount == 3 && UsernameNameGenerator.IsGeneratedName(notice.parent.name), "Notice container missing or extra fingerprint added");
        }

        private static void MetadataSubtreeIsolation()
        {
            foreach (var mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
                foreach (bool rename in new[] { false, true })
                {
                    var source = Root(); var marker = source.GetComponent<AvatarBuildObfuscator>();
                    marker.renameGameObjects = rename; marker.embedOwnershipNotice = marker.embedBuildFingerprint = true;
                    marker.rightsHolder = "Example holder"; marker.auditBuild = true;
                    var sourceRenderer = Renderer(source, "Body", Mesh());
                    sourceRenderer.transform.localPosition = new Vector3(1, 2, 3);
                    sourceRenderer.transform.localRotation = Quaternion.Euler(5, 15, 25);
                    sourceRenderer.transform.localScale = new Vector3(.8f, 1.2f, 1);
                    string sourceSettings = EditorJsonUtility.ToJson(marker);
                    var build = Track(Object.Instantiate(source)); var renderer = build.GetComponentInChildren<SkinnedMeshRenderer>();
                    var position = renderer.transform.localPosition; var rotation = renderer.transform.localRotation; var scale = renderer.transform.localScale;
                    var report = Process(build, mode);
                    var english = build.GetComponentsInChildren<Transform>(true).Single(t => t.name.StartsWith("AUTHORIZATION_NOTICE: ", StringComparison.Ordinal));
                    var container = english.parent;
                    Check(container.parent == build.transform && UsernameNameGenerator.IsGeneratedName(container.name) &&
                        container.name.StartsWith("Synthetic_User_", StringComparison.Ordinal) && container.childCount == 5, "Metadata is not grouped beneath an ordinary generated name");
                    Check(build.transform.childCount == 2 && container != renderer.transform && container.localPosition == Vector3.zero &&
                        container.localRotation == Quaternion.identity && container.localScale == Vector3.one, "Container alters functional hierarchy or transforms");
                    var leaves = container.GetComponentsInChildren<Transform>(true);
                    Check(leaves.All(t => t.GetComponents<Component>().Length == 1) && leaves.All(t => t.gameObject.hideFlags == HideFlags.None), "Metadata has runtime components or unsafe hide flags");
                    Check(leaves.Any(t => t.name == OwnershipNotice.Chinese) && leaves.Any(t => t.name == OwnershipNotice.Japanese) &&
                        leaves.Any(t => t.name == "BUILD_FINGERPRINT_" + report.BuildFingerprint) && leaves.Any(t => t.name == "DECLARED_RIGHTS_HOLDER: Example holder"), "Readable metadata lost");
                    Check(renderer.transform.parent == build.transform && renderer.transform.localPosition == position && renderer.transform.localRotation == rotation &&
                        renderer.transform.localScale == scale && source.transform.childCount == 1 && sourceRenderer.name == "Body" &&
                        EditorJsonUtility.ToJson(marker) == sourceSettings, "Source or functional object modified");
                    Check(!report.AuditEntries.Any(e => e.category == "retained-object-name" && leaves.Any(t =>
                        AnimationUtility.CalculateTransformPath(t, build.transform) == e.path)), "Intentional metadata misreported as a retained original name");
                }
        }

        private static void LateAudit()
        {
            string path = Folder + "/Fixtures/EnhancedSource.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Unlit/Color")); AssetDatabase.CreateAsset(material, path); }
            AssetDatabase.SaveAssetIfDirty(material); var bytes = File.ReadAllBytes(path);
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.auditBuild = true;
            var r = Renderer(root, "Body", Mesh()); r.sharedMaterial = material;
            var report = Process(root); Check(report.AuditStage == "ndmf-pass", "Wrong manual audit stage");
            r.name = "Body"; r.sharedMaterial = material;
            new BuildAuditCompletion().OnPreprocessAvatar(root);
            Check(report.AuditStage == "sdk-preprocess" && report.AuditEntries.Any(e => e.category == "source-resource-reference" && e.path == path), "Late reference invisible to audit");
            Check(report.AuditEntries.Any(e => e.category == "retained-object-name" && e.name == "Body"), "Late original name invisible");
            AssetDatabase.SaveAssetIfDirty(material); Check(bytes.SequenceEqual(File.ReadAllBytes(path)), "Audit changed source bytes");
        }

        private static Mesh ZeroMesh(bool multiple, Vector3 normal, Vector3 position)
        {
            var mesh = Mesh();
            if (multiple) mesh.AddBlendShapeFrame("Empty", 50, Enumerable.Repeat(position, 3).ToArray(), Enumerable.Repeat(normal, 3).ToArray(), new Vector3[3]);
            mesh.AddBlendShapeFrame("Empty", 100, Enumerable.Repeat(position, 3).ToArray(), Enumerable.Repeat(normal, 3).ToArray(), new Vector3[3]);
            return mesh;
        }

        private static void EmptyShape(bool multiple, bool normalOnly)
        {
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.pruneEmptyBlendShapes = true; marker.freezeUnusedBlendShapes = false;
            var mesh = ZeroMesh(multiple, normalOnly ? Vector3.up * .1f : Vector3.zero, Vector3.zero);
            var r = Renderer(root, "Body", mesh); var clip = Clip("Body", "Empty"); Controller(root, clip);
            var before = Baked(r).vertices; var report = Process(root);
            Check(r.sharedMesh.blendShapeCount == (normalOnly ? 1 : 0), "Wrong zero-delta decision");
            Check(report.PrunedEmptyShapes == (normalOnly ? 0 : 1), "Wrong prune count");
            Check(AnimationUtility.GetCurveBindings(root.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single()).Length == (normalOnly ? 1 : 0), "Curve removal differs from shape removal");
            Check(mesh.blendShapeCount == 1 && AnimationUtility.GetCurveBindings(clip).Length == 1, "Source shape/curve changed");
            Check(Difference(before, Baked(r).vertices) < 1e-7f, "Pruning changed positions");
        }

        private static void TinyShape()
        {
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.pruneEmptyBlendShapes = true; marker.freezeUnusedBlendShapes = false;
            var source = ZeroMesh(false, Vector3.zero, Vector3.right * .0001f);
            var delta = new Vector3[3]; source.GetBlendShapeFrameVertices(0, 0, delta, new Vector3[3], new Vector3[3]);
            Check(delta.Any(v => v.x != 0), "Native mesh API discarded the test input before processing");
            var r = Renderer(root, "Body", source);
            Check(Process(root).PrunedEmptyShapes == 0 && r.sharedMesh.blendShapeCount == 1, "Nonzero data lost");
        }

        private static void NativeEmptyShape()
        {
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.pruneEmptyBlendShapes = true;
            var r = Renderer(root, "Body", ZeroMesh(false, Vector3.zero, Vector3.zero));
            var d = root.AddComponent<VRCAvatarDescriptor>(); d.lipSync = VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
            d.VisemeSkinnedMesh = r; d.VisemeBlendShapes = new[] { "Empty" };
            Check(Process(root).PrunedEmptyShapes == 0 && r.sharedMesh.blendShapeCount == 1, "Viseme contract removed");
            Check(r.sharedMesh.GetBlendShapeIndex(d.VisemeBlendShapes[0]) >= 0, "Viseme rename stale");
        }

        private static void PreservedEmptyShapes()
        {
            foreach (bool proxy in new[] { false, true })
            {
                var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.pruneEmptyBlendShapes = true;
                var r = Renderer(root, "Body", ZeroMesh(false, Vector3.zero, Vector3.zero));
                if (proxy) Controller(root, Clip("Body", "Empty", "proxy_Fixed"));
                else marker.preserveBlendShapes = new[] { "Empty" };
                Check(Process(root).PrunedEmptyShapes == 0 && r.sharedMesh.GetBlendShapeIndex("Empty") == 0, "Preserved contract changed");
            }
        }

        private static void SynchronizedShapes(string variant)
        {
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mergeSynchronizedBlendShapes = true; marker.freezeUnusedBlendShapes = false;
            var mesh = Mesh("DriveLeft", "DriveRight"); var r = Renderer(root, "Body", mesh);
            r.SetBlendShapeWeight(0, 25); r.SetBlendShapeWeight(1, variant == "weights" ? 35 : 25);
            var clip = Clip("Body", "DriveLeft");
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.DriveRight"),
                AnimationCurve.Linear(0, 0, 1, variant == "curves" ? 60 : 50));
            var controller = Controller(root, clip);
            if (variant == "missing") controller.layers[0].stateMachine.AddState("Second").motion = Clip("Body", "DriveLeft", "Second");
            if (variant == "manual") marker.preserveBlendShapes = new[] { "DriveLeft" };
            if (variant == "native")
            {
                var d = root.AddComponent<VRCAvatarDescriptor>(); d.lipSync = VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
                d.VisemeSkinnedMesh = r; d.VisemeBlendShapes = new[] { "DriveLeft" };
            }
            var expected = Baked(r).vertices; var report = Process(root); bool merge = variant == "equal";
            Check(report.MergedShapePairs == (merge ? 1 : 0) && r.sharedMesh.blendShapeCount == (merge ? 1 : 2), "Wrong merge eligibility: " + variant);
            Check(Difference(expected, Baked(r).vertices) < 1e-6f, "Merging changed initial geometry");
            var mergedMesh = r.sharedMesh;
            if (merge)
                foreach (float weight in new[] { 0f, 13.5f, 50f, 100f })
                {
                    r.sharedMesh = mesh; r.SetBlendShapeWeight(0, weight); r.SetBlendShapeWeight(1, weight); var pose = Baked(r).vertices;
                    r.sharedMesh = mergedMesh;
                    r.SetBlendShapeWeight(0, weight); Check(Difference(pose, Baked(r).vertices) < 1e-6f, "Merged pose differs at " + weight);
                }
            Check(mesh.blendShapeCount == 2 && AnimationUtility.GetCurveBindings(clip).Length == 2, "Source blendshape basis changed");
        }

        private static Tuple<GameObject, SkinnedMeshRenderer, Material, Texture2D> UvFixture()
        {
            var root = Root(); root.GetComponent<AvatarBuildObfuscator>().experimentalUvTransform = true;
            var mesh = Mesh(); mesh.uv = new[] { new Vector2(.02f, .07f), new Vector2(.83f, .04f), new Vector2(.12f, .91f) };
            var r = Renderer(root, "Body", mesh);
            var texture = Track(new Texture2D(8, 8, TextureFormat.RGBA32, true, false));
            texture.SetPixels32(Enumerable.Range(0, 64).Select(i => new Color32((byte)(i * 3), (byte)(255 - i * 3), (byte)(i * 2), (byte)(128 + i))).ToArray());
            texture.Apply(); texture.filterMode = FilterMode.Bilinear;
            var material = Track(new Material(Shader.Find("Unlit/Texture"))); material.mainTexture = texture;
            material.mainTextureScale = new Vector2(1.25f, -.75f); material.mainTextureOffset = new Vector2(.13f, .27f);
            r.sharedMaterial = material;
            return Tuple.Create(root, r, material, texture);
        }

        private static void UvRotation(TextureWrapMode wrap, string shaderName = "Unlit/Texture")
        {
            var fixture = UvFixture(); var root = fixture.Item1; var r = fixture.Item2; var material = fixture.Item3; var source = fixture.Item4;
            material.shader = Shader.Find(shaderName);
            source.wrapMode = wrap; var originalUv = r.sharedMesh.uv;
            var mips = Enumerable.Range(0, source.mipmapCount).Select(i => source.GetPixelData<byte>(i).ToArray()).ToArray();
            var before = UvPixels(root); var report = Process(root); var texture = (Texture2D)r.sharedMaterial.mainTexture; var after = UvPixels(root);
            Check(report.TransformedUvMeshes == 1 && texture != source && r.sharedMaterial != material, "UV transform did not isolate data");
            Check(texture.format == source.format && texture.graphicsFormat == source.graphicsFormat && texture.mipmapCount == source.mipmapCount, "Texture format changed");
            for (int mip = 0; mip < source.mipmapCount; mip++)
            {
                var pixels = texture.GetPixelData<byte>(mip).ToArray(); var old = mips[mip];
                Check(old.SequenceEqual(source.GetPixelData<byte>(mip).ToArray()), "Source mip changed");
                for (int i = 0; i < old.Length; i++) Check(old[i] == pixels[(old.Length / 4 - 1 - i / 4) * 4 + i % 4], "Mip rotation is lossy");
            }
            Check(originalUv.Zip(r.sharedMesh.uv, (a, b) => Vector2.Distance(Vector2.one - a, b)).Max() < 1e-7f, "UV transform wrong");
            int max = before.Zip(after, (a, b) => new[] { Math.Abs(a.r - b.r), Math.Abs(a.g - b.g), Math.Abs(a.b - b.b), Math.Abs(a.a - b.a) }.Max()).Max();
            Check(max <= 2 && before.Any(c => c.r != before[0].r), "GPU output changed or blank: " + max);
        }

        private static void UvUnsupported(string reason)
        {
            var fixture = UvFixture(); var root = fixture.Item1; var r = fixture.Item2;
            if (reason == "manual") root.GetComponent<AvatarBuildObfuscator>().excludeUvRenderers = new Renderer[] { r };
            if (reason == "shader") r.sharedMaterial.shader = Shader.Find("Standard");
            if (reason == "unreadable") fixture.Item4.Apply(false, true);
            if (reason == "mirror-once") fixture.Item4.wrapModeU = TextureWrapMode.MirrorOnce;
            if (reason == "property-block")
            { var block = new MaterialPropertyBlock(); block.SetVector("_MainTex_ST", new Vector4(1, 1, .5f, .5f)); r.SetPropertyBlock(block); }
            if (reason == "animation")
            {
                var clip = Track(new AnimationClip()); AnimationUtility.SetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve("Body", typeof(SkinnedMeshRenderer), "material._MainTex_ST.x"), AnimationCurve.Linear(0, 0, 1, 1));
                Controller(root, clip);
            }
            if (reason == "sharing")
            { var second = Renderer(root, "Second", Object.Instantiate(r.sharedMesh)); Track(second.sharedMesh); second.sharedMaterial = fixture.Item3; }
            var uv = r.sharedMesh.uv; var report = Process(root);
            Check(report.TransformedUvMeshes == 0 && r.sharedMesh.uv.SequenceEqual(uv), "Unsupported UV contract changed: " + reason);
        }

        private static Color32[] UvPixels(GameObject root)
        {
            var scene = EditorSceneManager.NewPreviewScene(); var previous = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(128, 128, 24); var cpu = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var bakedMeshes = new System.Collections.Generic.List<Mesh>();
            try
            {
                var clone = Object.Instantiate(root); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone, scene);
                foreach (var t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                foreach (var skin in clone.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var baked = new Mesh(); skin.BakeMesh(baked); bakedMeshes.Add(baked);
                    var go = skin.gameObject; var mats = skin.sharedMaterials;
                    Object.DestroyImmediate(skin); go.AddComponent<MeshFilter>().sharedMesh = baked; go.AddComponent<MeshRenderer>().sharedMaterials = mats;
                }
                var cameraGo = new GameObject("Validation Camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, scene);
                var camera = cameraGo.AddComponent<Camera>(); camera.enabled = false; camera.cullingMask = 1 << 31;
                camera.scene = scene; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                camera.orthographic = true; camera.orthographicSize = .65f; camera.transform.position = new Vector3(.5f, .5f, 2); camera.transform.LookAt(new Vector3(.5f, .5f, 0));
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.targetTexture = rt;
                var lightGo = new GameObject("Validation Light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, scene); lightGo.AddComponent<Light>().type = LightType.Directional;
                camera.Render(); RenderTexture.active = rt; cpu.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); cpu.Apply(); return cpu.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(cpu); EditorSceneManager.ClosePreviewScene(scene);
                foreach (var mesh in bakedMeshes) Object.DestroyImmediate(mesh);
            }
        }
    }
}
