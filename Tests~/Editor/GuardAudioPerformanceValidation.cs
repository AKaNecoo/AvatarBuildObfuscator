// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AvatarTools.Obfuscation.Editor;
using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        public static string RunAudioPerformance(string output)
        {
            results = new Results(); Directory.CreateDirectory(Folder + "/Fixtures"); AssetDatabase.Refresh();
            foreach (ProtectionMode mode in new[] { ProtectionMode.Conservative, ProtectionMode.Aggressive })
            {
                foreach (AudioClipLoadType load in new[] { AudioClipLoadType.DecompressOnLoad, AudioClipLoadType.CompressedInMemory, AudioClipLoadType.Streaming })
                {
                    var capturedMode = mode; var capturedLoad = load;
                    Run("Native audio preserves encoded contract and shared references: " + mode + "/" + load, () => NativeAudio(capturedMode, capturedLoad, false));
                }
                var captured = mode;
                Run("Late SDK native audio is independently persisted: " + mode, () => NativeAudio(captured, AudioClipLoadType.DecompressOnLoad, true));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, JsonUtility.ToJson(results, true));
            return JsonUtility.ToJson(results, true);
        }

        private static void NativeAudio(ProtectionMode mode, AudioClipLoadType loadType, bool late)
        {
            string folder = Folder + "/Fixtures/NativeAudio_" + Guid.NewGuid().ToString("N"); Directory.CreateDirectory(folder);
            string path = folder + "/Source.wav";
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                const int count = 4096, channels = 2, frequency = 22050;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * channels * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)channels); writer.Write(frequency); writer.Write(frequency * channels * 2);
                writer.Write((short)(channels * 2)); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * channels * 2);
                for (int i = 0; i < count * channels; i++) writer.Write((short)(Math.Sin(i * .13) * 10000));
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path); var sample = importer.defaultSampleSettings;
            sample.loadType = loadType; sample.compressionFormat = loadType == AudioClipLoadType.DecompressOnLoad ? AudioCompressionFormat.PCM : AudioCompressionFormat.Vorbis;
            sample.preloadAudioData = true; importer.defaultSampleSettings = sample; importer.loadInBackground = false; importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            var bytes = File.ReadAllBytes(path); var meta = File.ReadAllBytes(path + ".meta"); string original = EditorJsonUtility.ToJson(source);
            var root = Root(); var marker = root.GetComponent<AvatarBuildObfuscator>(); marker.mode = mode; marker.userName = new string('禾', 32);
            var a = root.AddComponent<AudioSource>(); var child = new GameObject("SharedAudio"); child.transform.SetParent(root.transform); var b = child.AddComponent<AudioSource>();
            if (!late) a.clip = b.clip = source;
            string output = folder + "/Output"; Directory.CreateDirectory(output); AssetDatabase.Refresh();
            var container = ScriptableObject.CreateInstance<nadena.dev.ndmf.runtime.SubAssetContainer>(); AssetDatabase.CreateAsset(container, output + "/Container.asset");
            var processor = new GuardProcessor(root, new GuardSettings(marker), value => { if (!EditorUtility.IsPersistent(value)) AssetDatabase.AddObjectToAsset(value, container); });
            typeof(GuardProcessor).GetField("UseNativeImportedAudio", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(processor, true);
            processor.PrepareImportedAssets(output); var report = processor.Run();
            if (late) { a.clip = b.clip = source; typeof(GuardProcessor).GetMethod("FinishSdkBuild", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(processor, null); }
            AssetDatabase.SaveAssetIfDirty(container);
            var copy = a.clip;
            Check(copy != source && copy == b.clip && EditorUtility.IsPersistent(copy) && UsernameNameGenerator.IsGeneratedName(copy.name), "Native audio identity, persistence or sharing changed");
            Check(copy.name.StartsWith(marker.userName + "_", StringComparison.Ordinal) && report.NativeAudioCopies == 1, "Native naming/count missing");
            Check(!Directory.GetFiles(output, "*.wav").Any() && AssetDatabase.GetAssetPath(copy).EndsWith(".asset", StringComparison.Ordinal), "Audio was reimported instead of saved independently");
            string generatedName = copy.name; copy.name = source.name;
            Check(EditorJsonUtility.ToJson(copy) == original, "Native encoded resource, compression or loading flags changed"); copy.name = generatedName;
            if (loadType == AudioClipLoadType.DecompressOnLoad)
            {
                var aa = new float[source.samples * source.channels]; var bb = new float[copy.samples * copy.channels];
                Check(source.GetData(aa, 0) && copy.GetData(bb, 0) && aa.SequenceEqual(bb), "Native PCM samples changed");
            }
            processor.PrepareImportedAssets(output);
            typeof(GuardProcessor).GetMethod("FinishSdkBuild", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(processor, null);
            Check(a.clip == copy && b.clip == copy && report.NativeAudioCopies == 1, "Repeated completion recopied native audio");
            Check(original == EditorJsonUtility.ToJson(source) && bytes.SequenceEqual(File.ReadAllBytes(path)) && meta.SequenceEqual(File.ReadAllBytes(path + ".meta")), "Audio source/importer changed");
        }
    }
}
