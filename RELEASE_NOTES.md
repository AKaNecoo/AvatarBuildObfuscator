## 中文

0.3.0-beta.1 将名称改为每位用户自己的“用户名 + 128 个 ASCII I/l”，支持中文。两档均扩展到层级、支持的 Humanoid 骨骼、自定义参数和 Animator 内部名称；激进接受未知依赖风险。新增独立的 BlendShape 索引、GameObject 同级顺序和顶点顺序混淆，无需运行时解密。

动画、Mask、菜单、Driver、PhysBone、Contact、口型与眼皮引用同步更新；SDK proxy 保持真实身份，NadeVision 等明确固定接口保留。贴图（含不可读/Crunch）和音频使用独立 Importer 副本，并统一批量导入，避免逐份复制反复触发导入。源场景、原资产和 .meta 不覆盖。

需要 Unity2022.3、AvatarSDK3.7.0+、NDMF1.14.8+。从 README 的 beta Git 地址安装一次，以后 Package Manager 点击 Update。main 保持稳定，旧 Releases 附件不等同最新 beta。没有 OSC、密码、运行时解密或自动上传；贴图、最终几何和必要形变仍可提取。验证范围见 TESTING.md，实际 VRChat 客户端/设备验收尚未完成。

## English

0.3.0-beta.1 uses each user's visible Unicode username plus 128 ASCII I/l characters. Both modes expand to hierarchy, supported Humanoid bones, custom parameters and Animator internal names; Aggressive accepts unknown dependency risks. Independent switches permute morph indices, sibling order and vertex order without runtime decryption.

Animation/mask, menu/driver, PhysBone/Contact and native lip/eyelid references are remapped. SDK proxies retain canonical identity; recognized fixed contracts such as NadeVision remain intact. Imported texture/audio copies use a single batch, including unreadable/Crunch textures, without editing original assets/importers/GUIDs.

Requires Unity2022.3, AvatarSDK3.7.0+, NDMF1.14.8+. Install the README beta Git URL once and later click Package Manager Update. Main remains stable; older release attachments may lag beta. No OSC, password, runtime decoder or automatic upload. Textures, final geometry and required morphs remain extractable. See TESTING.md for evidence; actual VRChat client/device acceptance is pending.

## 日本語

0.3.0-beta.1 は各ユーザーの Unicode ユーザー名に 128 個の ASCII I/l を付けます。両モードとも階層、対応 Humanoid 骨、カスタムパラメーター、Animator 内部名を難読化します。激進モードは未知の依存リスクを許容します。形状インデックス・兄弟順・頂点順は独立したスイッチで、実行時復号は不要です。

アニメーション・Mask、メニュー・Driver、PhysBone・Contact、口形・まぶた参照を更新します。SDK proxy の本来の識別と NadeVision などの固定契約を保持します。読み取り不可・Crunch を含むテクスチャと音声は独立コピーを一括インポートし、元アセット・Importer・GUID は変更しません。

Unity2022.3、AvatarSDK3.7.0+、NDMF1.14.8+ が必要です。README の beta Git URL を一度追加し、以後 Package Manager の Update を使います。main は安定版です。OSC、パスワード、実行時復号、自動アップロードはありません。テクスチャ・最終形状・必要な形状データは抽出可能です。検証範囲は TESTING.md を参照してください。実クライアント・実機の確認は未完了です。