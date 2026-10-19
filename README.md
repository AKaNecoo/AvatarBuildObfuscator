# 一个木禾 · Avatar Guard

[English](README.en.md) · [日本語](README.ja.md)

在 VRChat Avatar 根对象添加一个组件，在构建时自动混淆资源与动态 BlendShape 名称，并烘焙、移除能确认不再需要变化的 BlendShape。默认前缀是 **一个木禾**，支持中文，可自行修改。

**这是一款名称混淆与形变裁剪工具。它不加密贴图或网格，不需要 OSC，也不能阻止别人从上传文件中提取贴图和保留的动态 BlendShape。** 烘焙后的最终外观仍然包含在网格中。

## 安装与使用

1. 使用 Unity **2022.3** 的 VRChat Avatar 工程，先通过 VCC 安装 Avatar SDK 与 [NDMF](https://github.com/bdunderscore/ndmf)。已安装 Modular Avatar 的工程通常已有 NDMF。
2. 从 [Releases](https://github.com/AKaNecoo/KiineAvatarGuard/releases) 下载 `KiineAvatarGuard-0.1.0.unitypackage`，导入工程。项目中只安装一份本工具。
3. 选择挂有 `VRCAvatarDescriptor` 的 Avatar 根对象，选择 **Add Component → 一个木禾 → Avatar Guard**。
4. 选择 **保守** 或 **激进**，设置名称前缀。默认三个处理选项全部开启。
5. 使用 VRChat SDK 正常构建/上传。NDMF 会先处理构建副本，随后本工具处理副本中的资源。Console 的 `[Avatar Guard]` 日志会列出数量与跳过原因。

Avatar Optimizer 不是必需依赖。若已安装，本工具在它完成优化后处理，不修改它的设置。不需要更换 Shader。

源码包也包含标准 VPM/UPM 目录 `Packages/dev.kiine.avatar-guard`；开发者可在依赖已安装的工程中作为本地包导入。

## 两种模式

| 项目 | 保守（默认） | 激进 |
| --- | --- | --- |
| 安全可复制的资源名称 | 混淆 | 混淆 |
| 动画曲线引用的 BlendShape | 保留数据，同步修改形变名与曲线 | 相同 |
| SDK 口型、眨眼形变 | 保留数据，同步更新名称或索引 | 相同 |
| 未被驱动的普通静态形变 | 按当前权重烘焙，然后移除 | 相同 |
| MMD、日文/中文兼容形变名称 | 保留数据和名称 | 可烘焙未引用形变；保留形变可改名 |
| 未知驱动、网格替换、重复路径 | 跳过对应形变处理 | 同样跳过 |
| 多帧/非标准帧或超出 0–100 的权重 | 不烘焙；符合其他条件时可只改名 | 相同 |

激进模式可能影响依赖固定形变名称的外部世界或工具。动画中即使是常量曲线，也视为动态使用，不据此删除。额外依赖可写入 **额外保留原名的形变**，也可通过 **跳过形变处理的 Renderer** 排除整个 Renderer。这些保留设置不关闭你自己配置的其他插件；如需保留某形变，还应在 AAO 等上游工具中配置保留。

保守模式按较宽的规则保护日文/中文与常见 MMD 名称，可能保留一些实际未使用的形变。未知自定义运行组件或状态机 Behaviour 会让本工具保留全部形变，仅继续资源名称处理。

## 处理范围

- 可改名：可复制的 Mesh、Material、Texture2D、AnimationClip、AnimatorController、AnimatorOverrideController、BlendTree、AvatarMask、Avatar 和 VRC 表情菜单/参数资源对象。
- 保留兼容名称：骨骼与 GameObject 层级、运行时参数字符串、状态机状态名、菜单可见文字、Shader、SDK 代理动画。
- 不可读（Read/Write 关闭）和 Crunch 纹理保留原资源及名称；不修改压缩、像素或导入设置。可读且非 Crunch 的 Texture2D 才参与资源名混淆。其他纹理类型、音频等不在首版处理范围。
- 动画换网格的 Renderer 保留其 BlendShape 结构；动画中的 Mesh/Material 引用仍同步到资源副本。
- 场景和工程内原 PNG、FBX、材质、动画及其 `.meta` 不重命名、不覆盖。只有用户添加的组件需要随场景/Prefab 正常保存。
- 不写 VRChat 本地参数文件，不请求网络，不自动上传 Avatar。

形变烘焙当前只处理单帧、帧权重 100、当前权重位于 0–100 的常见形变，保留原始 Bounds。骨骼、权重、UV、拓扑、顶点属性和保留形变的数据随网格副本保留。

## 开发与验证

见 [TESTING.md](TESTING.md) 与 [ARCHITECTURE.md](ARCHITECTURE.md)。`Tests~` 是需要主动导入的 Editor 验证工具，不会随正式 Unity 包安装。真实 Avatar 和测试生成资产均不包含在开源仓库或发布包中。

## 许可证

[MIT](LICENSE)。依赖的 NDMF、VRChat SDK、可选的 Avatar Optimizer 使用各自的许可证，未捆绑在本发布包中。
