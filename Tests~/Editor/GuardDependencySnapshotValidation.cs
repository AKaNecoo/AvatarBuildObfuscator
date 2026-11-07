// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AvatarTools.Obfuscation.Editor;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        public static string RunDependencySnapshots(string output)
        {
            results = new Results(); Directory.CreateDirectory(Folder + "/Fixtures"); AssetDatabase.Refresh();
            foreach (ProtectionMode mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            {
                var selected = mode;
                Run("Audit refreshes changed resource references without modifying them: " + mode, () => SnapshotAudit(selected));
                Run("Public preparation sees resources added after construction and between calls: " + mode, () => SnapshotPreparation(selected));
                foreach (bool audit in new[] { false, true })
                {
                    bool enabled = audit;
                    Run("SDK completion refreshes its graph on every invocation: " + mode + ", audit=" + audit,
                        () => SnapshotCompletion(selected, enabled));
                }
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string json = JsonUtility.ToJson(results, true); File.WriteAllText(output, json); return json;
        }

        private static Texture2D SnapshotTexture(string folder, string name)
        {
            var texture = Track(new Texture2D(4, 4, TextureFormat.RGBA32, false));
            texture.SetPixels(Enumerable.Repeat(new Color(.2f, .4f, .6f, .8f), 16).ToArray()); texture.Apply();
            string path = folder + "/" + name + ".png"; File.WriteAllBytes(path, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static string SnapshotFolder()
        {
            string folder = Folder + "/Fixtures/Snapshot_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(folder + "/Generated"); AssetDatabase.Refresh(); return folder;
        }

        private static void SnapshotAudit(ProtectionMode mode)
        {
            string folder = SnapshotFolder();
            var first = SnapshotTexture(folder, "First"); var second = SnapshotTexture(folder, "Later");
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode; marker.auditBuild = true;
            marker.renameAssets = false; marker.renameGameObjects = false;
            var menu = Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>()); menu.name = "OriginalMenu";
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Visible", icon = first, subMenu = menu });
            root.AddComponent<VRCAvatarDescriptor>().expressionsMenu = menu;
            var processor = new GuardProcessor(root, new GuardSettings(marker), x => Track(x));
            var report = processor.Run();
            int initial = report.OutputTextures; string sourceBefore = EditorJsonUtility.ToJson(first);
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Late visible", icon = second });
            string menuBefore = EditorJsonUtility.ToJson(menu);
            processor.AuditOutput("changed-graph");
            Check(report.OutputTextures == initial + 1 && report.AuditEntries.Any(e => e.type == "Texture2D" && e.name == second.name),
                "Audit missed a resource introduced after the earlier snapshot");
            var entries = report.AuditEntries.Select(e => JsonUtility.ToJson(e)).ToArray();
            processor.AuditOutput("repeated-graph");
            Check(entries.SequenceEqual(report.AuditEntries.Select(e => JsonUtility.ToJson(e))), "Unchanged graph produced different audit entries");
            Check(EditorJsonUtility.ToJson(menu) == menuBefore && EditorJsonUtility.ToJson(first) == sourceBefore &&
                menu.controls[0].icon == first && menu.controls[1].icon == second && menu.controls[0].subMenu == menu,
                "Read-only auditing changed resources or cyclic/shared references");
        }

        private static void SnapshotPreparation(ProtectionMode mode)
        {
            string folder = SnapshotFolder(); var first = SnapshotTexture(folder, "First"); var second = SnapshotTexture(folder, "Later");
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode;
            var descriptor = root.AddComponent<VRCAvatarDescriptor>();
            var menu = Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>()); descriptor.expressionsMenu = menu;
            var processor = new GuardProcessor(root, new GuardSettings(marker), x => Track(x));
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "First", icon = first });
            processor.PrepareImportedAssets(folder + "/Generated");
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Second", icon = second });
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Shared", icon = first });
            processor.PrepareImportedAssets(folder + "/Generated"); var report = processor.Run();
            var copy = descriptor.expressionsMenu;
            Check(report.ImportedTextureCopies == 2 && copy.controls[0].icon != first && copy.controls[1].icon != second &&
                copy.controls[0].icon == copy.controls[2].icon && copy.controls.All(c => UsernameNameGenerator.IsGeneratedName(c.icon.name)),
                "Public preparation reused an obsolete snapshot or lost texture sharing");
            Check(menu.controls[0].icon == first && menu.controls[1].icon == second, "Source menu changed");
        }

        private static void SnapshotCompletion(ProtectionMode mode, bool audit)
        {
            string folder = SnapshotFolder(); var first = SnapshotTexture(folder, "LateOne"); var second = SnapshotTexture(folder, "LateTwo");
            var inputs = new[] { first, second };
            var bytes = inputs.Select(t => File.ReadAllBytes(AssetDatabase.GetAssetPath(t))).ToArray();
            var metas = inputs.Select(t => File.ReadAllBytes(AssetDatabase.GetAssetPath(t) + ".meta")).ToArray();
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode; marker.auditBuild = audit;
            var descriptor = root.AddComponent<VRCAvatarDescriptor>();
            var menu = Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>()); descriptor.expressionsMenu = menu;
            var processor = new GuardProcessor(root, new GuardSettings(marker), x => Track(x));
            processor.PrepareImportedAssets(folder + "/Generated"); var report = processor.Run(); var copy = descriptor.expressionsMenu;
            var finish = typeof(GuardProcessor).GetMethod("FinishSdkBuild", BindingFlags.Instance | BindingFlags.NonPublic);
            for (int i = 0; i < inputs.Length; i++)
            {
                copy.controls.Add(new VRCExpressionsMenu.Control { name = "Visible " + i, icon = inputs[i], subMenu = copy });
                finish.Invoke(processor, null);
                Check(report.ImportedTextureCopies == i + 1 && copy.controls[i].icon != inputs[i] &&
                    UsernameNameGenerator.IsGeneratedName(copy.controls[i].icon.name), "A later SDK invocation missed its new resource");
                Check(!audit || report.OutputTextures == i + 1 && !report.AuditEntries.Any(e => e.category == "source-resource-reference" && e.type == "Texture2D"),
                    "Final audit used a pre-rewrite snapshot or classified copied textures as sources");
            }
            var copies = copy.controls.Select(c => c.icon).ToArray(); finish.Invoke(processor, null);
            Check(report.ImportedTextureCopies == 2 && copies.SequenceEqual(copy.controls.Select(c => c.icon)) &&
                copy.controls.All(c => c.subMenu == copy) && menu.controls.Count == 0, "Repeated completion changed output identity or the source menu");
            for (int i = 0; i < inputs.Length; i++)
                Check(bytes[i].SequenceEqual(File.ReadAllBytes(AssetDatabase.GetAssetPath(inputs[i]))) &&
                    metas[i].SequenceEqual(File.ReadAllBytes(AssetDatabase.GetAssetPath(inputs[i]) + ".meta")), "Source texture/importer changed");
        }
    }
}
