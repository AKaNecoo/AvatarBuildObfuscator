# Avatar Build Obfuscator 0.2.0

在 `VRCAvatarDescriptor` 所在的 Avatar 根对象添加 **Avatar Tools / Build Obfuscator**，填写你自己的**用户名**，构建时由 NDMF 自动运行。用户名支持中文，仅用于派生 `o_…` 形式的乱码名称，不直接拼接到资源名中；新组件默认留空。

- **保守（默认）**：资源名混淆、普通静态 BlendShape 烘焙移除、动态形变名混淆；保留 MMD、日文/中文兼容形变。
- **激进**：进一步处理未引用的 MMD 兼容形变；可能影响依赖固定名称的外部世界和工具。
- 两档均保留动画、口型、眨眼需要的形变；未知驱动、网格替换、特殊数据与手动排除项会保留。
- 不可读和 Crunch 贴图保留原资源及名称。Shader、骨骼、层级和运行参数保持兼容名称。
- 处理 NDMF 构建副本，不覆盖源 FBX、贴图、材质、动画或 .meta。Unity 2022.3，需要 Avatar SDK 与 NDMF，AAO 可选。

用户名不是密码。不提供贴图/网格加密，不需要 OSC。上传内容中的贴图和保留的动态形变仍可提取。

说明、源码与验证记录：<https://github.com/AKaNecoo/AvatarBuildObfuscator>

## English

Add **Avatar Tools / Build Obfuscator** to your avatar root and enter your own username before building with NDMF. Unicode usernames derive opaque `o_…` names; the literal username is not appended. The username is empty by default. Source assets remain unchanged. Conservative/Aggressive modes retain dynamic shapes and skip uncertain integrations. Unreadable/Crunch textures and compatibility-sensitive names are retained. This is obfuscation and static-shape pruning, not encryption; no OSC is required.

## 日本語

ルートに **Avatar Tools / Build Obfuscator** を追加し、自分のユーザー名を入力して NDMF でビルドします。Unicode ユーザー名から不透明な `o_…` 名を生成し、ユーザー名そのものは名前に付けません。既定は空欄です。元アセットは変更しません。保守・激進の両モードとも必要な動的形状と不明な依存を保持します。読み取り不可・Crunch テクスチャなどは互換名を保持します。暗号化ではありません。OSC は不要です。
