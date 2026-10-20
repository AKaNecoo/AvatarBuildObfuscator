# Avatar Build Obfuscator

[中文](README.md) · [English](README.en.md)

VRChat アバターをビルド時に難読化するツールです。コンポーネントを追加すると NDMF がビルド用コピーを処理し、元アセットは上書きしません。実行時の復号は不要です。

## 機能

- リソース、オブジェクト、ボーン、パラメーター、Animator 内部、動的 BlendShape を「ユーザー名＋`Ì/Í/Î/Ï`」で改名。
- 対応可能な固定・未使用 BlendShape をベイクして除去し、必要な動的形状は保持。
- BlendShape、兄弟 GameObject、メッシュ頂点の順序を個別に並べ替え、関連参照を更新。

## インストール

**Unity 2022.3、Avatar SDK 3.7.0+、NDMF 1.14.8+** が必要です。先に VCC から SDK と [NDMF](https://github.com/bdunderscore/ndmf) を導入してください。

**Window → Package Manager → ＋ → Add package from git URL…** を開き、次を入力します。

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

テスト版は末尾を `#beta` に変更します。[Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) の `.unitypackage` も利用できます。重複を避けるため、導入方法はどちらか一方にしてください。

## 使い方

1. アバターのルートを選び、**Avatar Tools → Avatar Build Obfuscator** を追加します。
2. ユーザー名とモードを設定します。新規追加時は既存の Avatar ID が初期値になります。自由に変更でき、ID がない場合は手動入力します。
3. VRChat SDK で通常どおりビルドします。結果はビルド用コピーで確認します。元の Hierarchy と Project の名前は変わりません。

表示は簡体字中国語・English・日本語に対応します。処理項目を個別に切り替え、保持する項目も指定できます。

| モード | 説明 |
| --- | --- |
| 保守（既定） | 機能の維持を優先し、確認できない依存関係を保持します。 |
| 激進 | 処理範囲を広げ、未知の互換性リスクを許容します。外部連携に影響する場合があります。 |

## 更新

コンポーネントのバージョン・更新欄から確認・更新できます。Git 導入は Package Manager も利用できます。自動確認は初期状態でオン、自動インストールはオフです。パッケージ方式の更新では一時ダウンロードを処理後に削除します。

## 注意

難読化は抽出を防ぐものではありません。テクスチャの画素、最終形状、必要な動的 BlendShape は抽出可能です。激進モードは先にテストビルドで機能を確認してください。Console の `[Avatar Build Obfuscator]` ログで処理件数と保持理由を確認できます。

## 関連文書

[詳細ルールと実装](ARCHITECTURE.md) · [検証範囲](TESTING.md) · [更新履歴](CHANGELOG.md) · [GPL-3.0 ライセンス](LICENSE)

0.5.0 以降は GPL-3.0-only です。以前の公開版は元の MIT ライセンスを維持し、依存関係には各自のライセンスが適用されます。

[cocokoishi/AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator) の名前難読化の考え方を参考に、ビルド用コピー、参照の再マッピング、互換性ルールを独立して保守しています。
