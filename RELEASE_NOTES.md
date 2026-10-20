## 中文

**Avatar Build Obfuscator 0.4.0** 将本轮已验证的功能发布为稳定版。通过模型根对象上的组件配置，NDMF 在构建副本上自动运行，保留源资产、Importer 和脚本 GUID。

- 名称使用每位用户的“用户名 + 64 个 Ì/Í/Î/Ï”（U+00CC–00CF）。用户名同时派生种子；新组件默认读取已有 Avatar ID，可手动修改，普通编辑、清空或切换语言不会反复回填。
- 混淆支持的资源、GameObject、Humanoid 骨骼、自定义参数、Animator 内部及动态 BlendShape 名称；烘焙移除可处理的固定/未使用形变。独立开关控制形变索引、同级对象及顶点顺序重排，同步相关引用。保守模式保护未知依赖，激进模式允许未知兼容风险。
- 修复长用户名使音频复制路径过长而无法进入 Play 的问题，兼容 NDMF 默认生成目录。保留左标签、右输入框和简体中文／English／日本語界面。
- Git 与标准 unitypackage 支持检查、一键更新及可选自动更新。默认自动检查开启、自动安装关闭；下载包经校验后导入，完成、失败或取消后删除。CI 生成并回读三种格式及 SHA256 文件。

本轮通过 59 项处理回归、51 项输入断言、60 项更新策略断言、15 项实际名称生成器断言和 6 项打包测试。完整模型基线与两档构建、保存重载后的 39 个姿势及 102 对贴图采样比较通过。最终 Git 版 SDK/NDMF Play 正常进入、退出，记录 400 个混淆对象、53 根 Humanoid 骨骼和零错误。

需要 Unity 2022.3、Avatar SDK 3.7.0+、NDMF 1.14.8+。稳定版使用 README 的 `#main` Git 地址，测试版使用 `#beta`；已有 Git 安装可继续通过 Package Manager 或组件更新区更新。无需 OSC、密码或运行时解密。名称与结构混淆不保证防提取；贴图像素、最终几何和必要动态形变仍可提取。实际 VRChat 客户端及设备验收尚未完成，详细验证范围见 [TESTING.md](TESTING.md)。

## English

**Avatar Build Obfuscator 0.4.0** promotes the verified feature set to stable. Configure the component on the avatar root; NDMF processes its build copy while preserving source assets, importers and script GUIDs.

- Names use each user's visible username plus 64 symbols from Ì/Í/Î/Ï (U+00CC–00CF). The username also derives the seed. Newly added components default to an existing avatar ID and remain editable; ordinary edits, clearing and language changes do not refill the field.
- Rename supported resources, GameObjects, Humanoid bones, custom parameters, Animator internals and dynamic BlendShapes; bake and remove supported fixed/unused shapes. Independent switches permute morph indices, sibling order and vertex order, remapping related references. Conservative protects unknown dependencies; Aggressive permits unknown compatibility risks.
- Fix long copied-audio paths blocking Play with long usernames and support NDMF's default generated tree. Retain the label-left/input-right field and Chinese/English/Japanese interface.
- Check, manually update or optionally auto-update Git and standard unitypackage installations. Automatic checks default on; automatic installs default off. Downloaded installers are verified and removed after completion, failure or cancellation. CI builds and reads back all three archive formats and SHA256 files.

This iteration passes 59 processing regressions, 51 input assertions, 60 update-policy assertions, 15 checks against the actual name generator and six packaging tests. Baseline and both complete model builds pass, followed by 39 poses and 102 texture-pair samples after save/reload. Final Git SDK/NDMF Play enters and exits successfully, recording 400 generated object names, 53 Humanoid bones and zero errors.

Requires Unity 2022.3, Avatar SDK 3.7.0+ and NDMF 1.14.8+. Use the README `#main` Git URL for stable or `#beta` for testing; existing Git installations can continue through Package Manager or the component update panel. No OSC, password or runtime decoder is required. Name/structure obfuscation does not prevent extraction: texture pixels, final geometry and required dynamic morphs remain extractable. Actual VRChat client/device acceptance is not completed; see [TESTING.md](TESTING.md) for the scope.

## 日本語

**Avatar Build Obfuscator 0.4.0** は検証済みの機能を安定版として公開します。アバターのルートに追加したコンポーネントで設定し、NDMF がビルド用コピーを処理します。元アセット、Importer、スクリプト GUID は保持します。

- 名前は各ユーザーの「ユーザー名 + 64 個の Ì/Í/Î/Ï」（U+00CC–00CF）を使い、ユーザー名からシードも生成します。新規追加時は既存の Avatar ID を初期値にし、自由に編集できます。通常の編集・消去・言語切り替えでは再入力しません。
- 対応するリソース、GameObject、Humanoid の骨、カスタムパラメーター、Animator 内部、動的 BlendShape の名前を難読化し、対応可能な固定・未使用形状をベイクして除去します。形状インデックス・兄弟順・頂点順は独立したスイッチで並べ替え、関連参照を更新します。保守モードは未知の依存を保護し、積極モードは未知の互換性リスクを許容します。
- 長いユーザー名による音声コピー先のパス長が Play を阻止する問題を修正し、NDMF の既定生成先に対応します。左ラベル・右入力欄と簡体字中国語・English・日本語の表示を保持します。
- Git と標準 unitypackage の更新確認・手動更新・任意の自動更新に対応します。自動確認は初期状態でオン、自動インストールはオフです。ダウンロードしたインストーラーは検証してから導入し、完了・失敗・キャンセル後に削除します。CI は3形式と SHA256 ファイルを生成し、内容を読み戻して確認します。

今回の検証は処理回帰59項目、入力51項目、更新ポリシー60項目、実際の名前生成器15項目、パッケージ6項目を通過しました。モデルの基準ビルドと両モード、および保存・再読み込み後の39姿勢と102組のテクスチャサンプル比較も通過しました。最終 Git 版の SDK/NDMF Play は正常に開始・終了し、難読化オブジェクト400個、Humanoid 骨53本、エラー0件を記録しました。

Unity 2022.3、Avatar SDK 3.7.0+、NDMF 1.14.8+ が必要です。安定版は README の `#main` Git URL、テスト版は `#beta` を使います。既存の Git 導入は Package Manager またはコンポーネントの更新欄から更新できます。OSC、パスワード、実行時復号は不要です。名前と構造の難読化は抽出を防ぐものではなく、テクスチャの画素・最終形状・必要な動的形状は抽出可能です。実際の VRChat クライアント・実機の確認は未完了です。検証範囲は [TESTING.md](TESTING.md) を参照してください。
