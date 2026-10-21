// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed class BuildComparisonWindow : EditorWindow
    {
        private InspectorLanguage language;
        private GameObject source;
        private BuildComparison session;
        private Label status;
        private Image baseline, obfuscated;
        private Button run;
        private string T(string zh, string en, string ja) => BuildReportWindow.T(language, zh, en, ja);

        public static void Open(GameObject source, InspectorLanguage language)
        {
            var window = GetWindow<BuildComparisonWindow>(); window.source = source; window.language = language;
            window.titleContent = new GUIContent("Avatar Build Obfuscator · Compare"); window.minSize = new Vector2(650, 450);
            window.CreateGUI(); window.Show();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement; root.Clear();
            root.style.paddingLeft = root.style.paddingRight = 8;
            root.style.paddingTop = root.style.paddingBottom = 8;
            var target = new ObjectField(T("源 Avatar 根对象", "Source avatar root", "元アバターのルート")) { objectType = typeof(GameObject), allowSceneObjects = true, value = source };
            target.RegisterValueChangedCallback(e => source = e.newValue as GameObject); root.Add(target);
            root.Add(new HelpBox(T("生成两份临时构建副本，采样初始几何、手臂姿势和可匹配形变。不会上传或保存源场景。关闭窗口会清理本工具的临时产物。", "Builds two temporary copies and samples initial geometry, an arm pose and matching BlendShapes. Does not upload or save the source scene. Closing this window cleans this tool's temporary output.", "一時コピーを2つビルドし、初期形状・腕のポーズ・対応する形状を検査します。アップロードや元シーンの保存は行いません。閉じると本ツールの一時出力を削除します。"), HelpBoxMessageType.Info));
            run = new Button(() => { run.SetEnabled(false); root.schedule.Execute(Run); }) { text = T("构建并检查", "Build and inspect", "ビルドして検査") }; root.Add(run);
            var statusScroll = new ScrollView(); statusScroll.style.maxHeight = 120; statusScroll.style.flexShrink = 0; root.Add(statusScroll);
            status = new Label(); status.style.whiteSpace = WhiteSpace.Normal; statusScroll.Add(status);
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexGrow = 1; row.style.flexBasis = 0; row.style.minHeight = 0; row.style.marginTop = 6; root.Add(row);
            baseline = Panel(row, T("未混淆构建副本", "Baseline build copy", "難読化なしのコピー"));
            obfuscated = Panel(row, T("混淆构建副本", "Obfuscated build copy", "難読化したコピー"));
            root.Add(new HelpBox(T("采样通过不代表所有功能已验证。Animator 状态切换、物理、联网、外部设备和 VRChat 客户端行为仍需实际测试。", "Passing these samples does not verify all functionality. Animator transitions, physics, network, external devices and VRChat client behavior require actual testing.", "サンプルの合格は全機能の検証ではありません。Animator の遷移・物理・通信・外部機器・VRChat 内の動作は実際のテストが必要です。"), HelpBoxMessageType.Warning));
        }

        private static Image Panel(VisualElement parent, string title)
        {
            var column = new VisualElement(); column.style.flexGrow = 1; column.style.flexBasis = 0; column.style.minHeight = 0; parent.Add(column); column.Add(new Label(title));
            var image = new Image { scaleMode = ScaleMode.ScaleToFit }; image.style.flexGrow = 1; image.style.flexBasis = 0; image.style.minHeight = 0; column.Add(image); return image;
        }

        private void Run()
        {
            run.SetEnabled(false);
            try
            {
                baseline.image = obfuscated.image = null;
                session?.Dispose(); session = new BuildComparison();
                var result = session.Run(source);
                baseline.image = session.Render(false); obfuscated.image = session.Render(true);
                string outcome = result.status == "sample-checks-passed" ? T("采样检查通过", "Sample checks passed", "サンプル検査に合格") : result.status == "source-change-detected" ? T("检测到源数据变化，请查看结果", "Source changes detected; inspect the result", "元データの変更を検出。結果をご確認ください") : result.status == "failed" ? T("检查未完成", "Check did not complete", "検査が完了しませんでした") : T("检测到差异，请查看结果", "Differences detected; inspect the result", "差分を検出。結果をご確認ください");
                status.text = outcome + "\n" + T("几何采样／最大位置差／未解析绑定", "Geometry samples / maximum position delta / unresolved bindings", "形状サンプル・最大位置差・未解決のバインド") + ": " + result.geometrySamples + " / " + result.maximumPositionError + " / " + result.missingBindings +
                    "\n" + T("源内存未变／源文件未变／检查文件数", "Source memory unchanged / source files unchanged / files checked", "元メモリ不変・元ファイル不変・検査ファイル数") + ": " + result.sourceMemoryUnchanged + " / " + result.sourceFilesUnchanged + " / " + result.sourceFilesChecked +
                    "\n" + (result.error ?? "") + "\n" + string.Join("\n", result.problems);
            }
            catch (Exception e) { status.text = e.Message; session?.Dispose(); session = null; }
            finally { run.SetEnabled(true); }
        }

        private void OnDisable() { session?.Dispose(); session = null; }
    }
}
