using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Explicit authoring only. Native Light and Animator assets own illumination and timing.
public static class WaterLightingAuthoring
{
    const string Root = "Assets/_Project/VFX/ZeroWater";
    static readonly Color Coolant = new Color(.075f, .48f, 1f);
    static readonly string[] Names = { "BubblePreparation", "BubbleDetonation", "FountainPreparation", "RisingFountain", "WaterModeAura" };

    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        Folder(parent); AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
    static AnimationCurve Curve(params float[] values)
    {
        var keys = Enumerable.Range(0, values.Length / 2).Select(i => new Keyframe(values[i * 2], values[i * 2 + 1])).ToArray();
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        return curve;
    }
    static AnimationClip Clip(string name, AnimationCurve curve, int count, bool loop)
    {
        string path = Root + "/Animations/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip { name = name }; AssetDatabase.CreateAsset(clip, path); }
        for (int i = 0; i < count; i++)
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Source " + i, typeof(Light), "m_Intensity"), curve);
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip);
        return clip;
    }
    static AnimatorController Controller(string name, AnimationClip active, AnimationClip release)
    {
        string path = Root + "/Animations/" + name + ".controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Active") ?? machine.AddState("Active");
        state.motion = active; state.writeDefaultValues = false; machine.defaultState = state;
        if (release != null)
        {
            if (!controller.parameters.Any(p => p.name == "Release")) controller.AddParameter("Release", AnimatorControllerParameterType.Trigger);
            var end = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Release") ?? machine.AddState("Release");
            end.motion = release; end.writeDefaultValues = false;
            var transition = state.transitions.FirstOrDefault(t => t.destinationState == end) ?? state.AddTransition(end);
            transition.hasExitTime = false; transition.duration = .05f; transition.hasFixedDuration = true;
            transition.conditions = new[] { new AnimatorCondition { mode = AnimatorConditionMode.If, parameter = "Release" } };
        }
        EditorUtility.SetDirty(controller); return controller;
    }
    public static void Apply(GameObject effect)
    {
        string name = effect.name;
        if (!Names.Contains(name)) return;
        Folder(Root + "/Animations");
        bool mode = name == "WaterModeAura", fountain = name.Contains("Fountain");
        int count = fountain ? 4 : 1;
        AnimationCurve envelope;
        switch (name)
        {
            case "BubblePreparation": envelope = Curve(0, 0, .06f, .5f, .24f, 1.6f, .32f, 0); break;
            case "BubbleDetonation": envelope = Curve(0, 0, .025f, 6, .13f, 4, .4f, 2, .8f, .4f, 1.1f, 0); break;
            case "FountainPreparation": envelope = Curve(0, 0, .08f, .65f, .22f, .5f, .3f, 0); break;
            case "RisingFountain": envelope = Curve(0, 0, .04f, 2.4f, .18f, 1.2f, .26f, 2.4f, .40f, 1.2f, .48f, 2.4f, .62f, 1.2f, .70f, 2.4f, .95f, 1, 1.35f, 0); break;
            default: envelope = Curve(0, 1.15f, .3f, 1.3f, .7f, 1.1f, 1, 1.15f); break;
        }
        var active = Clip(name + "Light", envelope, count, mode);
        var release = mode ? Clip("WaterModeLightRelease", Curve(0, 1.15f, .35f, 0, .6f, 0), 1, false) : null;
        var controller = Controller(name + "Light", active, release);
        var branch = effect.transform.Find("Water Lighting");
        if (branch == null) { branch = new GameObject("Water Lighting").transform; branch.SetParent(effect.transform, false); }
        var animator = branch.GetComponent<Animator>();
        if (animator == null) animator = branch.gameObject.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        for (int i = 0; i < count; i++)
        {
            var child = branch.Find("Source " + i);
            if (child == null) { child = new GameObject("Source " + i).transform; child.SetParent(branch, false); }
            child.localPosition = new Vector3(fountain ? (i - 1.5f) * 1.45f : 0, fountain ? .45f : mode ? .65f : 0, -.45f);
            var light = child.GetComponent<Light>();
            if (light == null) light = child.gameObject.AddComponent<Light>();
            light.type = LightType.Point; light.color = Coolant; light.intensity = mode ? 1.15f : 0;
            light.range = name == "BubbleDetonation" ? 3.8f : name == "FountainPreparation" ? 1.45f : mode ? 2.5f : 2.3f;
            light.shadows = LightShadows.None; light.renderMode = LightRenderMode.ForcePixel; light.bounceIntensity = 0;
        }
        var serialized = new SerializedObject(effect.GetComponent<WaterEffect>());
        serialized.FindProperty("sustainedLighting").objectReferenceValue = mode ? animator : null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    public static object ApplyAll()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Author lights in Edit Mode.");
        foreach (var name in Names)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            var effect = PrefabUtility.LoadPrefabContents(path);
            try { Apply(effect); PrefabUtility.SaveAsPrefabAsset(effect, path); }
            finally { PrefabUtility.UnloadPrefabContents(effect); }
        }
        var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith(Root + "/Animations/") || p == Root + "/Editor/WaterLightingAuthoring.cs").ToArray();
        foreach (var path in paths)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path); if (asset == null || AssetDatabase.IsValidFolder(path)) continue;
            AssetDatabase.SetLabels(asset, AssetDatabase.GetLabels(asset).Where(l => l != "Keep").Append("Replace").Distinct().ToArray());
        }
        MechaAssetLabels.Apply(paths, 0, paths.Length);
        var script = AssetDatabase.LoadMainAssetAtPath(Root + "/Editor/WaterLightingAuthoring.cs");
        if (script != null) AssetDatabase.SetLabels(script, AssetDatabase.GetLabels(script).Where(l => l != "Others").Append("Code").Distinct().ToArray());
        AssetDatabase.SaveAssets();
        return Names.Select(n => new { effect = n, lights = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + n + ".prefab").GetComponentsInChildren<Light>(true).Length }).ToArray();
    }
}
