// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

namespace AvatarTools.Obfuscation.Validation
{
    /// <summary>Opt-in observer for actual Play/SDK callbacks, surviving domain reload.</summary>
    [InitializeOnLoad]
    public static class PlayModeValidation
    {
        private const string ActiveKey = "ABO.Validation.Play.Active";
        private const string OutputKey = "ABO.Validation.Play.Output";
        [Serializable] public sealed class Result
        {
            public string stage, startedUtc, packageVersion, packageSource;
            public bool enteredPlay, returnedToEdit;
            public int obfuscatedAvatars, obfuscatedObjects, remainingMarkers, humanoidBones;
            public List<string> transitions = new List<string>();
            public List<string> errors = new List<string>();
        }

        static PlayModeValidation()
        {
            EditorApplication.playModeStateChanged += OnState;
            Application.logMessageReceived += OnLog;
        }

        public static void Arm(string output)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start from Edit Mode");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            SessionState.SetString(OutputKey, output);
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AvatarBuildObfuscator).Assembly);
            Write(new Result { stage = "armed", startedUtc = DateTime.UtcNow.ToString("O"),
                packageVersion = info.version, packageSource = info.source.ToString() });
            SessionState.SetBool(ActiveKey, true);
        }

        public static string Finish()
        {
            var result = Read();
            if (result == null) throw new InvalidOperationException("No Play validation armed");
            result.stage = result.enteredPlay && result.returnedToEdit && result.errors.Count == 0 &&
                result.obfuscatedAvatars > 0 && result.remainingMarkers == 0 ? "passed" : "failed";
            Write(result); SessionState.SetBool(ActiveKey, false);
            return JsonUtility.ToJson(result, true);
        }

        private static Result Read()
        {
            var output = SessionState.GetString(OutputKey, "");
            return File.Exists(output) ? JsonUtility.FromJson<Result>(File.ReadAllText(output)) : null;
        }
        private static void Write(Result value)
        { File.WriteAllText(SessionState.GetString(OutputKey, ""), JsonUtility.ToJson(value, true)); }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (!SessionState.GetBool(ActiveKey, false) ||
                !(type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) return;
            var result = Read(); if (result == null || result.errors.Count >= 30) return;
            result.errors.Add(message + "\n" + stack); Write(result);
        }

        private static void OnState(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            var result = Read(); if (result == null) return;
            result.transitions.Add(state.ToString());
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                result.enteredPlay = true; result.stage = "entered-play";
                foreach (var descriptor in UnityEngine.Object.FindObjectsOfType<VRCAvatarDescriptor>(true))
                {
                    var root = descriptor.gameObject;
                    int generated = root.GetComponentsInChildren<Transform>(true)
                        .Count(t => Editor.UsernameNameGenerator.IsGeneratedName(t.name));
                    if (generated == 0) continue;
                    result.obfuscatedAvatars++; result.obfuscatedObjects += generated;
                    result.remainingMarkers += root.GetComponentsInChildren<AvatarBuildObfuscator>(true).Length;
                    var animator = root.GetComponent<Animator>();
                    if (animator == null || animator.avatar == null || !animator.isHuman) continue;
                    for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                        if (animator.GetBoneTransform((HumanBodyBones)i) != null) result.humanoidBones++;
                }
            }
            if (state == PlayModeStateChange.EnteredEditMode)
            { result.returnedToEdit = true; result.stage = "returned-to-edit"; }
            Write(result);
        }
    }
}
