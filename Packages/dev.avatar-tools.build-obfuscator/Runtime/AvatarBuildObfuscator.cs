// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace AvatarTools.Obfuscation
{
    public enum ProtectionMode { Conservative, Aggressive }
    public enum InspectorLanguage { SimplifiedChinese, English, Japanese }

    /// <summary>Editor-only build instructions. NDMF runs them on its build copy.</summary>
    [AddComponentMenu("Avatar Tools/Avatar Build Obfuscator")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/AKaNecoo/AvatarBuildObfuscator")]
    [MovedFrom(true, "Kiine.AvatarGuard", "dev.kiine.avatar-guard.runtime", "AvatarGuard")]
    public sealed class AvatarBuildObfuscator : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public InspectorLanguage inspectorLanguage = InspectorLanguage.SimplifiedChinese;
        public ProtectionMode mode = ProtectionMode.Conservative;
        [FormerlySerializedAs("namePrefix")]
        [Tooltip("新添加时默认使用模型的 avtr_… ID，可自行修改。处理后的名称为 用户名_ÌÍÎÏ…；用户名也用于派生混淆种子。")]
        public string userName = "";
        public bool renameAssets = true;
        public bool freezeUnusedBlendShapes = true;
        public bool renameDynamicBlendShapes = true;
        [Tooltip("打乱保留形变的索引顺序，同步权重和 SDK 眼皮索引。独立于名称混淆。")]
        public bool shuffleBlendShapeOrder = true;
        [Tooltip("混淆构建副本对象名，同时重写动画与 AvatarMask 路径；保守模式保留尚无法验证的依赖。")]
        public bool renameGameObjects = true;
        [Tooltip("打乱构建副本的同级对象顺序，不改变父子关系和坐标。UI、重复同名和已知顺序依赖保留。")]
        public bool shuffleGameObjectOrder = true;
        public bool renameParameters = true;
        public bool renameAnimatorInternals = true;
        public bool renameBones = true;
        [Tooltip("在复制的网格中重排顶点及其全部关联数据，无需解密。保守模式只处理能确认材质不依赖顶点 ID 的网格。")]
        public bool reorderMeshVertices = true;
        [Tooltip("保留自定义参数名；末尾 * 表示前缀匹配。VRChat 内置参数自动保留。")]
        public string[] preserveParameters = new string[0];
        [Tooltip("保留这些对象及其子树和祖先的层级名称，适用于按名称查找的外部工具。")]
        public Transform[] preserveGameObjects = new Transform[0];
        [Tooltip("这些形变保留原名和原始数据。适用于外部工具、特殊世界等依赖。")]
        public string[] preserveBlendShapes = new string[0];
        [Tooltip("这些 Renderer 的形变完全跳过；资源名称仍可混淆。")]
        public SkinnedMeshRenderer[] excludedRenderers = new SkinnedMeshRenderer[0];

#if UNITY_EDITOR
        private void Reset()
        {
            if (!string.IsNullOrWhiteSpace(userName)) return;
            // PipelineManager lives in VRCCore-Editor. Read its serialized ID without
            // introducing an editor-only SDK DLL dependency into this runtime assembly.
            foreach (var component in GetComponents<Component>())
            {
                if (component == null || component.GetType().FullName != "VRC.Core.PipelineManager") continue;
                using (var serialized = new UnityEditor.SerializedObject(component))
                {
                    var id = serialized.FindProperty("blueprintId");
                    if (id != null && id.propertyType == UnityEditor.SerializedPropertyType.String &&
                        id.stringValue.StartsWith("avtr_", System.StringComparison.Ordinal)) userName = id.stringValue;
                }
                break;
            }
        }
#endif
    }
}
