# Avatar Build Obfuscator (ABO)

[English](README.en.md) · [日本語](README.ja.md)

用于 VRChat Avatar 的构建时混淆工具。添加组件后，NDMF 自动处理构建副本，不覆盖源资产，无需运行时解密。

## 功能

- 混淆资源、对象、骨骼、参数、Animator 内部及动态 BlendShape 名称，使用“用户名＋`Ì/Í/Î/Ï` 乱码”。
- 烘焙并移除可处理的固定／未使用 BlendShape，保留必要的动态形变。
- 可独立打乱 BlendShape、同级 GameObject、网格顶点和蒙皮骨骼索引表，并更新相关引用。
- 每次构建可重新随机化，移除无效果空形变，并生成本地残留审计报告。
- 可嵌入三语授权声明和构建指纹；实验性的 UV/贴图旋转与同步形变合并默认关闭。

实验贴图旋转支持部分 lilToon 2.4.1、Poiyomi 10.0.27、UnlitWF 2.14.0，以及已验证的 NonToon 0.3.0 默认单贴图配置，具体范围见[兼容说明](ARCHITECTURE.md#exact-pruning-and-experimental-transforms)。

## 安装

需要 **Unity 2022.3、Avatar SDK 3.7.0+、NDMF 1.14.8+**。先通过 [ALCOM](https://vrc-get.anatawa12.com/zh-hans/alcom/)／[VCC](https://vrchat.com/home/download)／[VRCLL](https://start.gamemode1.com/pages/vrclearnlauncher/) 安装 SDK 和 [NDMF](https://github.com/bdunderscore/ndmf)。

**[ALCOM](https://vrc-get.anatawa12.com/zh-hans/alcom/)／[VCC](https://vrchat.com/home/download)／[VRCLL](https://start.gamemode1.com/pages/vrclearnlauncher/)：** 打开[软件包仓库页](https://akanecoo.github.io/AvatarBuildObfuscator/)，点击添加仓库，或在管理器中手动添加页面上的仓库地址，再到工程的软件包管理中安装 **Avatar Build Obfuscator**。测试版需开启预发布版本；页面也提供 NDMF 官方仓库入口。

**Git：** 打开 **Window → Package Manager → ＋ → Add package from git URL…**，输入：

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

测试版将末尾改为 `#beta`。也可从 [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) 下载 `.unitypackage`；安装方式只选一种，避免重复安装。

转用 VPM 时，Git 版先在 Package Manager 中移除本包，再从 [ALCOM](https://vrc-get.anatawa12.com/zh-hans/alcom/)／[VCC](https://vrchat.com/home/download)／[VRCLL](https://start.gamemode1.com/pages/vrclearnlauncher/) 安装。VPM 可按 GUID 迁移旧 `.unitypackage` 目录。插件目录内不要存放自己的模型资产，场景上的组件设置无需删除。

## 使用

1. 在 **Hierarchy** 选中 Avatar 根对象，到右侧 **Inspector** 底部点击 **Add Component（添加组件）**，搜索 **Avatar Build Obfuscator**，选择 **Avatar Tools → Avatar Build Obfuscator**。
2. 填写用户名并选择模式。新组件默认读取已有 Avatar ID，可自行修改；没有 ID 时手动填写。
3. 使用 VRChat SDK 正常构建。效果在构建副本中查看，源 Hierarchy 和 Project 名称保持原样。

用户名的前缀（最多 32 字符）会公开出现在构建产物名称中；保留默认值时也会包含 Avatar ID 的一部分。不希望公开该值时，请在构建前更换。

组件中的 **查看最近一次构建报告** 可搜索处理记录、查看保留原因并核对声明／指纹；**测试构建与对照检查** 可生成临时副本进行采样比较，关闭窗口后清理。采样不代替 VRChat 内的完整功能测试。局部保留列表可跳过特定 Renderer 的处理，预设仅保存处理开关与模式。

组件支持简体中文、English、日本語。各处理项可单独开关，也可配置保留项。填写的权利人和联系地址会随声明公开；声明不认证身份，也不保证 AI 会拒绝协助逆向本模型。

| 模式 | 说明 |
| --- | --- |
| 保守（默认） | 优先保持功能，保留尚无法确认的依赖。 |
| 激进 | 扩大处理范围，允许未知兼容风险，可能影响外部联动。 |

**按子树设置模式** 可为身体、衣服或配件分别选择。最近的配置祖先生效；共享资源及全局参数／控制器保守优先，无法确认的优化器合并来源会回退保守并报告。

## 更新

VPM 安装在 **[ALCOM](https://vrc-get.anatawa12.com/zh-hans/alcom/)／[VCC](https://vrchat.com/home/download)／[VRCLL](https://start.gamemode1.com/pages/vrclearnlauncher/)** 中更新。Git 和 `.unitypackage` 安装可在组件的 **版本与更新** 中操作，Git 也可使用 Package Manager。自动检查默认开启，自动安装默认关闭；安装包更新后会删除临时下载文件。

Git 的 `#main` 和 `#beta` 独立更新，不会自动跨渠道切换。测试版转正式版时，在 Package Manager 用末尾为 `#main` 的同一 Git URL 重新添加，现有组件设置会保留。

## 注意

混淆可能增加 Avatar 的下载大小，增幅因模型和选项而异；实验性贴图旋转可能进一步增大体积。上传前请查看 SDK 构建统计。

需要 MMD 表情时，请保持 **“允许混淆 MMD 表情”和“允许混淆 MMD 对象名”均关闭**。对象改名会改变 `Body` 等路径，已在实际 MMD 世界中确认可导致表情失效，即使形变名称、数据和索引仍被保留。

MMD 兼容保护默认开启。命中兼容规则的网格，保守模式保留全部形变；激进模式保留匹配的名称、**全部形变数据和索引**，同时混淆其余名称。规则含日文／汉字及 `Blink`、`a` 等常见名称；非标准世界可能依赖额外名称，激进仍需实测。仅在不需要相应 MMD 兼容时开启上述选项。

混淆不能保证防提取：贴图像素、最终几何和必要动态 BlendShape 仍可提取。激进模式请先在测试构建中确认功能；Console 的 `[Avatar Build Obfuscator]` 日志可查看处理数量和保留原因。

## 更多

[详细规则与实现](ARCHITECTURE.md) · [验证范围](TESTING.md) · [更新日志](CHANGELOG.md) · [GPL-3.0 许可证](LICENSE)

自 0.5.0 起采用 GPL-3.0-only；更早已发布版本保留原 MIT 授权。依赖遵循各自许可证。

参考 [cocokoishi/AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator) 的名称混淆思路，独立维护构建副本、引用重映射与兼容规则。
