using UnityEditor;
using UnityEngine;

namespace Kiine.AvatarGuard.Editor
{
    [CustomEditor(typeof(AvatarGuard))]
    internal sealed class AvatarGuardInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("一个木禾 · Avatar Guard", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("在 Avatar 根对象添加一次即可。构建时处理 NDMF 副本，源 FBX、贴图、材质和动画保持原样。", MessageType.Info);
            var mode = serializedObject.FindProperty("mode");
            mode.enumValueIndex = EditorGUILayout.Popup("处理模式", mode.enumValueIndex,
                new[] { "保守（推荐）", "激进" });
            EditorGUILayout.PropertyField(serializedObject.FindProperty("namePrefix"), new GUIContent("混淆名称前缀"));
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
            if (serializedObject.FindProperty("namePrefix").stringValue.IndexOfAny(new[] { '/', '\\', ':', '\n', '\r', '\t' }) >= 0)
                EditorGUILayout.HelpBox("前缀中的路径分隔符和控制字符会自动替换。", MessageType.Warning);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
