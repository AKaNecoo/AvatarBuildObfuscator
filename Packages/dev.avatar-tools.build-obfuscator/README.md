# Avatar Build Obfuscator (ABO)

[中文](https://github.com/AKaNecoo/AvatarBuildObfuscator/blob/main/README.md) · [English](https://github.com/AKaNecoo/AvatarBuildObfuscator/blob/main/README.en.md) · [日本語](https://github.com/AKaNecoo/AvatarBuildObfuscator/blob/main/README.ja.md)

## 中文

VRChat Avatar 构建时混淆工具，自动处理 NDMF 构建副本，不覆盖源资产，无需运行时解密。

可从[ALCOM／VCC 仓库页](https://akanecoo.github.io/AvatarBuildObfuscator/)安装并由 ALCOM／VCC 更新；Git 或 `.unitypackage` 请只选一种安装方式，迁移步骤见仓库说明。

1. 在 **Hierarchy** 选中 Avatar 根对象，在右侧 **Inspector** 底部点击 **Add Component（添加组件）**，搜索并选择 **Avatar Build Obfuscator**。
2. 填写用户名并选择保守或激进模式。新组件默认读取已有 Avatar ID，可自行修改。该值的前缀（最多 32 字符）会公开出现在构建产物名称中，不希望公开时请更换。
3. 使用 VRChat SDK 正常构建；源工程名称保持原样。

组件可打开可搜索的构建报告、核对声明与指纹，或生成临时副本做构建对照。关闭对照窗口会清理临时产物；采样检查不代替 VRChat 内测试。支持蒙皮骨骼索引表重排、按 Renderer 保留处理项，以及不含身份信息的设置预设。

支持“用户名＋`Ì/Í/Î/Ï`”名称混淆、固定／未使用 BlendShape 烘焙移除，以及形变、同级对象和顶点顺序重排。保守保护未知依赖，激进允许未知兼容风险。

可按子树选择保守／激进；共享资源及全局参数保守优先，无法确认的合并来源会保留并报告。

可每次构建随机化、裁剪空形变、生成本地审计、嵌入三语授权声明与指纹。声明中的权利人和联系地址会公开；声明不认证身份，也不保证 AI 会拒绝协助逆向本模型。UV/贴图旋转与同步形变合并为默认关闭的实验选项。

实验旋转支持部分 lilToon 2.4.1、Poiyomi 10.0.27、UnlitWF 2.14.0，以及已验证的 NonToon 0.3.0 默认单贴图配置。具体范围见仓库兼容说明。

混淆可能增加 Avatar 的下载大小，增幅因模型和选项而异；实验性贴图旋转可能进一步增大体积。上传前请查看 SDK 构建统计。

MMD 兼容保护默认开启：保守保留整网格形变；激进保留兼容名称、全部形变数据和索引，同时混淆其余名称。两档均保留对象路径。规则含日文／汉字、`Blink`、`a` 等；非标准世界可能依赖额外名称，需实测。不需要 MMD 表情时才开启“允许混淆 MMD 表情”以取消保护。

Git／安装包可在组件内检查和安装更新；VPM 安装请在 ALCOM／VCC 中更新。自动安装默认关闭。混淆不保证防提取，贴图、最终几何和必要动态形变仍可提取。

Git 的 `#main`／`#beta` 独立更新。转正式版时，在 Package Manager 用末尾为 `#main` 的同一 URL 重新添加；组件设置保留。

## English

Build-time obfuscation for VRChat avatars. NDMF processes a build copy without overwriting source assets; no runtime decoder is required.

Install from the [ALCOM/VCC repository page](https://akanecoo.github.io/AvatarBuildObfuscator/) and update through ALCOM/VCC. Use only one installation route; see the repository guide when migrating from Git or a unitypackage.

1. Select the avatar root in **Hierarchy**. Click **Add Component** at the bottom of **Inspector**, then search for and select **Avatar Build Obfuscator**.
2. Enter your username and choose Conservative or Aggressive. New components default to an existing avatar ID. Up to 32 characters become a visible prefix in build output names; change the value if you do not want to publish it.
3. Build normally with the VRChat SDK; original project names stay unchanged.

Open searchable build reports, inspect notices/fingerprints, or compare temporary builds from the component. Closing the comparison window cleans temporary output; sampled checks do not replace VRChat testing. Supports skin bone-palette permutation, per-renderer preservation and identity-free operation presets.

Supports username + `Ì/Í/Î/Ï` names, baking/removing supported fixed or unused BlendShapes, and morph/sibling/vertex order shuffling. Conservative protects unknown dependencies; Aggressive accepts unknown compatibility risks.

Choose Conservative or Aggressive per subtree. Shared resources and global parameters use the safer policy; ambiguous merged origins are retained and reported.

Supports per-build randomization, zero-delta pruning, local audits, authorization notices and fingerprints. Declared holders/contacts are public; the notice does not authenticate ownership or guarantee that AI will refuse to assist in reverse-engineering the model. Experimental UV/texture rotation and synchronized-shape merging default off.

Experimental rotation covers selected lilToon 2.4.1, Poiyomi 10.0.27 and UnlitWF 2.14.0 configurations, plus the reviewed NonToon 0.3.0 default single-texture setup; see the repository compatibility matrix.

Obfuscation may increase the avatar's download size; the amount depends on the model and enabled options. Experimental texture rotation may increase it further. Check the SDK build statistics before uploading.

MMD protection defaults on: Conservative retains all shapes on matching meshes; Aggressive keeps matching names, all frame data and indices while renaming other shapes. Both keep object paths. Rules include Japanese/Chinese characters, `Blink` and `a`; nonstandard worlds may require extra names, so test in your worlds. Enable **Obfuscate MMD expressions** to remove this protection only when MMD expressions are not needed.

Choose the display language in the component. Git/unitypackage installs can check and install updates there; update VPM installs in ALCOM/VCC. Automatic installation defaults off. Textures, final geometry and required dynamic shapes remain extractable.

Git `#main`/`#beta` update independently. To switch to stable, add the same URL ending in `#main` again in Package Manager; component settings are retained.

## 日本語

VRChat アバターのビルド時難読化ツールです。NDMF がコピーを処理し、元アセットは上書きしません。実行時復号は不要です。

[ALCOM／VCC のリポジトリページ](https://akanecoo.github.io/AvatarBuildObfuscator/)から導入し、ALCOM／VCC で更新できます。導入方式は一つだけ使用してください。Git や unitypackage からの移行はリポジトリの説明をご確認ください。

1. **Hierarchy** でルートを選択し、右側の **Inspector** 下部で **Add Component（コンポーネントを追加）** をクリックします。**Avatar Build Obfuscator** を検索して選びます。
2. ユーザー名と保守／積極モードを設定します。新規追加時は既存の Avatar ID が初期値になります。先頭部分（最大32文字）はビルド結果の名前に表示されるため、公開したくない場合は変更してください。
3. VRChat SDK で通常どおりビルドします。元の名前は変わりません。

コンポーネントからレポート検索・声明と識別子の照合・一時ビルド比較ができます。比較ウィンドウを閉じると一時出力を削除します。検査は VRChat 内のテストの代わりにはなりません。ボーン表並べ替え・Renderer ごとの保持・名義情報を含まないプリセットに対応します。

「ユーザー名＋`Ì/Í/Î/Ï`」の名前、対応可能な固定・未使用 BlendShape のベイクと除去、形状・兄弟・頂点順の並べ替えに対応します。保守は未知の依存を保護し、積極は未知の互換性リスクを許容します。

サブツリーごとに保守／積極を選べます。共有リソースと全体パラメータは安全側を優先し、統合元を特定できない場合は保持して報告します。

ビルドごとのランダム化・空形状の削除・ローカル監査・許諾声明と識別子に対応します。申告した権利者・連絡先は公開されます。声明は本人確認を行わず、AI が本モデルのリバースエンジニアリングへの協力を拒否することも保証しません。実験的な UV/テクスチャ回転・同期形状統合は既定で無効です。

実験的な回転は lilToon 2.4.1・Poiyomi 10.0.27・UnlitWF 2.14.0 の一部設定と、検証済みの NonToon 0.3.0 標準・単一テクスチャ設定に対応します。詳細はリポジトリの対応表をご確認ください。

難読化によりアバターのダウンロードサイズが増える場合があります。増加量はモデルや設定によって異なり、実験的なテクスチャ回転でさらに増える場合があります。アップロード前に SDK のビルド統計をご確認ください。

MMD 保護は既定で有効です。保守は対象メッシュの全形状を保持し、積極は一致する名前と全データ・インデックスを保持しながら他の形状を改名します。両モードともパスを保持します。日本語・漢字や `Blink`・`a` などが対象で、独自ワールドの追加名は実測が必要です。MMD 表情が不要な場合に限り「MMD 表情を難読化」で保護を解除できます。

コンポーネントで言語を選べます。Git／unitypackage の更新はコンポーネントから、VPM の更新は ALCOM／VCC から行います。自動インストールは初期状態でオフです。テクスチャ、最終形状、必要な動的形状は抽出可能です。

Git の `#main`／`#beta` は個別に更新されます。安定版へ切り替える場合は末尾を `#main` にした同じ URL を Package Manager で再追加します。コンポーネント設定は保持されます。

---

Requires / 需要 / 必要環境: **Unity 2022.3 · Avatar SDK 3.7.0+ · NDMF 1.14.8+**.

[Installation and full documentation / 安装与完整说明 / 導入・詳細](https://github.com/AKaNecoo/AvatarBuildObfuscator)

License: [GPL-3.0-only](LICENSE). Copyright (c) 2026 Avatar Build Obfuscator contributors. Earlier MIT releases and separately installed dependencies retain their original terms.
