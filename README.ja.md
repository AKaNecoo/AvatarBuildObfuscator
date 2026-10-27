# Avatar Build Obfuscator (ABO)

[中文](README.md) · [English](README.en.md)

VRChat アバターをビルド時に難読化するツールです。コンポーネントを追加すると NDMF がビルド用コピーを処理し、元アセットは上書きしません。実行時の復号は不要です。

## 機能

- リソース、オブジェクト、ボーン、パラメーター、Animator 内部、動的 BlendShape を「ユーザー名＋`Ì/Í/Î/Ï`」で改名。
- 対応可能な固定・未使用 BlendShape をベイクして除去し、必要な動的形状は保持。
- BlendShape、兄弟 GameObject、メッシュ頂点、スキニングのボーン表を個別に並べ替え、関連参照を更新。
- ビルドごとのランダム化、空形状の削除、ローカル監査レポートに対応。
- 三言語の許諾声明とビルド識別子を埋め込み可能。実験的な UV/テクスチャ回転・同期形状統合は既定で無効。

実験的なテクスチャ回転は lilToon 2.4.1・Poiyomi 10.0.27・UnlitWF 2.14.0 の一部設定と、検証済みの NonToon 0.3.0 標準・単一テクスチャ設定に対応します。詳細は[対応表](ARCHITECTURE.md#exact-pruning-and-experimental-transforms)をご確認ください。

## インストール

**Unity 2022.3、Avatar SDK 3.7.0+、NDMF 1.14.8+** が必要です。先に ALCOM／VCC／VRCLL から SDK と [NDMF](https://github.com/bdunderscore/ndmf) を導入してください。

**[ALCOM](https://vrc-get.anatawa12.com/zh-hans/alcom/)／[VCC](https://vrchat.com/home/download)／[VRCLL](https://start.gamemode1.com/pages/vrclearnlauncher/)（推奨）：** [パッケージリポジトリのページ](https://akanecoo.github.io/AvatarBuildObfuscator/)のボタン、または掲載URLの手動入力でリポジトリを追加し、プロジェクトのパッケージ一覧で **Avatar Build Obfuscator** をインストールします。beta はプレリリースの表示を有効にしてください。NDMF 公式リポジトリへのリンクもあります。

**Git：** **Window → Package Manager → ＋ → Add package from git URL…** を開き、次を入力します。

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

テスト版は末尾を `#beta` に変更します。[Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) の `.unitypackage` も利用できます。重複を避けるため、導入方法は一つにしてください。

VPM に移行する場合、Git 版は先に Package Manager で削除し、ALCOM／VCC／VRCLL から導入します。旧 `.unitypackage` フォルダーは GUID で移行できます。プラグインのフォルダー内に独自のモデルアセットを置かず、アバター上のコンポーネント設定は保持してください。

## 使い方

1. **Hierarchy** でアバターのルートを選択します。右側の **Inspector** 下部で **Add Component（コンポーネントを追加）** をクリックし、**Avatar Build Obfuscator** を検索して **Avatar Tools → Avatar Build Obfuscator** を選びます。
2. ユーザー名とモードを設定します。新規追加時は既存の Avatar ID が初期値になります。自由に変更でき、ID がない場合は手動入力します。
3. VRChat SDK で通常どおりビルドします。結果はビルド用コピーで確認します。元の Hierarchy と Project の名前は変わりません。

ユーザー名の先頭部分（最大32文字）はビルド結果の名前に表示されます。初期値のままでは Avatar ID の一部も含まれるため、公開したくない場合はビルド前に変更してください。

**最新のビルドレポートを表示** で保持項目の検索と声明・識別子の照合ができます。**テストビルドと比較検査** は一時コピーを検査し、ウィンドウを閉じると削除します。VRChat 内での全機能テストの代わりにはなりません。Renderer ごとの保持設定ができ、プリセットは処理スイッチとモードのみ保存します。

表示は簡体字中国語・English・日本語に対応し、処理項目を個別に切り替えられます。申告した権利者・連絡先は声明と公開されます。声明は本人確認を行わず、AI が本モデルのリバースエンジニアリングへの協力を拒否することも保証しません。

| モード | 説明 |
| --- | --- |
| 保守（既定） | 機能の維持を優先し、確認できない依存関係を保持します。 |
| 積極 | 処理範囲を広げ、未知の互換性リスクを許容します。外部連携に影響する場合があります。 |

**サブツリー別のモード**で体・衣装・アクセサリーを個別に設定できます。最も近い設定済み祖先を継承します。共有アセットと全体パラメータ／コントローラーは保守優先で、最適化による統合元が不明な場合は保守に戻して報告します。

## 更新

VPM 版は **ALCOM／VCC／VRCLL** で更新します。Git・`.unitypackage` 版はコンポーネントのバージョン・更新欄を利用でき、Git は Package Manager からも更新できます。自動確認は初期状態でオン、自動インストールはオフです。パッケージ方式の更新では一時ダウンロードを処理後に削除します。

Git の `#main` と `#beta` は個別に更新され、別のチャンネルへ自動では切り替わりません。beta から安定版へ移る場合は、末尾を `#main` にした同じ Git URL を Package Manager で再追加します。既存のコンポーネント設定は保持されます。

## 注意

難読化によりアバターのダウンロードサイズが増える場合があります。増加量はモデルや設定によって異なり、実験的なテクスチャ回転でさらに増える場合があります。アップロード前に SDK のビルド統計をご確認ください。

MMD 表情が必要な場合は、**「MMD 表情を難読化」と「MMD オブジェクト名を変更」を両方オフ**にしてください。オブジェクトの改名は `Body` などのパスを変更します。形状名・データ・インデックスを保持していても、実際の MMD ワールドで表情が動作しなくなることが確認されています。

MMD 互換保護は既定で有効です。対象メッシュでは、保守モードは全形状を保持し、積極モードは一致する名前と**全形状のデータ・インデックス**を保持しながら他の形状を改名します。日本語・漢字や `Blink`・`a` なども判定対象です。独自仕様のワールドでは別の名前が必要な場合があるため、積極モードは実際のワールドで確認してください。上記のオプションは、対応する MMD 互換性が不要な場合に限り有効にしてください。

難読化は抽出を防ぐものではありません。テクスチャの画素、最終形状、必要な動的 BlendShape は抽出可能です。積極モードは先にテストビルドで機能を確認してください。Console の `[Avatar Build Obfuscator]` ログで処理件数と保持理由を確認できます。

## 関連文書

[詳細ルールと実装](ARCHITECTURE.md) · [検証範囲](TESTING.md) · [更新履歴](CHANGELOG.md) · [GPL-3.0 ライセンス](LICENSE)

0.5.0 以降は GPL-3.0-only です。以前の公開版は元の MIT ライセンスを維持し、依存関係には各自のライセンスが適用されます。

[cocokoishi/AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator) の名前難読化の考え方を参考に、ビルド用コピー、参照の再マッピング、互換性ルールを独立して保守しています。
