# Avatar Build Obfuscator

[中文](README.md) · [English](README.en.md)

`VRCAvatarDescriptor` があるアバターのルートに **Avatar Tools / Build Obfuscator** を追加すると、NDMF のビルド用コピーに対してアセット名・BlendShape 名の難読化と、固定 BlendShape のベイク・削除を実行します。各ユーザーが自分のユーザー名を入力します（Unicode 対応）。ユーザー名から `o_…` 形式の不透明な名前を生成し、名前にユーザー名そのものは付けません。新規コンポーネントのユーザー名は空欄です。

## 導入

Unity 2022.3 の VRChat Avatar プロジェクトに、VCC で Avatar SDK と [NDMF](https://github.com/bdunderscore/ndmf) を導入してください。[Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) の `.unitypackage` をインポートし、ルートにコンポーネントを追加し、自分のユーザー名を入力して通常通りビルドします。同じツールを二重に導入しないでください。AAO は任意で、既存の設定は変更しません。

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
