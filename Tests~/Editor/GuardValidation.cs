using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AvatarTools.Obfuscation;
using AvatarTools.Obfuscation.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Validation
{
    /// <summary>Opt-in Editor validation. Creates synthetic fixtures only under Folder.</summary>
    public static class GuardValidation
    {
        public const string Folder = "Assets/__AvatarBuildObfuscatorValidation_20261019";
        [Serializable] public sealed class Case { public string name, status, detail; }
        [Serializable] public sealed class Results { public List<Case> cases = new List<Case>(); public int passed, failed; }
        private static readonly List<Object> cleanup = new List<Object>();
        private static Results results;

        public static string RunAll(string output)
        {
            Directory.CreateDirectory(Folder + "/Fixtures"); AssetDatabase.Refresh();
            results = new Results();
            Run("Same username generates the same username-prefixed Il sequence", () => {
                var a = new UsernameNameGenerator("示例用户"); var b = new UsernameNameGenerator("示例用户");
                var names = new HashSet<string>();
                for (int i = 0; i < 2048; i++) {
                    string value = a.Next();
                    Check(value == b.Next() && UsernameNameGenerator.IsGeneratedName(value), "Sequence not deterministic or unsafe name");
                    Check(names.Add(value) && value.StartsWith("示例用户_", StringComparison.Ordinal), "Duplicate name or missing username");
                    Check(value.Substring(value.Length - 128).All(c => c == 'I' || c == 'l'), "Suffix is not Il obfuscation");
                }
            });
            Run("Different usernames generate different sequences", () => {
                var a = new UsernameNameGenerator("UserA"); var b = new UsernameNameGenerator("UserB");
                for (int i = 0; i < 64; i++) Check(a.Next() != b.Next(), "Username did not influence output");
            });
            Run("Unicode normalization and input separators never enter resource names", () => {
                Check(new UsernameNameGenerator(" café ").Next() == new UsernameNameGenerator("cafe\u0301").Next(), "NFC/trim mismatch");
                Check(UsernameNameGenerator.IsGeneratedName(new UsernameNameGenerator("示例/用户\\:").Next()), "Path characters entered output");
                Check(!new UsernameNameGenerator("示例/用户\\:").Next().Any(c => "/\\:*?\"<>|".Contains(c)), "Unsafe prefix");
                Check(new UsernameNameGenerator(new string('中', 200)).Next().Length <= 161, "Username prefix not bounded");
            });
            Run("New components require each user's own username", () => {
                var root = Track(new GameObject("NewComponent")); var marker = root.AddComponent<AvatarBuildObfuscator>();
                Check(marker.userName == "", "Author identity was prefilled");
                bool rejected = false; try { new GuardSettings(marker); } catch (ArgumentException) { rejected = true; }
                Check(rejected, "Blank username accepted");
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
            Run("Conservative preserves unknown drivers", UnknownComponent);
            Run("Renaming disabled still isolates and rewrites shape animation", RenameAssetsDisabled);
            Run("Cyclic expression menus remain cyclic copies", CyclicMenus);
            Run("Aggressive hierarchy rewrites float/object curves and AvatarMask, preserving source bytes", () => HierarchyBindings());
            Run("Shared nested controllers rewrite hierarchy paths per root", HierarchyNested);
            Run("Bones, manual subtrees and ancestors keep their paths", HierarchyPreserves);
            Run("Duplicate paths remain unchanged", HierarchyDuplicates);
            Run("Proxy and event string paths protect their ancestors", HierarchyProxyAndEvents);
            Run("Hierarchy option off and Conservative keep object names", HierarchyDisabled);
            Run("Unknown driver policy differs by mode", HierarchyUnknown);
            Run("Conservative hierarchy also rewrites all known bindings", () => HierarchyBindings(ProtectionMode.Conservative));
            Run("Parameters, menu, driver, Animator fields and PhysBone suffixes stay linked", ParameterGraph);
            Run("Vertex permutation preserves topology, channels, skinning and dynamic shapes", MeshPermutation);
            Run("Imported unreadable texture clones retain pixels and importer bytes", () => ImportedTexture(false));
            Run("Imported Crunch texture clones retain pixels and importer bytes", () => ImportedTexture(true));
            Run("NDMF default package output supports unreadable texture copies", () => ImportedTexture(false, true));
            Run("NDMF default package output supports Crunch texture copies", () => ImportedTexture(true, true));
            Run("Long username audio copies fit native paths and preserve samples/source bytes", () => ImportedAudioPath(false));
            Run("An overlong output folder retains the original audio without aborting", () => ImportedAudioPath(true));
            Run("Generated-folder guard rejects package sources and traversal", GeneratedFolderGuard);
            Run("Imported assets use one batch instead of an import per file", BatchedImports);
            Run("Custom Shader copies preserve properties and source files", ShaderCopy);
            Run("Serialized reference walk retains generated resource names", AssetNamesSurviveTraversal);
            Run("Shape order shuffle remaps weights, frames, animation and eyelid indices", ShapeOrder);
            Run("Shape order switch and excluded renderer preserve indices", ShapeOrderDisabled);
            Run("Sibling shuffle keeps transforms and animation targets in both modes", SiblingOrder);
            Run("Sibling shuffle preserves duplicate names, UI and manual exclusions", SiblingOrderPreserves);
            Run("Sibling shuffle switch is independent of renaming", SiblingOrderDisabled);
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
            var root = Track(new GameObject("GuardFixture")); var marker = root.AddComponent<AvatarBuildObfuscator>();
            marker.userName = "Synthetic User"; marker.reorderMeshVertices = false;
            marker.shuffleBlendShapeOrder = false; marker.shuffleGameObjectOrder = false; return root;
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
            root.GetComponent<AvatarBuildObfuscator>().mode = mode;
            return new GuardProcessor(root, new GuardSettings(root.GetComponent<AvatarBuildObfuscator>()), x => Track(x)).Run();
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
            Check(UsernameNameGenerator.IsGeneratedName(name) && name.StartsWith("Synthetic_User_", StringComparison.Ordinal), "Shape name missing username");
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
        private static void ShapeOrder()
        {
            foreach (var mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            {
                var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>();
                marker.shuffleBlendShapeOrder = true; marker.freezeUnusedBlendShapes = false;
                marker.renameDynamicBlendShapes = false; marker.renameAssets = false;
                var mesh = Mesh("Drive0", "Drive1", "Drive2", "Drive3", "Drive4");
                mesh.AddBlendShapeFrame("Multi", 40, Enumerable.Repeat(Vector3.up * .05f,3).ToArray(), new Vector3[3], new Vector3[3]);
                mesh.AddBlendShapeFrame("Multi", 100, Enumerable.Repeat(Vector3.up * .2f,3).ToArray(), new Vector3[3], new Vector3[3]);
                var r = Renderer(root,"Body",mesh);
                for (int i=0;i<6;i++) r.SetBlendShapeWeight(i, i*13+3);
                var d = root.AddComponent<VRCAvatarDescriptor>(); var eye=d.customEyeLookSettings;
                eye.eyelidsSkinnedMesh=r; eye.eyelidsBlendshapes=new[]{1,4,5}; d.customEyeLookSettings=eye;
                var clip=Clip("Body","Drive3");Controller(root,clip);
                var before=Baked(r); var report=Process(root,mode); var after=Baked(r);
                Check(report.ShuffledShapeMeshes==1 && r.sharedMesh!=mesh,report.Summary);
                Check(Enumerable.Range(0,6).Any(i=>r.sharedMesh.GetBlendShapeName(i)!=mesh.GetBlendShapeName(i)),"Shape indices unchanged");
                for (int i=0;i<6;i++)
                    Check(Mathf.Abs(r.GetBlendShapeWeight(r.sharedMesh.GetBlendShapeIndex(mesh.GetBlendShapeName(i)))-(i*13+3))<.00001f,"Weight moved to wrong shape");
                var lidNames=d.customEyeLookSettings.eyelidsBlendshapes.Select(r.sharedMesh.GetBlendShapeName).ToArray();
                Check(lidNames.SequenceEqual(new[]{"Drive1","Drive4","Multi"}),"Eyelid index changed target");
                int multi=r.sharedMesh.GetBlendShapeIndex("Multi");
                Check(r.sharedMesh.GetBlendShapeFrameCount(multi)==2 && r.sharedMesh.GetBlendShapeFrameWeight(multi,0)==40,"In-between frames changed");
                Check(Difference(before.vertices,after.vertices)<.00001f && Difference(before.normals,after.normals)<.00001f,"Shuffled current pose changed");
                var copied=root.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
                var binding=AnimationUtility.GetCurveBindings(copied).Single();
                Check(root.transform.Find(binding.path)==r.transform && binding.propertyName=="blendShape.Drive3","Shuffled animation target changed");
                Check(mesh.GetBlendShapeName(0)=="Drive0" && mesh.GetBlendShapeName(5)=="Multi","Source shape order changed");
            }
        }
        private static void ShapeOrderDisabled()
        {
            var root=Root(); var marker=root.GetComponent<AvatarBuildObfuscator>();
            marker.freezeUnusedBlendShapes=false;marker.renameDynamicBlendShapes=false;marker.renameAssets=false;
            var mesh=Mesh("One","Two","Three");var r=Renderer(root,"Body",mesh);
            Process(root);Check(r.sharedMesh==mesh,"Disabled shape shuffle copied mesh");
            marker.shuffleBlendShapeOrder=true;marker.excludedRenderers=new[]{r};
            Process(root,ProtectionMode.Aggressive);Check(r.sharedMesh==mesh,"Excluded shape indices changed");
        }
        private static void SiblingOrder()
        {
            foreach (var mode in new[]{ProtectionMode.Conservative,ProtectionMode.Aggressive})
            {
                var root=Root(); var marker=root.GetComponent<AvatarBuildObfuscator>();marker.shuffleGameObjectOrder=true;
                var children=Enumerable.Range(0,8).Select(i=>Renderer(root,"Child"+i,Mesh("Driven"))).ToArray();
                for(int i=0;i<children.Length;i++) children[i].transform.localPosition=new Vector3(i*.1f,i*.2f,-i*.1f);
                var position=children.Select(r=>r.transform.localPosition).ToArray();Controller(root,Clip("Child3","Driven"));
                var report=Process(root,mode);
                Check(report.ShuffledSiblingGroups==1 && report.ReorderedGameObjects>0,report.Summary);
                for(int i=0;i<children.Length;i++)
                    Check(children[i].transform.parent==root.transform && children[i].transform.localPosition==position[i],"Parent or local pose changed");
                var output=root.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
                Check(root.transform.Find(AnimationUtility.GetCurveBindings(output).Single().path)==children[3].transform,"Shuffle changed animated object identity");
            }
        }
        private static void SiblingOrderPreserves()
        {
            var root=Root();var marker=root.GetComponent<AvatarBuildObfuscator>();marker.shuffleGameObjectOrder=true;marker.renameGameObjects=false;
            var duplicate=new GameObject("Duplicates");duplicate.transform.SetParent(root.transform);
            var first=new GameObject("Same");first.transform.SetParent(duplicate.transform);
            var second=new GameObject("Same");second.transform.SetParent(duplicate.transform);
            var ui=new GameObject("UI",typeof(RectTransform),typeof(Canvas));ui.transform.SetParent(root.transform);
            var uiFirst=new GameObject("UIFirst",typeof(RectTransform));uiFirst.transform.SetParent(ui.transform);
            var uiSecond=new GameObject("UISecond",typeof(RectTransform));uiSecond.transform.SetParent(ui.transform);
            var manual=new GameObject("Manual");manual.transform.SetParent(root.transform);
            var a=new GameObject("A");a.transform.SetParent(manual.transform);var b=new GameObject("B");b.transform.SetParent(manual.transform);
            marker.preserveGameObjects=new[]{manual.transform}; Process(root,ProtectionMode.Aggressive);
            Check(first.transform.GetSiblingIndex()==0 && second.transform.GetSiblingIndex()==1,"Duplicate order changed");
            Check(uiFirst.transform.GetSiblingIndex()==0 && uiSecond.transform.GetSiblingIndex()==1,"UI order changed");
            Check(a.transform.GetSiblingIndex()==0 && b.transform.GetSiblingIndex()==1 && manual.transform.GetSiblingIndex()==2,"Manual order changed");
        }
        private static void SiblingOrderDisabled()
        {
            var root=Root();var marker=root.GetComponent<AvatarBuildObfuscator>();marker.renameGameObjects=false;
            var a=new GameObject("A");a.transform.SetParent(root.transform);var b=new GameObject("B");b.transform.SetParent(root.transform);
            Process(root);Check(a.transform.GetSiblingIndex()==0 && b.transform.GetSiblingIndex()==1,"Disabled shuffle changed indices");
            marker.shuffleGameObjectOrder=true;Process(root);
            Check(a.name=="A" && b.name=="B" && a.transform.GetSiblingIndex()==1 && b.transform.GetSiblingIndex()==0,"Shuffle requires rename or failed");
        }
        private static void BatchedImports()
        {
            string folder=Folder+"/Fixtures/Batch_"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var root=Root();var d=root.AddComponent<VRCAvatarDescriptor>();var menu=Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>());d.expressionsMenu=menu;
            var texture=Track(new Texture2D(4,4,TextureFormat.RGBA32,false));
            byte[] png=texture.EncodeToPNG();
            for(int i=0;i<8;i++)File.WriteAllBytes(folder+"/Input"+i+".png",png);
            AssetDatabase.Refresh();
            for(int i=0;i<8;i++)menu.controls.Add(new VRCExpressionsMenu.Control{name="Icon"+i,icon=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Input"+i+".png")});
            var original=menu.controls.Select(c=>c.icon).ToArray();var processor=new GuardProcessor(root,new GuardSettings(root.GetComponent<AvatarBuildObfuscator>()),x=>Track(x));
            BatchImportObserver.Folder=folder+"/Generated";Directory.CreateDirectory(BatchImportObserver.Folder);AssetDatabase.Refresh();
            BatchImportObserver.Calls=0;BatchImportObserver.Files=0;BatchImportObserver.Enabled=true;
            try{processor.PrepareImportedAssets(BatchImportObserver.Folder);}finally{BatchImportObserver.Enabled=false;}
            Check(BatchImportObserver.Files==8 && BatchImportObserver.Calls==1,"Import batches="+BatchImportObserver.Calls+", files="+BatchImportObserver.Files);
            var report=processor.Run();Check(report.ImportedTextureCopies==8,"Copied texture count wrong");
            for(int i=0;i<8;i++)Check(d.expressionsMenu.controls[i].icon!=original[i] && menu.controls[i].icon==original[i],"Batch copy changed source or left source reference");
        }
        private static void AssetNamesSurviveTraversal()
        {
            var root=Root();var r=Renderer(root,"Body",Mesh("Driven"));var sourceMaterial=Track(new Material(Shader.Find("Unlit/Color")) {name="DescriptiveMaterial"});
            r.sharedMaterials=new[]{sourceMaterial};var controller=Controller(root,Clip("Body","Driven"));
            controller.AddParameter("Blend",AnimatorControllerParameterType.Float);
            var state=controller.layers[0].stateMachine.states[0].state;var tree=Track(new BlendTree{name="DescriptiveTree"});tree.AddChild(state.motion,0);state.motion=tree;
            var d=root.AddComponent<VRCAvatarDescriptor>();var menu=Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>());menu.name="DescriptiveMenu";d.expressionsMenu=menu;
            var parameters=Track(ScriptableObject.CreateInstance<VRCExpressionParameters>());parameters.name="DescriptiveParameters";d.expressionParameters=parameters;
            Process(root);var output=(AnimatorController)root.GetComponent<Animator>().runtimeAnimatorController;
            foreach(var asset in new Object[]{r.sharedMaterial,output,output.layers[0].stateMachine.states[0].state.motion,d.expressionsMenu,d.expressionParameters})
                Check(UsernameNameGenerator.IsGeneratedName(asset.name),"Serialized traversal restored source name: "+asset.GetType().Name+" "+asset.name);
            Check(sourceMaterial.name=="DescriptiveMaterial" && controller.name=="OriginalController" && tree.name=="DescriptiveTree" && menu.name=="DescriptiveMenu","Source resource name changed");
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
            var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.preserveBlendShapes = new[] { "Keep" }; marker.excludedRenderers = new[] { b };
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
            var report = Process(root, ProtectionMode.Conservative);
            Check(report.FrozenShapes == 0 && report.RenamedShapes == 0 && r.sharedMesh.blendShapeCount == 1, "Unknown driver shape edited");
        }
        private static void RenameAssetsDisabled()
        {
            var root = Root(); var r = Renderer(root, "Body", Mesh("Driven"));
            var source = Clip("Body", "Driven"); var controller = Controller(root, source);
            root.GetComponent<AvatarBuildObfuscator>().renameAssets = false; var report = Process(root);
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
        private static void HierarchyBindings(ProtectionMode mode = ProtectionMode.Aggressive)
        {
            var source = Root(); var group = new GameObject("Clothing"); group.transform.SetParent(source.transform);
            var r = Renderer(group, "Body", Mesh("Driven"));
            var clip = Clip("Clothing/Body", "Driven");
            var enabled = EditorCurveBinding.FloatCurve("Clothing/Body", typeof(GameObject), "m_IsActive");
            AnimationUtility.SetEditorCurve(clip, enabled, AnimationCurve.Constant(0, 1, 1));
            var material = Track(new Material(Shader.Find("Unlit/Texture")) { name = "OriginalSwap" });
            var swap = EditorCurveBinding.PPtrCurve("Clothing/Body", typeof(SkinnedMeshRenderer), "m_Materials.Array.data[0]");
            AnimationUtility.SetObjectReferenceCurve(clip, swap, new[] { new ObjectReferenceKeyframe { value = material } });
            var controller = Controller(source, clip);
            var mask = Track(new AvatarMask()); mask.transformCount = 2;
            mask.SetTransformPath(0, ""); mask.SetTransformActive(0, true);
            mask.SetTransformPath(1, "Clothing/Body"); mask.SetTransformActive(1, false);
            var layers = controller.layers; layers[0].avatarMask = mask; controller.layers = layers;
            string assetPath = Folder + "/Fixtures/Hierarchy_" + Guid.NewGuid().ToString("N") + ".anim";
            AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); var bytes = File.ReadAllBytes(assetPath);
            var clone = Track(Object.Instantiate(source));
            var report = Process(clone, mode);
            var outputRenderer = clone.GetComponentInChildren<SkinnedMeshRenderer>();
            string path = AnimationUtility.CalculateTransformPath(outputRenderer.transform, clone.transform);
            Check(path != "Clothing/Body" && path.Split('/').All(UsernameNameGenerator.IsGeneratedName), "Object names not obfuscated");
            var copied = (AnimatorController)clone.GetComponent<Animator>().runtimeAnimatorController;
            var outputClip = copied.animationClips.Single();
            Check(AnimationUtility.GetCurveBindings(outputClip).All(b => b.path == path), "Float paths stale");
            Check(AnimationUtility.GetObjectReferenceCurveBindings(outputClip).Single().path == path, "Object path stale");
            Check(copied.layers[0].avatarMask != mask && copied.layers[0].avatarMask.GetTransformPath(1) == path &&
                !copied.layers[0].avatarMask.GetTransformActive(1), "Mask path or flags changed");
            Check(r.name == "Body" && group.name == "Clothing" && mask.GetTransformPath(1) == "Clothing/Body", "Source hierarchy/mask changed");
            AssetDatabase.SaveAssetIfDirty(clip); Check(bytes.SequenceEqual(File.ReadAllBytes(assetPath)), "Source clip bytes changed");
            Check(report.RenamedGameObjects == 3 && report.RewrittenPaths >= 4 && UsernameNameGenerator.IsGeneratedName(clone.name), report.Summary);
        }

        private static void HierarchyNested()
        {
            var root = Root(); var a = new GameObject("A"); var b = new GameObject("B");
            a.transform.SetParent(root.transform); b.transform.SetParent(root.transform);
            var ra = Renderer(a, "Body", Mesh("Driven")); var rb = Renderer(b, "Body", Mesh("Driven"));
            var controller = Controller(a, Clip("Body", "Driven")); b.AddComponent<Animator>().runtimeAnimatorController = controller;
            Process(root, ProtectionMode.Aggressive);
            foreach (var renderer in new[] { ra, rb })
            {
                var output = renderer.transform.parent.GetComponent<Animator>().runtimeAnimatorController.animationClips.Single();
                Check(AnimationUtility.GetCurveBindings(output).Single().path == renderer.name, "Nested root path wrong");
                Check(UsernameNameGenerator.IsGeneratedName(renderer.name), "Nested object not renamed");
            }
            Check(a.GetComponent<Animator>().runtimeAnimatorController != b.GetComponent<Animator>().runtimeAnimatorController, "Nested graphs incorrectly shared");
        }

        private static void HierarchyPreserves()
        {
            var root = Root(); var armature = new GameObject("Armature"); armature.transform.SetParent(root.transform);
            root.GetComponent<AvatarBuildObfuscator>().renameBones = false;
            var bone = new GameObject("Bone"); bone.transform.SetParent(armature.transform);
            var r = Renderer(root, "Body", Mesh()); r.bones = new[] { bone.transform }; r.rootBone = bone.transform;
            var keep = new GameObject("ExternalTool"); keep.transform.SetParent(root.transform);
            var child = new GameObject("Child"); child.transform.SetParent(keep.transform);
            root.GetComponent<AvatarBuildObfuscator>().preserveGameObjects = new[] { keep.transform };
            var report = Process(root, ProtectionMode.Aggressive);
            Check(bone.name == "Bone" && armature.name == "Armature" && keep.name == "ExternalTool" && child.name == "Child", "Protected paths changed");
            Check(UsernameNameGenerator.IsGeneratedName(r.name) && r.bones[0] == bone.transform, "Safe sibling not renamed or bone identity lost");
            Check(report.RenamedGameObjects == 2 && report.PreservedGameObjects == 4, report.Summary);
        }

        private static void HierarchyDuplicates()
        {
            var root = Root(); var a = Renderer(root, "Twin", Mesh()); var b = Renderer(root, "Twin", Mesh());
            var report = Process(root, ProtectionMode.Aggressive);
            Check(a.name == "Twin" && b.name == "Twin" && report.RenamedGameObjects == 1, "Ambiguous path rewritten");
        }

        private static void HierarchyProxyAndEvents()
        {
            var root = Root(); var group = new GameObject("Group"); group.transform.SetParent(root.transform);
            var target = Renderer(group, "Body", Mesh()); var spare = Renderer(root, "Spare", Mesh());
            var clip = Clip("Group/Body", "Any", "proxy_path_test"); Controller(root, clip);
            Process(root, ProtectionMode.Aggressive);
            Check(group.name == "Group" && target.name == "Body" && UsernameNameGenerator.IsGeneratedName(spare.name), "Proxy path not protected");
            var second = Root(); var eventTarget = Renderer(second, "EventTarget", Mesh());
            var eventClip = Track(new AnimationClip());
            AnimationUtility.SetAnimationEvents(eventClip, new[] { new AnimationEvent { functionName = "TestEvent", stringParameter = "EventTarget" } });
            Controller(second, eventClip); Process(second, ProtectionMode.Conservative);
            Check(eventTarget.name == "EventTarget", "Event string reference not protected");
        }

        private static void HierarchyDisabled()
        {
            foreach (var mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            {
                var root = Root(); var r = Renderer(root, "Body", Mesh());
                root.GetComponent<AvatarBuildObfuscator>().renameGameObjects = false;
                var report = Process(root, mode); Check(r.name == "Body" && report.RenamedGameObjects == 0, "Hierarchy flag ignored");
            }
        }

        private static void HierarchyUnknown()
        {
            foreach (var mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            {
                var root = Root(); var r = Renderer(root, "Body", Mesh()); root.AddComponent<UnknownShapeDriver>();
                var report = Process(root, mode);
                if (mode == ProtectionMode.Conservative) Check(r.name == "Body" && report.RenamedGameObjects == 0, "Conservative unknown driver ignored");
                else Check(UsernameNameGenerator.IsGeneratedName(r.name) && report.RenamedGameObjects == 2, "Aggressive incorrectly skipped unknown driver");
            }
        }

        private static void ParameterGraph()
        {
            var root = Root(); var clip = Track(new AnimationClip { name = "ParameterCurve" });
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), "Blend"), AnimationCurve.Linear(0, 0, 1, 1));
            var controller = Controller(root, clip);
            foreach (string p in new[] { "Blend", "Outfit", "Touch", "Hair_IsGrabbed", "FT/v2/JawOpen", "IsLocal", "Keep" })
                controller.AddParameter(p, AnimatorControllerParameterType.Float);
            var state = controller.layers[0].stateMachine.states[0].state; state.speedParameterActive = true; state.speedParameter = "Blend";
            var transition = state.AddExitTransition(); transition.AddCondition(AnimatorConditionMode.Greater, .5f, "Outfit");
            var driver = state.AddStateMachineBehaviour<VRCAvatarParameterDriver>();
            driver.parameters.Add(new VRC.SDKBase.VRC_AvatarParameterDriver.Parameter { name = "Outfit", source = "Blend", type = VRC.SDKBase.VRC_AvatarParameterDriver.ChangeType.Copy });
            var d = root.AddComponent<VRCAvatarDescriptor>();
            var parameters = Track(ScriptableObject.CreateInstance<VRCExpressionParameters>());
            parameters.parameters = new[] { new VRCExpressionParameters.Parameter { name = "Outfit", valueType = VRCExpressionParameters.ValueType.Float, defaultValue = .3f, saved = true } };
            d.expressionParameters = parameters;
            var menu = Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>());
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Outfit", parameter = new VRCExpressionsMenu.Control.Parameter { name = "Outfit" } }); d.expressionsMenu = menu;
            var pb = root.AddComponent<VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone>(); pb.parameter = "Hair";
            var contact = root.AddComponent<VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver>(); contact.parameter = "Touch";
            root.GetComponent<AvatarBuildObfuscator>().preserveParameters = new[] { "Keep" };
            var report = Process(root);
            var copy = (AnimatorController)root.GetComponent<Animator>().runtimeAnimatorController;
            var copiedState = copy.layers[0].stateMachine.states[0].state;
            var outputName = d.expressionParameters.parameters[0].name;
            Check(UsernameNameGenerator.IsGeneratedName(outputName) && copy.parameters.Any(p => p.name == outputName), "Parameter definitions disagree");
            Check(d.expressionsMenu.controls[0].parameter.name == outputName && d.expressionsMenu.controls[0].name == "Outfit", "Menu binding/visible label wrong");
            Check(copiedState.transitions.Single().conditions.Single().parameter == outputName, "Transition parameter stale");
            var copiedDriver = (VRCAvatarParameterDriver)copiedState.behaviours.Single();
            Check(copiedDriver.parameters[0].name == outputName && copiedDriver.parameters[0].source == copiedState.speedParameter, "Driver or speed parameter stale");
            Check(AnimationUtility.GetCurveBindings(copy.animationClips.Single()).Single().propertyName == copiedState.speedParameter, "Animator parameter curve stale");
            Check(copy.parameters.Any(p => p.name == pb.parameter + "_IsGrabbed") && UsernameNameGenerator.IsGeneratedName(pb.parameter), "PhysBone suffix contract broken");
            Check(copy.parameters.Any(p => p.name == contact.parameter), "Contact parameter stale");
            Check(new[] { "IsLocal", "FT/v2/JawOpen", "Keep" }.All(n => copy.parameters.Any(p => p.name == n)), "External/reserved/manual parameter changed");
            Check(UsernameNameGenerator.IsGeneratedName(copiedState.name) && UsernameNameGenerator.IsGeneratedName(copy.layers[0].name), "Animator internals not renamed");
            Check(parameters.parameters[0].name == "Outfit" && menu.controls[0].parameter.name == "Outfit" && driver.parameters[0].name == "Outfit", "Source graph mutated");
            Check(report.RenamedParameters >= 5 && report.RenamedAnimatorNames >= 3, report.Summary);
        }

        private static void MeshPermutation()
        {
            var root = Root(); var mesh = Mesh("Dynamic");
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up };
            mesh.SetUVs(7, new List<Vector4> { Vector4.zero, Vector4.one, new Vector4(.2f,.3f,.4f,.5f) });
            mesh.colors32 = new[] { new Color32(1,2,3,4), new Color32(11,12,13,14), new Color32(21,22,23,24) };
            var renderer = Renderer(root, "Body", mesh);
            var material = Track(new Material(Shader.Find("Unlit/Texture"))); renderer.sharedMaterial = material;
            var bone = new GameObject("SkinBone"); bone.transform.SetParent(root.transform);
            renderer.bones = new[] { bone.transform }; renderer.rootBone = bone.transform; mesh.bindposes = new[] { Matrix4x4.identity };
            mesh.boneWeights = Enumerable.Repeat(new BoneWeight { boneIndex0 = 0, weight0 = 1 }, 3).ToArray();
            Controller(root, Clip("Body", "Dynamic")); renderer.SetBlendShapeWeight(0, 35);
            var before = Baked(renderer); var originalVertices = mesh.vertices; var originalTriangles = mesh.triangles;
            var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.reorderMeshVertices = true;
            var report = Process(root, ProtectionMode.Aggressive); var output = renderer.sharedMesh; var after = Baked(renderer);
            Check(report.ReorderedMeshes == 1, report.Summary);
            var originalUV8 = new List<Vector4>(); var outputUV8 = new List<Vector4>(); mesh.GetUVs(7, originalUV8); output.GetUVs(7, outputUV8);
            for (int i = 0; i < output.vertexCount; i++)
            {
                int old = Array.IndexOf(originalVertices, output.vertices[i]); Check(old >= 0, "Rest vertex changed");
                Check(Vector3.Distance(before.vertices[old], after.vertices[i]) < .00001f, "Skinned/shape position changed");
                Check(mesh.colors32[old].Equals(output.colors32[i]) && originalUV8[old] == outputUV8[i] && mesh.uv[old] == output.uv[i], "Vertex channel association lost");
            }
            for (int i = 0; i < originalTriangles.Length; i++)
                Check(originalVertices[originalTriangles[i]] == output.vertices[output.triangles[i]], "Topology changed");
            Check(mesh.vertices.SequenceEqual(originalVertices) && mesh.triangles.SequenceEqual(originalTriangles), "Source mesh changed");
        }

        private static void GeneratedFolderGuard()
        {
            var root = Root();
            foreach (string path in new[] { "Packages/nadena.dev.ndmf/Editor", "Packages/nadena.dev.ndmf/__GeneratedOther",
                "Assets/../Packages/nadena.dev.ndmf/Editor", "Packages/nadena.dev.ndmf/__Generated/../Editor" })
            {
                bool rejected = false;
                try { new GuardProcessor(root, new GuardSettings(root.GetComponent<AvatarBuildObfuscator>())).PrepareImportedAssets(path); }
                catch (ArgumentException) { rejected = true; }
                Check(rejected, "Unsafe generated folder accepted: " + path);
            }
        }
        private static void ImportedTexture(bool crunch, bool ndmfTemporary = false)
        {
            string path = Folder + "/Fixtures/InputTexture_" + Guid.NewGuid().ToString("N") + ".png";
            var temporary = Track(new Texture2D(32, 32, TextureFormat.RGBA32, true));
            temporary.SetPixels(Enumerable.Range(0,1024).Select(i => new Color((i%32)/31f,(i/32)/31f,.5f,1)).ToArray()); temporary.Apply();
            File.WriteAllBytes(path, temporary.EncodeToPNG()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.isReadable = false; importer.crunchedCompression = crunch;
            importer.textureCompression = TextureImporterCompression.Compressed; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path); var before = Pixels(texture);
            var sourceBytes = File.ReadAllBytes(path); var sourceMeta = File.ReadAllBytes(path + ".meta");
            var root = Root(); var r = Renderer(root, "Body", Mesh()); var mat = Track(new Material(Shader.Find("Unlit/Texture"))); mat.mainTexture = texture; r.sharedMaterial = mat;
            string folder = (ndmfTemporary ? "Packages/nadena.dev.ndmf/__Generated/ABO_Validation_" : Folder + "/Fixtures/CopiedTextures_") + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            var processor = new GuardProcessor(root, new GuardSettings(root.GetComponent<AvatarBuildObfuscator>()), x => Track(x));
            processor.PrepareImportedAssets(folder); var report = processor.Run(); var output = (Texture2D)r.sharedMaterial.mainTexture;
            Check(output != texture && UsernameNameGenerator.IsGeneratedName(output.name) && !output.isReadable, "Imported texture not independently renamed");
            Check(output.format == texture.format && before.SequenceEqual(Pixels(output)), "Texture format/pixels changed");
            Check(sourceBytes.SequenceEqual(File.ReadAllBytes(path)) && sourceMeta.SequenceEqual(File.ReadAllBytes(path + ".meta")), "Source file/importer changed");
            Check(report.ImportedTextureCopies == 1, report.Summary);
        }

        private static void ImportedAudioPath(bool overlong)
        {
            string inputPath = Folder + "/Fixtures/Audio_" + Guid.NewGuid().ToString("N") + ".wav";
            using (var writer = new BinaryWriter(File.Create(inputPath)))
            {
                const int count = 1024;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(22050); writer.Write(44100);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                for (int i = 0; i < count; i++) writer.Write((short)(Math.Sin(i * .1) * 10000));
            }
            AssetDatabase.ImportAsset(inputPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(inputPath);
            var defaults = importer.defaultSampleSettings;
            defaults.loadType = AudioClipLoadType.DecompressOnLoad; defaults.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = defaults; importer.SaveAndReimport();
            var input = AssetDatabase.LoadAssetAtPath<AudioClip>(inputPath);
            var sourceBytes = File.ReadAllBytes(inputPath); var sourceMeta = File.ReadAllBytes(inputPath + ".meta");
            string folder = Folder + "/Fixtures/AudioOut_" + Guid.NewGuid().ToString("N");
            if (overlong) folder += "/" + new string('p', 55);
            Directory.CreateDirectory(folder); AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.userName = new string('u', 32);
            var source = root.AddComponent<AudioSource>(); source.clip = input;
            Check(Path.GetFullPath(folder + "/" + new UsernameNameGenerator(marker.userName).Next() + ".wav").Length >= 260,
                "Fixture would not reproduce the original long-path failure");
            var processor = new GuardProcessor(root, new GuardSettings(marker), x => Track(x));
            processor.PrepareImportedAssets(folder); var report = processor.Run(); var copy = source.clip;
            if (overlong)
            {
                Check(copy == input && report.Notes.Any(x => x.Contains("输出目录过长")), "Overlong-folder fallback not reported or identity lost");
                Check(!Directory.GetFiles(folder).Any(), "Unsafe copy was attempted");
            }
            else
            {
                Check(copy != input && UsernameNameGenerator.IsGeneratedName(copy.name) && copy.name.StartsWith(marker.userName + "_"),
                    "Compact disk name lost the full internal username/I-l name");
                string path = AssetDatabase.GetAssetPath(copy);
                Check(Path.GetFullPath(path).Length <= 240 && path.EndsWith(".wav"), "Native-import path budget exceeded");
                Check(copy.samples == input.samples && copy.frequency == input.frequency && copy.channels == input.channels, "Audio format changed");
                var a = new float[input.samples * input.channels]; var b = new float[copy.samples * copy.channels];
                Check(input.GetData(a, 0) && copy.GetData(b, 0) && a.SequenceEqual(b), "Audio samples changed");
                Check(sourceBytes.SequenceEqual(File.ReadAllBytes(path)), "Copied audio bytes changed");
            }
            Check(sourceBytes.SequenceEqual(File.ReadAllBytes(inputPath)) && sourceMeta.SequenceEqual(File.ReadAllBytes(inputPath + ".meta")),
                "Source audio/importer changed");
        }

        private static void ShaderCopy()
        {
            string path = Folder + "/Fixtures/Shader_" + Guid.NewGuid().ToString("N") + ".shader";
            File.WriteAllText(path, "Shader \"ABO/Fixture\" { Properties { _Color(\"Color\", Color) = (1,1,1,1) } SubShader { Pass { CGPROGRAM\n#pragma vertex vert\n#pragma fragment frag\n#include \"UnityCG.cginc\"\nfloat4 _Color; float4 vert(float4 v:POSITION):SV_POSITION { return UnityObjectToClipPos(v); } float4 frag():SV_Target { return _Color; }\nENDCG } } }");
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            var bytes = File.ReadAllBytes(path); var root = Root(); var r = Renderer(root, "Body", Mesh());
            var mat = Track(new Material(shader)); mat.SetColor("_Color", Color.magenta); r.sharedMaterial = mat;
            Process(root); var output = r.sharedMaterial;
            Check(output.shader != shader && UsernameNameGenerator.IsGeneratedName(output.shader.name) && !ShaderUtil.ShaderHasError(output.shader), "Shader clone invalid");
            Check(output.GetColor("_Color") == Color.magenta && bytes.SequenceEqual(File.ReadAllBytes(path)), "Shader property/source changed");
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
            Check(output != input && UsernameNameGenerator.IsGeneratedName(output.name), "Texture not renamed on a copy");
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
