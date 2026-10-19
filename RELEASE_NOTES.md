## 中文

首个公开版本。导入 Unity 包后，在 Avatar 根对象添加「一个木禾 / Avatar Guard」，构建时自动处理。提供保守与激进模式，支持中文前缀，处理资源名称、静态形变烘焙移除及动态形变名称混淆。无需 OSC。

验证：26 项合成检查通过；完整模型两种模式构建及保存重载检查通过；最终构建未记录到 Unity Error/Assert。请先阅读 README 的处理范围和兼容限制。不可读及 Crunch 贴图保留原名。

本工具不加密贴图或网格，保留的动态形变仍可提取。未进行真实 VRChat 客户端或设备验收。依赖 Unity 2022.3、Avatar SDK 与 NDMF；AAO 可选。

## English

First public release. Import the Unity package, add **一个木禾 / Avatar Guard** to your avatar root, and build normally. Includes Conservative/Aggressive modes, Unicode prefixes, copied-asset naming, supported static-shape baking/removal and retained-shape name rewriting. No OSC required.

Validation: 26 synthetic checks passed; both modes completed full-avatar NDMF builds and saved-scene reload checks without recorded build Error/Assert logs. Read the scope and compatibility notes before use. Unreadable and Crunch textures keep their original names.

This is not texture/mesh encryption; retained dynamic shapes remain extractable. Actual VRChat client/device acceptance has not been performed. Requires Unity 2022.3, Avatar SDK and NDMF. AAO is optional.

## 日本語

初回公開版です。Unity パッケージを導入し、アバターのルートに **一个木禾 / Avatar Guard** を追加して通常通りビルドします。保守・激進モード、Unicode 接頭辞、アセット名の変更、対応する固定形状のベイク・削除、保持する動的形状の改名に対応します。OSC は不要です。

検証：26 項目の合成チェックに合格し、両モードの NDMF ビルド・保存後の再読み込みを確認しました。最終ビルドで Error/Assert は記録されませんでした。README の対応範囲をご確認ください。読み取り不可・Crunch テクスチャは元の名前を保持します。

テクスチャ・メッシュの暗号化ではなく、動的形状の抽出は可能です。実際の VRChat クライアント・実機での受け入れ確認は未実施です。Unity 2022.3、Avatar SDK、NDMF が必要です。AAO は任意です。
