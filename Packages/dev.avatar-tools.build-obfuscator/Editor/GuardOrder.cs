using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardProcessor
    {
        private readonly Dictionary<Transform, Transform[]> siblingOrders = new Dictionary<Transform, Transform[]>();

        // Consume the same username-derived stream as naming; no runtime decoder or
        // permutation table is needed. Avoid an unchanged permutation for small sets.
        private int[] ShuffledIndices(int count)
        {
            var order = Enumerable.Range(0, count).ToArray();
            if (count < 2) return order;
            var random = new System.Random(names.NextSeed());
            for (int i = count - 1; i > 0; i--)
            { int j = random.Next(i + 1); int value = order[i]; order[i] = order[j]; order[j] = value; }
            if (order.Select((value, index) => value == index).All(x => x))
            { int value = order[0]; order[0] = order[1]; order[1] = value; }
            return order;
        }

        private void PlanSiblingOrder()
        {
            if (unknownDriver && settings.Mode == ProtectionMode.Conservative)
            { report.Note("同级重排：保守模式保留未知驱动的对象顺序"); return; }
            var blocked = new HashSet<Transform>();
            Action<Transform> blockTree = t => {
                if (t == null || !(t == root.transform || t.IsChildOf(root.transform))) return;
                foreach (var descendant in t.GetComponentsInChildren<Transform>(true)) blocked.Add(descendant);
            };
            foreach (var manual in settings.PreserveObjects)
            {
                blockTree(manual);
                // Also retain its index at each ancestor. A manual exclusion must be
                // useful to integrations that use both names and numeric GetChild.
                for (var t = manual; t != null && t != root.transform && t.IsChildOf(root.transform); t = t.parent)
                    if (t.parent != null) blocked.Add(t.parent);
            }
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                if (animator.avatar != null && (!animator.avatar.isValid || !animator.avatar.isHuman))
                { blockTree(animator.transform); report.Note("同级重排：Generic/无效 Avatar 层级保持顺序"); }
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.childCount < 2 || blocked.Contains(t)) continue;
                var children = Enumerable.Range(0, t.childCount).Select(t.GetChild).ToArray();
                // Transform.Find and name-based animation resolution can pick the
                // first duplicate sibling. RectTransform/Canvas ordering controls UI.
                if (children.GroupBy(c => c.name, StringComparer.Ordinal).Any(g => g.Count() > 1))
                { report.Note("同级重排：重复同名对象组保持顺序"); continue; }
                if (t is RectTransform || t.GetComponentInParent<Canvas>(true) != null ||
                    children.Any(c => c is RectTransform || c.GetComponent<Canvas>() != null))
                { report.Note("同级重排：UI 布局和绘制顺序保留"); continue; }
                siblingOrders[t] = ShuffledIndices(children.Length).Select(i => children[i]).ToArray();
            }
        }

        private void ApplySiblingOrder()
        {
            // Rebinding a Human Avatar must still resolve to the exact same Transform
            // objects after sibling traversal changes. Roll back if Unity disagrees.
            var expected = new Dictionary<Animator, Transform[]>();
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                if (animator.avatar != null && animator.avatar.isValid && animator.avatar.isHuman)
                    expected[animator] = Enumerable.Range(0, (int)HumanBodyBones.LastBone)
                        .Select(i => animator.GetBoneTransform((HumanBodyBones)i)).ToArray();
            var original = siblingOrders.ToDictionary(p => p.Key,
                p => Enumerable.Range(0, p.Key.childCount).Select(p.Key.GetChild).ToArray());
            foreach (var pair in siblingOrders)
                for (int i = 0; i < pair.Value.Length; i++) pair.Value[i].SetSiblingIndex(i);
            bool valid = true;
            foreach (var pair in expected)
            {
                var avatar = pair.Key.avatar; pair.Key.avatar = null; pair.Key.avatar = avatar;
                for (int i = 0; i < pair.Value.Length; i++)
                    if (pair.Key.GetBoneTransform((HumanBodyBones)i) != pair.Value[i]) valid = false;
            }
            if (!valid)
            {
                foreach (var pair in original)
                    for (int i = 0; i < pair.Value.Length; i++) pair.Value[i].SetSiblingIndex(i);
                foreach (var animator in expected.Keys)
                { var avatar = animator.avatar; animator.avatar = null; animator.avatar = avatar; }
                report.Note("同级重排：Humanoid 骨骼映射预检不一致，已恢复全部同级顺序");
                return;
            }
            report.ShuffledSiblingGroups = siblingOrders.Count;
            foreach (var pair in original)
                report.ReorderedGameObjects += pair.Value.Select((child, index) => child.GetSiblingIndex() != index).Count(x => x);
        }
    }
}
