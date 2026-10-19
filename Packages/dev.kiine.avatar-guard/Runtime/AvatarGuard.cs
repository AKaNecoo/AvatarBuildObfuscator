using UnityEngine;

namespace Kiine.AvatarGuard
{
    public enum ProtectionMode { Conservative, Aggressive }

    /// <summary>Editor-only build instructions. NDMF runs them on its build copy.</summary>
    [AddComponentMenu("一个木禾/Avatar Guard")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/AKaNecoo/KiineAvatarGuard")]
    public sealed class AvatarGuard : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public ProtectionMode mode = ProtectionMode.Conservative;
        [Tooltip("中文可用。资源名称会变成：前缀_唯一随机串。")]
        public string namePrefix = "一个木禾";
        public bool renameAssets = true;
        public bool freezeUnusedBlendShapes = true;
        public bool renameDynamicBlendShapes = true;
        [Tooltip("这些形变保留原名和原始数据。适用于外部工具、特殊世界等依赖。")]
        public string[] preserveBlendShapes = new string[0];
        [Tooltip("这些 Renderer 的形变完全跳过；资源名称仍可混淆。")]
        public SkinnedMeshRenderer[] excludedRenderers = new SkinnedMeshRenderer[0];
    }
}
