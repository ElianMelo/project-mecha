using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public static class MechaControlsSetup
{
    private static Sprite frame;
    private static TMP_FontAsset font;
    private static UiFeelSettings style;
    private static LocalizedString Text(string key, string value) => MechaLocalizationAuthoring.Capture("Input", key, value);
    [MenuItem("Tools/Mecha/Configure Remappable Controls")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/MainScene.unity") throw new InvalidOperationException("Open MainScene first.");
        var menu = UnityEngine.Object.FindFirstObjectByType<GameOptionsMenu>();
        if (menu.transform.Find("Controls Settings") != null) throw new InvalidOperationException("Controls are already authored; update them in place.");
        frame = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Localization/Fonts/EnglishDefault.asset");
        style = MechaPresentationSetup.Profile<UiFeelSettings>("UiFeel");
        PrepareActions();
        var profile = MechaPresentationSetup.Create<ControlBindingSettings>("ControlBindings");
        profile.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Inputs/InputSystem_Actions.inputactions");
        profile.captureTimeout = 10f;
        profile.descriptions = new[] {
            Description("Player/Move", "move", "Move", "Walk left or right. Double-tap a direction to dash.", true),
            Description("Player/Jump", "jump", "Jump", "Jump; hold to control your ascent.", true),
            Description("Player/Down", "down", "Down", "Crouch, drop through platforms, or modify an aerial attack.", true),
            Description("Player/Attack", "attack", "Light attack", "Chain light attacks. Hold near an unlocked portal to travel.", true),
            Description("Player/Tech", "heavy", "Heavy attack", "Use the heavy branch of your equipped combo.", true),
            Description("Player/Dash", "dash", "Dash", "Dash in your movement direction; combine with Jump for Rocket.", true),
            Description("Player/SecondaryFire", "fire", "Secondary fire", "Fire one round from your secondary weapon.", true),
            Description("Player/SkillA", "skill.one", "Skill 1 · 25 MP", "Use the first equipped skill (25 MP).", true),
            Description("Player/SkillS", "skill.two", "Skill 2 · 40 MP", "Use the second equipped skill (40 MP).", true),
            Description("Player/SkillD", "skill.three", "Skill 3 · 60 MP", "Use the third equipped skill (60 MP).", true),
            Description("System/Menu", "menu", "Configuration", "Open Configuration or return to the previous screen.", true),
            Description("System/ControlsHelp", "help", "Controls tutorial", "Show or hide the combat controls tutorial.", false),
            Description("System/ChatVisibility", "chat.visibility", "Chat visibility", "Show or hide chat during co-op gameplay.", false),
            Description("System/Chat", "chat", "Open chat", "Start typing a message during co-op gameplay.", false),
            Description("System/Diagnostics", "diagnostics", "Combat lab", "Open or close the combat diagnostics panel.", false),
            Description("System/LocalHost", "host", "Solo host", "Start a local solo session.", false),
            Description("System/LocalJoin", "join", "Local join", "Join a local host at the configured address.", false),
            Description("UI/Navigate", "navigate", "Navigate menus", "Move the selection between visible menu controls.", false),
            Description("UI/Submit", "confirm", "Confirm", "Activate the selected button or confirm a choice.", false),
            Description("UI/Cancel", "back", "Back", "Close the current list or return to the previous screen.", false),
            Description("UI/Click", "click", "Pointer click", "Activate the control under the pointer.", false),
            Description("UI/RightClick", "right.click", "Pointer secondary", "Send a secondary pointer click to the interface.", false),
            Description("UI/MiddleClick", "middle.click", "Pointer middle", "Send a middle pointer click to the interface.", false)
        };
        CaptureStrings(); EditorUtility.SetDirty(profile);
        var manager = UnityEngine.Object.FindFirstObjectByType<GameManager>(); Assign(manager, "controlBindings", profile);
        var homeCard = menu.transform.Find("Options Home/Options");
        var homeButton = Button(homeCard, "Controls", Text("controls.title", "CONTROLS"), 40, 466, 500, 52);
        var ordered = new[] { "Resume", "Lobby", "Audio", "Video", "General", "Controls" }.Select(n => homeCard.Find(n).GetComponent<UnityEngine.UI.Button>()).ToArray();
        for (int i = 0; i < ordered.Length; i++) { var rect = (RectTransform)ordered[i].transform; rect.anchoredPosition = new Vector2(40, -132 - i * 67); rect.sizeDelta = new Vector2(500, 52); ordered[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit, selectOnUp = ordered[(i+5)%6], selectOnDown = ordered[(i+1)%6] }; }
        var overlay = Rect(menu.transform, "Controls Settings", 0, 0, 0, 0); Stretch(overlay); Image(overlay, new Color(0.005f,0.012f,0.023f,0.97f), true);
        var panel = Rect(overlay, "Controls", 0, 0, 1160, 680); panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one*0.5f; panel.anchoredPosition = Vector2.zero; Image(panel,new Color(0.025f,0.05f,0.085f));
        var presenter = overlay.gameObject.AddComponent<ControlsConfigurationMenu>();
        var body = Rect(panel, "Body", 0, 0, 0, 0); Stretch(body); var group = body.gameObject.AddComponent<CanvasGroup>();
        Label(body, "Title", Text("controls.title", "CONTROLS"), 30, 18, 600, 38, 28);
        var controller = Button(body, "Controller", Text("tab.controller", "CONTROLLER"), 30, 68, 540, 44);
        var keyboard = Button(body, "Keyboard", Text("tab.keyboard", "KEYBOARD"), 590, 68, 540, 44);
        var ci = Image(Rect(controller.transform,"Active Tab",0,40,540,4),style.focusColor);
        var ki = Image(Rect(keyboard.transform,"Active Tab",0,40,540,4),style.focusColor);
        var viewportRoot = Rect(body,"Bindings",30,130,1100,392); var scroll = viewportRoot.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        var viewport = Rect(viewportRoot,"Viewport",0,0,1100,392); Image(viewport,Color.white,true); var mask=viewport.gameObject.AddComponent<UnityEngine.UI.Mask>(); mask.showMaskGraphic=false;
        var content=Rect(viewport,"Content",0,0,1100,0); content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(0.5f,1);content.sizeDelta=Vector2.zero;
        var layout=content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=8;layout.padding=new RectOffset(6,6,6,6);layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
        content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scroll.content=content;scroll.viewport=viewport;scroll.horizontal=false;scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=42;
        viewportRoot.gameObject.AddComponent<UiSelectionScroll>();
        var rowRect=Rect(body,"Binding Template",0,0,1088,86); Image(rowRect,new Color(0.04f,0.08f,0.12f)); rowRect.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight=86;
        var row=rowRect.gameObject.AddComponent<ControlBindingRow>();
        row.title=Label(rowRect,"Action",null,12,7,455,27,19);row.description=Label(rowRect,"Description",null,12,36,455,44,14);
        row.remap=Button(rowRect,"Remap",null,490,18,310,50); row.binding=row.remap.GetComponentInChildren<TMP_Text>(true);row.binding.fontSize=16;
        row.clear=Button(rowRect,"Clear",Text("binding.clear","CLEAR"),818,18,115,50);
        row.reset=Button(rowRect,"Reset",Text("binding.reset","RESET"),950,18,115,50);
        rowRect.gameObject.SetActive(false);
        var status=Label(body,"Status",null,30,532,1100,30,15);
        var hint=Label(body,"Navigation",null,30,566,1100,42,13); NavigationHint(hint,default);
        var defaults=Button(body,"Defaults",Text("binding.defaults","RESTORE TAB DEFAULTS"),30,620,540,42);
        var back=Button(body,"Back",MechaLocalizationAuthoring.Capture("Settings","navigation.back","BACK"),590,620,540,42);
        var modal=Rect(panel,"Capture",0,0,0,0);Stretch(modal);Image(modal,new Color(0.005f,0.012f,0.023f,0.99f),true);modal.gameObject.AddComponent<UiNavigationScope>();
        var captureText=Label(modal,"Message",null,180,210,800,160,25);captureText.alignment=TextAlignmentOptions.Center;
        var cancel=Button(modal,"Cancel",Text("binding.cancel","CANCEL"),380,400,400,60);modal.gameObject.SetActive(false);
        Assign(presenter,"settings",profile);Assign(presenter,"controllerTab",controller);Assign(presenter,"keyboardTab",keyboard);Assign(presenter,"controllerIndicator",ci);Assign(presenter,"keyboardIndicator",ki);
        Assign(presenter,"content",content);Assign(presenter,"rowTemplate",row);Assign(presenter,"capturePanel",modal.gameObject);Assign(presenter,"body",group);Assign(presenter,"status",status);Assign(presenter,"captureText",captureText);
        Assign(presenter,"defaults",defaults);Assign(presenter,"back",back);Assign(presenter,"cancel",cancel);Assign(presenter,"scroll",scroll);Assign(menu,"controls",presenter);Assign(menu,"controlsButton",homeButton);
        overlay.gameObject.SetActive(false);
        // Replace fixed button-position prose with current bindings, preserving each screen's useful introduction.
        foreach (var label in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (label.name != "Help" || label.transform.IsChildOf(overlay)) continue;
            string value = label.text;
            if (!value.Contains("Bottom button") && !value.Contains("Right button")) continue;
            string intro = value.StartsWith("0%") ? "0% mutes a layer. SFX includes combat, movement and destruction." : value.StartsWith("V-sync") ? "V-sync uses the display rate. Texture quality controls texture resolution." : value.StartsWith("Up to 4") ? "Up to 4 pilots. Every pilot must be ready." : "";
            NavigationHint(label,string.IsNullOrEmpty(intro)?null:MechaLocalizationAuthoring.Capture(MechaLocalizationAuthoring.Category(label.transform.parent.name),"navigation.introduction",intro));
        }
        MechaLocalizationAuthoring.LocalizeScene(scene);
        PrefabUtility.ApplyPrefabInstance(menu.gameObject,InteractionMode.AutomatedAction);
        foreach (var root in scene.GetRootGameObjects())
            if (PrefabUtility.IsPartOfPrefabInstance(root) && root != menu.gameObject && root.GetComponentsInChildren<ControlNavigationHint>(true).Length>0) PrefabUtility.ApplyPrefabInstance(root,InteractionMode.AutomatedAction);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    private static ControlBindingSettings.ActionDescription Description(string action,string key,string title,string description,bool tutorial) =>
        new ControlBindingSettings.ActionDescription { action=action,title=action=="Player/Dash"?MechaLocalizationAuthoring.Capture("Combat","term.Dash",title):Text("action."+key,title),description=Text("help."+key,description),tutorial=tutorial };
    private static void CaptureStrings()
    {
        Text("binding.unbound","UNBOUND");Text("binding.direction","{0} · {1}");
        foreach(var direction in new[]{"up","down","left","right"}) Text("direction."+direction,char.ToUpperInvariant(direction[0])+direction.Substring(1));
        Text("binding.saved","Changes are saved automatically. Select a binding to remap it.");
        Text("binding.listen","Press a new input.\nEscape or click Cancel to stop.\nReturning in {0} seconds.");
        Text("binding.listen.controller","Tap a new input to assign it.\nHold {1} to cancel.\nReturning in {0} seconds.");
        Text("binding.conflict","Already used by {0}. Choose another input; your previous binding is kept.");
        Text("binding.cancelled","Remapping cancelled. Your previous binding is kept.");Text("binding.restored","Default bindings restored for this tab.");
        Text("tutorial.action","{0}  {1}");Text("tutorial.chord","{0} + {1}: {2}");Text("tutorial.hold","Hold {0}: {1}");Text("tutorial.then","{0}, then {1}: {2}");
        MechaLocalizationAuthoring.Capture("Combat","term.Rocket","Rocket");Text("action.dive","Dive attack");Text("action.portal","Enter portal");MechaLocalizationAuthoring.Capture("Combat","term.Mushidon","Mushidon");
        Text("navigation.hint","{0}: {1}   •   {2}: {3}   •   {4}: {5}");Text("tutorial.standby","{0}: {1}   •   {2}: {3}   •   {4}: {5}");
        Text("control.leftStick","Left stick");Text("control.rightStick","Right stick");Text("control.dpad","D-pad");
    }
    private static void PrepareActions()
    {
        const string path="Assets/_Project/Inputs/InputSystem_Actions.inputactions";
        var clone=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(path));
        try
        {
            foreach(var name in new[]{"Submit","Cancel"})
            {
                var action=clone.FindAction("UI/"+name);
                for(int i=action.bindings.Count-1;i>=0;i--) action.ChangeBinding(i).Erase();
                action.AddBinding(name=="Submit"?"<Keyboard>/enter":"<Keyboard>/escape",groups:"Keyboard&Mouse");
                action.AddBinding(name=="Submit"?"<Gamepad>/buttonSouth":"<Gamepad>/buttonEast",groups:"Gamepad");
            }
            var back=clone.FindAction("System/Back"); if(!back.bindings.Any(b=>b.groups=="Keyboard&Mouse"))back.AddBinding("<Keyboard>/escape",groups:"Keyboard&Mouse");
            var chat=clone.FindAction("System/Chat",false)??clone.FindActionMap("System").AddAction("Chat",InputActionType.Button,expectedControlLayout:"Button");
            if(chat.bindings.Count==0) { chat.AddBinding("<Keyboard>/enter",groups:"Keyboard&Mouse");chat.AddBinding("<Keyboard>/numpadEnter",groups:"Keyboard&Mouse");chat.AddBinding("<Gamepad>/leftStickPress",groups:"Gamepad"); }
            // Every shortcut has a slot on both tabs, initially unbound when no default exists.
            foreach(var name in new[]{"ChatVisibility","LocalHost","LocalJoin"})
            {var action=clone.FindAction("System/"+name);if(!action.bindings.Any(b=>b.groups=="Gamepad"))action.AddBinding("",groups:"Gamepad");}
            string json=clone.ToJson();
            if(File.ReadAllText(path)!=json)
            {
                string temporary=path+".tmp";File.WriteAllText(temporary,json);File.Replace(temporary,path,null);
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(clone);}
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
    }
    private static void NavigationHint(TMP_Text label,LocalizedString introduction)
    {
        var binding=label.GetComponent<LocalizeStringEvent>();if(binding!=null)UnityEngine.Object.DestroyImmediate(binding);
        var hint=MechaPresentationSetup.Ensure<ControlNavigationHint>(label.gameObject);Assign(hint,"text",label);
        typeof(ControlNavigationHint).GetField("introduction",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(hint,introduction??new LocalizedString());EditorUtility.SetDirty(hint);
    }
    private static void Assign(Component target,string name,UnityEngine.Object value)=>MechaPresentationSetup.Assign(target,name,value);
    private static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;}
    private static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
    private static UnityEngine.UI.Image Image(RectTransform r,Color color,bool raycast=false)
    {var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.sprite=frame;i.type=UnityEngine.UI.Image.Type.Sliced;i.color=color;i.raycastTarget=raycast;return i;}
    private static TMP_Text Label(Transform parent,string name,LocalizedString reference,float x,float y,float width,float height,float size)
    {
        var t=Rect(parent,name,x,y,width,height).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=size;t.color=new Color(0.88f,0.95f,0.99f);t.raycastTarget=false;
        if(reference!=null&&!reference.IsEmpty){var l=t.gameObject.AddComponent<LocalizeStringEvent>();l.StringReference=reference;UnityEventTools.AddPersistentListener(l.OnUpdateString,(UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>),t,"set_text"));l.OnUpdateString.SetPersistentListenerState(0,UnityEventCallState.EditorAndRuntime);l.RefreshString();}
        return t;
    }
    private static UnityEngine.UI.Button Button(Transform parent,string name,LocalizedString reference,float x,float y,float width,float height)
    {var r=Rect(parent,name,x,y,width,height);var image=Image(r,new Color(0.1f,0.23f,0.3f),true);var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=image;var t=Label(r,"Label",reference,0,0,width,height,17);t.alignment=TextAlignmentOptions.Center;MechaUiFeelSetup.Button(b,style);r.gameObject.AddComponent<UiAudioFeedback>();return b;}
}
