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
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        public static string RunNativeTextureCopies(string output)
        {
            results = new Results();
            foreach (ProtectionMode mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            {
                var selected = mode;
                Run("Native DXT1 shared color texture: " + mode,
                    () => NativeTextureCopy(selected, TextureImporterFormat.DXT1, true, true, false, true));
                Run("Native DXT5 unreadable normal map: " + mode,
                    () => NativeTextureCopy(selected, TextureImporterFormat.DXT5, true, false, true, true));
                Run("Native BC7 late SDK texture: " + mode,
                    () => NativeTextureCopy(selected, TextureImporterFormat.BC7, true, false, false, true, true));
                Run("Native BC7 texture without mips: " + mode,
                    () => NativeTextureCopy(selected, TextureImporterFormat.BC7, false, false, false, true, mipmapped: false));
                Run("Mipmapped texture without streaming keeps its importer: " + mode,
                    () => NativeTextureCopy(selected, TextureImporterFormat.DXT5, false, false, false, false));
                Run("Unsupported format keeps its importer: " + mode,
                    () => NativeTextureCopy(selected, TextureImporterFormat.RGBA32, true, false, false, false));
                Run("Imported audio keeps the native route when textures are also present: " + mode,
                    () => NativeTextureCopy(selected, TextureImporterFormat.DXT1, true, true, false, false, withAudio: true));
                Run("Late SDK audio uses its importer when a native texture was already saved: " + mode,
                    () => NativeTextureCopy(selected, TextureImporterFormat.DXT1, true, true, false, true, withAudio: true, lateAudio: true));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string json = JsonUtility.ToJson(results, true);
            File.WriteAllText(output, json);
            return json;
        }

        private static void NativeTextureCopy(ProtectionMode mode, TextureImporterFormat format, bool streaming,
            bool readable, bool normal, bool expectNative, bool late = false, bool mipmapped = true,
            bool withAudio = false, bool lateAudio = false)
        {
            string folder = Folder + "/Fixtures/NativeTexture_" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(folder + "/Generated");
            var input = Track(new Texture2D(32, 32, TextureFormat.RGBA32, false));
            input.SetPixels32(Enumerable.Range(0, 1024).Select(i =>
                new Color32((byte)(i * 7), (byte)(i * 13), (byte)(i * 37), (byte)(i * 11))).ToArray());
            input.Apply();
            string path = folder + "/Input.png";
            File.WriteAllBytes(path, input.EncodeToPNG());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.isReadable = readable;
            importer.mipmapEnabled = mipmapped;
            importer.streamingMipmaps = streaming;
            importer.streamingMipmapsPriority = 4;
            importer.filterMode = FilterMode.Trilinear;
            importer.wrapModeU = TextureWrapMode.Mirror;
            importer.wrapModeV = TextureWrapMode.Clamp;
            if (!normal) importer.alphaIsTransparency = true;
            var platform = importer.GetPlatformTextureSettings("Standalone");
            platform.overridden = true;
            platform.format = format;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Check(source != null && (source.mipmapCount > 1) == mipmapped && source.streamingMipmaps == streaming,
                "Invalid texture fixture");
            byte[] sourceBytes = File.ReadAllBytes(path), sourceMeta = File.ReadAllBytes(path + ".meta");
            string sourceName = source.name;
            var sourceHash = source.imageContentsHash;

            var root = Root();
            var marker = root.GetComponent<AvatarBuildObfuscator>();
            marker.mode = mode;
            AudioSource speaker = null;
            AudioClip originalAudio = null;
            if (withAudio)
            {
                originalAudio = CreateNativeAudioFixture(folder + "/Input.wav", AudioClipLoadType.DecompressOnLoad);
                Check(originalAudio != null, "Invalid audio fixture");
                speaker = root.AddComponent<AudioSource>();
                if (!lateAudio) speaker.clip = originalAudio;
            }
            var descriptor = root.AddComponent<VRCAvatarDescriptor>();
            var menu = Track(ScriptableObject.CreateInstance<VRCExpressionsMenu>());
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "First", icon = late ? null : source, subMenu = menu });
            menu.controls.Add(new VRCExpressionsMenu.Control { name = "Second", icon = late ? null : source });
            descriptor.expressionsMenu = menu;
            var container = ScriptableObject.CreateInstance<nadena.dev.ndmf.runtime.SubAssetContainer>();
            AssetDatabase.CreateAsset(container, folder + "/Generated/Container.asset");
            var processor = new GuardProcessor(root, new GuardSettings(marker), value =>
            {
                if (!EditorUtility.IsPersistent(value)) AssetDatabase.AddObjectToAsset(value, container);
            });
            const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(GuardProcessor).GetField("UseNativeImportedTextures", hidden).SetValue(processor, true);
            typeof(GuardProcessor).GetField("UseNativeImportedAudio", hidden).SetValue(processor, withAudio);
            processor.PrepareImportedAssets(folder + "/Generated");
            var report = processor.Run();
            var finish = typeof(GuardProcessor).GetMethod("FinishSdkBuild", hidden);
            if (late)
            {
                descriptor.expressionsMenu.controls[0].icon = source;
                descriptor.expressionsMenu.controls[1].icon = source;
                finish.Invoke(processor, null);
            }
            if (lateAudio)
            {
                speaker.clip = originalAudio;
                finish.Invoke(processor, null);
            }
            var copy = descriptor.expressionsMenu.controls[0].icon as Texture2D;
            Check(copy != null && copy != source && copy == descriptor.expressionsMenu.controls[1].icon &&
                UsernameNameGenerator.IsGeneratedName(copy.name), "Texture independence, naming or sharing changed");
            Check(report.NativeTextureCopies == (expectNative ? 1 : 0) && report.ImportedTextureCopies == (expectNative ? 0 : 1),
                "Texture took the wrong copy route");
            Check((AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(copy)) is TextureImporter) != expectNative,
                "Texture using the native route was reimported or the fallback lost its importer");
            Check(copy.graphicsFormat == source.graphicsFormat && copy.mipmapCount == source.mipmapCount &&
                copy.isReadable == readable && copy.streamingMipmaps == source.streamingMipmaps &&
                copy.streamingMipmapsPriority == source.streamingMipmapsPriority && copy.filterMode == source.filterMode &&
                copy.wrapModeU == source.wrapModeU && copy.wrapModeV == source.wrapModeV && copy.imageContentsHash == sourceHash,
                "Texture storage or sampling settings changed");
            if (readable) Check(copy.GetRawTextureData().SequenceEqual(source.GetRawTextureData()), "Encoded mip data changed");
            if (withAudio)
            {
                var copiedAudio = speaker.clip;
                Check(copiedAudio != null && copiedAudio != originalAudio && copiedAudio.loadType == originalAudio.loadType,
                    "Audio reference or import settings changed");
                Check(report.NativeAudioCopies == (lateAudio ? 0 : 1) &&
                    (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(copiedAudio)) is AudioImporter) == lateAudio,
                    "Audio and texture copies did not keep independent routes");
            }
            finish.Invoke(processor, null);
            Check(copy == descriptor.expressionsMenu.controls[0].icon && report.NativeTextureCopies + report.ImportedTextureCopies == 1,
                "Repeated SDK completion copied the texture again");
            Check(source.name == sourceName && source.imageContentsHash == sourceHash && sourceBytes.SequenceEqual(File.ReadAllBytes(path)) &&
                sourceMeta.SequenceEqual(File.ReadAllBytes(path + ".meta")) && menu.controls[0].icon == (late ? null : source),
                "Source texture, importer or menu changed");
        }
    }
}
