# Avatar Build Obfuscator

ユーザー名は左ラベル・右入力欄の1行配置を維持し、フォーカス・IME・Undo をネイティブのバインド済みテキスト欄で処理します。Add Component では **Avatar Build Obfuscator** を検索し、**Avatar Tools / Avatar Build Obfuscator** を選択します。

[中文](README.md) · [English](README.en.md)

`VRCAvatarDescriptor` のあるルートに **Avatar Tools / Avatar Build Obfuscator** を追加し、自分のユーザー名を入力すると、NDMF のビルド用コピーを処理します。**0.3.1** の名前は **`ユーザー名_IlIl…`**。Unicode の表示プレフィックスと、ASCII の大文字 `I` / 小文字 `l` 128 文字で構成します。正規化したユーザー名全体を生成シードにも使います。新規コンポーネントは空欄です。

**OSC・パスワード・実行時 Shader 復号は不要**です。暗号化ではなく、テクスチャ、最終形状、必要な動的 BlendShape はアップロードデータに残ります。

## インストールと更新

Unity **2022.3**、VCCで導入した Avatar SDK **3.7.0+** と [NDMF](https://github.com/bdunderscore/ndmf) **1.14.8+** が必要です。Git導入はVPM依存を自動解決しません。AAO・MA・VRCFury・TTTは任意です。

**Window → Package Manager → ＋ → Add package from git URL…** に安定版URLを一度入力します。

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

テスト版は末尾を `#beta` にします。[Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) の `.unitypackage` をインポートする方法も利用できます。二重導入せず、方式を変更する前に他方のプラグインだけを削除し、Avatarは残してください。スクリプトGUIDは維持します。移行後に参照を確認してください。

コンポーネントの **バージョンと更新** に、バージョン・チャンネル・状態と **更新を確認 / 今すぐ更新 / 変更履歴** を表示します。自動確認は初期状態でオン、キャッシュは6時間です。自動インストールはプロジェクト単位で任意に有効化できます。起動60秒後以降、Unityがバックグラウンドで待機中かつSDKパネルが閉じているときに実行し、Play・ビルド・コンパイル・インポート中は待機します。同じコミットでの失敗は自動再試行しません。オフラインでもプラグインは使用できます。

Git導入はPackage Managerから、そのチャンネルでPackage CIに成功した正確なコミットを導入し、以後もチャンネルを維持します。インストールパッケージ方式はCI公開の `.unitypackage` を取得し、GitHubのSHA-256を確認してインポートします。一時ファイル `Library/AvatarBuildObfuscatorUpdates/pending.unitypackage` は成功・失敗・キャンセル後に削除し、リロード後も清掃を再開します。更新はプラグインのコードを置換し再コンパイルします。モデル処理やモデル素材の変更は行いません。

設定はこのプロジェクトの `UserSettings/AvatarBuildObfuscatorUpdates.asset` のみに保存し、Avatarには保存しません。ユーザー名やモデルは送信しません。手動でタグ・コミットを固定した導入、他リポジトリ、移動したAssetsコピー、ローカル `file:` 開発版は上書きしません。パッケージ方式の初期チャンネルは安定版main・プレリリースbetaで、以後も維持します。元のGit URLの再追加やPackage Managerによる手動更新も可能です。GitとAssetsコピーを同時に導入しないでください。

## CI と配布

[Package CI](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/workflows/package-ci.yml) はmain/betaへのpushとPRで構成・GUID・更新ポリシーを検証します。再現可能なUPM `.tgz`・ZIP・`.unitypackage`・チェックサムを生成し、全アーカイブを元ファイルと照合します。Release公開は本リポジトリのmain/betaへのpushのみです。PRでは公開しません。mainは安定版、betaはプレリリースです。公開済み版の製品内容を差し替えず、変更時はバージョンを更新します。

クラウドCIは独立した更新ポリシーのコンパイル・テストと配布パッケージを検証します。**Unity Editorや実Avatarの検証の代替ではありません**。mainに反映する前に認可されたテストプロジェクトでUnityコンパイル・NDMF・入力・Playを検証します。[TESTING.md](TESTING.md) を参照してください。モデル・私有ログ・工程はアップロードしません。

## 使い方とモード

上部の **语言 / Language / 言語** で簡体字中国語（既定）・English・日本語を選択できます。選択はコンポーネントごとに保存され、ラベル・説明・ツールチップのみ変更します。ユーザー名・モード・ビルド結果には影響しません。

Play・SDK ビルドでは NDMF 既定の `Packages/nadena.dev.ndmf/__Generated`、手動生成では Assets 配下を使用できます。

ユーザー名、モード、処理項目を設定して通常通りビルドします。表示プレフィックスは安全な最大 32 文字にし、危険なファイル名文字は `_` に置換します。シードは Trim/NFC 正規化した入力全体です。同じ入力と呼び出し順序で同じ系列になりますが、最適化ツールの走査順序が変わるとリソースごとの名前も変わり得ます。

**元の Hierarchy / Scene / Project に元の名前が残るのは正常**です。Console の **`[Avatar Build Obfuscator]`** の件数・保持理由と、実際のビルド結果を確認してください。

両モードとも広い処理範囲を目指します。保守モードは依存の認識と一貫した再マッピングを優先し、激進モードは未知の名前依存リスクを許容します。アセット名、形状、階層、ボーン、パラメーター、Animator 内部名、頂点順序を個別に切り替えられます。

- 共通：対応アセットのコピー、階層・Mask パスの更新、対応 Humanoid Avatar の再構築、独自パラメーター・Animator 内部名の変更、必須の動的形状の保持。
- 保守：不明なドライバー、外部文字列パス、MMD 互換形状を保持。頂点の並べ替えは確認済みの内蔵 Shader に限定します。
- 激進：不明な依存、互換形状名、自作 Shader の頂点順序も処理できます。外部連携が動かなくなる可能性があります。
- 形状、Renderer、階層オブジェクト/子ツリー、パラメーターの保持設定があります。パラメーターは末尾 `*` の前方一致に対応します。

## 対象と制限

読み取り不可・Crunch を含む導入済みテクスチャと音声は、生成フォルダー内の独立ファイル/Importer コピーで処理します。元の圧縮データや設定を変更しません。アニメーション、各種 Controller、同期レイヤーの上書き、BlendTree、Mask、表情アセットを対応します。

パラメーターは定義、Animator カーブ、条件、状態フィールド、BlendTree、メニュー主/副パラメーター、Driver、PhysBone の接頭辞と自動接尾辞、Contact を合わせて更新します。メニューの表示文字は保持します。VRChat 内蔵パラメーター、既知の外部面捕/OSC 入力、SDK 代理アニメーションの実体は固定契約を保持します。

Humanoid はボーン名と内部の親名を合わせて再構築し、対応とスケールを確認します。未対応の Generic/無効 Avatar、曖昧なパス、再構築に失敗した階層は保持します。頂点順序の変更では生の頂点ストリーム、UV/色、トポロジー、可変ボーンウェイト、形状フレームを一緒に並べ替えるため、実行時の復元は不要です。Cloth、追加頂点ストリーム、読み取り不可メッシュは順序を保持します。

読み取れる `.shader` は属性・キーワード・Pass の契約を維持して別名で再コンパイルできます。内蔵/モバイル許可 Shader、コンテナー生成 Shader、NadeVision の固定 Shader 名は保持します。Shader 属性プロトコルの難読化は未実装です。未対応リソースは保持理由を記録します。

ベイクは単一フレーム、フレームウェイト 100、現在ウェイト 0–100 の通常形状が対象です。定数カーブも動的使用とし、メッシュ差し替えと明示除外には保護規則があります。

元シーン、モデル、Importer、元 `.meta` GUID は上書きしません。コンポーネント追加だけでは処理せず、アバターの自動アップロードも行いません。保守モードも、すべての未知のワールド・機器・拡張への互換保証ではありません。

[TESTING.md](TESTING.md)、[ARCHITECTURE.md](ARCHITECTURE.md)、[CHANGELOG.md](CHANGELOG.md)、[MIT](LICENSE) を参照してください。`Tests~` は任意導入で、実モデルや非公開の検証データはインストールパッケージに含みません。Editor 検証はクライアント/実機の受け入れ試験と区別します。

旧コンポーネントの GUID と移行情報は保持します。0.3 beta では要望によりユーザー名の表示プレフィックスを復活し、新項目を既定で有効化しました。更新後は設定を確認してください。[AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator) を発想の参考にし、ASCII `I/l` と独立したコピー/参照更新を採用しています。

## 順序の難読化

BlendShape のインデックス順と GameObject の兄弟順を独立したスイッチで変更できます。現在のウェイト・まぶたインデックスを更新し、全フレームを保持します。親子関係とローカル座標は変えません。UI、同名の兄弟、Generic 骨格、手動保持の順序は維持し、Humanoid の骨対応も再確認します。

インポートするコピーは一括処理し、ファイルごとの再インポートを避けます。コピー中にキャンセルでき、例外時もインポート停止を解除します。Unity ネイティブのインポート・Shader コンパイルは即時中断できない場合があります。
