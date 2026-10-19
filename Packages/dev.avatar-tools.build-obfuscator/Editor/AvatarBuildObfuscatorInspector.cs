using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Editor
{
    [CustomEditor(typeof(AvatarBuildObfuscator))]
    internal sealed class AvatarBuildObfuscatorInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Avatar Build Obfuscator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("在 Avatar 根对象添加一次即可。构建时处理 NDMF 副本，源 FBX、贴图、材质和动画保持原样。", MessageType.Info);
            var mode = serializedObject.FindProperty("mode");
            mode.enumValueIndex = EditorGUILayout.Popup("处理模式", mode.enumValueIndex,
                new[] { "保守（推荐）", "激进" });
            var userName = serializedObject.FindProperty("userName");
            EditorGUILayout.PropertyField(userName, new GUIContent("用户名"));
            EditorGUILayout.HelpBox("每个人填写自己的用户名（支持中文）。用户名作为混淆种子，生成 o_… 形式的乱码名称；资源名不附加用户名。相同用户名及相同调用顺序生成相同名称序列。", MessageType.None);
            if (string.IsNullOrWhiteSpace(userName.stringValue))
                EditorGUILayout.HelpBox("请填写你自己的用户名后再构建。新组件不预填作者姓名。", MessageType.Warning);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renameAssets"), new GUIContent("混淆资源名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("freezeUnusedBlendShapes"), new GUIContent("烘焙固定和未使用形变"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renameDynamicBlendShapes"), new GUIContent("混淆保留的形变名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("preserveBlendShapes"), new GUIContent("额外保留原名的形变"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("excludedRenderers"), new GUIContent("跳过形变处理的 Renderer"), true);
            if (mode.enumValueIndex == (int)ProtectionMode.Aggressive)
                EditorGUILayout.HelpBox("激进模式允许处理未被本地动画引用的 MMD 形变，并混淆更多兼容名称，可能影响外部世界按名称驱动的表情。动画、口型、眨眼仍会保留。无法安全处理的资源仍会跳过。", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("保守模式保留 MMD 兼容形变；未知形变驱动、网格替换、特殊形变帧等情况会自动跳过。", MessageType.None);
            EditorGUILayout.HelpBox("这是名称混淆和形变裁剪，不包含贴图加密。保留的动态形变仍存在于上传网格中。骨骼、层级路径、运行参数、Shader 和代理动画保持兼容名称。", MessageType.None);
            EditorGUILayout.HelpBox("不可读贴图与 Crunch 贴图保留原名称和内容，不修改导入设置。", MessageType.None);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
