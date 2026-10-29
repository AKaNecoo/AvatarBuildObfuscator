// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.IO;
using System.Linq;
using AvatarTools.Obfuscation.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        public static string RunLateResources(string output)
        {
            Directory.CreateDirectory(Folder + "/Fixtures"); AssetDatabase.Refresh();
            results = new Results();
            foreach (var mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            {
                var selected = mode;
                foreach (bool audit in new[] { false, true })
                {
                    bool enabled = audit;
                    Run("Late unreadable menu icons retain data: " + mode + ", audit=" + audit,
                        () => LateMenuIcons(selected, enabled));
                }
                foreach (bool naming in new[] { false, true })
                {
                    bool enabled = naming;
                    Run("Late Animator graph retains protocol and existing copies: " + mode + ", resources=" + naming,
                        () => LateAnimatorGraph(selected, enabled));
                }
                Run("Late Animator internal-name opt-out remains effective: " + mode, () => LateAnimatorGraph(selected, true, false));
                Run("Late clip on an owned nested Animator retains its binding root: " + mode, () => LateNestedClip(selected));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string json = JsonUtility.ToJson(results, true); File.WriteAllText(output, json); return json;
        }

        private static string LateFolder()
        {
            string path = Folder + "/Fixtures/Late_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(path + "/Generated"); AssetDatabase.Refresh(); return path;
        }

        private static void LateMenuIcons(ProtectionMode mode, bool audit)
        {
            string folder = LateFolder(), path = folder + "/LateIcon.png";
            var image = Track(new Texture2D(16, 16, TextureFormat.RGBA32, false));
            image.SetPixels32(Enumerable.Range(0, 256).Select(i => new Color32((byte)i, (byte)(255-i), 73, (byte)(i % 2 == 0 ? 128 : 255))).ToArray());
            image.Apply(); File.WriteAllBytes(path, image.EncodeToPNG()); AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = false; importer.mipmapEnabled = true; importer.streamingMipmaps = true; importer.streamingMipmapsPriority = 3;
            var platform = importer.GetPlatformTextureSettings("Standalone"); platform.name = "Standalone";
            platform.overridden = true; platform.format = TextureImporterFormat.DXT5; platform.maxTextureSize = 32;
            importer.SetPlatformTextureSettings(platform); importer.SaveAndReimport();
            var input = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            byte[] bytes = File.ReadAllBytes(path), meta = File.ReadAllBytes(path + ".meta");
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode; marker.auditBuild = audit;
            var descriptor = root.AddComponent<VRCAvatarDescriptor>();
            var sourceMenu = Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>());
            descriptor.expressionsMenu = sourceMenu;
            var processor = new GuardProcessor(root, new GuardSettings(marker), x => Track(x));
            processor.PrepareImportedAssets(folder + "/Generated"); var report = processor.Run();
            var menu = descriptor.expressionsMenu;
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Visible late label", icon = input, subMenu = menu,
                labels = new[] { new VRCExpressionsMenu.Control.Label { name = "Visible axis label", icon = input } } });
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Shared icon", icon = input });
            int children = root.transform.childCount;
            Check(new BuildAuditCompletion().OnPreprocessAvatar(root), "SDK completion failed");
            var copy = menu.controls[0].icon;
            Check(copy != null && copy != input && UsernameNameGenerator.IsGeneratedName(copy.name), "Late imported icon kept original identity/name");
            Check(copy == menu.controls[1].icon && copy == menu.controls[0].labels[0].icon && menu.controls[0].subMenu == menu,
                "Shared icon or cyclic submenu reference changed");
            Check(copy.width == input.width && copy.height == input.height && copy.format == input.format &&
                copy.graphicsFormat == input.graphicsFormat && copy.mipmapCount == input.mipmapCount && copy.isReadable == input.isReadable &&
                copy.streamingMipmaps == input.streamingMipmaps && copy.streamingMipmapsPriority == input.streamingMipmapsPriority,
                "Late icon import/storage/streaming changed");
            Check(bytes.SequenceEqual(File.ReadAllBytes(AssetDatabase.GetAssetPath(copy))) && bytes.SequenceEqual(File.ReadAllBytes(path)) &&
                meta.SequenceEqual(File.ReadAllBytes(path + ".meta")), "PNG payload or source importer changed");
            Check(menu.controls[0].name == "Visible late label" && menu.controls[0].labels[0].name == "Visible axis label" &&
                sourceMenu.controls.Count == 0 && root.transform.childCount == children, "Visible labels, source menu or hierarchy changed");
            int copied = report.CopiedObjects;
            Check(new BuildAuditCompletion().OnPreprocessAvatar(root) && menu.controls[0].icon == copy && report.CopiedObjects == copied,
                "Repeated SDK callback changed the output again");
            Check(!audit || report.AuditStage == "sdk-preprocess", "Audit stage did not follow completion");
        }

        private static void LateAnimatorGraph(ProtectionMode mode, bool resourceNames, bool animatorNames = true)
        {
            string folder = LateFolder();
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode; marker.renameAssets = resourceNames;
            marker.renameAnimatorInternals = animatorNames;
            marker.auditBuild = resourceNames;
            var descriptor = root.AddComponent<VRCAvatarDescriptor>();
            var controller = AnimatorController.CreateAnimatorControllerAtPath(folder + "/Before.controller");
            controller.AddParameter("Existing", AnimatorControllerParameterType.Bool);
            var clip = new AnimationClip { name = "ExistingClip" }; AssetDatabase.AddObjectToAsset(clip, controller);
            controller.AddMotion(clip); AssetDatabase.SaveAssetIfDirty(controller);
            root.AddComponent<Animator>().runtimeAnimatorController = controller;
            var container = ScriptableObject.CreateInstance<nadena.dev.ndmf.runtime.SubAssetContainer>();
            AssetDatabase.CreateAsset(container, folder + "/BeforeCopies.asset");
            var processor = new GuardProcessor(root, new GuardSettings(marker), x => {
                Track(x); if (!EditorUtility.IsPersistent(x)) AssetDatabase.AddObjectToAsset(x, container);
            });
            processor.PrepareImportedAssets(folder + "/Generated"); processor.Run();
            var oldCopy = (AnimatorController)root.GetComponent<Animator>().runtimeAnimatorController;
            var oldState = oldCopy.layers[0].stateMachine.states[0].state;
            string oldName = oldState.name, existingParameter = oldCopy.parameters[0].name;
            var late = AnimatorController.CreateAnimatorControllerAtPath(folder + "/Late.controller"); late.name = "Late source controller";
            late.AddParameter("Late/Protocol", AnimatorControllerParameterType.Bool);
            late.AddParameter(existingParameter, AnimatorControllerParameterType.Bool);
            var machine = late.layers[0].stateMachine; machine.name = "Late machine";
            var first = machine.AddState("Late entry"); var second = machine.AddState("Late receive");
            first.motion = oldState.motion;
            first.AddTransition(second).AddCondition(AnimatorConditionMode.If, 0, "Late/Protocol");
            machine.defaultState = first; AssetDatabase.SaveAssetIfDirty(late);
            descriptor.baseAnimationLayers = new[] { new VRCAvatarDescriptor.CustomAnimLayer {
                type = VRCAvatarDescriptor.AnimLayerType.Action, animatorController = late, isDefault = false } };
            string sourceBefore = EditorJsonUtility.ToJson(late), stateBefore = EditorJsonUtility.ToJson(first);
            Check(new BuildAuditCompletion().OnPreprocessAvatar(root), "SDK completion failed");
            var result = (AnimatorController)descriptor.baseAnimationLayers[0].animatorController;
            Check(result != late && (!resourceNames || UsernameNameGenerator.IsGeneratedName(result.name)), "Late controller was not copied/named");
            var copiedMachine = result.layers[0].stateMachine;
            Check(animatorNames ? UsernameNameGenerator.IsGeneratedName(copiedMachine.name) &&
                copiedMachine.states.All(s => UsernameNameGenerator.IsGeneratedName(s.state.name)) :
                copiedMachine.name == "Late machine" && copiedMachine.states.Select(s => s.state.name).SequenceEqual(new[] { "Late entry", "Late receive" }),
                "Late state/machine naming did not respect the switch");
            var entry = copiedMachine.defaultState;
            Check(entry != null && entry.motion == oldState.motion && entry.transitions.Length == 1 &&
                copiedMachine.states.Any(s => s.state == entry.transitions[0].destinationState), "Motion sharing/default state/transition destination changed");
            Check(result.parameters.Any(p => p.name == "Late/Protocol") && result.parameters.Any(p => p.name == existingParameter) &&
                entry.transitions[0].conditions[0].parameter == "Late/Protocol", "Late protocol or previously remapped parameter changed");
            Check(oldState.name == oldName && root.GetComponent<Animator>().runtimeAnimatorController == oldCopy &&
                EditorJsonUtility.ToJson(late) == sourceBefore && EditorJsonUtility.ToJson(first) == stateBefore, "Existing output graph or late source input mutated");
            // Existing owned objects saved under a custom Assets generation root
            // must not be reclassified as original input assets during completion.
            if (resourceNames)
            {
                var reportPath = Path.Combine(BuildMetadataInspection.ReportsFolder, "latest.json");
                var report = JsonUtility.FromJson<GuardReport>(File.ReadAllText(reportPath));
                Check(!report.AuditEntries.Any(e => e.category == "source-resource-reference" && UsernameNameGenerator.IsGeneratedName(e.name)),
                    "Owned generated graph was reported as a source-resource leak");
            }
        }

        private static void LateNestedClip(ProtectionMode mode)
        {
            string folder = LateFolder();
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode;
            var rig = new GameObject("NestedRig"); rig.transform.SetParent(root.transform);
            var renderer = Renderer(root, "Body", Mesh("Driven")); renderer.transform.SetParent(rig.transform);
            var clip = Clip("Body", "Driven");
            var controller = Track(new AnimatorController { name = "NestedController" }); controller.AddLayer("Base");
            controller.AddMotion(clip);
            var animator = rig.AddComponent<Animator>(); animator.runtimeAnimatorController = controller;
            var processor = new GuardProcessor(root, new GuardSettings(marker), x => Track(x)); processor.Run();
            var copy = (AnimatorController)animator.runtimeAnimatorController;
            var state = copy.layers[0].stateMachine.states[0].state;
            var late = new AnimationClip { name = "Late source clip" };
            late.SetCurve("Body", typeof(SkinnedMeshRenderer), "blendShape.Driven", AnimationCurve.Linear(0, 0, 1, 100));
            string path = folder + "/Late.anim"; AssetDatabase.CreateAsset(late, path);
            // Reading curve bindings can populate Unity's editor-curve cache. Warm
            // that read-only cache before comparing the managed serialization view.
            foreach (var sourceBinding in AnimationUtility.GetCurveBindings(late)) AnimationUtility.GetEditorCurve(late, sourceBinding);
            string before = EditorJsonUtility.ToJson(late); byte[] bytes = File.ReadAllBytes(path);
            state.motion = late;
            Check(new BuildAuditCompletion().OnPreprocessAvatar(root), "SDK completion failed");
            var result = (AnimationClip)state.motion;
            var binding = AnimationUtility.GetCurveBindings(result).Single();
            Check(result != late && binding.path == AnimationUtility.CalculateTransformPath(renderer.transform, rig.transform) &&
                binding.propertyName == "blendShape." + renderer.sharedMesh.GetBlendShapeName(0), "Nested Animator root or renamed shape binding lost");
            bool controllerUnchanged = animator.runtimeAnimatorController == copy;
            bool clipUnchanged = EditorJsonUtility.ToJson(late) == before;
            Check(controllerUnchanged && clipUnchanged && bytes.SequenceEqual(File.ReadAllBytes(path)),
                "Owned controller or late source clip changed; controller=" + controllerUnchanged + ", clip=" + clipUnchanged);
        }
    }
}
