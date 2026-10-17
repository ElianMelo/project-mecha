using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Author native animation assets once, preserving subsequent Inspector tuning.
public static class MechaUiFeelSetup
{
    private const string Folder = "Assets/_Project/Animations/Interface";
    [MenuItem("Tools/Mecha/Apply UI Feel")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before applying UI feel.");
        ApplyHud(UnityEngine.Object.FindFirstObjectByType<MechaGameplayHud>());
        ApplyLobby(UnityEngine.Object.FindFirstObjectByType<MechaLobbyPanel>());
        AssetDatabase.SaveAssets();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    }
    private static UiFeelSettings Assets()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project/Animations", "Interface");
        var style = MechaPresentationSetup.Create<UiFeelSettings>("UiFeel");
        style.frameSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (style.buttonController == null) style.buttonController = ButtonController();
        if (style.readoutController == null) style.readoutController = ReadoutController();
        if (style.comboController == null) style.comboController = ComboController();
        if (style.panelController == null) style.panelController = RevealController("UiPanel", false);
        if (style.keyboardController == null) style.keyboardController = RevealController("UiKeyboard", true);
        EditorUtility.SetDirty(style); return style;
    }
    public static void ApplyHud(MechaGameplayHud hud)
    {
        if (hud == null) return;
        var style = Assets(); var data = new SerializedObject(hud);
        var health = data.FindProperty("healthFill").objectReferenceValue as UnityEngine.UI.Image;
        var mana = data.FindProperty("manaFill").objectReferenceValue as UnityEngine.UI.Image;
        var hpText = data.FindProperty("healthValue").objectReferenceValue as TMPro.TMP_Text;
        var mpText = data.FindProperty("energyValue").objectReferenceValue as TMPro.TMP_Text;
        if (health == null || mana == null || hpText == null || mpText == null) return;
        var feedback = MechaPresentationSetup.Ensure<GameplayHudFeedback>(hud.gameObject);
        Assign(feedback, "settings", style);
        Assign(feedback, "armor", Resource(health, hpText, style, true));
        Assign(feedback, "energy", Resource(mana, mpText, style, false));
        var combo = data.FindProperty("comboReadout").objectReferenceValue as TMPro.TMP_Text;
        var group = MechaPresentationSetup.Ensure<CanvasGroup>(combo.gameObject); group.alpha = 0f; group.blocksRaycasts = false;
        Assign(feedback, "combo", Animator(combo.gameObject, style.comboController));
        var labels = data.FindProperty("skills");
        var animators = new UnityEngine.Object[labels.arraySize]; var meters = new UnityEngine.Object[labels.arraySize];
        for (int i = 0; i < labels.arraySize; i++)
        {
            var label = labels.GetArrayElementAtIndex(i).objectReferenceValue as TMPro.TMP_Text;
            animators[i] = Animator(label.gameObject, style.readoutController);
            var rect = Child(label.transform.parent, "Readiness Meter");
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1f, 0f); rect.pivot = Vector2.zero;
            rect.offsetMin = new Vector2(8f, 4f); rect.offsetMax = new Vector2(-8f, 6f);
            var image = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject);
            image.sprite = style.frameSprite; image.type = UnityEngine.UI.Image.Type.Filled;
            image.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; image.fillOrigin = 0; image.fillAmount = 1f;
            image.color = style.focusColor; image.raycastTarget = false; meters[i] = image;
        }
        Array(feedback, "skillReadouts", animators); Array(feedback, "skillMeters", meters);
        var diagnostics = hud.transform.Find("F3 Diagnostics");
        if (diagnostics != null) Reveal(diagnostics.gameObject, style, false);
        foreach (var button in hud.GetComponentsInChildren<UnityEngine.UI.Button>(true)) Button(button, style);
    }
    public static void ApplyLobby(MechaLobbyPanel lobby)
    {
        if (lobby == null) return;
        var style = Assets(); var panel = lobby.transform.Find("Lobby Panel");
        if (panel == null) return;
        Reveal(panel.gameObject, style, false);
        var keyboard = panel.Find("Controller Keyboard");
        if (keyboard != null) Reveal(keyboard.gameObject, style, true);
        var navigation = MechaPresentationSetup.Ensure<UiNavigationScope>(panel.gameObject);
        Assign(navigation, "excludedControls", keyboard);
        foreach (var button in lobby.GetComponentsInChildren<UnityEngine.UI.Button>(true)) Button(button, style);
        foreach (var field in lobby.GetComponentsInChildren<TMPro.TMP_InputField>(true)) Button(field, style);
    }
    private static HudResourceFeedback Resource(UnityEngine.UI.Image fill, TMPro.TMP_Text text, UiFeelSettings style, bool armor)
    {
        var rect = Child(fill.transform.parent, "Loss Trail");
        rect.anchorMin = fill.rectTransform.anchorMin; rect.anchorMax = fill.rectTransform.anchorMax; rect.pivot = fill.rectTransform.pivot;
        rect.sizeDelta = fill.rectTransform.sizeDelta; rect.anchoredPosition = fill.rectTransform.anchoredPosition; rect.SetAsFirstSibling();
        var trail = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(rect.gameObject);
        fill.type = trail.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = trail.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = trail.fillOrigin = 0;
        trail.sprite = fill.sprite; trail.fillAmount = fill.fillAmount; trail.color = armor ? style.damageTrailColor : style.energyTrailColor; trail.raycastTarget = false;
        var feedback = MechaPresentationSetup.Ensure<HudResourceFeedback>(fill.transform.parent.gameObject);
        Assign(feedback, "settings", style); Assign(feedback, "fill", fill); Assign(feedback, "trail", trail); Assign(feedback, "valueLabel", text);
        Assign(feedback, "readout", Animator(text.gameObject, style.readoutController));
        var data = new SerializedObject(feedback); data.FindProperty("armor").boolValue = armor; data.ApplyModifiedPropertiesWithoutUndo();
        return feedback;
    }
    public static void Button(UnityEngine.UI.Selectable button, UiFeelSettings style)
    {
        var frame = Child(button.transform, "Focus Frame");
        frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one; frame.offsetMin = new Vector2(-3f, -3f); frame.offsetMax = new Vector2(3f, 3f);
        frame.SetAsFirstSibling();
        var graphic = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(frame.gameObject);
        graphic.sprite = style.frameSprite; graphic.type = UnityEngine.UI.Image.Type.Sliced; graphic.fillCenter = false;
        graphic.color = new Color(style.focusColor.r, style.focusColor.g, style.focusColor.b, 0f); graphic.raycastTarget = false;
        Animator(button.gameObject, style.buttonController);
        var feedback = MechaPresentationSetup.Ensure<UiInteractionFeedback>(button.gameObject);
        Assign(feedback, "settings", style);
        var border = Child(button.transform, "Selection Border");
        border.anchorMin = Vector2.zero; border.anchorMax = Vector2.one;
        border.offsetMin = new Vector2(-5f, -5f); border.offsetMax = new Vector2(5f, 5f);
        var borderImage = MechaPresentationSetup.Ensure<UnityEngine.UI.Image>(border.gameObject);
        borderImage.sprite = style.frameSprite; borderImage.type = UnityEngine.UI.Image.Type.Sliced;
        borderImage.fillCenter = false; borderImage.color = style.focusColor;
        borderImage.raycastTarget = false; borderImage.enabled = false;
        if (button is UnityEngine.UI.Slider && button.GetComponentInParent<AudioConfigurationMenu>() != null)
        {
            // Enclose the label, rail and readout; leave the moving handle unobscured.
            frame.offsetMin = border.offsetMin = new Vector2(-184f, -10f);
            frame.offsetMax = border.offsetMax = new Vector2(100f, 10f);
            border.SetAsFirstSibling();
        }
        Assign(feedback, "selectionBorder", borderImage);
    }
    private static void Reveal(GameObject panel, UiFeelSettings style, bool keyboard)
    {
        MechaPresentationSetup.Ensure<CanvasGroup>(panel).alpha = 1f;
        Animator(panel, keyboard ? style.keyboardController : style.panelController);
        Assign(MechaPresentationSetup.Ensure<UiPanelReveal>(panel), "settings", style);
    }
    private static Animator Animator(GameObject target, RuntimeAnimatorController controller)
    {
        var animator = MechaPresentationSetup.Ensure<Animator>(target); animator.runtimeAnimatorController = controller;
        animator.updateMode = AnimatorUpdateMode.UnscaledTime; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.applyRootMotion = false;
        return animator;
    }
    private static RectTransform Child(Transform parent, string name)
    { return MechaPresentationSetup.Ensure<RectTransform>(MechaScenarioSetup.Child(parent, name)); }
    private static void Assign(Component target, string field, UnityEngine.Object value) => MechaPresentationSetup.Assign(target, field, value);
    private static void Array(Component target, string field, UnityEngine.Object[] values)
    {
        var data = new SerializedObject(target); var array = data.FindProperty(field); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static AnimationClip Clip(string name, params (string path, Type type, string property, AnimationCurve curve)[] curves)
    {
        var clip = new AnimationClip { name = name, frameRate = 60f };
        foreach (var curve in curves) AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(curve.path, curve.type, curve.property), curve.curve);
        AssetDatabase.CreateAsset(clip, Folder + "/" + name + ".anim"); return clip;
    }
    private static AnimationCurve Curve(params float[] values)
    {
        var keys = new Keyframe[values.Length / 2];
        for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(values[i * 2], values[i * 2 + 1], 0f, 0f);
        return new AnimationCurve(keys);
    }
    private static AnimationClip ScaleClip(string name, string path, AnimationCurve scale, AnimationCurve alpha = null)
    {
        var curves = new System.Collections.Generic.List<(string, Type, string, AnimationCurve)>();
        foreach (var axis in new[] { "x", "y" }) curves.Add((path, typeof(Transform), "m_LocalScale." + axis, scale));
        if (alpha != null) curves.Add(("Focus Frame", typeof(UnityEngine.UI.Image), "m_Color.a", alpha));
        return Clip(name, curves.ToArray());
    }
    private static AnimatorState State(AnimatorStateMachine machine, string name, Motion clip)
    { var state = machine.AddState(name); state.motion = clip; state.writeDefaultValues = false; return state; }
    private static void Transition(AnimatorState from, AnimatorState to, float duration, params (string parameter, bool value)[] conditions)
    {
        var transition = from.AddTransition(to); transition.hasExitTime = false; transition.duration = duration;
        foreach (var c in conditions) transition.AddCondition(c.value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, c.parameter);
    }
    private static void Exit(AnimatorState from, AnimatorState to, params (string parameter, bool value)[] conditions)
    {
        var transition = from.AddTransition(to); transition.hasExitTime = true; transition.exitTime = 0.95f; transition.duration = 0.04f;
        foreach (var c in conditions) transition.AddCondition(c.value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, c.parameter);
    }
    private static void Any(AnimatorStateMachine machine, AnimatorState to, string parameter, bool trigger, params (string parameter, bool value)[] conditions)
    {
        var transition = machine.AddAnyStateTransition(to); transition.canTransitionToSelf = false; transition.hasExitTime = false; transition.duration = 0.04f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
        foreach (var c in conditions) transition.AddCondition(c.value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, c.parameter);
    }
    private static AnimatorController ButtonController()
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/UiButton.controller");
        foreach (var name in new[] { "Focused", "Pressed", "Disabled" }) controller.AddParameter(name, AnimatorControllerParameterType.Bool);
        controller.AddParameter("Confirm", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var idle = State(machine, "Idle", ScaleClip("ButtonIdle", "Label", Curve(0, 1, 0.1f, 1), Curve(0, 0, 0.1f, 0)));
        var focus = State(machine, "Focus", ScaleClip("ButtonFocus", "Label", Curve(0, 1.04f, 0.1f, 1.04f), Curve(0, 0.9f, 0.1f, 0.9f)));
        var press = State(machine, "Press", ScaleClip("ButtonPress", "Label", Curve(0, 0.96f, 0.08f, 0.96f), Curve(0, 1, 0.08f, 1)));
        var confirm = State(machine, "Confirm", ScaleClip("ButtonConfirm", "Label", Curve(0, 0.96f, 0.07f, 1.08f, 0.24f, 1.04f), Curve(0, 1, 0.24f, 0.9f)));
        var disabled = State(machine, "Disabled", ScaleClip("ButtonDisabled", "Label", Curve(0, 1, 0.1f, 1), Curve(0, 0, 0.1f, 0)));
        machine.defaultState = idle;
        Any(machine, disabled, "Disabled", false); Any(machine, press, "Pressed", false, ("Disabled", false)); Any(machine, confirm, "Confirm", true, ("Disabled", false));
        Transition(idle, focus, 0.08f, ("Focused", true), ("Disabled", false)); Transition(focus, idle, 0.1f, ("Focused", false));
        foreach (var state in new[] { press, disabled })
        {
            Transition(state, focus, 0.08f, ("Pressed", false), ("Disabled", false), ("Focused", true));
            Transition(state, idle, 0.08f, ("Pressed", false), ("Disabled", false), ("Focused", false));
        }
        Exit(confirm, focus, ("Focused", true)); Exit(confirm, idle, ("Focused", false)); return controller;
    }
    private static AnimatorController ReadoutController()
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/UiReadout.controller");
        controller.AddParameter("Pulse", AnimatorControllerParameterType.Trigger); controller.AddParameter("Low", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var idle = State(machine, "Idle", ScaleClip("ReadoutIdle", "", Curve(0, 1, 0.1f, 1)));
        var pulse = State(machine, "Pulse", ScaleClip("ReadoutPulse", "", Curve(0, 1, 0.055f, 1.11f, 0.27f, 1)));
        var lowClip = ScaleClip("ReadoutLow", "", Curve(0, 1, 0.45f, 1.035f, 0.9f, 1));
        var loop = AnimationUtility.GetAnimationClipSettings(lowClip); loop.loopTime = true; AnimationUtility.SetAnimationClipSettings(lowClip, loop);
        var low = State(machine, "Low", lowClip); machine.defaultState = idle;
        Any(machine, pulse, "Pulse", true); Transition(idle, low, 0.1f, ("Low", true)); Transition(low, idle, 0.1f, ("Low", false));
        Exit(pulse, low, ("Low", true)); Exit(pulse, idle, ("Low", false)); return controller;
    }
    private static AnimatorController ComboController()
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/UiCombo.controller");
        controller.AddParameter("Visible", AnimatorControllerParameterType.Bool); controller.AddParameter("Pulse", AnimatorControllerParameterType.Trigger);
        var machine = controller.layers[0].stateMachine;
        var hidden = State(machine, "Hidden", Clip("ComboHidden", ("", typeof(CanvasGroup), "m_Alpha", Curve(0, 0, 0.1f, 0))));
        var shown = State(machine, "Shown", Clip("ComboShown", ("", typeof(CanvasGroup), "m_Alpha", Curve(0, 1, 0.1f, 1))));
        machine.defaultState = hidden;
        Transition(hidden, shown, 0.12f, ("Visible", true)); Transition(shown, hidden, 0.28f, ("Visible", false));
        controller.AddLayer("Hit Feedback"); var layers = controller.layers; layers[1].defaultWeight = 1f; controller.layers = layers;
        var pulseMachine = controller.layers[1].stateMachine;
        var idle = State(pulseMachine, "Idle", ScaleClip("ComboIdle", "", Curve(0, 1, 0.1f, 1)));
        var pulse = State(pulseMachine, "Pulse", ScaleClip("ComboPulse", "", Curve(0, 1, 0.05f, 1.17f, 0.24f, 1)));
        pulseMachine.defaultState = idle; Any(pulseMachine, pulse, "Pulse", true); Exit(pulse, idle); return controller;
    }
    private static AnimatorController RevealController(string name, bool keyboard)
    {
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/" + name + ".controller");
        var curve = Curve(0, 0, 0.23f, 1);
        var clip = keyboard ? Clip(name + "Reveal", ("", typeof(CanvasGroup), "m_Alpha", curve),
            ("Keyboard Card", typeof(Transform), "m_LocalScale.x", Curve(0, 0.965f, 0.16f, 1.012f, 0.26f, 1)),
            ("Keyboard Card", typeof(Transform), "m_LocalScale.y", Curve(0, 0.965f, 0.16f, 1.012f, 0.26f, 1))) :
            Clip(name + "Reveal", ("", typeof(CanvasGroup), "m_Alpha", curve));
        controller.layers[0].stateMachine.defaultState = State(controller.layers[0].stateMachine, "Reveal", clip); return controller;
    }
}
