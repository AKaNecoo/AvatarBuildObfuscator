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
            EditorGUILayout.HelpBox("在 Avatar 根对象添加一次即可。两种模式都只处理 NDMF 构建副本；构建后，Hierarchy / Scene / Project 中的源对象和资源仍显示原名，这是正常现象。", MessageType.Info);
            var mode = serializedObject.FindProperty("mode");
            mode.enumValueIndex = EditorGUILayout.Popup("处理模式", mode.enumValueIndex,
                new[] { "保守（兼容性优先）", "激进（接受未知依赖风险）" });
            var userName = serializedObject.FindProperty("userName");
            EditorGUILayout.PropertyField(userName, new GUIContent("用户名"));
            EditorGUILayout.HelpBox("处理后的名称为 用户名_IlIl…，后缀由 128 个大写 I / 小写 l 组成。用户名支持中文，也参与生成混淆种子；前缀最多保留 32 字符，路径/文件名不安全字符替换为下划线。相同用户名及调用顺序生成相同名称序列。", MessageType.None);
            if (string.IsNullOrWhiteSpace(userName.stringValue))
                EditorGUILayout.HelpBox("请填写你自己的用户名后再构建。新组件不预填作者姓名。", MessageType.Warning);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renameAssets"), new GUIContent("混淆资源名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("freezeUnusedBlendShapes"), new GUIContent("烘焙固定和未使用形变"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renameDynamicBlendShapes"), new GUIContent("混淆保留的形变名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shuffleBlendShapeOrder"), new GUIContent("打乱 BlendShape 索引顺序"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renameGameObjects"), new GUIContent("混淆构建副本对象名"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shuffleGameObjectOrder"), new GUIContent("打乱 GameObject 同级顺序"));
            using (new EditorGUI.DisabledScope(!serializedObject.FindProperty("renameGameObjects").boolValue))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("renameBones"), new GUIContent("混淆骨骼并重建 Humanoid 映射"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renameParameters"), new GUIContent("混淆自定义运行参数"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("renameAnimatorInternals"), new GUIContent("混淆动画层、状态机与状态名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reorderMeshVertices"), new GUIContent("重排网格顶点（无需解密）"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("preserveGameObjects"), new GUIContent("保留层级名称的对象/子树"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("preserveParameters"), new GUIContent("保留原名的参数（支持末尾 *）"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("preserveBlendShapes"), new GUIContent("额外保留原名的形变"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("excludedRenderers"), new GUIContent("跳过形变处理的 Renderer"), true);
            if (mode.enumValueIndex == (int)ProtectionMode.Aggressive)
                EditorGUILayout.HelpBox("激进模式继续处理未知驱动和可能依赖名称的对象，扩大 MMD 形变处理，并允许自定义 Shader 网格顶点重排。外部世界、工具或 Shader 可能受影响；可用保留列表或开关缩小范围。明确的 SDK 固定接口仍保留。", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("保守模式同样处理层级、自定义参数和动画内部名称；Avatar 重建需通过骨骼映射预检。未知驱动、外部字符串路径和未验证的顶点 ID 依赖会保留；MMD 兼容形变维持原名。", MessageType.None);
            EditorGUILayout.HelpBox("构建结果可直接使用，无 OSC、密码或运行时解密。贴图与必要动态形变仍可提取。VRChat 内置参数、已知面捕接口、代理动画及渲染属性契约保持兼容。", MessageType.None);
            EditorGUILayout.HelpBox("导入贴图（含不可读与 Crunch）在构建临时目录复制，保持导入格式；源文件与 Read/Write 不变。未支持的导入类型会报告保留原因。", MessageType.None);
            EditorGUILayout.HelpBox("验证是否生效：构建后在 Console 搜索 [Avatar Build Obfuscator]，查看形变、资源、对象改名/保留及路径改写数量与原因。激进模式的对象名变化应在构建副本或产物中检查。", MessageType.None);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
