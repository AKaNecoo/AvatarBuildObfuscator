using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using Object = UnityEngine.Object;

namespace Kiine.AvatarGuard.Editor
{
    public sealed class GuardSettings
    {
        public ProtectionMode Mode;
        public string Prefix;
        public bool RenameAssets, FreezeShapes, RenameShapes;
        public HashSet<string> PreserveShapes;
        public HashSet<SkinnedMeshRenderer> Exclusions;

        public GuardSettings(AvatarGuard component)
        {
            Mode = component.mode;
            Prefix = SanitizePrefix(component.namePrefix);
            RenameAssets = component.renameAssets;
            FreezeShapes = component.freezeUnusedBlendShapes;
            RenameShapes = component.renameDynamicBlendShapes;
            PreserveShapes = new HashSet<string>(component.preserveBlendShapes ?? new string[0], StringComparer.Ordinal);
            Exclusions = new HashSet<SkinnedMeshRenderer>(component.excludedRenderers ?? new SkinnedMeshRenderer[0]);
        }

        public static string SanitizePrefix(string input)
        {
            var result = new StringBuilder();
            foreach (var c in (input ?? "").Trim())
                result.Append(char.IsControl(c) || "/\\:*?\"<>|".IndexOf(c) >= 0 ? '_' : c);
            return result.Length == 0 ? "一个木禾" : result.ToString();
        }
    }

    [Serializable]
    public sealed class GuardReport
    {
        public int ShapesBefore, ShapesAfter, FrozenShapes, RenamedShapes, RenamedAssets, CopiedObjects;
        public List<string> Notes = new List<string>();
        public string Summary => "形变 " + ShapesBefore + " → " + ShapesAfter +
            "，烘焙/移除 " + FrozenShapes + "，形变改名 " + RenamedShapes +
            "，资源改名 " + RenamedAssets + "，复制对象 " + CopiedObjects;
        public void Note(string text) { if (!Notes.Contains(text)) Notes.Add(text); }
    }

    /// <summary>
    /// Mutates only the supplied build copy and newly instantiated assets. Never edits source
    /// importers, source assets, scene files or prefab files. Animation graphs are deep-copied
    /// through serialized references, including synced-layer overrides and nested BlendTrees.
    /// </summary>
    public sealed class GuardProcessor
    {
        private readonly GameObject root;
        private readonly GuardSettings settings;
        private readonly Action<Object> saveAsset;
        private readonly GuardReport report = new GuardReport();
        private readonly HashSet<Object> owned = new HashSet<Object>();
        private readonly Dictionary<Object, Object> sharedAssets = new Dictionary<Object, Object>();
        private readonly Dictionary<Transform, Dictionary<Object, Object>> animationAssets =
            new Dictionary<Transform, Dictionary<Object, Object>>();
        private readonly Dictionary<SkinnedMeshRenderer, Dictionary<string, string>> shapeRenames =
            new Dictionary<SkinnedMeshRenderer, Dictionary<string, string>>();
        private readonly List<ClipSite> clips = new List<ClipSite>();
        private readonly HashSet<SkinnedMeshRenderer> meshSwaps = new HashSet<SkinnedMeshRenderer>();
        private readonly Dictionary<SkinnedMeshRenderer, HashSet<string>> animated =
            new Dictionary<SkinnedMeshRenderer, HashSet<string>>();
        private readonly Dictionary<SkinnedMeshRenderer, HashSet<string>> nativeShapes =
            new Dictionary<SkinnedMeshRenderer, HashSet<string>>();
        private bool unknownDriver;

        private sealed class ClipSite
        {
            public AnimationClip Clip;
            public Transform Root;
        }

        public GuardProcessor(GameObject buildCopy, GuardSettings configuration, Action<Object> saver = null)
        {
            root = buildCopy != null ? buildCopy : throw new ArgumentNullException(nameof(buildCopy));
            settings = configuration ?? throw new ArgumentNullException(nameof(configuration));
            saveAsset = saver;
        }

        public GuardReport Run()
        {
            if (PrefabUtility.IsPartOfPrefabAsset(root))
                throw new InvalidOperationException("Avatar Guard accepts a build copy, not a prefab asset.");
            CollectUsage();
            if (settings.FreezeShapes || settings.RenameShapes) ProcessShapes();
            else foreach (var r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (r.sharedMesh != null) report.ShapesBefore += r.sharedMesh.blendShapeCount;
            if (!settings.FreezeShapes && !settings.RenameShapes) report.ShapesAfter = report.ShapesBefore;
            RewriteComponentAssets();
            return report;
        }

        private void CollectUsage()
        {
            var sites = new HashSet<string>();
            Action<RuntimeAnimatorController, Transform> addController = (controller, relativeRoot) =>
            {
                if (controller == null) return;
                foreach (var clip in controller.animationClips)
                    AddClip(clip, relativeRoot, sites);
                foreach (var behaviour in FindBehaviours(controller))
                    if (behaviour != null && !IsKnownNamespace(behaviour.GetType())) unknownDriver = true;
            };
            var descriptor = root.GetComponent<VRCAvatarDescriptor>();
            if (descriptor != null)
            {
                foreach (var layer in (descriptor.baseAnimationLayers ?? new VRCAvatarDescriptor.CustomAnimLayer[0])
                    .Concat(descriptor.specialAnimationLayers ?? new VRCAvatarDescriptor.CustomAnimLayer[0]))
                    addController(layer.animatorController, root.transform);
                var face = descriptor.VisemeSkinnedMesh;
                foreach (var name in descriptor.VisemeBlendShapes ?? new string[0]) Mark(nativeShapes, face, name);
                Mark(nativeShapes, face, descriptor.MouthOpenBlendShapeName);
                var eye = descriptor.customEyeLookSettings;
                if (eye.eyelidsSkinnedMesh != null && eye.eyelidsSkinnedMesh.sharedMesh != null)
                    foreach (var index in eye.eyelidsBlendshapes ?? new int[0])
                        if (index >= 0 && index < eye.eyelidsSkinnedMesh.sharedMesh.blendShapeCount)
                            Mark(nativeShapes, eye.eyelidsSkinnedMesh,
                                eye.eyelidsSkinnedMesh.sharedMesh.GetBlendShapeName(index));
            }
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                addController(animator.runtimeAnimatorController, animator.transform);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) { unknownDriver = true; report.Note("发现 Missing Script，形变数据保留"); continue; }
                if (component is Animation) { unknownDriver = true; report.Note("发现 Legacy Animation，形变数据保留"); }
                if (component is MonoBehaviour && !(component is AvatarGuard) && !IsKnownNamespace(component.GetType()))
                {
                    unknownDriver = true;
                    report.Note("未知组件 " + component.GetType().FullName + "，形变数据保留");
                }
                // Direct clip references in SDK components also count as potential animation drivers.
                if (component is Transform) continue;
                using (var serialized = new SerializedObject(component))
                {
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (property.objectReferenceValue is AnimationClip clip)
                                AddClip(clip, component is Animator a ? a.transform : root.transform, sites);
                            else if (property.objectReferenceValue is RuntimeAnimatorController controller)
                                addController(controller, component is Animator a2 ? a2.transform : root.transform);
                        }
                }
            }
            if (unknownDriver) report.Note("无法完整确认形变驱动；两种模式都跳过形变烘焙与改名，资源名称仍处理");
            foreach (var site in clips)
            {
                foreach (var binding in AnimationUtility.GetCurveBindings(site.Clip))
                {
                    if (!binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)) continue;
                    foreach (var renderer in ResolveRenderers(site.Root, binding.path))
                        Mark(animated, renderer, binding.propertyName.Substring(11));
                }
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(site.Clip))
                {
                    var frames = AnimationUtility.GetObjectReferenceCurve(site.Clip, binding);
                    if (binding.propertyName == "m_Mesh" || frames.Any(f => f.value is Mesh))
                        foreach (var renderer in ResolveRenderers(site.Root, binding.path)) meshSwaps.Add(renderer);
                }
            }
        }

        private void AddClip(AnimationClip clip, Transform relativeRoot, HashSet<string> seen)
        {
            if (clip == null) return;
            if (!seen.Add(clip.GetInstanceID() + ":" + relativeRoot.GetInstanceID())) return;
            clips.Add(new ClipSite { Clip = clip, Root = relativeRoot });
            AddClip(AnimationUtility.GetAnimationClipSettings(clip).additiveReferencePoseClip, relativeRoot, seen);
        }

        private static bool IsKnownNamespace(Type type)
        {
            var ns = type.Namespace ?? "";
            return ns.StartsWith("VRC.", StringComparison.Ordinal) ||
                ns.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                ns.StartsWith("nadena.dev.ndmf", StringComparison.Ordinal);
        }

        private static IEnumerable<StateMachineBehaviour> FindBehaviours(RuntimeAnimatorController controller)
        {
            var visited = new HashSet<Object>();
            var pending = new Stack<Object>(); pending.Push(controller);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current == null || !visited.Add(current)) continue;
                if (current is StateMachineBehaviour behaviour) yield return behaviour;
                if (!(current is RuntimeAnimatorController || current is AnimatorStateMachine ||
                    current is AnimatorState || current is StateMachineBehaviour)) continue;
                using (var so = new SerializedObject(current))
                {
                    var p = so.GetIterator();
                    while (p.Next(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue != null)
                            pending.Push(p.objectReferenceValue);
                }
            }
        }

        private static void Mark(Dictionary<SkinnedMeshRenderer, HashSet<string>> map,
            SkinnedMeshRenderer renderer, string name)
        {
            if (renderer == null || string.IsNullOrEmpty(name)) return;
            if (!map.TryGetValue(renderer, out var values)) map[renderer] = values = new HashSet<string>(StringComparer.Ordinal);
            values.Add(name);
        }

        // Enumerate instead of Transform.Find so ambiguous duplicate paths are kept safe.
        private static IEnumerable<SkinnedMeshRenderer> ResolveRenderers(Transform relativeRoot, string path)
        {
            return relativeRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => AnimationUtility.CalculateTransformPath(r.transform, relativeRoot) == path);
        }

        private static bool CompatibilityShape(string name)
        {
            if (name.StartsWith("mmd", StringComparison.OrdinalIgnoreCase)) return true;
            // Protect Japanese/Chinese world-driven names, including less common MMD variants.
            if (name.Any(c => c >= '\u3040' && c <= '\u9fff' || c >= '\uff66' && c <= '\uff9f')) return true;
            switch (name.ToLowerInvariant())
            {
                case "blink": case "blink_l": case "blink_r": case "wink": case "wink_l": case "wink_r":
                case "a": case "i": case "u": case "e": case "o": case "smile": case "angry": case "sad":
                    return true;
                default: return false;
            }
        }

        private void ProcessShapes()
        {
            foreach (var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var source = renderer.sharedMesh;
                if (source == null) continue;
                report.ShapesBefore += source.blendShapeCount;
                if (source.blendShapeCount == 0) continue;
                if (unknownDriver || settings.Exclusions.Contains(renderer) || meshSwaps.Contains(renderer) ||
                    ResolveRenderers(root.transform, AnimationUtility.CalculateTransformPath(renderer.transform, root.transform)).Count() != 1)
                {
                    report.ShapesAfter += source.blendShapeCount;
                    report.Note(renderer.name + "：未知驱动、手动排除、网格替换或重复路径，形变保留");
                    continue;
                }
                ProcessRenderer(renderer, source);
            }
        }

        private void ProcessRenderer(SkinnedMeshRenderer renderer, Mesh source)
        {
            Mesh copy = null;
            try
            {
                int count = source.blendShapeCount;
                var names = Enumerable.Range(0, count).Select(source.GetBlendShapeName).ToArray();
                if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
                    throw new InvalidOperationException("重复形变名称");
                var weights = Enumerable.Range(0, count).Select(renderer.GetBlendShapeWeight).ToArray();
                var freeze = new bool[count];
                var outputNames = (string[])names.Clone();
                var renameMap = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 0; i < count; i++)
                {
                    var name = names[i];
                    bool preserve = settings.PreserveShapes.Contains(name) ||
                        settings.Mode == ProtectionMode.Conservative && CompatibilityShape(name);
                    bool driven = animated.TryGetValue(renderer, out var used) && used.Contains(name) ||
                        nativeShapes.TryGetValue(renderer, out var native) && native.Contains(name);
                    // Deliberately skip unusual in-between/extrapolation setups, even in Aggressive mode.
                    bool ordinary = source.GetBlendShapeFrameCount(i) == 1 &&
                        Mathf.Approximately(source.GetBlendShapeFrameWeight(i, 0), 100f) &&
                        weights[i] >= 0f && weights[i] <= 100f && !float.IsNaN(weights[i]);
                    freeze[i] = settings.FreezeShapes && !preserve && !driven && ordinary;
                    if (!driven && !preserve && !ordinary)
                        report.Note(renderer.name + "/" + name + "：特殊帧或权重，形变数据保留");
                    if (!freeze[i] && !preserve && settings.RenameShapes)
                    {
                        outputNames[i] = NewName();
                        renameMap.Add(name, outputNames[i]);
                    }
                }
                if (!freeze.Any(x => x) && renameMap.Count == 0)
                { report.ShapesAfter += count; return; }
                copy = Object.Instantiate(source);
                copy.name = settings.RenameAssets ? NewName() : source.name;
                var vertices = source.vertices;
                var normals = source.normals;
                var tangents = source.tangents;
                var dv = new Vector3[source.vertexCount];
                var dn = new Vector3[source.vertexCount];
                var dt = new Vector3[source.vertexCount];
                copy.ClearBlendShapes();
                for (int i = 0; i < count; i++)
                {
                    for (int frame = 0; frame < source.GetBlendShapeFrameCount(i); frame++)
                    {
                        source.GetBlendShapeFrameVertices(i, frame, dv, dn, dt);
                        if (freeze[i])
                        {
                            float factor = weights[i] / 100f;
                            for (int v = 0; v < vertices.Length; v++)
                            {
                                vertices[v] += dv[v] * factor;
                                if (normals.Length == vertices.Length) normals[v] += dn[v] * factor;
                                if (tangents.Length == vertices.Length)
                                {
                                    var tangent = tangents[v];
                                    tangent.x += dt[v].x * factor; tangent.y += dt[v].y * factor; tangent.z += dt[v].z * factor;
                                    tangents[v] = tangent;
                                }
                            }
                        }
                        else copy.AddBlendShapeFrame(outputNames[i], source.GetBlendShapeFrameWeight(i, frame), dv, dn, dt);
                    }
                }
                if (vertices.Any(v => !Finite(v))) throw new InvalidOperationException("非有限顶点数据");
                copy.vertices = vertices;
                if (normals.Length == vertices.Length) copy.normals = normals;
                if (tangents.Length == vertices.Length) copy.tangents = tangents;
                // Keep author-provided bounds; tightening them can cause animation culling.
                copy.bounds = source.bounds;
                Own(copy);
                if (settings.RenameAssets) report.RenamedAssets++;
                renderer.sharedMesh = copy;
                int nextIndex = 0;
                var newIndices = new int[count];
                for (int i = 0; i < count; i++)
                {
                    newIndices[i] = freeze[i] ? -1 : nextIndex;
                    if (!freeze[i]) renderer.SetBlendShapeWeight(nextIndex++, weights[i]);
                }
                shapeRenames[renderer] = renameMap;
                UpdateDescriptor(renderer, renameMap, newIndices);
                report.FrozenShapes += freeze.Count(x => x);
                report.RenamedShapes += renameMap.Count;
                report.ShapesAfter += nextIndex;
            }
            catch (Exception exception)
            {
                // No renderer assignment has happened for data-read failures.
                if (copy != null && !owned.Contains(copy)) Object.DestroyImmediate(copy);
                if (renderer.sharedMesh != source) throw;
                report.ShapesAfter += source.blendShapeCount;
                report.Note(renderer.name + "：" + exception.GetType().Name + "，保留原形变（" + exception.Message + "）");
            }
        }

        private static bool Finite(Vector3 value)
        {
            return !(float.IsNaN(value.x) || float.IsInfinity(value.x) || float.IsNaN(value.y) ||
                float.IsInfinity(value.y) || float.IsNaN(value.z) || float.IsInfinity(value.z));
        }

        private void UpdateDescriptor(SkinnedMeshRenderer renderer, Dictionary<string, string> names, int[] indices)
        {
            var descriptor = root.GetComponent<VRCAvatarDescriptor>();
            if (descriptor == null) return;
            if (descriptor.VisemeSkinnedMesh == renderer)
            {
                var visemes = (string[])(descriptor.VisemeBlendShapes ?? new string[0]).Clone();
                for (int i = 0; i < visemes.Length; i++)
                    if (visemes[i] != null && names.TryGetValue(visemes[i], out var value)) visemes[i] = value;
                descriptor.VisemeBlendShapes = visemes;
                if (descriptor.MouthOpenBlendShapeName != null && names.TryGetValue(descriptor.MouthOpenBlendShapeName, out var mouth))
                    descriptor.MouthOpenBlendShapeName = mouth;
            }
            var eye = descriptor.customEyeLookSettings;
            if (eye.eyelidsSkinnedMesh == renderer && eye.eyelidsBlendshapes != null)
            {
                var result = (int[])eye.eyelidsBlendshapes.Clone();
                for (int i = 0; i < result.Length; i++)
                    if (result[i] >= 0 && result[i] < indices.Length) result[i] = indices[result[i]];
                eye.eyelidsBlendshapes = result;
                descriptor.customEyeLookSettings = eye;
            }
        }

        private void RewriteComponentAssets()
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null || component is Transform || component is AvatarGuard) continue;
                var relativeRoot = component is Animator animator ? animator.transform : root.transform;
                using (var so = new SerializedObject(component))
                {
                    var p = so.GetIterator(); bool changed = false;
                    while (p.Next(true))
                    {
                        if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                        var original = p.objectReferenceValue;
                        var replacement = CloneAsset(original, relativeRoot);
                        if (original != replacement) { p.objectReferenceValue = replacement; changed = true; }
                    }
                    if (changed) so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private Object CloneAsset(Object original, Transform relativeRoot)
        {
            if (original == null || owned.Contains(original)) return original;
            if (original is AnimationClip proxy && IsProxy(proxy))
            { report.Note("SDK/内置代理动画保留名称和身份"); return original; }
            bool animationObject = original is RuntimeAnimatorController || original is AnimatorStateMachine ||
                original is AnimatorState || original is AnimatorTransitionBase || original is StateMachineBehaviour ||
                original is BlendTree || original is AvatarMask || original is AnimationClip;
            bool namedAsset = original is RuntimeAnimatorController || original is BlendTree || original is AvatarMask ||
                original is AnimationClip || original is Mesh || original is Material || original is Texture2D ||
                original is Avatar || original is VRCExpressionsMenu || original is VRCExpressionParameters;
            if (!animationObject && !namedAsset) return original;
            // With resource renaming disabled, copy only graphs which may contain renamed shape curves.
            if (!settings.RenameAssets && !animationObject) return original;
            if (original is Texture2D texture && (!texture.isReadable ||
                texture.format == TextureFormat.DXT1Crunched || texture.format == TextureFormat.DXT5Crunched))
            { report.Note("不可读或 Crunch 纹理保留原资源和名称，避免复制错误或改变导入设置"); return original; }
            var map = sharedAssets;
            if (animationObject)
            {
                if (!animationAssets.TryGetValue(relativeRoot, out map))
                    animationAssets[relativeRoot] = map = new Dictionary<Object, Object>();
            }
            if (map.TryGetValue(original, out var existing)) return existing;
            Object copy;
            try { copy = CreateAssetCopy(original); }
            catch (Exception e)
            {
                // A failed animation-graph clone cannot safely be mixed with renamed shape bindings.
                if (animationObject) throw new InvalidOperationException("Cannot safely copy animation graph: " + original.name, e);
                report.Note(original.GetType().Name + "：无法安全复制，保留原资源"); return original;
            }
            copy.name = settings.RenameAssets && namedAsset ? NewName() : original.name;
            map[original] = copy; // Register before walking cyclic state machines or menus.
            Own(copy);
            if (settings.RenameAssets && namedAsset) report.RenamedAssets++;
            if (original is AnimatorOverrideController overrides)
            {
                // Unity rebuilds native override keys when the base controller changes. Restore
                // overrides after assigning the copied base, using its copied clip identities.
                var target = (AnimatorOverrideController)copy;
                target.runtimeAnimatorController = (RuntimeAnimatorController)CloneAsset(overrides.runtimeAnimatorController, relativeRoot);
                var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrides.GetOverrides(pairs);
                target.ApplyOverrides(pairs.Select(pair => new KeyValuePair<AnimationClip, AnimationClip>(
                    (AnimationClip)CloneAsset(pair.Key, relativeRoot),
                    (AnimationClip)CloneAsset(pair.Value, relativeRoot))).ToList());
            }
            else if (original is AnimationClip clip) RewriteClip(clip, (AnimationClip)copy, relativeRoot);
            else if (!(original is Mesh || original is Texture2D || original is Avatar))
                RewriteSerializedReferences(original, copy, relativeRoot);
            return copy;
        }

        private void RewriteSerializedReferences(Object source, Object copy, Transform relativeRoot)
        {
            using (var read = new SerializedObject(source))
            using (var write = new SerializedObject(copy))
            {
                var p = read.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var original = p.objectReferenceValue;
                    if (original == null) continue;
                    var replacement = CloneAsset(original, relativeRoot);
                    // Always overwrite from source: Instantiate can implicitly clone some children
                    // while continuing to share others. Source traversal gives one consistent graph.
                    var target = write.FindProperty(p.propertyPath);
                    if (target != null) target.objectReferenceValue = replacement;
                }
                write.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void RewriteClip(AnimationClip source, AnimationClip copy, Transform relativeRoot)
        {
            var oldBindings = new List<EditorCurveBinding>();
            var newBindings = new List<EditorCurveBinding>();
            var curves = new List<AnimationCurve>();
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var mapped = MapBinding(binding, relativeRoot);
                if (mapped.propertyName == binding.propertyName) continue;
                oldBindings.Add(binding); newBindings.Add(mapped);
                curves.Add(AnimationUtility.GetEditorCurve(source, binding));
            }
            if (oldBindings.Count > 0)
            {
                AnimationUtility.SetEditorCurves(copy, oldBindings.ToArray(), new AnimationCurve[oldBindings.Count]);
                AnimationUtility.SetEditorCurves(copy, newBindings.ToArray(), curves.ToArray());
            }
            var objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(source);
            var objectCurves = new ObjectReferenceKeyframe[objectBindings.Length][];
            for (int i = 0; i < objectBindings.Length; i++)
            {
                var values = AnimationUtility.GetObjectReferenceCurve(source, objectBindings[i]);
                for (int f = 0; f < values.Length; f++) values[f].value = CloneAsset(values[f].value, relativeRoot);
                objectCurves[i] = values;
            }
            if (objectBindings.Length > 0) AnimationUtility.SetObjectReferenceCurves(copy, objectBindings, objectCurves);
            var clipSettings = AnimationUtility.GetAnimationClipSettings(copy);
            if (clipSettings.additiveReferencePoseClip != null)
            {
                clipSettings.additiveReferencePoseClip = (AnimationClip)CloneAsset(clipSettings.additiveReferencePoseClip, relativeRoot);
                AnimationUtility.SetAnimationClipSettings(copy, clipSettings);
            }
            var events = AnimationUtility.GetAnimationEvents(copy);
            foreach (var evt in events) evt.objectReferenceParameter = CloneAsset(evt.objectReferenceParameter, relativeRoot);
            if (events.Length > 0) AnimationUtility.SetAnimationEvents(copy, events);
        }

        private EditorCurveBinding MapBinding(EditorCurveBinding binding, Transform relativeRoot)
        {
            if (!binding.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)) return binding;
            var targets = ResolveRenderers(relativeRoot, binding.path).ToArray();
            if (targets.Length == 1 && shapeRenames.TryGetValue(targets[0], out var names) &&
                names.TryGetValue(binding.propertyName.Substring(11), out var name)) binding.propertyName = "blendShape." + name;
            return binding;
        }

        private static bool IsProxy(AnimationClip clip)
        {
            var path = AssetDatabase.GetAssetPath(clip).Replace('\\', '/');
            return clip.name.StartsWith("proxy_", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Packages/com.vrchat.", StringComparison.Ordinal) ||
                path.IndexOf("/VRCSDK/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                path == "Resources/unity_builtin_extra" || path == "Library/unity default resources";
        }

        private static Object CreateAssetCopy(Object source)
        {
            Object copy;
            // Instantiate recursively duplicates Unity's native strong references inside NDMF's
            // temporary controller graphs and can emit kStrongPPtrMask assertions. CopySerialized
            // preserves all fields without implicitly cloning that graph; our explicit traversal
            // below then replaces every supported reference, including cycles and overrides.
            if (source is AnimatorOverrideController) return new AnimatorOverrideController();
            if (source is AnimatorController) copy = new AnimatorController();
            else if (source is AnimatorStateMachine) copy = new AnimatorStateMachine();
            else if (source is AnimatorState) copy = new AnimatorState();
            else if (source is AnimatorStateTransition) copy = new AnimatorStateTransition();
            else if (source is AnimatorTransition) copy = new AnimatorTransition();
            else if (source is BlendTree) copy = new BlendTree();
            else if (source is AvatarMask) copy = new AvatarMask();
            else if (source is AnimationClip) copy = new AnimationClip();
            else if (source is ScriptableObject) copy = ScriptableObject.CreateInstance(source.GetType());
            else if (source is Material material) return new Material(material);
            else return Object.Instantiate(source);
            EditorUtility.CopySerialized(source, copy);
            return copy;
        }

        private void Own(Object value)
        {
            owned.Add(value); report.CopiedObjects++;
            saveAsset?.Invoke(value);
        }

        private string NewName() { return settings.Prefix + "_" + Guid.NewGuid().ToString("N"); }
    }
}
