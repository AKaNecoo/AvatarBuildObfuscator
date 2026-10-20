# Avatar Build Obfuscator

用户名保留左侧标签、右侧输入框的单行布局，使用原生绑定文本控件处理焦点、输入法与撤销。添加组件时搜索 **Avatar Build Obfuscator**，选择 **Avatar Tools / Avatar Build Obfuscator**。

[English](README.en.md) · [日本語](README.ja.md)

在 VRChat Avatar 根对象添加 **Avatar Tools / Avatar Build Obfuscator**，填写自己的用户名，NDMF 构建时自动处理副本。名称使用 **`用户名_IlIl…`**：支持中文前缀，后缀由 128 个大写 `I` / 小写 `l` 组成；用户名同时参与生成确定性名称序列。新组件的用户名默认留空。

本文描述 **0.3.1**。`beta` 接收测试更新，`main` 保持稳定版本。构建产物可以直接使用，**没有 OSC、密码或 Shader 解密步骤**。名称与结构混淆不保证防提取：贴图像素、最终几何和必要动态 BlendShape 仍在上传数据中。

## 安装与更新

需要 Unity **2022.3**，并通过 VCC 安装 Avatar SDK **3.7.0+** 和 [NDMF](https://github.com/bdunderscore/ndmf) **1.14.8+**。Git 安装不会自动补齐 VPM 依赖。AAO、MA、VRCFury、TTT 可选。

在 **Window → Package Manager → ＋ → Add package from git URL…** 输入一次稳定版地址：

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

测试版把末尾 `#main` 改为 `#beta`。也可以从 [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) 下载 `.unitypackage` 导入；只安装一份插件，切换方式前仅移除另一种方式的插件文件，不删除 Avatar。脚本 GUID 保持一致，迁移后核对组件引用。

组件中的 **版本与更新** 显示版本、渠道、更新状态，提供 **检查更新 / 立即更新 / 更新说明**。自动检查默认开启，结果缓存6小时；自动安装默认关闭，可按工程开启。自动安装在启动至少60秒后、Unity在后台空闲且SDK控制面板关闭时执行，避开Play、构建、编译和导入。同一提交失败后不会自动重试，断网不会阻止使用插件。

Git 安装通过 Package Manager 更新到当前渠道**通过 Package CI 的具体提交**，保留渠道记录以便继续更新；`.unitypackage` 安装下载 CI 发布的安装包并验证 GitHub 提供的 SHA-256 后导入。下载文件位于工程 `Library/AvatarBuildObfuscatorUpdates/pending.unitypackage`，成功、失败或取消后删除；重载后会恢复清理。更新仅替换插件代码，不运行模型混淆，也不修改模型资源。更新过程会编译和重载。

设置仅保存在本工程 `UserSettings/AvatarBuildObfuscatorUpdates.asset`，不保存在Avatar上，也不发送用户名/模型。手动固定标签/提交、其他仓库、改位置的Assets安装和本地 `file:` 开发包不会自动覆盖。安装包的初始渠道由是否为预发布版本决定（稳定版main、预发布beta），后续保持渠道。Git仍可通过重新添加原 `#main`/`#beta` URL或Package Manager手动更新；不要同时安装Git包和Assets副本。

## CI 与发行

[Package CI](https://github.com/AKaNecoo/AvatarBuildObfuscator/actions/workflows/package-ci.yml) 在 main/beta 推送和 PR 时检查包结构、GUID、更新策略，构建并回读校验可复现的 UPM `.tgz`、ZIP、`.unitypackage` 和 SHA256SUMS。仅本仓库 main/beta 推送可发布 Release；PR不能发布。main为稳定版，beta为预发布。同一版本不允许换产品内容覆盖已发布包，产品改动需要加版本号。

云端CI编译并测试独立的更新策略代码、验证打包；它**不等于 Unity 编辑器或真实 Avatar 的自动验收**。Unity编译、NDMF回归、输入与Play测试见 [TESTING.md](TESTING.md)，由维护者在授权测试工程验证后再推进main。CI不上传模型、私有日志或工程。

## 使用

组件顶部的 **语言 / Language / 言語** 可选择 **简体中文、English、日本語**。默认简体中文，语言保存在各组件自身，只改变 Inspector 的标签、说明和提示，不改变用户名、模式或构建结果。

Play 调试和 VRChat SDK 构建均支持 NDMF 默认的 `Packages/nadena.dev.ndmf/__Generated` 临时目录；手动生成到 Assets 的流程同样支持。

1. 选择包含 `VRCAvatarDescriptor` 的 Avatar 根对象，添加 **Avatar Tools → Avatar Build Obfuscator**。
2. 填写自己的用户名，选择模式和处理选项。前缀最多保留 32 字符，不安全的路径/文件名字符替换为下划线；完整规范化用户名仍参与种子计算。
3. 使用 VRChat SDK 正常构建。Console 中的 **`[Avatar Build Obfuscator]`** 列出形变、对象、资源、参数、动画内部名称、路径、Avatar 重建和顶点重排的数量与保留原因。
4. 在构建副本或实际产物中查看效果。**源 Hierarchy、Scene 和 Project 中的名称保持原样**；这不代表构建副本没有处理。

用户名不是密码。相同用户名和相同遍历顺序生成相同序列；上游优化器可能改变遍历顺序，不保证资源跨所有构建始终同名。I/l 名称仍是可被机器区分的标识符。

## 两种模式

两档都以全面混淆为目标，差别在处理未知兼容风险的门槛。可以分别关闭资源名、形变烘焙、形变名、形变顺序、对象名、同级对象顺序、骨骼、参数、动画内部名称和顶点重排。

| 范围 | 保守（默认） | 激进 |
| --- | --- | --- |
| 支持的资源副本名称 | 处理 | 处理 |
| 对象/层级、动画与 AvatarMask 路径 | 完整重映射；保留未知依赖 | 允许继续处理未知名称依赖 |
| Humanoid 骨骼 | 重建 Avatar，先验证骨骼映射和尺度 | 同样执行重建预检 |
| 自定义参数 | 同步控制器、曲线、菜单、Driver、PhysBone、Contacts | 同样同步，并扩大内部面捕参数处理 |
| 动画层、状态机、状态与过渡名称 | 处理已知图；保留未知驱动 | 允许未知驱动 |
| 普通固定/未使用 BlendShape | 按当前权重烘焙后移除 | 相同 |
| 动画、口型、眨眼必需形变 | 保留数据，更新名称/绑定/索引 | 相同 |
| MMD、中文/日文兼容形变 | 保留数据与名称 | 可处理未使用形变并改名保留形变 |
| BlendShape 索引顺序 | 打乱，重映射权重/眼皮索引 | 相同 |
| GameObject 同级顺序 | 打乱，保留已知顺序依赖 | 相同；未知驱动可继续 |
| 顶点顺序 | 仅确认没有顶点 ID 依赖的内置 Shader | 允许自定义 Shader，存在兼容风险 |

**额外保留原名的形变**、**保留层级名称的对象/子树**、**保留参数**和 Renderer 排除项可指定已知外部依赖；参数支持末尾 `*` 前缀匹配。需要避免其他插件提前删除相关数据时，也应在那些插件里配置保留。

保守模式依靠已识别契约、完整重映射和验证来维持功能；它不等于对任意外部世界、设备或未识别插件的兼容保证。激进模式接受未知风险，明确已知不能正确处理的契约仍保留并报告。

## 处理范围与例外

- 资源包括 Mesh、Material、动画图、AvatarMask、Avatar、VRC 表情资源、导入贴图与音频，以及可复制的 Shader 源码对象。
- 导入贴图（包括 Read/Write 关闭和 Crunch）、音频通过构建临时目录中的独立文件/Importer 副本改名，不更改源文件、压缩或读取设置。非主资源、内置资源和不支持的导入类型保留，日志会说明原因。
- Humanoid 重建同时更新公开骨骼名及 Unity 的内部父名称，保留人体语义、肌肉配置和尺度。Generic/无效 Avatar、无法可靠重建的骨架、歧义路径保留相应层级。
- 自定义参数同步包含动画参数曲线、条件、状态参数、BlendTree、菜单主/子参数、Driver 的目标/来源、PhysBone 前缀与生成后缀、Contact 参数。菜单可见文字保持可用。
- VRChat 内置参数、已知外部面捕/OSC 输入接口、SDK 代理动画的真实资产身份，以及固定渲染属性/关键字保持契约。NadeVision 的固定 Shader 名称也保留，避免破坏其构建检查。
- 形变顺序重排保留每个形变的全部帧，并同步当前权重和 SDK 眼皮索引。对象只打乱同级顺序，父子关系与局部坐标不变；UI、重复同名、Generic 骨架和手动保留的顺序不动，Humanoid 映射会再次核对。两个顺序开关独立于改名。
- 导入副本通过一个批次统一导入，避免逐个 CopyAsset 反复触发导入。复制阶段有取消进度，异常后也会释放导入暂停；正在执行的 Unity 原生导入/Shader 编译不能保证即时中断。
- 顶点重排同时重排全部原始顶点流、UV/颜色、索引、可变骨骼权重和全部保留形变帧；不改变模型的目标形状，不需要运行时还原。Cloth、额外顶点流和不可读网格保持顺序。
- Shader 名称复制仅适用于可读取并重新编译的 `.shader` 源码，保持属性、关键字、Pass 与引用；内置/移动白名单/容器生成 Shader 保留。未实施需要改写外部读取端的 Shader 属性协议混淆。
- 网格切换的形变结构、特殊形变帧、超出 0–100 的烘焙权重和手动排除项有保护规则。动画常量曲线也视为动态使用。

## 源文件与验证

只处理 NDMF 构建副本和新生成资源，不覆盖源场景、FBX、纹理、材质、动画、Importer 或原 `.meta` GUID。添加组件本身不会执行处理，也不会自动上传 Avatar。

见 [TESTING.md](TESTING.md)、[ARCHITECTURE.md](ARCHITECTURE.md) 和 [CHANGELOG.md](CHANGELOG.md)。`Tests~` 为主动导入的开发测试，不随正式包安装。真实 Avatar、生成资产、原始日志和本地接续记录不上传仓库。客户端/设备验证与 Editor 验证分别记录。

0.1.0 / 0.2.0 组件保留脚本 GUID 和迁移元数据；`namePrefix` 会迁移至 `userName`。0.3 beta 按新要求重新显示用户名作为前缀，新增开关默认启用；升级后检查设置，再构建测试。

## 许可证与参考

[MIT](LICENSE)。依赖分别使用自己的许可证，不捆绑用户 Avatar。参考 [cocokoishi/AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator) 的名称混淆思路；本实现使用 ASCII `I/l`，并独立维护复制、引用重映射和兼容规则。
