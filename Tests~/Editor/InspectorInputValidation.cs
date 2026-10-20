// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AvatarTools.Obfuscation.Validation
{
    public sealed class InspectorInputValidationView : VisualElement, IDisposable
    {
        private UnityEditor.Editor inspector;
        public TextField Username => this.Q<TextField>("avatar-build-obfuscator-username");

        public void Bind(AvatarBuildObfuscator marker)
        {
            this.Unbind();
            Clear();
            if (inspector != null) UnityEngine.Object.DestroyImmediate(inspector);
            inspector = UnityEditor.Editor.CreateEditor(marker);
            var visual = inspector.CreateInspectorGUI();
            if (visual == null) throw new Exception("Native Inspector tree missing");
            Add(visual);
            visual.Bind(inspector.serializedObject);
        }

        public void Dispose()
        {
            this.Unbind();
            if (inspector != null) UnityEngine.Object.DestroyImmediate(inspector);
        }
    }

    public sealed class InspectorInputValidationWindow : EditorWindow { }

    // Opt-in UI regression. Uses a preview-scene fixture, never the user's avatar.
    public static class InspectorInputValidation
    {
        [Serializable] public sealed class Result
        {
            public string stage = "running", version, source, failure;
            public int passed;
            public List<string> checks = new List<string>();
        }

        private static readonly Queue<Action> steps = new Queue<Action>();
        private static InspectorInputValidationWindow host;
        private static InspectorInputValidationView first, second;
        private static AvatarBuildObfuscator marker;
        private static Scene preview;
        private static Result result;
        private static string output;
        private static double nextStep;
        private static double waitingSince;
        private sealed class PendingCondition : Exception
        { public PendingCondition(string message) : base(message) { } }

        public static void Start(string outputPath)
        {
            if (result != null && result.stage == "running") throw new Exception("Input validation already active");
            output = outputPath;
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AvatarBuildObfuscator).Assembly);
            result = new Result { version = package.version, source = package.source.ToString() };
            steps.Clear();
            waitingSince = 0;
            preview = EditorSceneManager.NewPreviewScene();
            var fixture = new GameObject("Inspector input fixture") { hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild };
            SceneManager.MoveGameObjectToScene(fixture, preview);
            marker = fixture.AddComponent<AvatarBuildObfuscator>();
            host = ScriptableObject.CreateInstance<InspectorInputValidationWindow>();
            host.titleContent = new GUIContent("Inspector input validation");
            host.rootVisualElement.style.flexDirection = FlexDirection.Row;
            first = Open();
            second = Open();
            host.ShowUtility();
            host.position = new Rect(550, 120, 1280, 920);
            host.Focus();
            var labels = new[] { "用户名", "Username", "ユーザー名" };
            for (int language = 0; language < labels.Length; language++)
            {
                int selected = language;
                steps.Enqueue(() => host.rootVisualElement.Focus());
                steps.Enqueue(() =>
                {
                    var data = new SerializedObject(marker);
                    data.FindProperty("inspectorLanguage").enumValueIndex = selected;
                    data.FindProperty("userName").stringValue = "";
                    data.ApplyModifiedPropertiesWithoutUndo();
                });
                steps.Enqueue(() =>
                {
                    Check(first.Username.label == labels[selected] && second.Username.label == labels[selected], "Localized label " + selected);
                    Check(first.Username.value == "" && second.Username.value == "", "Empty value bound " + selected);
                    Check(first.Username.enabledInHierarchy && new SerializedObject(marker).FindProperty("userName").editable, "Fixture input editable " + selected);
                    Click(first);
                });
                steps.Enqueue(() => Check(Focused(first.Username), "Mouse click focuses input " + selected));
                const string typed = "abc_中文_かな";
                for (int i = 0; i < typed.Length; i++)
                {
                    char character = typed[i];
                    string expected = typed.Substring(0, i + 1);
                    steps.Enqueue(() => Key(first.Username, character));
                    steps.Enqueue(() => Check(marker.userName == expected && first.Username.value == expected,
                        "Key persists across frames " + selected + "/" + expected.Length));
                }
                steps.Enqueue(() =>
                {
                    Check(second.Username.value == typed, "Second Inspector synchronized " + selected);
                    Click(second);
                    second.Username.SelectRange(typed.Length, typed.Length);
                    Key(second.Username, '2');
                });
                steps.Enqueue(() => Check(marker.userName == typed + "2" && first.Username.value == typed + "2",
                    "Second Inspector edit persists " + selected));
            }
            const string beforeUndo = "abc_中文_かな2";
            steps.Enqueue(() =>
            {
                Click(first);
                first.Username.SelectRange(beforeUndo.Length, beforeUndo.Length);
                Undo.IncrementCurrentGroup();
                Key(first.Username, 'Z');
            });
            steps.Enqueue(() => { Check(marker.userName == beforeUndo + "Z", "Undo probe written"); Undo.PerformUndo(); });
            steps.Enqueue(() => { Check(marker.userName == beforeUndo && first.Username.value == beforeUndo, "Undo restores bound value"); Undo.PerformRedo(); });
            steps.Enqueue(() => Check(marker.userName == beforeUndo + "Z" && second.Username.value == beforeUndo + "Z", "Redo synchronizes both Inspectors"));
            steps.Enqueue(() =>
            {
                Click(first);
                string originalClipboard = EditorGUIUtility.systemCopyBuffer;
                try
                {
                    EditorGUIUtility.systemCopyBuffer = "Paste_中文_かな";
                    Command(first.Username, "SelectAll");
                    Command(first.Username, "Paste");
                }
                finally { EditorGUIUtility.systemCopyBuffer = originalClipboard; }
            });
            steps.Enqueue(() => Check(marker.userName == "Paste_中文_かな" && second.Username.value == marker.userName, "Unicode clipboard paste persists"));
            steps.Enqueue(() => first.Bind(marker));
            steps.Enqueue(() =>
            {
                Check(first.Username.value == "Paste_中文_かな", "Recreated Inspector binds existing value");
                Click(first);
                first.Username.SelectRange(marker.userName.Length, marker.userName.Length);
                Key(first.Username, 'R');
            });
            steps.Enqueue(() => Check(marker.userName == "Paste_中文_かなR" && second.Username.value == marker.userName, "Recreated Inspector accepts keys"));
            nextStep = EditorApplication.timeSinceStartup + 5;
            EditorApplication.update += Tick;
            Save();
        }

        private static InspectorInputValidationView Open()
        {
            var view = new InspectorInputValidationView();
            view.style.flexGrow = 1;
            view.style.flexBasis = 0;
            view.style.minWidth = 0;
            host.rootVisualElement.Add(view);
            view.Bind(marker);
            return view;
        }

        private static void Click(InspectorInputValidationView window)
        {
            var input = window.Username.Q("unity-text-input");
            if (input == null) throw new Exception("Text input missing");
            var position = input.worldBound.center;
            var target = input.panel.Pick(position);
            if (target == null || !window.Username.Contains(target)) throw new Exception("Input hit test failed: input="
                + input.worldBound + "; target=" + (target == null ? "null" : target.ToString() + ":" + target.worldBound));
            using (var evt = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, clickCount = 1, mousePosition = position })) target.SendEvent(evt);
            using (var evt = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, clickCount = 1, mousePosition = position })) target.SendEvent(evt);
        }

        private static bool Focused(TextField field)
        {
            var focused = field.focusController.focusedElement as VisualElement;
            return focused == field || (focused != null && field.Contains(focused));
        }

        private static VisualElement Target(TextField field)
        {
            if (!Focused(field)) throw new Exception("Input lost focus between frames");
            return field.panel.visualTree;
        }

        private static void Key(TextField field, char character)
        {
            using (var evt = KeyDownEvent.GetPooled(character, KeyCode.None, EventModifiers.None)) Target(field).SendEvent(evt);
            using (var evt = KeyUpEvent.GetPooled(character, KeyCode.None, EventModifiers.None)) Target(field).SendEvent(evt);
        }

        private static void Command(TextField field, string name)
        {
            using (var evt = ExecuteCommandEvent.GetPooled(name)) Target(field).SendEvent(evt);
        }

        private static void Check(bool valid, string name)
        {
            if (!valid) throw new PendingCondition(name + "; focused=" + (first.Username.focusController.focusedElement == null ? "null" : first.Username.focusController.focusedElement.ToString())
                + "; windowFocused=" + (EditorWindow.focusedWindow == host) + "; appActive=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive);
            if (!result.checks.Contains(name)) { result.checks.Add(name); result.passed++; }
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextStep) return;
            nextStep = EditorApplication.timeSinceStartup + .2;
            try
            {
                if (steps.Count != 0)
                {
                    if (waitingSince == 0) waitingSince = EditorApplication.timeSinceStartup;
                    try { steps.Peek()(); }
                    catch (PendingCondition) { if (EditorApplication.timeSinceStartup - waitingSince < 2) return; throw; }
                    steps.Dequeue();
                    waitingSince = 0;
                    Save();
                    return;
                }
                result.stage = "passed";
            }
            catch (Exception exception) { result.stage = "failed"; result.failure = exception.ToString(); }
            EditorApplication.update -= Tick;
            if (first != null) first.Dispose();
            if (second != null) second.Dispose();
            if (host != null) host.Close();
            if (marker != null) Undo.ClearUndo(marker);
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
            Save();
        }

        private static void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
        }
    }
}
