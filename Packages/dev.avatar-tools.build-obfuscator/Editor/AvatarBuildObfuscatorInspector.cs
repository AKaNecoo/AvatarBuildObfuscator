// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

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
        private bool showExperiments;
        private bool showSubtreeModes;
        private ObfuscationPreset preset;
        private Texture2D wordmark;
        private GUIStyle titleStyle;
        private readonly PackageUpdatePanel updates = new PackageUpdatePanel();

        private void OnEnable()
        {
            properties.Clear();
            wordmark = null;
            titleStyle = null;
            PackageUpdates.Changed += Repaint;
        }
        private void OnDisable() { PackageUpdates.Changed -= Repaint; }
        private SerializedProperty Property(string name)
        {
            if (!properties.TryGetValue(name, out var value)) properties[name] = value = serializedObject.FindProperty(name);
            return value;
        }
        private void Field(string name, bool children = false)
        { EditorGUILayout.PropertyField(Property(name), InspectorLocalization.Content(name, language), children); }
        private void Help(string key, MessageType type = MessageType.None)
        { EditorGUILayout.HelpBox(InspectorLocalization.Text(key, language), type); }

        public override void OnInspectorGUI() => DrawGUI(() =>
        {
            DrawHeader();
            updates.Draw(language);
            GUI.SetNextControlName("AvatarBuildObfuscator.Username");
            Field("userName");
            DrawOptions();
        });

        private void DrawGUI(System.Action draw)
        {
            serializedObject.Update();
            float previousWidth = EditorGUIUtility.labelWidth;
            try
            {
                EditorGUIUtility.labelWidth = LabelWidth(EditorGUIUtility.currentViewWidth);
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
            if (wordmark == null)
                wordmark = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    AssetDatabase.GUIDToAssetPath("b3c15ccde3df48af9d6279af188268e1"));
            if (wordmark != null)
            {
                var slot = GUILayoutUtility.GetRect(0f, 64f, GUILayout.ExpandWidth(true));
                float width = Mathf.Min(180f, slot.width);
                var textureRect = new Rect(0f, .22f, 1f, .56f);
                float height = width * wordmark.height * textureRect.height / wordmark.width;
                var rect = new Rect(slot.x + (slot.width - width) * .5f, slot.y + 4f, width, height);
                GUI.DrawTextureWithTexCoords(rect, wordmark, textureRect, true);
            }
            if (titleStyle == null)
                titleStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("Avatar Build Obfuscator", titleStyle);
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
            Field("renameGameObjects");
            Field("shuffleGameObjectOrder");
            using (new EditorGUI.DisabledScope(!Property("renameGameObjects").boolValue)) Field("renameBones");
            Field("renameParameters");
            Field("renameAnimatorInternals");
            EditorGUILayout.Space(4f);
            Field("freezeUnusedBlendShapes");
            Field("pruneEmptyBlendShapes");
            Field("renameDynamicBlendShapes");
            Field("shuffleBlendShapeOrder");
            EditorGUILayout.Space(4f);
            Field("allowMmdObfuscation");
            using (new EditorGUI.DisabledScope(!Property("renameGameObjects").boolValue || Property("allowMmdObfuscation").boolValue))
                Field("renameMmdObjectNames");
            EditorGUILayout.Space(4f);
            Field("reorderMeshVertices");
            Field("reorderBonePalette");
            EditorGUILayout.Space(4f);
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
