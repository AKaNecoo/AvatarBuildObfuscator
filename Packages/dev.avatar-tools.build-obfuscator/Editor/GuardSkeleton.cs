// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        private readonly Dictionary<Animator, Avatar> rebuiltAvatars = new Dictionary<Animator, Avatar>();

        private void PrepareRebuiltAvatars(Transform[] all, Action<Transform> protectTree)
        {
            if (!settings.RenameBones || objectNames.Count == 0) return;
            // A separate hierarchy preflight lets us reject an unsupported skeleton
            // before rewriting any real build-copy paths. No source Avatar is modified.
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
            {
                var sourceAvatar = animator.avatar;
                if (sourceAvatar == null || !sourceAvatar.isValid || !sourceAvatar.isHuman) continue;
                var descendants = animator.GetComponentsInChildren<Transform>(true);
                var renames = descendants.Where(objectNames.ContainsKey).GroupBy(t => t.name)
                    .Where(g => g.Count() == 1).ToDictionary(g => g.Key, g => objectNames[g.First()], StringComparer.Ordinal);
                var description = sourceAvatar.humanDescription;
                if (!description.skeleton.Any(x => renames.ContainsKey(x.name)) && !description.human.Any(x => renames.ContainsKey(x.boneName))) continue;
                var human = (HumanBone[])description.human.Clone();
                var skeleton = (SkeletonBone[])description.skeleton.Clone();
                // Unity serializes a private parentName as well as the public name.
                // Renaming only name produces an invalid HumanDescription in 2022.3.
                var parentField = typeof(SkeletonBone).GetField("parentName", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (parentField == null)
                { protectTree(animator.transform); report.Note("当前 Unity 未暴露可重映射的 SkeletonBone.parentName，保留该 Avatar 层级"); continue; }
                if (skeleton.Length > 0 && !human.Any(b => b.boneName == skeleton[0].name))
                    renames[skeleton[0].name] = objectNames.TryGetValue(animator.transform, out var rootName) ? rootName : animator.name;
                for (int i = 0; i < human.Length; i++)
                    if (renames.TryGetValue(human[i].boneName, out var name)) human[i].boneName = name;
                for (int i = 0; i < skeleton.Length; i++)
                {
                    if (renames.TryGetValue(skeleton[i].name, out var name)) skeleton[i].name = name;
                    object boxed = skeleton[i];
                    var parent = parentField.GetValue(boxed) as string;
                    if (parent != null && renames.TryGetValue(parent, out var parentName)) parentField.SetValue(boxed, parentName);
                    skeleton[i] = (SkeletonBone)boxed;
                }
                description.human = human; description.skeleton = skeleton;
                GameObject probe = null; Avatar avatar = null;
                try
                {
                    probe = Object.Instantiate(root); probe.name = root.name; probe.hideFlags = HideFlags.HideAndDontSave;
                    var copiedTransforms = probe.GetComponentsInChildren<Transform>(true);
                    if (copiedTransforms.Length != all.Length) throw new InvalidOperationException("骨架预检层级不一致");
                    var index = Array.IndexOf(all, animator.transform);
                    var probeAnimator = copiedTransforms[index].GetComponent<Animator>();
                    var expected = new Dictionary<HumanBodyBones, Transform>();
                    for (int i = 0; i < (int)HumanBodyBones.LastBone; i++)
                    {
                        var bone = probeAnimator.GetBoneTransform((HumanBodyBones)i);
                        if (bone != null) expected[(HumanBodyBones)i] = bone;
                    }
                    for (int i = 0; i < all.Length; i++)
                        if (objectNames.TryGetValue(all[i], out var name)) copiedTransforms[i].name = name;
                    avatar = AvatarBuilder.BuildHumanAvatar(probeAnimator.gameObject, description);
                    if (avatar == null || !avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("重建 Avatar 无效");
                    probeAnimator.avatar = avatar;
                    foreach (var pair in expected)
                        if (probeAnimator.GetBoneTransform(pair.Key) != pair.Value) throw new InvalidOperationException("Humanoid 骨骼映射变化：" + pair.Key);
                    if (Mathf.Abs(avatar.humanDescription.armStretch - sourceAvatar.humanDescription.armStretch) > .00001f ||
                        Mathf.Abs(probeAnimator.humanScale - animator.humanScale) > .0001f)
                        throw new InvalidOperationException("Humanoid 参数或尺度变化");
                    avatar.name = settings.RenameAssets ? NewName() : sourceAvatar.name;
                    rebuiltAvatars[animator] = avatar; Own(avatar); report.RebuiltAvatars++;
                    if (settings.RenameAssets) report.RenamedAssets++;
                    avatar = null;
                }
                catch (Exception e)
                {
                    protectTree(animator.transform);
                    report.Note("Humanoid 重建未通过预检，保留对应层级：" + e.Message);
                }
                finally
                {
                    if (probe != null) Object.DestroyImmediate(probe);
                    if (avatar != null) Object.DestroyImmediate(avatar);
                }
            }
        }

        private void ApplyRebuiltAvatars()
        {
            foreach (var pair in rebuiltAvatars) pair.Key.avatar = pair.Value;
        }
    }
}
