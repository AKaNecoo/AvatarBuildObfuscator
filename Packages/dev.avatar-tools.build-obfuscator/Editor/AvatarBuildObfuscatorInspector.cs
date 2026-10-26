// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

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
        private bool showExperiments;
        private bool showSubtreeModes;
        private ObfuscationPreset preset;

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
            DrawSubtreeModes();
            preset = (ObfuscationPreset)EditorGUILayout.ObjectField(InspectorLocalization.Text("preset", language), preset, typeof(ObfuscationPreset), false);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(InspectorLocalization.Text("savePreset", language)))
                { serializedObject.ApplyModifiedProperties(); ObfuscationPreset.Save((AvatarBuildObfuscator)target); }
                using (new EditorGUI.DisabledScope(preset == null))
                    if (GUILayout.Button(InspectorLocalization.Text("applyPreset", language)))
                    { serializedObject.ApplyModifiedProperties(); preset.Apply((AvatarBuildObfuscator)target); serializedObject.Update(); }
            }
            Help("naming");
            if (string.IsNullOrWhiteSpace(Property("userName").stringValue)) Help("usernameRequired", MessageType.Warning);
            Field("randomizeEachBuild");
            Field("auditBuild");
            if (GUILayout.Button(InspectorLocalization.Text("viewAudit", language)))
                BuildReportWindow.Open(language);
            if (GUILayout.Button(InspectorLocalization.Text("compareBuild", language)))
                BuildComparisonWindow.Open(((AvatarBuildObfuscator)target).gameObject, language);
            Field("renameAssets");
            Field("freezeUnusedBlendShapes");
            Field("pruneEmptyBlendShapes");
            Field("renameDynamicBlendShapes");
            Field("allowMmdObfuscation");
            Field("shuffleBlendShapeOrder");
            Field("renameGameObjects");
            Field("shuffleGameObjectOrder");
            using (new EditorGUI.DisabledScope(!Property("renameGameObjects").boolValue)) Field("renameBones");
            Field("renameParameters");
            Field("renameAnimatorInternals");
            Field("reorderMeshVertices");
            Field("reorderBonePalette");
            Field("embedBuildFingerprint");
            Field("embedOwnershipNotice");
            if (Property("embedOwnershipNotice").boolValue)
            {
                Field("rightsHolder");
                Field("rightsContact");
                Field("usageRestrictions");
                Help("ownershipHelp");
            }
            showExperiments = EditorGUILayout.Foldout(showExperiments, InspectorLocalization.Text("experiments", language), true);
            if (showExperiments)
            {
                Field("experimentalUvTransform");
                Field("mergeSynchronizedBlendShapes");
                Help("experimentsHelp", MessageType.Warning);
            }
            Field("preserveGameObjects", true);
            Field("preserveParameters", true);
            Field("preserveBlendShapes", true);
            Field("excludedRenderers", true);
            Field("preserveVertexOrderRenderers", true);
            Field("preserveBonePaletteRenderers", true);
            Field("excludeUvRenderers", true);
            if (mode.enumValueIndex == (int)ProtectionMode.Aggressive) Help("aggressiveHelp", MessageType.Warning);
            else Help("conservativeHelp");
            Help("limits");
            Help("imports");
            Help("verification");
        }

        private void DrawSubtreeModes()
        {
            var entries = Property("subtreeModes");
            showSubtreeModes = EditorGUILayout.Foldout(showSubtreeModes,
                InspectorLocalization.Text("subtreeModes", language) + " (" + entries.arraySize + ")", true);
            if (!showSubtreeModes) return;
            Help("subtreeModes.tip");
            for (int i = 0; i < entries.arraySize; i++)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var entry = entries.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("root"), InspectorLocalization.Content("subtreeRoot", language));
                    var selected = entry.FindPropertyRelative("mode");
                    selected.enumValueIndex = EditorGUILayout.Popup(InspectorLocalization.Text("mode", language), selected.enumValueIndex,
                        new[] { InspectorLocalization.Text("conservative", language), InspectorLocalization.Text("aggressive", language) });
                    if (GUILayout.Button(InspectorLocalization.Text("removeSubtreeMode", language)))
                    { entries.DeleteArrayElementAtIndex(i); break; }
                }
            }
            if (GUILayout.Button(InspectorLocalization.Text("addSubtreeMode", language)))
            {
                int index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                var added = entries.GetArrayElementAtIndex(index);
                added.FindPropertyRelative("root").objectReferenceValue = null;
                added.FindPropertyRelative("mode").enumValueIndex = Property("mode").enumValueIndex;
            }
        }
    }
}
