# Avatar Build Obfuscator

[English](README.en.md) · [日本語](README.ja.md)

在 VRChat Avatar 根对象添加一个组件，在构建时自动混淆资源与动态 BlendShape 名称，并烘焙、移除能确认不再需要变化的 BlendShape。每个人在组件中填写自己的**用户名**，支持中文。用户名用于派生混淆种子，生成 `o_…` 形式的不透明资源名，名称中不直接附加用户名。新组件不预填作者姓名。

**这是一款名称混淆与形变裁剪工具。它不加密贴图或网格，不需要 OSC，也不能阻止别人从上传文件中提取贴图和保留的动态 BlendShape。** 烘焙后的最终外观仍然包含在网格中。

**构建后，Unity 的 Hierarchy / Scene / Project 中仍显示原名是正常现象。** 保守和激进模式都只处理 NDMF 构建副本。判断是否生效，请查看 Console 中的 `[Avatar Build Obfuscator]` 数量与跳过原因，以及实际构建产物。当前激进模式的主要额外处理范围是 MMD 等兼容形变；它也保留骨骼、层级和运行参数名。

## 安装与使用

使用 Unity **2022.3** 的 VRChat Avatar 工程，先通过 VCC 安装 Avatar SDK 与 [NDMF](https://github.com/bdunderscore/ndmf)。已安装 Modular Avatar 的工程通常已有 NDMF。Git 安装本工具不会替代 VCC 安装这些依赖。Avatar Optimizer 是可选依赖，已有设置不会被本工具修改，也不需要更换 Shader。

### 通过 Git 安装（推荐）

电脑需要安装 [Git](https://git-scm.com/)；Windows 上需确保 Unity 能通过 `PATH` 找到 Git。

1. 打开 **Window → Package Manager**。
2. 点击左上角 **＋ → Add package from git URL…**。
3. 粘贴以下完整地址，点击 **Add**，等待导入和编译完成：

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

**这条地址只需安装时输入一次。以后更新：**在 **Package Manager** 中选中 **Avatar Build Obfuscator**，点击 **Update** 获取 `main` 分支的新提交，无需重新输入地址。

Unity 会锁定已安装的提交，不会在每次打开工程时自动拉取。`?path=…` 指定仓库中的包目录，不能省略；`#main` 让后续手动更新跟随该分支。参见 [Unity Git 安装说明](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-ui-giturl.html)与 [Git 更新及锁定提交说明](https://docs.unity3d.com/2022.3/Documentation/Manual/upm-git.html#git-locks)。

### 测试版与稳定版

`beta` 接收完成修改和相应验证后的日常更新；`main` 用于稳定版本，不随每次测试更新推进。测试版可能存在尚未发现的问题。需要跟进测试版时，使用以下地址安装：

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#beta
```

从 Git 稳定版切换到测试版时，在 **Add package from git URL…** 中输入测试版地址一次，即可更新同一个包的分支引用。之后选中该包并点击 **Update**，跟进 `beta` 的更新。`#main` 安装不会自动切换到 `beta`；切回稳定版时重新添加上面的 `#main` 地址。

### 从 Unity 安装包导入

也可以从 [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) 下载 `.unitypackage` 并导入工程。此方式通过导入新版安装包更新，不使用 Package Manager 的 Git 更新按钮。

**同一工程只安装一份。**如果已经通过 `.unitypackage` 导入，切换 Git 安装前先移除 `Assets/AvatarBuildObfuscator` 中的插件文件；保留 Avatar、场景和自己的其他资产。安装 Git 包后检查原组件与用户名设置，避免双重安装造成程序集冲突。

### 添加组件并构建

1. 选择挂有 `VRCAvatarDescriptor` 的 Avatar 根对象，选择 **Add Component → Avatar Tools → Build Obfuscator**。
2. 填写你自己的**用户名**，选择 **保守** 或 **激进**。默认三个处理选项全部开启。
3. 使用 VRChat SDK 正常构建/上传。NDMF 会先处理构建副本，随后本工具处理副本中的资源。Console 的 `[Avatar Build Obfuscator]` 日志会列出数量与跳过原因。

如需固定在某个版本，可将 Git 地址末尾的 `#main` 改为发布标签，例如 `#v0.2.0`；固定标签不会跟随 `main` 更新。源码也包含标准包目录 `Packages/dev.avatar-tools.build-obfuscator`，开发者可在依赖已安装的工程中作为本地包导入。

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

## 从 0.1.0 升级

0.2.0 使用通用名称 Avatar Build Obfuscator，仓库、组件菜单和安装目录已更名。旧组件的 `namePrefix` 字段会迁移成 `userName`，原先填写的值只用于派生混淆名。脚本 .meta GUID 保持不变，并包含类型迁移标记。在已有工程中升级后，检查用户名字段再构建；UPM/VPM 用户请移除旧包引用后安装新版，避免双重安装。

相同用户名与相同调用顺序会生成同一名称序列；上游优化器可能改变遍历顺序，不保证每个资源跨所有构建始终使用相同名称。用户名不是密码，名称混淆不提供加密保密性。
