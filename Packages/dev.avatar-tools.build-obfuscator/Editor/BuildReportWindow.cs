// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed class BuildReportWindow : EditorWindow
    {
        private InspectorLanguage language;
        private GuardReport report;
        private string selectedPath, query = "";
        private VisualElement body;
        private readonly List<AuditEntry> filtered = new List<AuditEntry>();
        private ListView entries;
        private GameObject metadataTarget;
        private Label metadataStatus;

        internal static string T(InspectorLanguage language, string zh, string en, string ja) => language == InspectorLanguage.English ? en : language == InspectorLanguage.Japanese ? ja : zh;
        private string T(string zh, string en, string ja) => T(language, zh, en, ja);

        [MenuItem("Tools/Avatar Build Obfuscator/Build Reports")]
        public static void OpenMenu() => Open(InspectorLanguage.SimplifiedChinese);
        public static void Open(InspectorLanguage language)
        {
            var window = GetWindow<BuildReportWindow>(); window.language = language;
            window.titleContent = new GUIContent("ABO · Reports"); window.minSize = new Vector2(540, 420);
            window.CreateGUI(); window.Show();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement; root.Clear();
            root.style.paddingLeft = root.style.paddingRight = 10;
            root.style.paddingTop = root.style.paddingBottom = 8;
            var locale = new PopupField<string>("Language", InspectorLocalization.LanguageNames.ToList(), (int)language);
            locale.RegisterValueChangedCallback(e => { language = (InspectorLanguage)locale.index; CreateGUI(); }); root.Add(locale);
            var toolbar = new Toolbar(); root.Add(toolbar);
            toolbar.Add(new ToolbarButton(RefreshHistory) { text = T("刷新报告", "Refresh reports", "レポートを更新") });
            toolbar.Add(new ToolbarButton(() => { if (File.Exists(selectedPath)) EditorUtility.RevealInFinder(selectedPath); }) { text = T("打开报告文件", "Reveal report file", "レポートファイルを表示") });
            body = new VisualElement(); body.style.flexGrow = 1; root.Add(body);
            var inspection = new Foldout { text = T("声明与指纹核对", "Notice and fingerprint inspection", "声明と識別子の照合"), value = false }; root.Add(inspection);
            var field = new ObjectField(T("构建副本／已导入模型", "Build copy / imported model", "ビルドコピー・導入済みモデル")) { objectType = typeof(GameObject), allowSceneObjects = true, value = metadataTarget };
            field.RegisterValueChangedCallback(e => metadataTarget = e.newValue as GameObject); inspection.Add(field);
            inspection.Add(new Button(InspectMetadata) { text = T("检查并匹配本地记录", "Inspect and match local records", "検査してローカル記録と照合") });
            metadataStatus = new Label(T("仅核对所选对象；缺失不证明原产物未写入，匹配也不证明所有权。", "Inspects the selected object. Missing metadata may have been stripped; a match does not prove ownership.", "選択対象のみ検査します。欠落は出力時の未記録を意味せず、一致も所有権の証明ではありません。"));
            metadataStatus.style.whiteSpace = WhiteSpace.Normal; inspection.Add(metadataStatus);
            RefreshHistory();
        }

        private void RefreshHistory()
        {
            body.Clear();
            var files = Directory.Exists(BuildMetadataInspection.ReportsFolder) ? new DirectoryInfo(BuildMetadataInspection.ReportsFolder)
                .GetFiles("*.json").OrderByDescending(f => f.LastWriteTimeUtc).ToArray() : new FileInfo[0];
            if (files.Length == 0) { body.Add(new HelpBox(InspectorLocalization.Text("noAudit", language), HelpBoxMessageType.Info)); return; }
            int index = Math.Max(0, Array.FindIndex(files, f => f.FullName == selectedPath));
            var choices = files.Select(f => f.Name).ToList();
            var history = new PopupField<string>(T("本地历史", "Local history", "ローカル履歴"), choices, index); body.Add(history);
            var content = new VisualElement(); content.style.flexGrow = 1; body.Add(content);
            history.RegisterValueChangedCallback(e => Load(files[history.index].FullName, content));
            Load(files[index].FullName, content);
        }

        private void Load(string path, VisualElement content)
        {
            selectedPath = path; content.Clear();
            try
            {
                report = JsonUtility.FromJson<GuardReport>(File.ReadAllText(path));
                if (report == null || string.IsNullOrEmpty(report.BuildFingerprint)) throw new InvalidDataException("Invalid report");
            }
            catch (Exception e) { content.Add(new HelpBox(e.Message, HelpBoxMessageType.Error)); return; }
            var heading = new Label((report.AvatarName ?? "") + " · " + report.Mode + " · " + report.PackageVersion + "\n" + report.CreatedUtc + "\n" + report.BuildFingerprint + " · " + report.AuditStage);
            heading.style.whiteSpace = WhiteSpace.Normal; content.Add(heading);
            if (string.IsNullOrEmpty(report.CreatedUtc)) content.Add(new HelpBox(T("旧版报告未记录全部统计；缺少的字段不能按零处理量解读。", "Older reports lack some statistics. Missing fields do not indicate zero processing.", "旧版の記録には一部の統計がありません。欠落項目を処理件数ゼロと解釈しないでください。"), HelpBoxMessageType.Info));
            var summary = new Label(T("形变", "Shapes", "形状") + ": " + report.ShapesBefore + " → " + report.ShapesAfter +
                "   |   " + T("资源改名", "Renamed assets", "アセット改名") + ": " + report.RenamedAssets +
                "   |   " + T("对象改名", "Renamed objects", "オブジェクト改名") + ": " + report.RenamedGameObjects +
                "\n" + T("顶点重排／骨骼表／UV 网格", "Vertex / bone-palette / UV meshes", "頂点・ボーン表・UV メッシュ") + ": " + report.ReorderedMeshes + " / " + report.ReorderedBonePalettes + " / " + report.TransformedUvMeshes +
                "\n" + T("材质声明／指纹／总材质", "Material notices / fingerprints / total materials", "声明・識別子・全マテリアル") + ": " + report.NoticeMaterials + " / " + report.FingerprintMaterials + " / " + report.OutputMaterials);
            summary.text += "\n" + T("输出对象／网格／贴图", "Output objects / meshes / textures", "出力オブジェクト・メッシュ・テクスチャ") + ": " + report.OutputObjects + " / " + report.OutputMeshes + " / " + report.OutputTextures;
            if (report.TransformedUvMaterials > 0)
            {
                summary.text += "\n" + T("采样变换材质／贴图／GPU解码", "Sampling materials / textures / GPU decoded", "サンプリング変換マテリアル・テクスチャ・GPU読出し") + ": " +
                    report.TransformedUvMaterials + " / " + report.TransformedUvTextures + " / " + report.DecodedUvTextures +
                    "  (" + (report.UvTextureBytes / (1024d * 1024)).ToString("F1") + " MiB)";
                summary.text += "\n" + T("无损压缩块旋转／BC7重压缩", "Lossless block rotation / BC7 re-encoded", "無損失ブロック回転・BC7再圧縮") + ": " +
                    report.LosslessCompressedUvTextures + " / " + report.RecompressedUvTextures;
            }
            summary.text += "\n" + T("已启用：顶点／骨骼表／UV／形变合并", "Enabled: vertex / palette / UV / shape merge", "有効: 頂点・ボーン表・UV・形状統合") + ": " + report.VertexPermutationEnabled + " / " + report.BonePaletteEnabled + " / " + report.UvEnabled + " / " + report.ShapeMergeEnabled;
            if (report.SubtreeOverrides != 0)
                summary.text += "\n" + T("子树配置／保守Renderer／激进Renderer／来源回退", "Subtree overrides / conservative / aggressive / provenance fallback", "サブツリー設定・保守Renderer・積極Renderer・由来不明の保持") + ": " +
                    report.SubtreeOverrides + " / " + report.ConservativeScopeRenderers + " / " + report.AggressiveScopeRenderers + " / " + report.ScopeFallbackRenderers;
            summary.style.whiteSpace = WhiteSpace.Normal; summary.style.marginTop = summary.style.marginBottom = 8; content.Add(summary);
            var notes = new Foldout { text = T("处理与保留说明", "Processing and preservation notes", "処理と保持の説明"), value = false };
            foreach (var note in report.Notes ?? new List<string>()) { var label = new Label(note); label.style.whiteSpace = WhiteSpace.Normal; notes.Add(label); }
            content.Add(notes);
            content.Add(new HelpBox(T("以下是保留项和原始引用，不是成功混淆列表。历史记录的源对象可能已不可定位。", "The list shows retained items and source references, not successful renames. Historical source objects may no longer resolve.", "以下は保持項目と元参照です。改名成功の一覧ではありません。履歴の元オブジェクトは特定できない場合があります。"), HelpBoxMessageType.Info));
            var search = new ToolbarSearchField { value = query }; search.RegisterValueChangedCallback(e => { query = e.newValue; Filter(); }); content.Add(search);
            entries = new ListView { itemsSource = filtered, fixedItemHeight = 76, selectionType = SelectionType.Single };
            entries.style.flexGrow = 1;
            entries.makeItem = () =>
            {
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
                var label = new Label(); label.name = "entry"; label.style.flexGrow = 1; label.style.whiteSpace = WhiteSpace.Normal; row.Add(label);
                var locate = new Button(() => Locate(row.userData as AuditEntry)) { text = T("定位", "Locate", "表示") }; row.Add(locate); return row;
            };
            entries.bindItem = (row, i) => { var entry = filtered[i]; row.userData = entry; row.Q<Label>("entry").text = entry.category + " · " + entry.type + " · " + entry.name + "\n" + entry.path + "\n" + entry.reason; };
            content.Add(entries); Filter();
        }

        private void Filter()
        {
            filtered.Clear();
            if (report?.AuditEntries != null) filtered.AddRange(report.AuditEntries.Where(e => string.IsNullOrEmpty(query) ||
                (e.category + " " + e.type + " " + e.name + " " + e.path + " " + e.reason).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));
            entries?.Rebuild();
        }

        private static void Locate(AuditEntry entry)
        {
            if (entry == null) return;
            UnityEngine.Object value = null;
            if (GlobalObjectId.TryParse(entry.sourceGlobalId ?? "", out var id)) value = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
            if (value == null && !string.IsNullOrEmpty(entry.path) && (entry.path.StartsWith("Assets/", StringComparison.Ordinal) || entry.path.StartsWith("Packages/", StringComparison.Ordinal)))
                value = AssetDatabase.LoadMainAssetAtPath(entry.path);
            if (value != null) { Selection.activeObject = value; EditorGUIUtility.PingObject(value); }
        }

        private void InspectMetadata()
        {
            if (metadataTarget == null) { metadataStatus.text = T("请先选择对象。", "Select an object first.", "対象を選択してください。"); return; }
            try
            {
                var result = BuildMetadataInspection.Inspect(metadataTarget);
                metadataStatus.text = T("材质：声明／指纹／总数", "Materials: notices / fingerprints / total", "マテリアル: 声明・識別子・合計") + " " + result.noticeMaterials + " / " + result.fingerprintMaterials + " / " + result.materials +
                    "\n" + T("层级：声明／指纹", "Hierarchy: notices / fingerprints", "階層: 声明・識別子") + " " + result.hierarchyNotices + " / " + result.hierarchyFingerprints +
                    "\n" + string.Join("\n", result.fingerprints) + "\n" + T("匹配的本地报告／无效标识", "Matching local reports / invalid IDs", "一致する記録・無効な識別子") + ": " + result.matchingReports.Count + " / " + result.invalidFingerprints;
            }
            catch (Exception e) { metadataStatus.text = e.Message; }
        }
    }
}
