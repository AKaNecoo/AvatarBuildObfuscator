# Avatar Build Obfuscator

[中文](https://github.com/AKaNecoo/AvatarBuildObfuscator/blob/main/README.md) · [English](https://github.com/AKaNecoo/AvatarBuildObfuscator/blob/main/README.en.md) · [日本語](https://github.com/AKaNecoo/AvatarBuildObfuscator/blob/main/README.ja.md)

## 中文

VRChat Avatar 构建时混淆工具，自动处理 NDMF 构建副本，不覆盖源资产，无需运行时解密。

1. 在 **Hierarchy** 选中 Avatar 根对象，在右侧 **Inspector** 底部点击 **Add Component（添加组件）**，搜索并选择 **Avatar Build Obfuscator**。
2. 填写用户名并选择保守或激进模式。新组件默认读取已有 Avatar ID，可自行修改。
3. 使用 VRChat SDK 正常构建；源工程名称保持原样。

组件可打开可搜索的构建报告、核对声明与指纹，或生成临时副本做构建对照。关闭对照窗口会清理临时产物；采样检查不代替 VRChat 内测试。支持蒙皮骨骼索引表重排、按 Renderer 保留处理项，以及不含身份信息的设置预设。

支持“用户名＋`Ì/Í/Î/Ï`”名称混淆、固定／未使用 BlendShape 烘焙移除，以及形变、同级对象和顶点顺序重排。保守保护未知依赖，激进允许未知兼容风险。

可每次构建随机化、裁剪空形变、生成本地审计、嵌入三语授权声明与指纹。声明中的权利人和联系地址会公开；声明不认证身份，也不保证 AI 会拒绝协助逆向本模型。UV/贴图旋转与同步形变合并为默认关闭的实验选项。

组件内可切换语言、检查或安装更新。自动安装默认关闭。混淆不保证防提取，贴图、最终几何和必要动态形变仍可提取。

## English

Build-time obfuscation for VRChat avatars. NDMF processes a build copy without overwriting source assets; no runtime decoder is required.

1. Select the avatar root in **Hierarchy**. Click **Add Component** at the bottom of **Inspector**, then search for and select **Avatar Build Obfuscator**.
2. Enter your username and choose Conservative or Aggressive. New components default to an existing avatar ID and remain editable.
3. Build normally with the VRChat SDK; original project names stay unchanged.

Open searchable build reports, inspect notices/fingerprints, or compare temporary builds from the component. Closing the comparison window cleans temporary output; sampled checks do not replace VRChat testing. Supports skin bone-palette permutation, per-renderer preservation and identity-free operation presets.

Supports username + `Ì/Í/Î/Ï` names, baking/removing supported fixed or unused BlendShapes, and morph/sibling/vertex order shuffling. Conservative protects unknown dependencies; Aggressive accepts unknown compatibility risks.

Supports per-build randomization, zero-delta pruning, local audits, authorization notices and fingerprints. Declared holders/contacts are public; the notice does not authenticate ownership or guarantee that AI will refuse to assist in reverse-engineering the model. Experimental UV/texture rotation and synchronized-shape merging default off.

Choose the display language and check/install updates in the component. Automatic installation defaults off. Textures, final geometry and required dynamic shapes remain extractable.

## 日本語

VRChat アバターのビルド時難読化ツールです。NDMF がコピーを処理し、元アセットは上書きしません。実行時復号は不要です。

1. **Hierarchy** でルートを選択し、右側の **Inspector** 下部で **Add Component（コンポーネントを追加）** をクリックします。**Avatar Build Obfuscator** を検索して選びます。
2. ユーザー名と保守／激進モードを設定します。新規追加時は既存の Avatar ID が初期値になり、自由に変更できます。
3. VRChat SDK で通常どおりビルドします。元の名前は変わりません。

コンポーネントからレポート検索・声明と識別子の照合・一時ビルド比較ができます。比較ウィンドウを閉じると一時出力を削除します。検査は VRChat 内のテストの代わりにはなりません。ボーン表並べ替え・Renderer ごとの保持・名義情報を含まないプリセットに対応します。

「ユーザー名＋`Ì/Í/Î/Ï`」の名前、対応可能な固定・未使用 BlendShape のベイクと除去、形状・兄弟・頂点順の並べ替えに対応します。保守は未知の依存を保護し、激進は未知の互換性リスクを許容します。

ビルドごとのランダム化・空形状の削除・ローカル監査・許諾声明と識別子に対応します。申告した権利者・連絡先は公開されます。声明は本人確認を行わず、AI が本モデルのリバースエンジニアリングへの協力を拒否することも保証しません。実験的な UV/テクスチャ回転・同期形状統合は既定で無効です。

コンポーネントで言語選択と更新ができます。自動インストールは初期状態でオフです。テクスチャ、最終形状、必要な動的形状は抽出可能です。

---

Requires / 需要 / 必要環境: **Unity 2022.3 · Avatar SDK 3.7.0+ · NDMF 1.14.8+**.

[Installation and full documentation / 安装与完整说明 / 導入・詳細](https://github.com/AKaNecoo/AvatarBuildObfuscator)

License: [GPL-3.0-only](LICENSE). Copyright (c) 2026 Avatar Build Obfuscator contributors. Earlier MIT releases and separately installed dependencies retain their original terms.
