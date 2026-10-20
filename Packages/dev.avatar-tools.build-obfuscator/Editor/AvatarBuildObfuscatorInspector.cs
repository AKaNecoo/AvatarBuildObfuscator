using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Editor
{
    [CustomEditor(typeof(AvatarBuildObfuscator))]
    internal sealed class AvatarBuildObfuscatorInspector : UnityEditor.Editor
    {
        private readonly Dictionary<string, SerializedProperty> properties = new Dictionary<string, SerializedProperty>();
        private InspectorLanguage language;

        private void OnEnable() { properties.Clear(); }
        private SerializedProperty Property(string name)
        {
            if (!properties.TryGetValue(name, out var value)) properties[name] = value = serializedObject.FindProperty(name);
            return value;
        }
        private void Field(string name, bool children = false)
        { EditorGUILayout.PropertyField(Property(name), InspectorLocalization.Content(name, language), children); }
        private void Help(string key, MessageType type = MessageType.None)
        { EditorGUILayout.HelpBox(InspectorLocalization.Text(key, language), type); }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            float previousWidth = EditorGUIUtility.labelWidth;
            try
            {
                EditorGUIUtility.labelWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth * .52f, 190f, 320f);
                EditorGUILayout.LabelField("Avatar Build Obfuscator", EditorStyles.boldLabel);
                var selector = Property("inspectorLanguage");
                language = (InspectorLanguage)selector.enumValueIndex;
                selector.enumValueIndex = EditorGUILayout.Popup(
                    new GUIContent("语言 / Language / 言語", InspectorLocalization.Text("language.tip", language)),
                    selector.enumValueIndex, InspectorLocalization.LanguageNames);
                language = (InspectorLanguage)selector.enumValueIndex;
                Help("intro", MessageType.Info);
                var mode = Property("mode");
                mode.enumValueIndex = EditorGUILayout.Popup(InspectorLocalization.Text("mode", language), mode.enumValueIndex,
                    new[] { InspectorLocalization.Text("conservative", language), InspectorLocalization.Text("aggressive", language) });
                Field("userName");
                Help("naming");
                if (string.IsNullOrWhiteSpace(Property("userName").stringValue)) Help("usernameRequired", MessageType.Warning);
                Field("renameAssets");
                Field("freezeUnusedBlendShapes");
                Field("renameDynamicBlendShapes");
                Field("shuffleBlendShapeOrder");
                Field("renameGameObjects");
                Field("shuffleGameObjectOrder");
                using (new EditorGUI.DisabledScope(!Property("renameGameObjects").boolValue)) Field("renameBones");
                Field("renameParameters");
                Field("renameAnimatorInternals");
                Field("reorderMeshVertices");
                Field("preserveGameObjects", true);
                Field("preserveParameters", true);
                Field("preserveBlendShapes", true);
                Field("excludedRenderers", true);
                if (mode.enumValueIndex == (int)ProtectionMode.Aggressive) Help("aggressiveHelp", MessageType.Warning);
                else Help("conservativeHelp");
                Help("limits");
                Help("imports");
                Help("verification");
                serializedObject.ApplyModifiedProperties();
            }
            finally { EditorGUIUtility.labelWidth = previousWidth; }
        }
    }
}