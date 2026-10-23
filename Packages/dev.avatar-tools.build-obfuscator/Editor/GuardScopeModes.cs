// SPDX-License-Identifier: GPL-3.0-only
// Copyright (c) 2026 Avatar Build Obfuscator contributors

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AvatarTools.Obfuscation.Editor
{
    public sealed partial class GuardSettings
    {
        private Transform scopeAvatar;
        private readonly Dictionary<Transform, ProtectionMode> scopeRules = new Dictionary<Transform, ProtectionMode>();
        private readonly Dictionary<Transform, ProtectionMode> capturedNodeModes = new Dictionary<Transform, ProtectionMode>();
        private readonly Dictionary<Renderer, ProtectionMode> capturedRendererModes = new Dictionary<Renderer, ProtectionMode>();
        private readonly Dictionary<Renderer, Mesh> capturedScopeMeshes = new Dictionary<Renderer, Mesh>();
        private readonly Dictionary<Renderer, ProtectionMode> resolvedRendererModes = new Dictionary<Renderer, ProtectionMode>();
        private readonly List<string> scopeWarnings = new List<string>();

        public bool HasSubtreeOverrides => scopeRules.Count != 0;
        public ProtectionMode GlobalMode => !HasSubtreeOverrides ? Mode :
            capturedNodeModes.Values.Contains(ProtectionMode.Conservative)
                ? ProtectionMode.Conservative : ProtectionMode.Aggressive;

        private void CaptureSubtreeModes(AvatarBuildObfuscator component)
        {
            scopeAvatar = component.transform;
            foreach (var entry in component.subtreeModes ?? new SubtreeModeOverride[0])
            {
                if (entry == null || entry.root == null) continue;
                if (!(entry.root == scopeAvatar || entry.root.IsChildOf(scopeAvatar)) ||
                    entry.mode != ProtectionMode.Conservative && entry.mode != ProtectionMode.Aggressive)
                { scopeWarnings.Add("子树模式：忽略Avatar外的对象或无效模式"); continue; }
                if (scopeRules.ContainsKey(entry.root)) scopeWarnings.Add("子树模式：同一根对象重复配置，使用列表中最后一项");
                scopeRules[entry.root] = entry.mode;
            }
            if (!HasSubtreeOverrides) return;
            foreach (var t in scopeAvatar.GetComponentsInChildren<Transform>(true))
                capturedNodeModes[t] = ResolveAncestorMode(t);
            foreach (var renderer in scopeAvatar.GetComponentsInChildren<Renderer>(true))
            {
                capturedRendererModes[renderer] = capturedNodeModes[renderer.transform];
                capturedScopeMeshes[renderer] = ScopeMesh(renderer);
            }
        }

        private ProtectionMode ResolveAncestorMode(Transform node)
        {
            for (var t = node; t != null && (t == scopeAvatar || t.IsChildOf(scopeAvatar)); t = t.parent)
                if (scopeRules.TryGetValue(t, out var selected)) return selected;
            return Mode;
        }

        private static Mesh ScopeMesh(Renderer renderer) => renderer is SkinnedMeshRenderer skin ? skin.sharedMesh :
            renderer is MeshRenderer ? renderer.GetComponent<MeshFilter>()?.sharedMesh : null;

        // Capture at Resolving and freeze the final policy before creating any copied mesh.
        // A surviving object keeps its original policy even after optimizer reparenting.
        internal void ResolveBuildScopes(GameObject avatar, GuardReport report)
        {
            foreach (var warning in scopeWarnings) report.Note(warning);
            if (!HasSubtreeOverrides) return;
            if (avatar.transform != scopeAvatar)
                throw new System.InvalidOperationException("Subtree modes must be captured from the supplied build copy");
            report.SubtreeOverrides = scopeRules.Count;
            bool lostConservativeRenderer = capturedRendererModes.Any(p => p.Value == ProtectionMode.Conservative && p.Key == null);
            foreach (var renderer in avatar.GetComponentsInChildren<Renderer>(true))
            {
                bool known = capturedRendererModes.TryGetValue(renderer, out var selected);
                if (!known) selected = ModeFor(renderer.transform);
                // Generic optimizer registries do not expose all many-to-one contributors.
                // Never apply an aggressive policy to a changed/new mesh that may contain
                // geometry from a removed conservative renderer.
                bool uncertain = lostConservativeRenderer && (!known || capturedScopeMeshes[renderer] != ScopeMesh(renderer));
                if (uncertain && selected == ProtectionMode.Aggressive)
                { selected = ProtectionMode.Conservative; report.ScopeFallbackRenderers++; }
                resolvedRendererModes[renderer] = selected;
                if (selected == ProtectionMode.Conservative) report.ConservativeScopeRenderers++;
                else report.AggressiveScopeRenderers++;
            }
            report.Note("子树模式：最近的已配置祖先生效；共享网格/材质和全局参数、控制器采用保守优先规则");
            if (report.ScopeFallbackRenderers != 0)
                report.Note("子树模式：优化器移除保守Renderer且改变合并来源，相关新/变更网格回退保守：" + report.ScopeFallbackRenderers);
        }

        public ProtectionMode ModeFor(Transform node)
        {
            if (!HasSubtreeOverrides || node == null) return Mode;
            if (capturedNodeModes.TryGetValue(node, out var selected)) return selected;
            // Newly generated objects can inherit an explicit surviving override. Otherwise
            // the conservative global rule handles provenance that is no longer recoverable.
            for (var t = node; t != null && (t == scopeAvatar || t.IsChildOf(scopeAvatar)); t = t.parent)
                if (scopeRules.TryGetValue(t, out selected)) return selected;
            return GlobalMode;
        }

        public ProtectionMode ModeFor(Renderer renderer)
        {
            if (!HasSubtreeOverrides || renderer == null) return Mode;
            return resolvedRendererModes.TryGetValue(renderer, out var selected) ? selected : ModeFor(renderer.transform);
        }
    }
}
