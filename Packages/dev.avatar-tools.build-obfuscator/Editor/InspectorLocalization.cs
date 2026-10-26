// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System.Collections.Generic;
using UnityEngine;

namespace AvatarTools.Obfuscation.Editor
{
    // Editor-only copy. No Localization/Addressables dependency or runtime locale state.
    internal static class InspectorLocalization
    {
        internal static readonly string[] LanguageNames = { "简体中文", "English", "日本語" };
        private static readonly Dictionary<string, string[]> Copy = new Dictionary<string, string[]>
        {
            { "update.vpm", new[] { "此安装由 VPM 管理。请在 ALCOM／VCC 的工程包列表中更新，组件不会通过 Git 或安装包覆盖它。", "This installation is managed by VPM. Update in your project's ALCOM/VCC package list; the component will not replace it through Git or unitypackage.", "VPM 管理のインストールです。ALCOM／VCC のプロジェクトのパッケージ一覧で更新してください。このコンポーネントは Git・unitypackage で上書きしません。" } },
            { "update.vpmGuide", new[] { "ALCOM／VCC 使用说明", "ALCOM / VCC guide", "ALCOM／VCC のガイド" } },
            { "subtreeModes", new[] { "按子树设置模式", "Subtree mode overrides", "サブツリー別のモード" } },
            { "subtreeModes.tip", new[] { "拖入本Avatar内的子树根对象。最近的配置祖先生效，同一对象重复时使用最后一项；共享资源和全局参数/控制器保守优先。优化器合并来源不明确时保留并报告。", "Choose subtree roots within this avatar. Nearest configured ancestor wins; the last duplicate wins. Shared resources and global parameters/controllers use conservative priority. Ambiguous optimizer merges are retained and reported.", "このAvatar内のサブツリーのルートを指定します。最も近い設定済み祖先を継承し、重複時は最後の設定を使用します。共有アセット・全体パラメータ・コントローラーは保守優先です。最適化による統合元が不明な場合は保持して報告します。" } },
            { "subtreeRoot", new[] { "子树根对象", "Subtree root", "サブツリーのルート" } },
            { "addSubtreeMode", new[] { "添加子树配置", "Add subtree override", "サブツリー設定を追加" } },
            { "removeSubtreeMode", new[] { "移除此配置", "Remove override", "この設定を削除" } },
            { "compareBuild", new[] { "测试构建与对照检查", "Test build and comparison", "テストビルドと比較検査" } },
            { "preset", new[] { "处理设置预设", "Operation preset", "処理設定プリセット" } },
            { "savePreset", new[] { "保存开关与模式", "Save operations and mode", "処理とモードを保存" } },
            { "applyPreset", new[] { "应用预设（保留身份与引用）", "Apply preset (keep identity / references)", "適用（名義・参照を保持）" } },
            { "reorderBonePalette", new[] { "重排蒙皮骨骼索引表", "Reorder skin bone palettes", "スキニングのボーン索引表を並べ替え" } },
            { "reorderBonePalette.tip", new[] { "同时重映射独立网格的骨骼权重、绑定矩阵和 Renderer 骨骼数组。不改父子关系；Cloth、网格切换和不完整骨骼表保留。", "Remaps copied mesh weights, bind poses and renderer bone arrays together. Keeps hierarchy intact; preserves Cloth, mesh swaps and incomplete palettes.", "コピーのウェイト・バインド行列・Renderer のボーン配列を一緒に再対応付けします。階層は変えず、Cloth・メッシュ切替・不完全な表を保持します。" } },
            { "preserveVertexOrderRenderers", new[] { "保留顶点顺序的 Renderer", "Preserve vertex order on renderers", "頂点順を保持する Renderer" } },
            { "preserveBonePaletteRenderers", new[] { "保留骨骼索引表的 Renderer", "Preserve bone palettes on renderers", "ボーン索引表を保持する Renderer" } },
            { "excludeUvRenderers", new[] { "跳过 UV 处理的 Renderer", "Skip UV processing on renderers", "UV 処理を除外する Renderer" } },
            { "randomizeEachBuild", new[] { "每次构建重新随机化", "Randomize each build", "ビルドごとにランダム化" } },
            { "randomizeEachBuild.tip", new[] { "同一用户名每次生成不同后缀与排列。关闭可复现相同输入；随机盐只存在于构建内存。", "Generates new suffixes and permutations each build. Disable for repeatable inputs; salts stay in build memory.", "同じユーザー名でも毎回異なる接尾辞と順序を生成します。無効時は同じ入力で再現可能です。ソルトはビルド中のメモリにのみ保持します。" } },
            { "auditBuild", new[] { "生成构建残留审计报告", "Write build audit report", "ビルド監査レポートを保存" } },
            { "auditBuild.tip", new[] { "报告保留名称、源资源引用和保留原因。仅写入工程 Library，不随模型上传。", "Reports retained names, source references and reasons. Stored in project Library, outside the avatar.", "保持名・元アセット参照・保持理由を記録します。プロジェクトの Library にのみ保存し、モデルには含めません。" } },
            { "viewAudit", new[] { "查看最近一次构建报告", "Show latest build report", "最新のビルドレポートを表示" } },
            { "noAudit", new[] { "尚无构建报告；启用审计并构建后再查看。", "No build report yet. Enable auditing and build first.", "レポートはまだありません。監査を有効にしてビルドしてください。" } },
            { "pruneEmptyBlendShapes", new[] { "移除无效果的空形变", "Prune zero-delta shapes", "変化のない空形状を削除" } },
            { "pruneEmptyBlendShapes.tip", new[] { "仅删除所有帧的位置、法线和切线位移都严格为零的形变，并移除对应动画曲线。保留 SDK 接口、手动保留和未知驱动。", "Removes shapes with exactly zero position, normal and tangent deltas in every frame and their curves. Preserves SDK contracts, exclusions and unknown drivers.", "全フレームの位置・法線・接線差分が厳密にゼロの形状と曲線のみ削除します。SDK 契約・保持設定・未知の駆動は保護します。" } },
            { "embedOwnershipNotice", new[] { "嵌入三语授权声明", "Embed authorization notice", "三言語の許諾声明を埋め込む" } },
            { "rightsHolder", new[] { "声明的权利人", "Declared rights holder", "申告する権利者" } },
            { "rightsContact", new[] { "公开联系地址", "Public contact", "公開の連絡先" } },
            { "usageRestrictions", new[] { "补充使用条款", "Additional terms", "追加の利用条件" } },
            { "ownershipHelp", new[] { "声明提示协助逆向者停止并核实原作者授权。内容会随模型公开，不证明身份、也不保证 AI 拒绝。未填写权利人时不指定任何人。", "Asks assistants to stop and verify the original creator's authorization. Public model metadata; it neither authenticates ownership nor guarantees AI refusal. A blank holder names nobody.", "解析を支援する者に中止と原作者の許諾確認を求めます。モデルと公開されるメタデータであり、本人確認や AI の拒否を保証しません。権利者が空欄なら誰も指定しません。" } },
            { "embedBuildFingerprint", new[] { "嵌入构建指纹", "Embed build fingerprint", "ビルド識別子を埋め込む" } },
            { "embedBuildFingerprint.tip", new[] { "在副本的材质标签与空对象内写入标识，可对照本地报告；可被移除，不是防篡改签名。关闭随机化时标识也可复现。", "Adds an ID to copied material tags and empty objects for matching local reports. Removable, not a tamper-proof signature. Repeatable when randomization is off.", "コピーのマテリアルタグと空オブジェクトに識別子を追加し、ローカルレポートと照合できます。削除可能で、改ざん防止署名ではありません。ランダム化が無効なら再現可能です。" } },
            { "experiments", new[] { "实验功能（默认关闭）", "Experimental features (off by default)", "実験的な機能（既定で無効）" } },
            { "experimentalUvTransform", new[] { "UV 与贴图旋转（实验）", "UV / texture rotation (experimental)", "UV・テクスチャ回転（実験）" } },
            { "mergeSynchronizedBlendShapes", new[] { "合并完全同步的动态形变（实验）", "Merge identically driven shapes", "完全に同期した動的形状を統合" } },
            { "experimentsHelp", new[] { "旋转支持内置 Unlit、部分 lilToon 2.4.1／Poiyomi 10.0.27／UnlitWF 2.14.0，以及已验证的 NonToon 0.3.0 默认单贴图配置。压缩贴图转换可能增加体积；不兼容项保留并记录。同步形变合并要求初始权重及全部已知曲线一致，并保留口型/眼皮。请先验证构建副本。", "Rotation supports built-in Unlit, selected lilToon 2.4.1 / Poiyomi 10.0.27 / UnlitWF 2.14.0 configurations, and the reviewed NonToon 0.3.0 default single-texture setup. Decoded compressed textures may increase size; incompatible inputs are retained and reported. Shape merging requires matching initial weights/all known curves and preserves visemes/eyelids. Validate build copies first.", "組み込み Unlit、lilToon 2.4.1・Poiyomi 10.0.27・UnlitWF 2.14.0 の一部設定、および検証済みの NonToon 0.3.0 標準・単一テクスチャ設定で回転に対応します。圧縮テクスチャの変換で容量が増える場合があります。未対応項目は保持・記録します。形状統合は初期ウェイトと既知の全曲線の一致が必要で、口形・まぶたを保持します。ビルドコピーで検証してください。" } },
            { "update.title", new[] { "版本与更新", "Version and updates", "バージョンと更新" } },
            { "update.ready", new[] { "可检查当前渠道的已验证更新。", "Check this channel for verified updates.", "現在のチャンネルの検証済み更新を確認できます。" } },
            { "update.checking", new[] { "正在后台检查更新…", "Checking in the background…", "バックグラウンドで確認中…" } },
            { "update.available", new[] { "有已通过 CI 的新版本可安装。", "A newer CI-verified version is available.", "CI を通過した新バージョンがあります。" } },
            { "update.current", new[] { "当前已是此渠道的最新可用版本。", "No newer installable version on this channel.", "このチャンネルに適用可能な新バージョンはありません。" } },
            { "update.failed", new[] { "更新检查或安装失败。保留当前版本，可稍后手动重试。", "Update check or installation failed. The current version is retained; retry manually later.", "確認またはインストールに失敗しました。現在のバージョンを保持します。後で手動で再試行できます。" } },
            { "update.unverified", new[] { "此渠道暂无通过 CI 的更新记录。", "No successful CI update is published on this channel yet.", "このチャンネルには CI を通過した更新記録がありません。" } },
            { "update.unmanaged", new[] { "本地包、其他仓库或手动固定版本不自动更新。请通过官方 Git main/beta 安装。", "Local packages, other repositories and manually pinned versions are not updated. Install the official Git main/beta channel.", "ローカル・別リポジトリ・手動固定版は更新しません。公式 Git main/beta チャンネルを使用してください。" } },
            { "update.busy", new[] { "编辑器繁忙；退出 Play/构建并关闭 VRChat SDK 控制面板后可更新。", "Editor busy. Leave Play/build and close the VRChat SDK control panel to update.", "エディターが使用中です。Play・ビルドを終了し、VRChat SDK パネルを閉じて更新してください。" } },
            { "update.installing", new[] { "正在安装，完成后 Unity 将重新编译。", "Installing; Unity will recompile after completion.", "インストール中です。完了後 Unity が再コンパイルします。" } },
            { "update.check", new[] { "检查更新", "Check now", "更新を確認" } },
            { "update.install", new[] { "立即更新", "Update now", "今すぐ更新" } },
            { "update.notes", new[] { "更新说明", "Changelog", "変更履歴" } },
            { "update.autoCheck", new[] { "自动检查更新（每 6 小时）", "Automatic checks (every 6 hours)", "自動確認（6時間ごと）" } },
            { "update.autoInstall", new[] { "打开工程后自动安装更新", "Install updates after opening the project", "プロジェクト起動後に自動インストール" } },
            { "update.autoInstall.tip", new[] { "默认关闭。开启自动检查后生效；启动至少60秒且Unity在后台空闲时执行，避开Play/构建/编译/导入及SDK面板。同一提交失败后不自动重试。设置仅保存在此工程UserSettings中。", "Off by default. Requires automatic checks; runs after 60 seconds with Unity idle in the background, outside Play/build/compile/import and with the SDK panel closed. A failed commit is not retried automatically. Stored in this project's UserSettings only.", "初期状態はオフです。自動確認が必要で、起動60秒後以降、Unity がバックグラウンドで待機中に実行します。Play・ビルド・コンパイル・インポート・SDK パネル表示中は待機します。同じコミットへの失敗は自動再試行しません。設定はこのプロジェクトの UserSettings に保存します。" } },
            { "language.tip", new[] { "仅切换本组件界面语言；不改变用户名、模式或混淆结果。", "Changes this component's Inspector language only; username, mode and build output are unaffected.", "このコンポーネントの表示言語のみ変更します。ユーザー名・モード・ビルド結果には影響しません。" } },
            { "intro", new[] { "在 Avatar 根对象添加一次即可。两种模式都只处理 NDMF 构建副本；源 Hierarchy / Scene / Project 中的对象和资源保持原名。", "Add once to the avatar root. Both modes process the NDMF build copy; source objects and assets in Hierarchy / Scene / Project keep their names.", "Avatar のルートに1つ追加します。両モードとも NDMF のビルドコピーを処理し、元の Hierarchy / Scene / Project の名前は保持します。" } },
            { "mode", new[] { "处理模式", "Mode", "処理モード" } },
            { "conservative", new[] { "保守（兼容性优先）", "Conservative (compatibility first)", "保守（互換性を優先）" } },
            { "aggressive", new[] { "激进（接受未知依赖风险）", "Aggressive (accept unknown risks)", "積極（未知の依存リスクを許容）" } },
            { "userName", new[] { "用户名", "Username", "ユーザー名" } },
            { "userName.tip", new[] { "新添加时默认填写模型的 avtr_… ID，可自行修改，支持中文。名称为 用户名_ÌÍÎÏ…，该前缀会公开出现在构建产物中；不希望公开默认 ID 时请更换。输入全体也参与生成种子。", "New components default to the avatar's avtr_… ID when available. Edit freely, including Unicode. Names use username_ÌÍÎÏ… with a publicly visible prefix; change the default ID if you do not want it published. The complete input seeds the sequence.", "新規追加時はモデルの avtr_… ID を既定値にします。Unicode を含め自由に変更できます。名前は ユーザー名_ÌÍÎÏ… となり、この接頭辞はビルド結果に表示されます。ID を公開したくない場合は変更してください。入力全体をシードにも使います。" } },
            { "naming", new[] { "名称为 用户名_ÌÍÎÏ…，后缀为 Ì/Í/Î/Ï（U+00CC–00CF）64 字符，承载 128 位命名信息。前缀最多 32 字符，不安全字符替换为下划线。相同输入与调用顺序生成相同序列；这是名称混淆，无需运行时解密。", "Names use username_ÌÍÎÏ… with 64 U+00CC–00CF symbols carrying 128 bits of naming information. Prefixes have at most 32 characters; unsafe characters become underscores. The same input and call order repeat the sequence. This is naming obfuscation, with no runtime decryption.", "名前は ユーザー名_ÌÍÎÏ… と U+00CC–00CF の64文字（命名情報128ビット）です。プレフィックスは最大32文字で、使用不可文字は下線に置換します。同じ入力と呼び出し順で同じ系列になります。名前の難読化であり、実行時の復号は不要です。" } },
            { "usernameRequired", new[] { "请先填写用户名再构建。新添加时使用模型 ID 作为默认值；没有 ID 时可自行填写。", "Enter a username before building. New components use the avatar ID as a default when available; otherwise fill it in manually.", "ビルド前にユーザー名を入力してください。新規追加時はモデルIDを既定値にします。IDがない場合は手動で入力できます。" } },
            { "renameAssets", new[] { "混淆资源名称", "Rename assets", "アセット名を難読化" } },
            { "freezeUnusedBlendShapes", new[] { "烘焙固定和未使用形变", "Bake static / unused shapes", "固定・未使用形状をベイク" } },
            { "renameDynamicBlendShapes", new[] { "混淆保留的形变名称", "Rename retained shapes", "保持する形状名を難読化" } },
            { "allowMmdObfuscation", new[] { "允许混淆 MMD 表情", "Obfuscate MMD expressions", "MMD 表情を難読化" } },
            { "allowMmdObfuscation.tip", new[] { "默认关闭：任一形变名符合兼容规则（含日文／汉字、Blink、a 等）时，保留该网格全部形变的名称、数据、顺序和对象路径。部分非 MMD 网格也可能被保留。开启后 MMD 世界表情可能失效。", "Off by default: one matching shape name (including Japanese/Chinese characters, Blink or a) preserves the mesh's complete shape names, data, order and object path. Some non-MMD meshes may also be retained. Turning this on may break expressions in MMD worlds.", "既定ではオフです。形状名が一つでもルールに一致すると（日本語・漢字、Blink、a など）、そのメッシュの全形状名・データ・順序とオブジェクトパスを保持します。MMD 用ではないメッシュも保持される場合があります。有効にすると MMD ワールドの表情が動かなくなる可能性があります。" } },
            { "shuffleBlendShapeOrder", new[] { "打乱 BlendShape 索引顺序", "Shuffle BlendShape order", "BlendShape の順序を変更" } },
            { "shuffleBlendShapeOrder.tip", new[] { "打乱保留形变的索引，同步权重和 SDK 眼皮索引。独立于名称混淆。", "Permutes retained shape indices and remaps weights and SDK eyelid indices. Independent of renaming.", "保持する形状のインデックスを変更し、ウェイトと SDK のまぶた参照を更新します。改名とは独立です。" } },
            { "renameGameObjects", new[] { "混淆构建副本对象名", "Rename build-copy objects", "ビルドコピーの名前を変更" } },
            { "renameGameObjects.tip", new[] { "同时重写动画与 AvatarMask 路径；保守模式保留无法验证的依赖。", "Rewrites animation and AvatarMask paths. Conservative preserves unverified dependencies.", "アニメーションと AvatarMask のパスも更新します。保守モードでは未確認の依存を保持します。" } },
            { "shuffleGameObjectOrder", new[] { "打乱 GameObject 同级顺序", "Shuffle sibling order", "GameObject の兄弟順を変更" } },
            { "shuffleGameObjectOrder.tip", new[] { "不改变父子关系和坐标。UI、重复同名和已知顺序依赖保留。", "Keeps parenting and local transforms. UI, duplicate names and known order dependencies retain order.", "親子関係とローカル座標は維持します。UI・同名の兄弟・既知の順序依存は保持します。" } },
            { "renameBones", new[] { "混淆 Humanoid 骨骼", "Rename Humanoid bones", "Humanoid ボーン名を変更" } },
            { "renameBones.tip", new[] { "重建 Humanoid Avatar，并核对骨骼映射与尺度。需要启用对象改名。", "Rebuilds the Human Avatar and checks bone identity and scale. Requires object renaming.", "Human Avatar を再構築し、ボーン対応とスケールを確認します。オブジェクトの改名が必要です。" } },
            { "renameParameters", new[] { "混淆自定义运行参数", "Rename custom parameters", "独自パラメーター名を変更" } },
            { "renameAnimatorInternals", new[] { "混淆 Animator 内部名称", "Rename Animator internals", "Animator 内部名を変更" } },
            { "renameAnimatorInternals.tip", new[] { "处理动画层、状态机、状态和过渡名称，维持执行顺序与层索引。", "Renames layers, state machines, states and transitions while keeping execution order and layer indices.", "レイヤー・ステートマシン・ステート・遷移名を変更し、実行順とレイヤー番号を維持します。" } },
            { "reorderMeshVertices", new[] { "重排网格顶点（无需解密）", "Permute mesh vertices", "頂点順を変更（復号不要）" } },
            { "reorderMeshVertices.tip", new[] { "同步全部关联数据。保守模式仅处理确认不依赖顶点 ID 的网格。", "Permutes all associated vertex data together. Conservative requires audited vertex-ID-independent shaders.", "関連する頂点データをまとめて変更します。保守モードは頂点 ID に依存しないと確認した Shader のみ対象です。" } },
            { "preserveGameObjects", new[] { "保留名称/顺序的对象与子树", "Preserve objects / subtrees", "名前・順序を保持する階層" } },
            { "preserveGameObjects.tip", new[] { "保留指定对象及子树和所需祖先的名称与同级顺序，适用于外部工具。", "Preserves names and sibling order for selected subtrees and required ancestors, for external integrations.", "外部連携用に、指定した階層と必要な祖先の名前・兄弟順を保持します。" } },
            { "preserveParameters", new[] { "保留原名的参数（末尾 *）", "Preserve parameters (* suffix)", "保持するパラメーター（末尾 *）" } },
            { "preserveParameters.tip", new[] { "末尾 * 表示前缀匹配。VRChat 内置参数自动保留。", "A trailing * matches a prefix. VRChat built-in parameters are automatically preserved.", "末尾の * は前方一致です。VRChat の組み込みパラメーターは自動保持します。" } },
            { "preserveBlendShapes", new[] { "额外保留原名的形变", "Preserve shape names / data", "元名・データを保持する形状" } },
            { "preserveBlendShapes.tip", new[] { "为外部工具或特殊世界保留形变原名和数据；索引顺序开关仍独立生效。", "Keeps original shape names and data for external tools/worlds. Shape-order permutation remains independent.", "外部ツール・ワールド用に形状名とデータを保持します。インデックス順の変更は独立した設定です。" } },
            { "excludedRenderers", new[] { "跳过形变处理的 Renderer", "Skip shapes on renderers", "形状処理を除外する Renderer" } },
            { "excludedRenderers.tip", new[] { "跳过这些 Renderer 的形变处理和形变索引重排，资源名仍可混淆。", "Skips shape editing and index permutation for these renderers. Asset names can still change.", "指定した Renderer の形状処理と形状インデックス変更を除外します。アセット名は変更できます。" } },
            { "aggressiveHelp", new[] { "激进模式处理未知驱动和名称依赖，也允许自定义 Shader 顶点重排。MMD 世界表情默认保留；只有主动开启“允许混淆 MMD 表情”才处理。外部世界、工具或 Shader 仍可能受影响。", "Aggressive processes unknown drivers and name dependencies and permits custom-shader vertex permutation. MMD world expressions are preserved by default and processed only when explicitly enabled. External worlds, tools or shaders may still be affected.", "積極モードは未知のドライバーや名前依存を処理し、独自 Shader の頂点順変更も許可します。MMD ワールドの表情は既定で保持し、明示的に有効化した場合のみ難読化します。外部ワールド・ツール・Shader への影響は残ります。" } },
            { "conservativeHelp", new[] { "保守模式同样处理层级、自定义参数和 Animator 内部名；Avatar 重建需通过骨骼预检。未知驱动、外部字符串路径和未验证的顶点 ID 依赖会保留，MMD 兼容形变保持原名。", "Conservative also handles hierarchy, custom parameters and Animator internals; Avatar rebuilding must pass bone checks. Unknown drivers, external string paths and unverified vertex-ID dependencies are preserved. MMD compatibility shapes keep their names.", "保守モードも階層・独自パラメーター・Animator 内部名を処理し、Avatar 再構築はボーン検証を必須にします。未知のドライバー・外部文字列パス・未確認の頂点 ID 依存を保持し、MMD 互換形状は元名を維持します。" } },
            { "limits", new[] { "构建结果可直接使用，无 OSC、密码或运行时解密。贴图与必要动态形变仍可提取。VRChat 内置参数、已知面捕接口、代理动画和渲染属性契约保持兼容。", "Build output needs no OSC, password or runtime decryption. Textures and required dynamic shapes remain extractable. VRChat built-ins, known face-tracking inputs, SDK proxy motions and rendering property contracts are preserved.", "OSC・パスワード・実行時復号は不要です。テクスチャと必要な動的形状は抽出可能です。VRChat 組み込み引数・既知の面捕入力・SDK proxy・描画属性の契約を保持します。" } },
            { "imports", new[] { "导入贴图（含不可读与 Crunch）在构建临时目录独立复制，保持格式；源文件与 Read/Write 不变。未支持类型会报告保留原因。", "Imported textures, including unreadable/Crunch assets, use independent build-temp copies with the same format. Source files and Read/Write settings stay unchanged. Unsupported types report why they were kept.", "読み取り不可・Crunch を含むテクスチャをビルド一時領域に独立コピーし、形式を維持します。元ファイルと Read/Write 設定は変えません。未対応の種類は保持理由を記録します。" } },
            { "verification", new[] { "构建后在 Console 搜索 [Avatar Build Obfuscator] 查看数量和保留原因，并检查实际构建副本或产物。", "After building, search Console for [Avatar Build Obfuscator] to inspect counts and preservation reasons. Check the actual build copy/output.", "ビルド後、Console で [Avatar Build Obfuscator] を検索して件数と保持理由を確認し、実際のビルドコピー・出力を調べてください。" } }
        };

        internal static string Text(string key, InspectorLanguage language)
        {
            int index = (int)language;
            if (index < 0 || index >= LanguageNames.Length) index = 0;
            return Copy.TryGetValue(key, out var values) ? values[index] : key;
        }

        internal static GUIContent Content(string key, InspectorLanguage language)
        {
            string tooltipKey = key + ".tip";
            return new GUIContent(Text(key, language), Copy.ContainsKey(tooltipKey) ? Text(tooltipKey, language) : "");
        }
    }
}
