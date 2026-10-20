# Avatar Build Obfuscator 0.3.0

0.3.0：组件搜索名为 **Avatar Build Obfuscator**，用户名保持左标签、右输入框的单行布局，使用原生绑定文本控件。Search **Avatar Build Obfuscator**; the username keeps its single-row layout with a native bound text field. **Avatar Build Obfuscator** を検索。ユーザー名は1行配置のネイティブ入力欄です。

## 中文

顶部 **语言 / Language / 言語** 支持简体中文、English、日本語，选择保存在组件中，只影响界面显示。0.3.0-beta.2 修复了默认 NDMF 临时目录被误拒绝、导致 Play 预处理失败的问题。

在 Avatar 根对象添加 **Avatar Tools / Avatar Build Obfuscator**，填写自己的用户名，NDMF 构建时自动处理副本。支持中文，名称显示为 `用户名_IlIl…`，后缀由 128 个 ASCII I/l 组成。新组件用户名留空。

两档均处理支持的资源、层级/动画/Mask 路径、Humanoid 骨骼、自定义参数及动画内部名称。保守保护未知依赖；激进允许未知风险。可独立打乱 BlendShape 索引、GameObject 同级顺序和网格顶点顺序，无需解密。权重、眼皮索引、动画与骨骼映射同步，UI/重复同名等明确顺序依赖保留。

贴图（含不可读/Crunch）与音频使用独立文件及 Importer 副本，一次批量导入，保持源压缩和读取设置。原场景/FBX/纹理/材质/动画/Importer/.meta 不覆盖。构建产物中的贴图与必要动态形变仍可提取。没有 OSC、密码、运行时解密或自动上传。

需要 Unity2022.3、Avatar SDK3.7.0+、NDMF1.14.8+。AAO/MA/VRCFury/TTT 可选。查看 Console 的 `[Avatar Build Obfuscator]` 数量与保留原因。源工程名称不变，检查实际构建副本。升级后检查新增开关和组件引用。

Git 测试版：

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#beta
```

在 Package Manager 的 Add package from git URL 输入一次，以后选中包点击 Update。`#main` 跟随稳定版。只安装一份；旧 Assets 安装迁移至 Git 前应移除重复插件文件。旧 Releases 附件可能落后于 beta。

## English

Use **语言 / Language / 言語** for Simplified Chinese, English or Japanese Inspector text. The per-component choice affects display only. Beta.2 fixes Play preprocessing by accepting NDMF's actual default generated-output tree.

Add the component to the avatar root and enter your own Unicode username. NDMF processes its build copy using visible username + 128 ASCII I/l names. Supported hierarchy, Humanoid, parameter and animation references are remapped. Independent switches shuffle morph indices, sibling order and mesh vertices. Conservative protects uncertain dependencies; Aggressive accepts unknown compatibility risks. Required shapes remain usable; no runtime decryption is needed.

Texture/audio copies use a single import batch without editing source assets/importers. Requires Unity2022.3, Avatar SDK3.7.0+, NDMF1.14.8+. Install the beta Git URL above once, then use Package Manager Update. Original assets remain unchanged; uploaded textures and necessary morphs remain extractable.

## 日本語

上部の **语言 / Language / 言語** で簡体字中国語・English・日本語を切り替えます。コンポーネントごとの表示設定で、処理結果は変わりません。beta.2 では NDMF 既定一時フォルダーを許可し、Play 前処理の失敗を修正しました。

ルートにコンポーネントを追加し、自分のユーザー名を入力します。NDMF のビルドコピーを `ユーザー名_IlIl…` で難読化し、階層・Humanoid・パラメーター・アニメーションの参照を更新します。形状インデックス、兄弟順、頂点順は独立したスイッチです。保守モードは不明な依存を保持し、激進モードは未知の互換性リスクを許容します。

テクスチャ・音声のコピーを一括インポートし、元アセット・Importer は変更しません。Unity2022.3、Avatar SDK3.7.0+、NDMF1.14.8+ が必要です。上記 Git URL を一度追加し、以後 Package Manager の Update を使います。暗号化や OSC は不要で、必要な形状とテクスチャの抽出を防ぐ保証はありません。

[Full documentation / 完整说明](https://github.com/AKaNecoo/AvatarBuildObfuscator)

## 更新 / Updates / 更新

组件内提供三语版本提示、检查与一键更新。自动检查开启，自动安装默认关闭。Git和官方Assets安装包均支持；本地file开发包和手动固定版本不覆盖。安装包校验SHA-256后导入，完成/失败/取消后删除临时下载。设置仅保存在工程UserSettings。

Git and official Assets installations support update checks, one-click updates and opt-in automatic installs. Checks default on, installs off. Temporary downloads are verified and deleted after import/failure/cancellation. Local development installs and manual pins are respected. Preferences are project-local.

Gitと公式Assets導入に対応し、更新確認・手動更新・任意の自動インストールを提供します。自動インストールは初期状態でオフです。一時パッケージは検証後、成功・失敗・キャンセル時に削除します。開発用ローカル版と手動固定版は保持し、設定は工程内UserSettingsに保存します。

CI builds UPM TGZ, ZIP, Unity archives and checksums. Hosted package checks do not replace Unity/Avatar runtime tests. See repository README/TESTING for scope.
