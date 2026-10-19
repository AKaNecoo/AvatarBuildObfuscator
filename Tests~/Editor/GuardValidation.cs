using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kiine.AvatarGuard;
using Kiine.AvatarGuard.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace Kiine.AvatarGuard.Validation
{
    /// <summary>Opt-in Editor validation. Creates synthetic fixtures only under Folder.</summary>
    public static class GuardValidation
    {
        public const string Folder = "Assets/__KiineGuardValidation_20261019";
        [Serializable] public sealed class Case { public string name, status, detail; }
        [Serializable] public sealed class Results { public List<Case> cases = new List<Case>(); public int passed, failed; }
        private static readonly List<Object> cleanup = new List<Object>();
        private static Results results;

        public static string RunAll(string output)
        {
            Directory.CreateDirectory(Folder + "/Fixtures"); AssetDatabase.Refresh();
            results = new Results();
            Run("Chinese prefix and invalid separator sanitization", () => {
                Check(GuardSettings.SanitizePrefix("一个木禾") == "一个木禾", "Chinese prefix changed");
                Check(GuardSettings.SanitizePrefix("木/禾\\名\n") == "木_禾_名", "Unsafe separators survived");
            });
            foreach (float weight in new[] { 0f, 35f, 100f })
            {
                float captured = weight;
                Run("Static bake geometry and dynamic curve at " + weight, () => BakeGeometry(captured));
            }
            Run("Conservative retains world-driven MMD names", () => ModeTest(ProtectionMode.Conservative));
            Run("Aggressive processes unreferenced MMD and keeps animated MMD", () => ModeTest(ProtectionMode.Aggressive));
            Run("Native visemes survive rename", NativeVisemes);
            Run("Eyelid indices remap after static shape removal", EyelidIndices);
            Run("Nested animator relative bindings", NestedAnimator);
            Run("Shared controller used by two animator roots", SharedController);
            Run("Persistent controller and BlendTree stay unchanged after forced save", PersistentGraph);
            Run("Synced layer motion and behaviour overrides survive deep copy", SyncedLayers);
            Run("AnimatorOverrideController survives and rewrites override clip", OverrideController);
            Run("Constant animation curves remain dynamic", ConstantCurve);
            Run("Manual preserve and renderer exclusion", ManualPreserve);
            Run("Animated mesh swaps retain shapes and copy object curves", MeshSwap);
            Run("Special frames and extrapolated weights are kept", SpecialFrames);
            Run("SDK proxy animation identity is retained", ProxyIdentity);
            Run("Unknown component disables shape editing", UnknownComponent);
            Run("Renaming disabled still isolates and rewrites shape animation", RenameAssetsDisabled);
            Run("Cyclic expression menus remain cyclic copies", CyclicMenus);
            Run("Unreadable textures retain original identity and pixels", UnreadableTexture);
            foreach (var format in new[] { TextureFormat.RGBA32, TextureFormat.DXT1, TextureFormat.DXT5, TextureFormat.BC7 })
            {
                var captured = format;
                Run("Readable texture pixel integrity " + format, () => TextureIntegrity(captured));
            }
            File.WriteAllText(output, JsonUtility.ToJson(results, true));
            return JsonUtility.ToJson(results, true);
        }

        private static void Run(string name, Action action)
        {
            var c = new Case { name = name };
            var nativeErrors = new List<string>();
            Application.LogCallback listener = (message, stack, type) => {
                if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception) nativeErrors.Add(message);
            };
            Application.logMessageReceived += listener;
            try { action(); Check(nativeErrors.Count == 0, "Unity native errors: " + string.Join("; ", nativeErrors.Take(3))); c.status = "PASS"; results.passed++; }
            catch (Exception e) { c.status = "FAIL"; c.detail = e.ToString(); results.failed++; }
            finally
            {
                Application.logMessageReceived -= listener;
                foreach (var item in cleanup.AsEnumerable().Reverse())
                    if (item != null && !EditorUtility.IsPersistent(item)) Object.DestroyImmediate(item);
                cleanup.Clear();
            }
            results.cases.Add(c);
        }

        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
        private static T Track<T>(T value) where T : Object { cleanup.Add(value); return value; }
        private static GameObject Root()
        {
            var root = Track(new GameObject("GuardFixture")); root.AddComponent<AvatarGuard>(); return root;
        }
        private static Mesh Mesh(params string[] names)
        {
            var mesh = Track(new Mesh { name = "OriginalMesh" });
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            mesh.normals = Enumerable.Repeat(Vector3.forward, 3).ToArray();
            mesh.tangents = Enumerable.Repeat(new Vector4(1, 0, 0, 1), 3).ToArray();
            mesh.triangles = new[] { 0, 1, 2 };
            foreach (var name in names)
                mesh.AddBlendShapeFrame(name, 100,
                    Enumerable.Repeat(new Vector3(.1f, .03f, .02f), 3).ToArray(),
                    Enumerable.Repeat(new Vector3(.01f, .02f, .03f), 3).ToArray(), new Vector3[3]);
            return mesh;
        }
        private static SkinnedMeshRenderer Renderer(GameObject parent, string name, Mesh mesh)
        {
            var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
            var renderer = go.AddComponent<SkinnedMeshRenderer>(); renderer.sharedMesh = mesh; return renderer;
        }
        private static AnimationClip Clip(string path, string shape, string name = "OriginalClip")
        {
            var clip = Track(new AnimationClip { name = name });
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), "blendShape." + shape),
                AnimationCurve.Linear(0, 0, 1, 50));
            return clip;
        }
        private static AnimatorController Controller(GameObject go, AnimationClip clip)
        {
            var controller = Track(new AnimatorController { name = "OriginalController" });
            controller.AddLayer("Layer"); controller.layers[0].stateMachine.AddState("State").motion = clip;
            var animator = go.GetComponent<Animator>();
            if (animator == null) animator = go.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            return controller;
        }
        private static GuardReport Process(GameObject root, ProtectionMode mode = ProtectionMode.Conservative)
        {
            root.GetComponent<AvatarGuard>().mode = mode;
            return new GuardProcessor(root, new GuardSettings(root.GetComponent<AvatarGuard>()), x => Track(x)).Run();
        }
        private static Mesh Baked(SkinnedMeshRenderer renderer)
        { var mesh = Track(new Mesh()); renderer.BakeMesh(mesh); return mesh; }
        private static float Difference(Vector3[] a, Vector3[] b)
        { Check(a.Length == b.Length, "Vertex count changed"); return a.Zip(b, (x, y) => Vector3.Distance(x, y)).DefaultIfEmpty(0).Max(); }

        private static void BakeGeometry(float weight)
        {
            var root = Root(); var source = Mesh("FaceTune", "ExpressionDrive", "Unused");
            var renderer = Renderer(root, "Body", source);
            renderer.SetBlendShapeWeight(0, weight); renderer.SetBlendShapeWeight(1, 25);
            var clip = Clip("Body", "ExpressionDrive"); Controller(root, clip);
            var before = Baked(renderer); var report = Process(root); var after = Baked(renderer);
            Check(report.FrozenShapes == 2 && renderer.sharedMesh.blendShapeCount == 1, report.Summary);
            Check(Difference(before.vertices, after.vertices) < .00001f, "Baked vertices changed");
            Check(Difference(before.normals, after.normals) < .00001f, "Baked normals changed");
            Check(source.blendShapeCount == 3 && source.vertices[0] == Vector3.zero, "Source mesh mutated");
            var output = root.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
            Check(output != clip, "Clip shared with source");
            var name = renderer.sharedMesh.GetBlendShapeName(0);
            Check(name.StartsWith("一个木禾_"), "Chinese shape prefix missing");
            Check(AnimationUtility.GetCurveBindings(output).Single().propertyName == "blendShape." + name, "Curve not rewritten");
            Check(AnimationUtility.GetCurveBindings(clip).Single().propertyName == "blendShape.ExpressionDrive", "Source curve mutated");
        }
        private static void ModeTest(ProtectionMode mode)
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh("あ", "笑い"));
            Controller(root, Clip("Body", "笑い")); var report = Process(root, mode);
            if (mode == ProtectionMode.Conservative)
                Check(r.sharedMesh.blendShapeCount == 2 && r.sharedMesh.GetBlendShapeName(0) == "あ" && report.RenamedShapes == 0, report.Summary);
            else Check(r.sharedMesh.blendShapeCount == 1 && report.FrozenShapes == 1 && report.RenamedShapes == 1, report.Summary);
        }
        private static void NativeVisemes()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh("Unused", "NativeMouth"));
            var d = root.AddComponent<VRCAvatarDescriptor>(); d.VisemeSkinnedMesh = r;
            d.VisemeBlendShapes = new[] { "NativeMouth" }; d.MouthOpenBlendShapeName = "NativeMouth";
            var report = Process(root);
            Check(report.FrozenShapes == 1 && r.sharedMesh.GetBlendShapeIndex(d.VisemeBlendShapes[0]) == 0, "Viseme removed or stale name");
            Check(d.MouthOpenBlendShapeName == d.VisemeBlendShapes[0], "Jaw reference not remapped");
        }
        private static void EyelidIndices()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh("Unused", "NativeEyelid"));
            var d = root.AddComponent<VRCAvatarDescriptor>();
            var eye = d.customEyeLookSettings; eye.eyelidsSkinnedMesh = r; eye.eyelidsBlendshapes = new[] { 1, 1, 1 }; d.customEyeLookSettings = eye;
            Process(root); Check(r.sharedMesh.blendShapeCount == 1 && d.customEyeLookSettings.eyelidsBlendshapes.All(i => i == 0), "Stale eyelid index");
        }
        private static void NestedAnimator()
        {
            var root = Root(); var nested = new GameObject("Nested"); nested.transform.SetParent(root.transform);
            var r = Renderer(nested, "Body", Mesh("Driven")); Controller(nested, Clip("Body", "Driven"));
            var report = Process(root);
            Check(report.FrozenShapes == 0 && r.sharedMesh.blendShapeCount == 1, "Nested animated shape removed");
            var clip = nested.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
            Check(AnimationUtility.GetCurveBindings(clip).Single().propertyName == "blendShape." + r.sharedMesh.GetBlendShapeName(0), "Wrong relative root");
        }
        private static void SharedController()
        {
            var root = Root(); var a = new GameObject("A"); var b = new GameObject("B");
            a.transform.SetParent(root.transform); b.transform.SetParent(root.transform);
            var ra = Renderer(a, "Body", Mesh("Driven")); var rb = Renderer(b, "Body", Mesh("Driven"));
            var controller = Controller(a, Clip("Body", "Driven")); b.AddComponent<Animator>().runtimeAnimatorController = controller;
            Process(root);
            var ca = a.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
            var cb = b.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
            Check(ca != cb, "Context-dependent clips incorrectly shared");
            Check(AnimationUtility.GetCurveBindings(ca).Single().propertyName == "blendShape." + ra.sharedMesh.GetBlendShapeName(0), "A binding wrong");
            Check(AnimationUtility.GetCurveBindings(cb).Single().propertyName == "blendShape." + rb.sharedMesh.GetBlendShapeName(0), "B binding wrong");
        }
        private static void PersistentGraph()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh("Driven"));
            string path = Folder + "/Fixtures/Persistent_" + Guid.NewGuid().ToString("N") + ".controller";
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var clip = Clip("Body", "Driven"); AssetDatabase.AddObjectToAsset(clip, controller);
            var tree = new BlendTree { name = "SourceTree", blendParameter = "Blend" };
            AssetDatabase.AddObjectToAsset(tree, controller); tree.AddChild(clip, 0);
            controller.AddParameter("IsAnimatorEnabled", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Blend", AnimatorControllerParameterType.Float);
            controller.layers[0].stateMachine.AddState("State").motion = tree;
            root.AddComponent<Animator>().runtimeAnimatorController = controller;
            AssetDatabase.SaveAssets(); var before = File.ReadAllBytes(path);
            Process(root);
            var copied = (AnimatorController)root.GetComponent<Animator>().runtimeAnimatorController;
            Check(copied != controller && copied.layers[0].stateMachine != controller.layers[0].stateMachine, "Persistent graph shared");
            Check(copied.layers[0].stateMachine.states[0].state.motion != tree, "Persistent BlendTree shared");
            Check(tree.children[0].motion == clip && !EditorUtility.IsDirty(tree), "Persistent tree changed or dirty");
            Check(copied.parameters.Any(p => p.name == "IsAnimatorEnabled" && p.type == AnimatorControllerParameterType.Bool), "Runtime parameter renamed");
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) AssetDatabase.SaveAssetIfDirty(asset);
            Check(before.SequenceEqual(File.ReadAllBytes(path)), "Forced save changed source bytes");
        }
        private static void SyncedLayers()
        {
            var root = Root(); Renderer(root, "Body", Mesh("Driven"));
            var controller = Controller(root, Clip("Body", "Driven")); controller.AddLayer("Synced");
            var layers = controller.layers; layers[1].syncedLayerIndex = 0;
            var state = layers[0].stateMachine.states[0].state;
            var over = Clip("Body", "Driven", "OverrideMotion");
            var behaviour = Track(ScriptableObject.CreateInstance<VRCAnimatorLayerControl>());
            layers[1].SetOverrideMotion(state, over); layers[1].SetOverrideBehaviours(state, new StateMachineBehaviour[] { behaviour });
            controller.layers = layers;
            Process(root);
            var copy = (AnimatorController)root.GetComponent<Animator>().runtimeAnimatorController;
            var copiedState = copy.layers[0].stateMachine.states[0].state;
            Check(copy.layers[1].GetOverrideMotion(copiedState) != null && copy.layers[1].GetOverrideMotion(copiedState) != over, "Synced motion lost or shared");
            var behaviours = copy.layers[1].GetOverrideBehaviours(copiedState);
            Check(behaviours != null && behaviours.Length == 1 && behaviours[0] != behaviour, "Synced behaviours lost or shared");
        }
        private static void OverrideController()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh("Driven"));
            var basic = Clip("Body", "Driven"); var controller = Controller(root, basic);
            var over = Clip("Body", "Driven", "Override");
            var aoc = Track(new AnimatorOverrideController(controller)); aoc[basic.name] = over;
            root.GetComponent<Animator>().runtimeAnimatorController = aoc; Process(root);
            var copy = root.GetComponent<Animator>().runtimeAnimatorController as AnimatorOverrideController;
            Check(copy != null && copy != aoc && copy.runtimeAnimatorController != controller, "Override controller not deep copied");
            Check(copy.animationClips.All(c => AnimationUtility.GetCurveBindings(c).All(b => b.propertyName == "blendShape." + r.sharedMesh.GetBlendShapeName(0))), "Override binding stale");
        }
        private static void ConstantCurve()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh("ConstantDriven"));
            var clip = Clip("Body", "ConstantDriven");
            AnimationUtility.SetEditorCurve(clip, AnimationUtility.GetCurveBindings(clip).Single(), AnimationCurve.Constant(0, 1, 0));
            Controller(root, clip); var report = Process(root); Check(report.FrozenShapes == 0 && r.sharedMesh.blendShapeCount == 1, "Constant curve shape removed");
        }
        private static void ManualPreserve()
        {
            var root = Root(); var a = Renderer(root, "A", Mesh("Keep")); var b = Renderer(root, "B", Mesh("Other"));
            var marker = root.GetComponent<AvatarGuard>(); marker.preserveBlendShapes = new[] { "Keep" }; marker.excludedRenderers = new[] { b };
            var report = Process(root, ProtectionMode.Aggressive);
            Check(report.FrozenShapes == 0 && report.RenamedShapes == 0 && a.sharedMesh.GetBlendShapeName(0) == "Keep" && b.sharedMesh.GetBlendShapeName(0) == "Other", "Manual keep ignored");
        }
        private static void MeshSwap()
        {
            var root = Root(); var source = Mesh("Swapped"); var r = Renderer(root, "Body", source);
            var clip = Track(new AnimationClip());
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("Body", typeof(SkinnedMeshRenderer), "m_Mesh"),
                new[] { new ObjectReferenceKeyframe { time = 0, value = source } });
            Controller(root, clip); var report = Process(root);
            Check(report.FrozenShapes == 0 && report.RenamedShapes == 0 && r.sharedMesh.blendShapeCount == 1, "Swapped mesh shape edited");
            var copiedClip = root.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
            var value = AnimationUtility.GetObjectReferenceCurve(copiedClip, AnimationUtility.GetObjectReferenceCurveBindings(copiedClip).Single())[0].value;
            Check(value == r.sharedMesh && value != source, "Mesh object curve not consistently remapped");
        }
        private static void SpecialFrames()
        {
            var root = Root(); var mesh = Mesh("Extrapolated");
            mesh.AddBlendShapeFrame("InBetween", 50, new Vector3[3], new Vector3[3], new Vector3[3]);
            mesh.AddBlendShapeFrame("InBetween", 100, new Vector3[3], new Vector3[3], new Vector3[3]);
            var r = Renderer(root, "Body", mesh); r.SetBlendShapeWeight(0, -20);
            var report = Process(root, ProtectionMode.Aggressive);
            Check(report.FrozenShapes == 0 && r.sharedMesh.blendShapeCount == 2, "Special data removed");
        }
        private static void ProxyIdentity()
        {
            var root = Root(); var clip = Track(new AnimationClip { name = "proxy_test" }); Controller(root, clip);
            Process(root); Check(root.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single() == clip, "Proxy clip replaced");
        }
        private static void UnknownComponent()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh("CouldBeDriven")); root.AddComponent<UnknownShapeDriver>();
            var report = Process(root, ProtectionMode.Aggressive);
            Check(report.FrozenShapes == 0 && report.RenamedShapes == 0 && r.sharedMesh.blendShapeCount == 1, "Unknown driver shape edited");
        }
        private static void RenameAssetsDisabled()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh("Driven"));
            var source = Clip("Body", "Driven"); var controller = Controller(root, source);
            root.GetComponent<AvatarGuard>().renameAssets = false; var report = Process(root);
            var copied = root.GetComponent<Animator>().runtimeAnimatorController;
            Check(report.RenamedAssets == 0 && copied != controller && copied.name == controller.name, "Asset rename flag ignored");
            Check(AnimationUtility.GetCurveBindings(copied.animationClips.Single()).Single().propertyName == "blendShape." + r.sharedMesh.GetBlendShapeName(0), "Binding stale without asset rename");
            Check(controller.layers[0].stateMachine.states[0].state.motion == source, "Source state modified");
        }
        private static void CyclicMenus()
        {
            var root = Root(); var descriptor = root.AddComponent<VRCAvatarDescriptor>();
            var a = Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>()); var b = Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>());
            a.name = "OriginalMenuA"; b.name = "OriginalMenuB";
            a.controls.Add(new VRCExpressionsMenu.Control { name = "Visible menu label", type = VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu = b });
            b.controls.Add(new VRCExpressionsMenu.Control { type = VRCExpressionsMenu.Control.ControlType.SubMenu, subMenu = a });
            descriptor.expressionsMenu = a; Process(root);
            var result = descriptor.expressionsMenu;
            Check(result != a && result.controls[0].subMenu != b && result.controls[0].subMenu.controls[0].subMenu == result, "Menu cycle broken");
            Check(result.controls[0].name == "Visible menu label", "User-facing label changed");
        }
        private static byte[] Pixels(Texture source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            var previous = RenderTexture.active; var cpu = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            try { Graphics.Blit(source, rt); RenderTexture.active = rt; cpu.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); cpu.Apply(); return cpu.GetRawTextureData(); }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); Object.DestroyImmediate(cpu); }
        }
        private static void TextureIntegrity(TextureFormat format)
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh());
            var input = Track(new Texture2D(32, 32, TextureFormat.RGBA32, true));
            input.SetPixels(Enumerable.Range(0, 1024).Select(i => new Color((i % 32) / 31f, (i / 32) / 31f, .5f, 1)).ToArray()); input.Apply();
            if (format != TextureFormat.RGBA32) EditorUtility.CompressTexture(input, format, TextureCompressionQuality.Normal);
            input.Apply(false, false); var before = Pixels(input);
            var material = Track(new Material(Shader.Find("Unlit/Texture"))); material.mainTexture = input; r.sharedMaterial = material;
            Process(root); var output = r.sharedMaterial.mainTexture;
            Check(output != input && output.name.StartsWith("一个木禾_"), "Texture not renamed on a copy");
            Check(before.SequenceEqual(Pixels(output)), "GPU pixel content changed");
            Check(before.SequenceEqual(Pixels(input)), "Source texture changed");
        }

        private static void UnreadableTexture()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh());
            var texture = Track(new Texture2D(8, 8, TextureFormat.RGBA32, false));
            texture.SetPixels(Enumerable.Repeat(Color.magenta, 64).ToArray()); texture.Apply(false, true);
            Check(!texture.isReadable, "Fixture should be unreadable"); var before = Pixels(texture);
            var material = Track(new Material(Shader.Find("Unlit/Texture"))); material.mainTexture = texture; r.sharedMaterial = material;
            var report = Process(root);
            Check(r.sharedMaterial != material && r.sharedMaterial.mainTexture == texture, "Unreadable texture copied");
            Check(before.SequenceEqual(Pixels(texture)) && report.Notes.Any(x => x.Contains("不可读")), "Unreadable skip not reported or source pixels changed");
        }
    }
}
