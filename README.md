<p align="center">
  <img src="Packages/dev.avatar-tools.build-obfuscator/Editor/Icons/ABOWordmark.png" alt="ABO" width="200">
</p>

<h1 align="center">Avatar Build Obfuscator (ABO)</h1>

<p align="center">
  <a href="README.en.md">English</a> · <a href="README.ja.md">日本語</a>
</p>

<p align="center">为 VRChat Avatar 提供构建时混淆，保留源资产，无需运行时解密。</p>

## 功能

- 混淆资源、对象、骨骼、参数、Animator 内部及动态 BlendShape 名称，格式为“用户名＋随机后缀”，后缀使用 Ì、Í、Î、Ï 四种字符。
- 烘焙并移除可处理的固定／未使用 BlendShape，清理无效果空形变，保留必要的动态形变。
- 独立打乱 BlendShape、同级 GameObject、网格顶点和蒙皮骨骼索引表，并更新相关引用。
- 每次构建可重新随机化，生成本地处理报告；可选嵌入三语授权声明和构建指纹。

## 安装

需要 **Unity 2022.3、Avatar SDK 3.7.0+、NDMF 1.14.8+**。请先在软件包管理器中安装 SDK 和 [NDMF](https://github.com/bdunderscore/ndmf)。

**[ALCOM](https://vrc-get.anatawa12.com/zh-hans/alcom/)／[VCC](https://vrchat.com/home/download)／[VCL](https://start.gamemode1.com/pages/vrclearnlauncher/)（推荐）**

1. 打开[软件包仓库页](https://akanecoo.github.io/AvatarBuildObfuscator/)，点击添加仓库；也可在管理器中手动填写页面上的仓库地址。
2. 在管理器中选择工程，到软件包管理中安装 **Avatar Build Obfuscator**。beta 版需开启预发布版本。
3. 回到 Unity，按下方[使用](#使用)步骤给 Avatar 添加组件。

仓库页也提供 NDMF 官方仓库入口。**安装方式只选一种，避免重复安装。**

<details>
<summary>Git、unitypackage 安装与迁移</summary>

**Git：** 打开 **Window → Package Manager → ＋ → Add package from git URL…**，输入：

```text
https://github.com/AKaNecoo/AvatarBuildObfuscator.git?path=/Packages/dev.avatar-tools.build-obfuscator#main
```

测试版将末尾改为 `#beta`。

**unitypackage：** 从 [Releases](https://github.com/AKaNecoo/AvatarBuildObfuscator/releases) 下载并导入 `.unitypackage`。

**转用 VPM：** Git 版先在 Package Manager 中移除本包，再从 ALCOM／VCC／VCL 安装。VPM 可按 GUID 迁移旧 `.unitypackage` 目录。插件目录内不要存放自己的模型资产，场景上的组件设置无需删除。

</details>

## 使用

1. 在 **Hierarchy** 选中 Avatar 根对象，到右侧 **Inspector** 底部点击 **Add Component（添加组件）**，搜索 **Avatar Build Obfuscator** 并添加。
2. 确认或填写用户名，选择处理模式。
3. 使用 VRChat SDK 正常构建。效果在构建副本中查看，源 Hierarchy 和 Project 名称保持原样。

| 模式 | 说明 |
| --- | --- |
| 保守（默认） | 优先保持兼容，保留尚无法确认的依赖。 |
| 激进 | 扩大混淆范围，允许未知兼容风险，使用前需实测功能与外部联动。 |

**效果提示：** 保守模式优先兼容，混淆变化可能不明显；激进模式的混淆范围更广，通常效果更明显，但兼容风险也更高。

**需要 MMD 表情时，请保持“允许混淆 MMD 表情”和“允许混淆 MMD 对象名”均关闭。** 对象改名会改变原有路径，可能导致 MMD 表情失效。

<details>
<summary>更多设置与构建检查</summary>

- 组件支持简体中文、English、日本語。处理项可独立开关，也可通过保留列表跳过指定 Renderer 的处理。
- **按子树设置模式** 可为身体、衣服或配件分别选择；预设只保存处理开关与模式。共享资源和其他详细规则见[实现说明](ARCHITECTURE.md)。
- **查看最近一次构建报告** 可搜索处理记录、查看保留原因并核对声明／指纹。Console 的 `[Avatar Build Obfuscator]` 日志也会显示处理数量与保留原因。
- **测试构建与对照检查** 会生成临时副本进行采样比较，关闭窗口后清理；采样不能代替 VRChat 内的功能测试。
- 实验性的 **UV／贴图旋转**和**同步形变合并**默认关闭，支持范围见[兼容说明](ARCHITECTURE.md#exact-pruning-and-experimental-transforms)。

</details>

## 注意

- **公开前缀：** 用户名的前缀（最多 32 字符）会出现在产物名称中。新组件默认读取已有 Avatar ID，可自行修改；没有 ID 时手动填写。不希望公开该值时，请在构建前更换。
- **下载体积：** 混淆可能增加 Avatar 下载大小，实验性贴图旋转可能进一步增大体积。上传前请查看 SDK 构建统计。
- **保护边界：** 混淆不能保证防提取，贴图像素、最终几何和必要动态 BlendShape 仍可提取。
- **授权声明：** 填写的权利人和联系地址会随声明公开；声明不认证身份，也不保证 AI 会拒绝协助逆向本模型。

## 更新

- **VPM：** 在 ALCOM／VCC／VCL 中更新。
- **Git／unitypackage：** 在组件的**版本与更新**中操作；Git 也可使用 Package Manager。
- 自动检查默认开启，自动安装默认关闭；安装包更新后会删除临时下载文件。

<details>
<summary>Git 渠道切换</summary>

`#main` 和 `#beta` 独立更新，不会自动跨渠道切换。测试版转正式版时，在 Package Manager 用末尾为 `#main` 的同一 Git URL 重新添加，现有组件设置会保留。

</details>

## 反馈与更多

Bug 反馈：[GitHub Issues](https://github.com/AKaNecoo/AvatarBuildObfuscator/issues) · [kiinemaki@qq.com](mailto:kiinemaki@qq.com)。请附上 ABO／Unity 版本、使用模式、复现步骤及相关日志片段。

[详细规则与实现](ARCHITECTURE.md) · [验证范围](TESTING.md) · [更新日志](CHANGELOG.md) · [GPL-3.0 许可证](LICENSE)

自 0.5.0 起采用 GPL-3.0-only；更早已发布版本保留原 MIT 授权。依赖遵循各自许可证。

参考 [cocokoishi/AvatarObfuscator](https://github.com/cocokoishi/AvatarObfuscator) 的名称混淆思路，独立维护构建副本、引用重映射与兼容规则。
