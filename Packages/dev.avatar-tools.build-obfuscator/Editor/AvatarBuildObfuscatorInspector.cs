using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace AvatarTools.Obfuscation.Editor
{
    [CustomEditor(typeof(AvatarBuildObfuscator))]
    internal sealed class AvatarBuildObfuscatorInspector : UnityEditor.Editor
    {
        private readonly Dictionary<string, SerializedProperty> properties = new Dictionary<string, SerializedProperty>();
        private InspectorLanguage language;
        private float inspectorWidth;

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

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement { name = "avatar-build-obfuscator-inspector" };
            var username = new TextField
            {
                name = "avatar-build-obfuscator-username",
                bindingPath = "userName",
                isDelayed = false
            };
            var header = new IMGUIContainer(() => DrawGUI(DrawHeader));
            var options = new IMGUIContainer(() => DrawGUI(DrawOptions));
            var updates = new PackageUpdatePanel((InspectorLanguage)Property("inspectorLanguage").enumValueIndex);
            root.Add(header);
            root.Add(updates);
            root.Add(username);
            root.Add(options);
            root.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                inspectorWidth = evt.newRect.width;
                username.labelElement.style.minWidth = LabelWidth(inspectorWidth);
                username.labelElement.style.width = LabelWidth(inspectorWidth);
                header.MarkDirtyRepaint();
                options.MarkDirtyRepaint();
            });
            root.TrackPropertyValue(Property("inspectorLanguage"), property =>
            {
                var selected = (InspectorLanguage)property.enumValueIndex;
                ((INotifyValueChanged<string>)username.labelElement).SetValueWithoutNotify(InspectorLocalization.Text("userName", selected));
                username.tooltip = InspectorLocalization.Text("userName.tip", selected);
                updates.SetLanguage(selected);
                header.MarkDirtyRepaint();
                options.MarkDirtyRepaint();
            });
            root.TrackPropertyValue(Property("userName"), property =>
            {
                if (username.value != property.stringValue) username.SetValueWithoutNotify(property.stringValue);
                options.MarkDirtyRepaint();
            });
            var initialLanguage = (InspectorLanguage)Property("inspectorLanguage").enumValueIndex;
            username.label = InspectorLocalization.Text("userName", initialLanguage);
            username.tooltip = InspectorLocalization.Text("userName.tip", initialLanguage);
            return root;
        }

        public override void OnInspectorGUI() => DrawGUI(() =>
        {
            DrawHeader();
            Field("userName");
            DrawOptions();
        });

        private void DrawGUI(System.Action draw)
        {
            serializedObject.Update();
            float previousWidth = EditorGUIUtility.labelWidth;
            try
            {
                EditorGUIUtility.labelWidth = LabelWidth(inspectorWidth > 0 ? inspectorWidth : EditorGUIUtility.currentViewWidth);
                language = (InspectorLanguage)Property("inspectorLanguage").enumValueIndex;
                draw();
                serializedObject.ApplyModifiedProperties();
            }
            finally { EditorGUIUtility.labelWidth = previousWidth; }
        }

        private static float LabelWidth(float width)
        { return Mathf.Min(Mathf.Clamp(width * .52f, 190f, 320f), Mathf.Max(0f, width - 110f)); }

        private void DrawHeader()
        {
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
        }

        private void DrawOptions()
        {
            var mode = Property("mode");
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
        }
    }
}
