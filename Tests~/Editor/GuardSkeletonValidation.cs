// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AvatarTools.Obfuscation;
using AvatarTools.Obfuscation.Editor;
using UnityEngine;

namespace AvatarTools.Obfuscation.Validation
{
    public static partial class GuardValidation
    {
        private static GameObject HumanoidFixture()
        {
            var root = Root();
            var bones = new Dictionary<HumanBodyBones, Transform>();
            Action<HumanBodyBones, HumanBodyBones, Vector3> add = (bone, parent, offset) =>
            {
                var t = new GameObject("Rig" + bone).transform;
                t.SetParent(parent == HumanBodyBones.LastBone ? root.transform : bones[parent], false);
                t.localPosition = offset; bones.Add(bone, t);
            };
            add(HumanBodyBones.Hips, HumanBodyBones.LastBone, Vector3.up);
            add(HumanBodyBones.Spine, HumanBodyBones.Hips, Vector3.up * .2f);
            add(HumanBodyBones.Chest, HumanBodyBones.Spine, Vector3.up * .2f);
            add(HumanBodyBones.Neck, HumanBodyBones.Chest, Vector3.up * .2f);
            add(HumanBodyBones.Head, HumanBodyBones.Neck, Vector3.up * .15f);
            foreach (bool left in new[] { true, false })
            {
                float side = left ? -1 : 1;
                var arm = left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
                var elbow = left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
                var hand = left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
                var leg = left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg;
                var knee = left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg;
                var foot = left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;
                add(arm, HumanBodyBones.Chest, new Vector3(side * .2f, .1f, 0));
                add(elbow, arm, Vector3.right * side * .3f);
                add(hand, elbow, Vector3.right * side * .25f);
                add(leg, HumanBodyBones.Hips, new Vector3(side * .1f, -.1f, 0));
                add(knee, leg, Vector3.down * .4f);
                add(foot, knee, new Vector3(0, -.4f, .06f));
            }
            var parentField = typeof(SkeletonBone).GetField("parentName", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var skeleton = root.GetComponentsInChildren<Transform>(true).Select(t =>
            {
                object bone = new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale };
                if (parentField != null) parentField.SetValue(bone, t.parent != null ? t.parent.name : "");
                return (SkeletonBone)bone;
            }).ToArray();
            var description = new HumanDescription
            {
                human = bones.Select(pair => new HumanBone { boneName = pair.Value.name, humanName = HumanTrait.BoneName[(int)pair.Key],
                    limit = new HumanLimit { useDefaultValues = true } }).ToArray(),
                skeleton = skeleton, upperArmTwist = .5f, lowerArmTwist = .5f, upperLegTwist = .5f,
                lowerLegTwist = .5f, armStretch = .05f, legStretch = .05f
            };
            var avatar = Track(AvatarBuilder.BuildHumanAvatar(root, description));
            Check(avatar != null && avatar.isValid && avatar.isHuman, "Synthetic Humanoid is invalid");
            root.AddComponent<Animator>().avatar = avatar;
            Controller(root, Track(new AnimationClip()));
            return root;
        }

        private static bool InvokeHumanoidFallback(GameObject root, HashSet<Transform> protectedPaths, out Dictionary<Transform, string> renames)
        {
            var processor = new GuardProcessor(root, new GuardSettings(root.GetComponent<AvatarBuildObfuscator>()), value => Track(value));
            renames = (Dictionary<Transform, string>)typeof(GuardProcessor).GetField("objectNames", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(processor);
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in all) renames[t] = "Mapped_" + renames.Count;
            Action<Transform> keep = t => protectedPaths.Add(t);
            return (bool)typeof(GuardProcessor).GetMethod("TryPreserveOriginalAvatar", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(processor, new object[] { root.GetComponent<Animator>(), all, keep });
        }

        private static void HumanoidFallback()
        {
            var root = HumanoidFixture(); var animator = root.GetComponent<Animator>(); var avatar = animator.avatar;
            var before = Enumerable.Range(0, (int)HumanBodyBones.LastBone).Select(i => animator.GetBoneTransform((HumanBodyBones)i)).ToArray();
            // Added after Avatar creation, so this decoration is outside its skeleton.
            var accessory = new GameObject("Accessory").transform; accessory.SetParent(root.transform, false);
            var names = root.GetComponentsInChildren<Transform>(true).ToDictionary(t => t, t => t.name);
            var kept = new HashSet<Transform>();
            Check(InvokeHumanoidFallback(root, kept, out var renames), "Original Avatar fallback was rejected");
            Check(kept.Contains(root.transform) && !kept.Contains(accessory) && before.Where(t => t != null).All(kept.Contains),
                "Fallback protected the accessory or missed a rig path");
            Check(names.All(pair => pair.Key.name == pair.Value) && animator.avatar == avatar, "Fallback probe changed its source");
            foreach (var pair in renames) if (!kept.Contains(pair.Key)) pair.Key.name = pair.Value;
            animator.Rebind();
            Check(accessory.name != "Accessory" && animator.avatar == avatar && before.Where(t => t != null).All(t => t.name == names[t]),
                "Original rig or accessory naming was not preserved correctly");
            for (int i = 0; i < before.Length; i++)
                Check(animator.GetBoneTransform((HumanBodyBones)i) == before[i], "Humanoid binding changed after fallback");
        }

        private static void HumanoidFallbackNested()
        {
            var root = HumanoidFixture();
            var child = new GameObject("NestedRig"); child.transform.SetParent(root.transform, false);
            child.AddComponent<Animator>().avatar = root.GetComponent<Animator>().avatar;
            var kept = new HashSet<Transform>();
            Check(!InvokeHumanoidFallback(root, kept, out var renames) && kept.Count == 0 && child.name == "NestedRig",
                "Nested Avatar fallback was accepted without a joint binding proof");
        }
    }
}
