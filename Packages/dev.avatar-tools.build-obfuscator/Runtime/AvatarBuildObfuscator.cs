using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

namespace AvatarTools.Obfuscation
{
    public enum ProtectionMode { Conservative, Aggressive }

    /// <summary>Editor-only build instructions. NDMF runs them on its build copy.</summary>
    [AddComponentMenu("Avatar Tools/Build Obfuscator")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/AKaNecoo/AvatarBuildObfuscator")]
    [MovedFrom(true, "Kiine.AvatarGuard", "dev.kiine.avatar-guard.runtime", "AvatarGuard")]
    public sealed class AvatarBuildObfuscator : MonoBehaviour, VRC.SDKBase.IEditorOnly
    {
        public ProtectionMode mode = ProtectionMode.Conservative;
        [FormerlySerializedAs("namePrefix")]
        [Tooltip("填写你自己的用户名，支持中文。用户名用于派生混淆种子，不作为资源名称前缀。")]
        public string userName = "";
        public bool renameAssets = true;
        public bool freezeUnusedBlendShapes = true;
        public bool renameDynamicBlendShapes = true;
        [Tooltip("这些形变保留原名和原始数据。适用于外部工具、特殊世界等依赖。")]
        public string[] preserveBlendShapes = new string[0];
        [Tooltip("这些 Renderer 的形变完全跳过；资源名称仍可混淆。")]
        public SkinnedMeshRenderer[] excludedRenderers = new SkinnedMeshRenderer[0];
    }
}
