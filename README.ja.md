# Avatar Build Obfuscator

[中文](README.md) · [English](README.en.md)

`VRCAvatarDescriptor` があるアバターのルートに **Avatar Tools / Build Obfuscator** を追加すると、NDMF のビルド用コピーに対してアセット名・BlendShape 名の難読化と、固定 BlendShape のベイク・削除を実行します。各ユーザーが自分のユーザー名を入力します（Unicode 対応）。ユーザー名から `o_…` 形式の不透明な名前を生成し、名前にユーザー名そのものは付けません。新規コンポーネントのユーザー名は空欄です。

**ビルド後も Unity の Hierarchy / Scene / Project に元の名前が表示されるのは正常です。** 両モードとも NDMF のビルド用コピーだけを処理します。Console の `[Avatar Build Obfuscator]` に表示される件数・スキップ理由と、実際のビルド結果で確認してください。激進モードは主に MMD 互換形状の処理範囲を広げますが、ボーン・階層・実行時パラメーター名は保持します。

## 導入

Unity 2022.3 の VRChat Avatar プロジェクトに、VCC で Avatar SDK と [NDMF](https://github.com/bdunderscore/ndmf) を先に導入してください。本ツールを Git から導入しても、これらの VPM 依存パッケージは自動導入されません。AAO は任意で、既存の設定は変更しません。

### Git からインストール（推奨）

先に [Git](https://git-scm.com/) をインストールしてください。Windows では、Unity が `PATH` から Git を見つけられる必要があります。

1. **Window → Package Manager** を開きます。
2. **＋ → Add package from git URL…** を選びます。
3. 次の URL 全体を貼り付けて **Add** を押し、コンパイルの完了を待ちます。

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

**URL の入力は初回だけです。以後の更新は、Package Manager で Avatar Build Obfuscator を選び、Update を押してください。** `main` の新しいコミットを取得でき、URL の再入力は不要です。

Unity は導入済みのコミットを固定するため、プロジェクトを開くたびに自動更新することはありません。パッケージはサブフォルダーにあるため `?path=…` を省略しないでください。`#main` が手動更新時に追跡するブランチです。[Unity の Git 導入手順](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-ui-giturl.html)と[コミットの固定・更新](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-git.html#git-locks)を参照してください。

### ベータ版と安定版

`beta` には変更と必要な検証が完了した日常の更新を反映します。`main` は安定版用で、ベータ版の更新ごとには進めません。ベータ版には未発見の問題が残る可能性があります。ベータ版を利用する場合は、次の URL でインストールしてください。

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#beta
```

Git 版から切り替える場合は、**Add package from git URL…** でこの URL を一度入力します。同じパッケージの参照先が `beta` に切り替わり、その後は選択して **Update** を押すと更新できます。`#main` 版は自動で `beta` に切り替わりません。安定版に戻す場合は `#main` の URL を再入力してください。

### Unity パッケージからインポート

[Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) の `.unitypackage` をインポートする方法もあります。この導入方法は新版の Unity パッケージをインポートして更新し、Git の更新ボタンは使用しません。

**同じツールを二重に導入しないでください。** Unity パッケージ版から Git 版へ切り替える場合は、先に `Assets/AvatarBuildObfuscator` のプラグインファイルを削除してください。アバター、シーン、その他のアセットは残し、Git 版の導入後に既存コンポーネントとユーザー名の設定を確認してください。

### コンポーネントを追加

`VRCAvatarDescriptor` があるルートに **Avatar Tools → Build Obfuscator** を追加し、自分のユーザー名とモードを設定して通常通りビルドします。

特定バージョンに固定する場合は、URL 末尾の `#main` を `#v0.2.0` などの公開タグに置き換えます。固定タグは `main` の更新を追跡しません。

## モード

- **保守 / Conservative（既定）**：対応アセットをコピーして改名し、アニメーションで使用されない通常の固定 BlendShape を現在のウェイトでベイクします。日本語・中国語および一般的な MMD 互換名は保持します。
- **激進 / Aggressive**：未使用の MMD 互換形状もベイク対象にし、保持する互換形状の名前も難読化できます。固定名に依存する外部ワールドやツールでは表情が動かなくなる場合があります。
- 両モードとも、アニメーション（定数カーブを含む）、SDK の口形・まばたきに必要な形状データを保持します。カーブ、口形名、まぶたのインデックスを合わせて更新します。
- 不明なドライバー、アニメーションによるメッシュ差し替え、重複パス、手動除外などは安全のためスキップします。特殊なフレーム構成や範囲外ウェイトはベイクしません。

名前の保持リスト・Renderer の除外リストを設定できます。他の最適化ツールの処理からも保護する必要がある場合、そのツール側の保持設定も必要です。

## 対象と制限

Mesh、Material、読み取り可能な非 Crunch の Texture2D、AnimationClip、各種 Animator Controller、BlendTree、AvatarMask、Avatar、VRC メニュー・パラメーターのアセットオブジェクトが対象です。ボーン・階層、実行時パラメーター文字列、状態名、メニューの表示文字、Shader、SDK の代理アニメーションは互換名を保持します。Read/Write が無効なテクスチャ、Crunch テクスチャと未対応のリソースは元のリソースと名前を保持します。Importer の設定は変更しません。

ベイク対象は単一フレーム・フレームウェイト 100・現在ウェイト 0–100 の通常形状です。元ファイル、Importer、`.meta` の GUID は書き換えません。コンポーネントの設定は通常通り Scene / Prefab に保存してください。

**暗号化ツールではありません。アップロード済みデータからテクスチャや保持した動的 BlendShape を抽出することは可能です。** OSC、パスワード、専用 Shader は不要です。ネットワークへの接続やアバターの自動アップロードは行いません。ベイク後の外観はメッシュ形状として残ります。

[検証](TESTING.md)・[設計](ARCHITECTURE.md)・[MIT ライセンス](LICENSE)。`Tests~` は任意の検証ツールで、正式な Unity パッケージには含まれません。
