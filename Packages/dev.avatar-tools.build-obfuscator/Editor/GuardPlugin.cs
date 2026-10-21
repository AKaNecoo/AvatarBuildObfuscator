// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(AvatarTools.Obfuscation.Editor.GuardPlugin))]

namespace AvatarTools.Obfuscation.Editor
{
    internal sealed class GuardBuildState
    {
        public GuardSettings Settings;
        public GuardReport Report;
    }

    internal sealed class GuardPlugin : Plugin<GuardPlugin>
    {
        public override string QualifiedName => "dev.avatar-tools.build-obfuscator";
        public override string DisplayName => "Avatar Build Obfuscator";

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).Run("Read Avatar Guard settings", context =>
            {
                var markers = context.AvatarRootObject.GetComponentsInChildren<AvatarBuildObfuscator>(true);
                foreach (var marker in markers)
                {
                    if (marker.gameObject == context.AvatarRootObject && marker.enabled)
                        context.GetState<GuardBuildState>().Settings = new GuardSettings(marker);
                    else if (marker.enabled)
                        Debug.LogWarning("[Avatar Build Obfuscator] 组件必须位于 Avatar 根对象；已跳过子对象上的组件。", marker);
                    Object.DestroyImmediate(marker);
                }
            });

            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("com.anatawa12.avatar-optimizer")
                .AfterPlugin("nadena.dev.modular-avatar")
                .AfterPlugin("com.vrcfury.vrcfury")
                .AfterPlugin("net.rs64.tex-trans-tool")
                .AfterPlugin("dev.cocokoishi.avatar-obfuscator")
                .Run("Bake static shapes and obfuscate copied assets", context =>
                {
                    using (PackageUpdates.DuringBuild())
                    {
                        var state = context.GetState<GuardBuildState>();
                        BuildComparison.Capture(context.AvatarRootObject);
                        if (state.Settings == null) return;
                        var processor = new GuardProcessor(context.AvatarRootObject, state.Settings,
                            asset => context.AssetSaver.SaveAsset(asset));
                        var containerPath = UnityEditor.AssetDatabase.GetAssetPath(context.AssetSaver.CurrentContainer);
                        if (!string.IsNullOrEmpty(containerPath))
                            processor.PrepareImportedAssets(System.IO.Path.GetDirectoryName(containerPath)?.Replace('\\', '/'));
                        using (context.OpenSerializationScope())
                        {
                            state.Report = processor.Run();
                        }
                        Debug.Log("[Avatar Build Obfuscator] " + state.Report.Summary, context.AvatarRootObject);
                        foreach (var note in state.Report.Notes)
                            Debug.Log("[Avatar Build Obfuscator] 跳过/保留：" + note, context.AvatarRootObject);
                    }
                });
        }
    }
}
