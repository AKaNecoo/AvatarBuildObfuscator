## 中文

**0.3.1** 新增三语更新区、按工程的自动检查与可选自动安装。Git安装跟随通过CI的具体提交；安装包方式校验已发布包的SHA-256后导入，完成/失败/取消后删除下载文件。默认自动安装关闭。新增Package CI自动校验、可复现打包与Release发布；云端包检查和实际Unity验证分别记录。

**0.3.0-beta.3** 将组件搜索名与界面标题改为 **Avatar Build Obfuscator**，保留原脚本 GUID 和现有组件设置。用户名使用原生绑定文本输入框，仍为左标签、右输入框的单行布局；其余选项和构建处理不变。

**0.3.0-beta.2** 修复进入 Play 时 `BuildFrameworkOptimizeHook` 失败：允许 NDMF 自己的默认生成目录，同时保留对其他包源码与路径穿越的拒绝。新增组件顶部简体中文／English／日本語切换，完整覆盖标签、说明与提示，只改变界面。52 项合成回归通过，并验证真实 SDK 回调下进入及退出 Play。

0.3.0-beta.1 将名称改为每位用户自己的“用户名 + 128 个 ASCII I/l”，支持中文。两档均扩展到层级、支持的 Humanoid 骨骼、自定义参数和 Animator 内部名称；激进接受未知依赖风险。新增独立的 BlendShape 索引、GameObject 同级顺序和顶点顺序混淆，无需运行时解密。

动画、Mask、菜单、Driver、PhysBone、Contact、口型与眼皮引用同步更新；SDK proxy 保持真实身份，NadeVision 等明确固定接口保留。贴图（含不可读/Crunch）和音频使用独立 Importer 副本，并统一批量导入，避免逐份复制反复触发导入。源场景、原资产和 .meta 不覆盖。

需要 Unity2022.3、AvatarSDK3.7.0+、NDMF1.14.8+。从 README 的 beta Git 地址安装一次，以后 Package Manager 点击 Update。main 保持稳定，旧 Releases 附件不等同最新 beta。没有 OSC、密码、运行时解密或自动上传；贴图、最终几何和必要形变仍可提取。验证范围见 TESTING.md，实际 VRChat 客户端/设备验收尚未完成。

## English

**0.3.1** adds a localized update panel, project-local automatic checks and opt-in automatic installs. Git follows exact CI-passed commits; asset-package installs verify the published SHA-256 and remove downloads after import/failure/cancellation. Automatic installation defaults off. Package CI validates and reproducibly builds release assets; hosted package checks and actual Unity acceptance are recorded separately.

**0.3.0-beta.3** renames the component menu and heading to **Avatar Build Obfuscator**, preserving script GUID and existing settings. The username uses a native bound text field with the same label-left/input-right layout; other options and build processing remain unchanged.

**0.3.0-beta.2** fixes Play preprocessing by accepting NDMF's default generated tree while retaining vendor-source/traversal rejection. A per-component Chinese/English/Japanese selector localizes labels, help and tooltips without changing build settings. All 52 synthetic checks pass, with actual SDK-driven Play entry/exit verified.

0.3.0-beta.1 uses each user's visible Unicode username plus 128 ASCII I/l characters. Both modes expand to hierarchy, supported Humanoid bones, custom parameters and Animator internal names; Aggressive accepts unknown dependency risks. Independent switches permute morph indices, sibling order and vertex order without runtime decryption.

Animation/mask, menu/driver, PhysBone/Contact and native lip/eyelid references are remapped. SDK proxies retain canonical identity; recognized fixed contracts such as NadeVision remain intact. Imported texture/audio copies use a single batch, including unreadable/Crunch textures, without editing original assets/importers/GUIDs.

Requires Unity2022.3, AvatarSDK3.7.0+, NDMF1.14.8+. Install the README beta Git URL once and later click Package Manager Update. Main remains stable; older release attachments may lag beta. No OSC, password, runtime decoder or automatic upload. Textures, final geometry and required morphs remain extractable. See TESTING.md for evidence; actual VRChat client/device acceptance is pending.

## 日本語

**0.3.1** は三言語の更新パネル、工程単位の自動確認と任意の自動インストールを追加します。GitはCI通過コミットを導入し、パッケージ方式は公開SHA-256を確認してインポート後・失敗・キャンセル時に一時ファイルを削除します。自動インストールは初期状態でオフです。Package CIで検証・再現可能な配布生成を行い、実Unity検証とは区別します。

**0.3.0-beta.3** ではコンポーネントの検索名と見出しを **Avatar Build Obfuscator** に変更しました。既存のスクリプト GUID と設定は保持します。ユーザー名は左ラベル・右入力欄の1行配置で、ネイティブのバインド済みテキスト欄を使います。他の設定とビルド処理は変更しません。

**0.3.0-beta.2** では NDMF 既定生成先を許可し、Play 前処理の失敗を修正しました。他パッケージのソースとパストラバーサルは引き続き拒否します。コンポーネントに簡体字中国語・English・日本語の表示切り替えを追加し、ラベル・説明・ツールチップを翻訳しました。52 テストと実際の SDK 経由の Play 開始・終了を確認しています。

0.3.0-beta.1 は各ユーザーの Unicode ユーザー名に 128 個の ASCII I/l を付けます。両モードとも階層、対応 Humanoid 骨、カスタムパラメーター、Animator 内部名を難読化します。激進モードは未知の依存リスクを許容します。形状インデックス・兄弟順・頂点順は独立したスイッチで、実行時復号は不要です。

アニメーション・Mask、メニュー・Driver、PhysBone・Contact、口形・まぶた参照を更新します。SDK proxy の本来の識別と NadeVision などの固定契約を保持します。読み取り不可・Crunch を含むテクスチャと音声は独立コピーを一括インポートし、元アセット・Importer・GUID は変更しません。

Unity2022.3、AvatarSDK3.7.0+、NDMF1.14.8+ が必要です。README の beta Git URL を一度追加し、以後 Package Manager の Update を使います。main は安定版です。OSC、パスワード、実行時復号、自動アップロードはありません。テクスチャ・最終形状・必要な形状データは抽出可能です。検証範囲は TESTING.md を参照してください。実クライアント・実機の確認は未完了です。