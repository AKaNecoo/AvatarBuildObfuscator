// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        // VRChat supplies these by their exact protocol names. Custom aliases used by
        // the SDK fallback controllers are also contractual rather than user labels.
        private static readonly HashSet<string> ReservedParameters = new HashSet<string>(StringComparer.Ordinal)
        {
            "IsLocal", "PreviewMode", "Viseme", "Voice", "GestureLeft", "GestureRight",
            "GestureLeftWeight", "GestureRightWeight", "AngularY", "VelocityX", "VelocityY", "VelocityZ",
            "VelocityMagnitude", "Upright", "Grounded", "Seated", "AFK", "TrackingType", "VRMode",
            "MuteSelf", "InStation", "Earmuffs", "IsOnFriendsList", "AvatarVersion", "IsAnimatorEnabled",
            "ScaleModified", "ScaleFactor", "ScaleFactorInverse", "EyeHeightAsMeters", "EyeHeightAsPercent",
            "VRCEmote", "VRCFaceBlendH", "VRCFaceBlendV"
        };
        private readonly Dictionary<string, string> parameterNames = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<Object> namedAnimatorObjects = new HashSet<Object>();

        private static IEnumerable<AnimatorController> BaseControllers(RuntimeAnimatorController controller)
        {
            var visited = new HashSet<RuntimeAnimatorController>();
            while (controller != null && visited.Add(controller))
            {
                if (controller is AnimatorController ac) { yield return ac; yield break; }
                controller = (controller as AnimatorOverrideController)?.runtimeAnimatorController;
            }
        }

        private bool KeepParameter(string value)
        {
            if (string.IsNullOrEmpty(value) || ReservedParameters.Contains(value)) return true;
            foreach (var pattern in settings.PreserveParameters)
            {
                if (string.IsNullOrEmpty(pattern)) continue;
                if (pattern.EndsWith("*", StringComparison.Ordinal)
                    ? value.StartsWith(pattern.Substring(0, pattern.Length - 1), StringComparison.Ordinal)
                    : value == pattern) return true;
            }
            // These are published OSC/face-tracking interfaces. Renaming them without
            // changing the external sender is a known functional break in either mode.
            if (value.StartsWith("FT/v2/", StringComparison.Ordinal) || value.StartsWith("Drive/FT/", StringComparison.Ordinal) ||
                value.StartsWith("v2/", StringComparison.Ordinal) || value.StartsWith("VRCFaceTracking/", StringComparison.Ordinal)) return true;
            return settings.GlobalMode == ProtectionMode.Conservative && value.StartsWith("FT/", StringComparison.Ordinal);
        }

        private void PlanParameterNames()
        {
            if (!settings.RenameParameters) return;
            if (unknownDriver && settings.GlobalMode == ProtectionMode.Conservative)
            { report.Note("未知驱动：保守模式保留运行参数与动画内部名称"); return; }
            var all = new HashSet<string>(controllers.SelectMany(BaseControllers).SelectMany(c => c.parameters).Select(p => p.name), StringComparer.Ordinal);
            foreach (var d in root.GetComponentsInChildren<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true))
                if (d.expressionParameters != null)
                    foreach (var p in d.expressionParameters.parameters ?? new VRCExpressionParameters.Parameter[0]) all.Add(p.name);
            var prefixes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                var type = component.GetType().FullName ?? "";
                if (!type.StartsWith("VRC.SDK3.Dynamics.", StringComparison.Ordinal)) continue;
                using (var so = new SerializedObject(component))
                {
                    var p = so.FindProperty("parameter");
                    if (p == null || p.propertyType != SerializedPropertyType.String || string.IsNullOrEmpty(p.stringValue)) continue;
                    all.Add(p.stringValue);
                    if (type.Contains("PhysBone")) prefixes.Add(p.stringValue);
                }
            }
            foreach (var value in all) if (value != null) usedNames.Add(value);
            foreach (var prefix in prefixes.OrderByDescending(x => x.Length).ThenBy(x => x, StringComparer.Ordinal))
            {
                if (KeepParameter(prefix) || all.Any(x => x != null && x.StartsWith(prefix + "_", StringComparison.Ordinal) && KeepParameter(x))) continue;
                parameterNames[prefix] = NewName();
            }
            foreach (var name in all.Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x, StringComparer.Ordinal))
            {
                if (KeepParameter(name) || parameterNames.ContainsKey(name)) continue;
                var prefix = prefixes.OrderByDescending(x => x.Length).FirstOrDefault(x => name.StartsWith(x + "_", StringComparison.Ordinal));
                if (prefix != null)
                {
                    if (parameterNames.TryGetValue(prefix, out var mapped)) parameterNames[name] = mapped + name.Substring(prefix.Length);
                    continue;
                }
                parameterNames[name] = NewName();
            }
            report.RenamedParameters = parameterNames.Count;
            if (all.Any(KeepParameter)) report.Note("VRChat 内置参数、明确的面捕/OSC 接口及手动保留参数维持契约名称");
        }

        private string MapParameter(string value)
        { return value != null && parameterNames.TryGetValue(value, out var result) ? result : value; }

        private string MapSerializedString(Object owner, SerializedProperty p, Transform relativeRoot)
        {
            var value = p.stringValue;
            var type = owner.GetType().FullName ?? "";
            if (type.StartsWith("VRC.", StringComparison.Ordinal) && p.name == "SourcePath") return MapPath(value, relativeRoot);
            if (owner is VRCExpressionParameters && p.name == "name" && p.propertyPath.StartsWith("parameters.", StringComparison.Ordinal))
                return MapParameter(value);
            if (owner is VRCExpressionsMenu && p.name == "name" &&
                (p.propertyPath.Contains(".parameter.") || p.propertyPath.Contains(".subParameters."))) return MapParameter(value);
            if (type.Contains("AvatarParameterDriver") && (p.name == "name" || p.name == "source") &&
                p.propertyPath.StartsWith("parameters.", StringComparison.Ordinal)) return MapParameter(value);
            if (type.StartsWith("VRC.SDK3.Dynamics.", StringComparison.Ordinal) && p.name == "parameter") return MapParameter(value);
            // Unknown custom drivers are permitted in Aggressive; update explicit parameter
            // fields, but never indiscriminately replace all user-facing strings.
            if (settings.GlobalMode == ProtectionMode.Aggressive && (p.name == "parameter" || p.name == "parameterName")) return MapParameter(value);
            return value;
        }

        private void RewriteAnimatorNames(Object copy)
        {
            if (!namedAnimatorObjects.Add(copy)) return;
            bool graphNames = settings.RenameAnimatorInternals && (!unknownDriver || settings.GlobalMode == ProtectionMode.Aggressive);
            if (copy is AnimatorController controller)
            {
                var parameters = controller.parameters;
                foreach (var p in parameters) p.name = MapParameter(p.name);
                controller.parameters = parameters;
                if (graphNames)
                {
                    var layers = controller.layers;
                    foreach (var layer in layers) { layer.name = NewName(); report.RenamedAnimatorNames++; }
                    controller.layers = layers;
                }
            }
            if (copy is AnimatorState state)
            {
                state.speedParameter = MapParameter(state.speedParameter);
                state.cycleOffsetParameter = MapParameter(state.cycleOffsetParameter);
                state.mirrorParameter = MapParameter(state.mirrorParameter);
                state.timeParameter = MapParameter(state.timeParameter);
            }
            if (copy is AnimatorTransitionBase transition)
            {
                var conditions = transition.conditions;
                for (int i = 0; i < conditions.Length; i++) conditions[i].parameter = MapParameter(conditions[i].parameter);
                transition.conditions = conditions;
            }
            if (copy is BlendTree tree)
            {
                tree.blendParameter = MapParameter(tree.blendParameter);
                tree.blendParameterY = MapParameter(tree.blendParameterY);
                var children = tree.children;
                for (int i = 0; i < children.Length; i++) children[i].directBlendParameter = MapParameter(children[i].directBlendParameter);
                tree.children = children;
            }
            if (graphNames && (copy is AnimatorState || copy is AnimatorStateMachine || copy is AnimatorTransitionBase))
            { copy.name = NewName(); report.RenamedAnimatorNames++; }
        }
    }
}
