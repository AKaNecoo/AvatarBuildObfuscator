<p align="center">
  <img src="Packages/dev.avatar-tools.build-obfuscator/Editor/Icons/ABOWordmark.png" alt="ABO" width="200">
</p>

<h1 align="center">Avatar Build Obfuscator (ABO)</h1>

<p align="center">
  <a href="README.md">中文</a> · <a href="README.en.md">English</a>
</p>

<p align="center">VRChat アバターをビルド時に難読化するツールです。元アセットを保持し、実行時の復号は不要です。</p>

## 機能

- リソース、オブジェクト、ボーン、パラメーター、Animator 内部、動的 BlendShape を「ユーザー名＋ランダムな接尾辞」で改名。接尾辞には Ì・Í・Î・Ï の4文字を使用。
- 対応可能な固定・未使用 BlendShape をベイクして除去し、変化のない形状を削除。必要な動的形状は保持。
- BlendShape、兄弟 GameObject、メッシュ頂点、スキニングのボーン表を個別に並べ替え、関連参照を更新。
- ビルドごとのランダム化とローカルレポートに対応。三言語の許諾声明とビルド識別子も任意で埋め込み可能。

## インストール

**Unity 2022.3、Avatar SDK 3.7.0+、NDMF 1.14.8+** が必要です。先にパッケージマネージャーで SDK と [NDMF](https://github.com/bdunderscore/ndmf) を導入してください。

**[ALCOM](https://vrc-get.anatawa12.com/zh-hans/alcom/)／[VCC](https://vrchat.com/home/download)／[VCL](https://start.gamemode1.com/pages/vrclearnlauncher/)（推奨）**

1. [パッケージリポジトリのページ](https://akanecoo.github.io/AvatarBuildObfuscator/)のボタンでリポジトリを追加するか、掲載URLをマネージャーに手動入力します。
2. マネージャーでプロジェクトを選び、パッケージ一覧から **Avatar Build Obfuscator** をインストールします。beta はプレリリースの表示を有効にしてください。
3. Unity に戻り、下の[使い方](#使い方)に沿ってコンポーネントを追加します。

同じページから NDMF 公式リポジトリも追加できます。**重複を避けるため、導入方法は一つにしてください。**

<details>
<summary>Git・unitypackage での導入と移行</summary>

**Git：** **Window → Package Manager → ＋ → Add package from git URL…** を開き、次を入力します。

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

テスト版は末尾を `#beta` に変更します。

**unitypackage：** [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) から `.unitypackage` をダウンロードしてインポートします。

**VPM への移行：** Git 版は先に Package Manager で削除し、ALCOM／VCC／VCL から導入します。旧 `.unitypackage` フォルダーは GUID で移行できます。プラグインのフォルダー内に独自のモデルアセットを置かず、アバター上のコンポーネント設定は保持してください。

</details>

## 使い方

1. **Hierarchy** でアバターのルートを選択します。右側の **Inspector** 下部で **Add Component（コンポーネントを追加）** をクリックし、**Avatar Build Obfuscator** を検索して追加します。
2. ユーザー名を確認または入力し、モードを選びます。
3. VRChat SDK で通常どおりビルドします。結果はビルド用コピーで確認します。元の Hierarchy と Project の名前は変わりません。

| モード | 説明 |
| --- | --- |
| 保守（既定） | 互換性を優先し、確認できない依存関係を保持します。 |
| 積極 | 難読化の範囲を広げ、未知の互換性リスクを許容します。使用前に機能と外部連携を確認してください。 |

**効果の目安：** 保守モードは互換性を優先するため、変化が目立たない場合があります。積極モードは難読化の範囲が広く、通常は効果がより明確ですが、互換性のリスクも高くなります。

**MMD 表情が必要な場合は、「MMD 表情を難読化」と「MMD オブジェクト名を変更」を両方オフにしてください。** オブジェクトの改名は階層パスを変えるため、MMD 表情が動作しなくなる場合があります。

<details>
<summary>追加設定とビルドの確認</summary>

- 表示は簡体字中国語・English・日本語に対応しています。処理項目を個別に切り替えたり、保持リストで特定 Renderer の処理を除外したりできます。
- **サブツリー別のモード**で体・衣装・アクセサリーを個別に設定できます。プリセットは処理スイッチとモードのみ保存します。共有アセットなどの詳細ルールは[実装ガイド](ARCHITECTURE.md)をご覧ください。
- **最新のビルドレポートを表示**で処理記録と保持理由を検索し、声明・識別子を照合できます。Console の `[Avatar Build Obfuscator]` ログにも処理件数と保持理由が表示されます。
- **テストビルドと比較検査**は一時コピーを使ってサンプリングし、ウィンドウを閉じると削除します。VRChat 内での機能テストの代わりにはなりません。
- 実験的な **UV/テクスチャ回転**と**同期形状統合**は既定で無効です。対応範囲は[互換性の説明](ARCHITECTURE.md#exact-pruning-and-experimental-transforms)をご確認ください。

</details>

## 注意

- **公開される接頭辞：** ユーザー名の先頭部分（最大32文字）はビルド結果の名前に表示されます。新規追加時は既存の Avatar ID が初期値になり、自由に変更できます。ID がない場合は手動入力し、公開したくない値はビルド前に変更してください。
- **ダウンロードサイズ：** 難読化によりサイズが増える場合があり、実験的なテクスチャ回転でさらに増える場合があります。アップロード前に SDK のビルド統計をご確認ください。
- **保護の限界：** 難読化は抽出の防止を保証しません。テクスチャの画素、最終形状、必要な動的 BlendShape は抽出可能です。
- **許諾声明：** 申告した権利者・連絡先は声明と公開されます。声明は本人確認を行わず、AI が本モデルのリバースエンジニアリングへの協力を拒否することも保証しません。

## 更新

- **VPM：** ALCOM／VCC／VCL で更新します。
- **Git／unitypackage：** コンポーネントのバージョン・更新欄を利用します。Git は Package Manager からも更新できます。
- 自動確認は初期状態でオン、自動インストールはオフです。パッケージ方式の更新では一時ダウンロードを処理後に削除します。

<details>
<summary>Git チャンネルの切り替え</summary>

`#main` と `#beta` は個別に更新され、別のチャンネルへ自動では切り替わりません。beta から安定版へ移る場合は、末尾を `#main` にした同じ Git URL を Package Manager で再追加します。既存のコンポーネント設定は保持されます。

</details>

## フィードバック・関連文書

不具合の報告は [GitHub Issues](https://github.com/AKaNecoo/AvatarBuildObfuscator/issues) または [kiinemaki@qq.com](mailto:kiinemaki@qq.com) へお願いします。ABO／Unity のバージョン、モード、再現手順、関連ログの抜粋を添えてください。

[詳細ルールと実装](ARCHITECTURE.md) · [検証範囲](TESTING.md) · [更新履歴](CHANGELOG.md) · [GPL-3.0 ライセンス](LICENSE)

0.5.0 以降は GPL-3.0-only です。以前の公開版は元の MIT ライセンスを維持し、依存関係には各自のライセンスが適用されます。

[cocokoishi/AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator) の名前難読化の考え方を参考に、ビルド用コピー、参照の再マッピング、互換性ルールを独立して保守しています。
