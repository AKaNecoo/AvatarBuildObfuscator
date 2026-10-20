# Avatar Build Obfuscator

[中文](https://github.com/AKaNecoo/AvatarBuildObfuscator/blob/main/README.md) · [English](https://github.com/AKaNecoo/AvatarBuildObfuscator/blob/main/README.en.md) · [日本語](https://github.com/AKaNecoo/AvatarBuildObfuscator/blob/main/README.ja.md)

## 中文

VRChat Avatar 构建时混淆工具，自动处理 NDMF 构建副本，不覆盖源资产，无需运行时解密。

1. 在 Avatar 根对象添加 **Avatar Tools → Avatar Build Obfuscator**。
2. 填写用户名并选择保守或激进模式。新组件默认读取已有 Avatar ID，可自行修改。
3. 使用 VRChat SDK 正常构建；源工程名称保持原样。

支持“用户名＋`Ì/Í/Î/Ï`”名称混淆、固定／未使用 BlendShape 烘焙移除，以及形变、同级对象和顶点顺序重排。保守保护未知依赖，激进允许未知兼容风险。

组件内可切换语言、检查或安装更新。自动安装默认关闭。混淆不保证防提取，贴图、最终几何和必要动态形变仍可提取。

## English

Build-time obfuscation for VRChat avatars. NDMF processes a build copy without overwriting source assets; no runtime decoder is required.

1. Add **Avatar Tools → Avatar Build Obfuscator** to the avatar root.
2. Enter your username and choose Conservative or Aggressive. New components default to an existing avatar ID and remain editable.
3. Build normally with the VRChat SDK; original project names stay unchanged.

Supports username + `Ì/Í/Î/Ï` names, baking/removing supported fixed or unused BlendShapes, and morph/sibling/vertex order shuffling. Conservative protects unknown dependencies; Aggressive accepts unknown compatibility risks.

Choose the display language and check/install updates in the component. Automatic installation defaults off. Textures, final geometry and required dynamic shapes remain extractable.

## 日本語

VRChat アバターのビルド時難読化ツールです。NDMF がコピーを処理し、元アセットは上書きしません。実行時復号は不要です。

1. ルートに **Avatar Tools → Avatar Build Obfuscator** を追加します。
2. ユーザー名と保守／激進モードを設定します。新規追加時は既存の Avatar ID が初期値になり、自由に変更できます。
3. VRChat SDK で通常どおりビルドします。元の名前は変わりません。

「ユーザー名＋`Ì/Í/Î/Ï`」の名前、対応可能な固定・未使用 BlendShape のベイクと除去、形状・兄弟・頂点順の並べ替えに対応します。保守は未知の依存を保護し、激進は未知の互換性リスクを許容します。

コンポーネントで言語選択と更新ができます。自動インストールは初期状態でオフです。テクスチャ、最終形状、必要な動的形状は抽出可能です。

---

Requires / 需要 / 必要環境: **Unity 2022.3 · Avatar SDK 3.7.0+ · NDMF 1.14.8+**.

[Installation and full documentation / 安装与完整说明 / 導入・詳細](https://github.com/AKaNecoo/AvatarBuildObfuscator)

License: [GPL-3.0-only](LICENSE). Copyright (c) 2026 Avatar Build Obfuscator contributors. Earlier MIT releases and separately installed dependencies retain their original terms.
