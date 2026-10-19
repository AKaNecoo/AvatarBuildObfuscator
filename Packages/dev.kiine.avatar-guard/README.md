# 一个木禾 · Avatar Guard 0.1.0

在 `VRCAvatarDescriptor` 所在的 Avatar 根对象添加 **一个木禾 / Avatar Guard**，构建时由 NDMF 自动运行。默认名称前缀：`一个木禾`。Unity 2022.3，需要 Avatar SDK 和 NDMF；AAO 可选。

- **保守（默认）**：资源名称混淆、普通静态 BlendShape 烘焙移除、动态形变名称混淆；保留 MMD、日文/中文兼容形变。
- **激进**：进一步处理未引用的 MMD 兼容形变；可能影响按固定名称驱动表情的外部世界和工具。
- 两档均保留动画、口型、眨眼所需的动态形变；未知驱动、网格替换、特殊数据与手动排除项会保留。
- 不可读（Read/Write 关闭）和 Crunch 贴图保留原资源及名称。Shader、骨骼、层级路径和运行参数保留兼容名称。
- 只改构建副本与新建资源，不覆盖源 FBX、贴图、材质、动画或 `.meta`。

**不包含贴图或网格加密。不需要 OSC。贴图和保留的动态 BlendShape 仍可从上传内容中提取。**

完整中英日文说明、源码与验证记录：<https://github.com/AKaNecoo/KiineAvatarGuard>

## English

Add **一个木禾 / Avatar Guard** to your avatar root and build normally with NDMF. Source assets remain unchanged. Conservative mode retains MMD compatibility shapes; Aggressive mode may break external name-based expression drivers. Unreadable/Crunch textures, shaders, bones, hierarchy paths and runtime parameters keep their compatible names. This is obfuscation and static-shape pruning, not encryption. Retained dynamic shapes and texture pixels remain extractable. No OSC is required.

## 日本語

アバターのルートに **一个木禾 / Avatar Guard** を追加し、NDMF で通常通りビルドしてください。元アセットは変更しません。保守モードは MMD 互換形状を保持します。激進モードは外部の名前依存ドライバーに影響する場合があります。読み取り不可・Crunch テクスチャ、Shader、ボーン、階層、実行時パラメーターは互換名を保持します。暗号化ではなく、保持した動的 BlendShape とテクスチャの抽出は可能です。OSC は不要です。
