using nadena.dev.ndmf;
using UnityEngine;

[assembly: ExportsPlugin(typeof(Kiine.AvatarGuard.Editor.GuardPlugin))]

namespace Kiine.AvatarGuard.Editor
{
    internal sealed class GuardBuildState
    {
        public GuardSettings Settings;
        public GuardReport Report;
    }

    internal sealed class GuardPlugin : Plugin<GuardPlugin>
    {
        public override string QualifiedName => "dev.kiine.avatar-guard";
        public override string DisplayName => "一个木禾 · Avatar Guard";

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).Run("Read Avatar Guard settings", context =>
            {
                var markers = context.AvatarRootObject.GetComponentsInChildren<AvatarGuard>(true);
                foreach (var marker in markers)
                {
                    if (marker.gameObject == context.AvatarRootObject && marker.enabled)
                        context.GetState<GuardBuildState>().Settings = new GuardSettings(marker);
                    else if (marker.enabled)
                        Debug.LogWarning("[Avatar Guard] 组件必须位于 Avatar 根对象；已跳过子对象上的组件。", marker);
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
                    var state = context.GetState<GuardBuildState>();
                    if (state.Settings == null) return;
                    using (context.OpenSerializationScope())
                    {
                        state.Report = new GuardProcessor(context.AvatarRootObject, state.Settings,
                            asset => context.AssetSaver.SaveAsset(asset)).Run();
                    }
                    Debug.Log("[Avatar Guard] " + state.Report.Summary, context.AvatarRootObject);
                    foreach (var note in state.Report.Notes)
                        Debug.Log("[Avatar Guard] 跳过/保留：" + note, context.AvatarRootObject);
                });
        }
    }
}
