using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine.Localization.Components;
using TMPro;

public static class ApplyCombatFollowup
{
    static void Save(UnityEngine.Object o){EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);}
    static void Assign(Component c,string field,UnityEngine.Object o){var s=new SerializedObject(c);s.FindProperty(field).objectReferenceValue=o;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Text(TMP_Text label,string category,string key,string value)
    {
        label.text=value;var binding=label.GetComponent<LocalizeStringEvent>();
        if(binding==null)binding=GameText.Bind(label,MechaLocalizationAuthoring.Capture(category,key,value));
        else binding.StringReference=MechaLocalizationAuthoring.Capture(category,key,value);
        EditorUtility.SetDirty(binding);
    }
    static void Localizations()
    {
        foreach(string category in new[]{"Settings","Routing"})
        {var c=UnityEditor.Localization.LocalizationEditorSettings.GetStringTableCollection(category);foreach(var t in c.StringTables)Save(t);Save(c.SharedData);Save(c);}
    }
    static void General(GameObject root)
    {
        var menu=root.GetComponentInChildren<GeneralConfigurationMenu>(true);var panel=menu.transform.Find("General");
        panel.GetComponent<RectTransform>().sizeDelta=new Vector2(900,440);
        var language=panel.Find("Language").GetComponent<TMP_Dropdown>();var label=panel.Find("Language Label").GetComponent<TMP_Text>();label.rectTransform.sizeDelta=new Vector2(330,44);
        var langRect=language.GetComponent<RectTransform>();langRect.anchoredPosition=new Vector2(400,-112);langRect.sizeDelta=new Vector2(460,44);
        foreach(bool self in new[]{true,false})
        {
            string name=self?"Self Highlight":"Players Highlight";float y=self?-180:-248;
            var t=panel.Find(name);var dropdown=t!=null?t.GetComponent<TMP_Dropdown>():UnityEngine.Object.Instantiate(language,panel,false);dropdown.name=name;
            var rect=dropdown.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(400,y);rect.sizeDelta=new Vector2(460,44);dropdown.onValueChanged=new TMP_Dropdown.DropdownEvent();
            var caption=dropdown.captionText.GetComponent<LocalizeStringEvent>();if(caption!=null)UnityEngine.Object.DestroyImmediate(caption);
            var lt=panel.Find(name+" Label");var title=lt!=null?lt.GetComponent<TMP_Text>():UnityEngine.Object.Instantiate(label,panel,false);title.name=name+" Label";title.rectTransform.anchoredPosition=new Vector2(40,y);title.rectTransform.sizeDelta=new Vector2(350,44);
            Text(title,"Settings",self?"highlight.self":"highlight.players",self?"Show self highlight":"Show players highlight");
            dropdown.ClearOptions();dropdown.AddOptions(new System.Collections.Generic.List<string>{"Off","On"});dropdown.SetValueWithoutNotify(self?0:1);
            Assign(menu,self?"selfHighlight":"playersHighlight",dropdown);
        }
        panel.Find("Back").GetComponent<RectTransform>().anchoredPosition=new Vector2(40,-350);
    }
    public static string OptionsAndRoute()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        MechaLocalizationAuthoring.Capture("Settings","option.off","Off");MechaLocalizationAuthoring.Capture("Settings","option.on","On");
        string path="Assets/_Project/Prefabs/Interface/GameOptions.prefab";var go=PrefabUtility.LoadPrefabContents(path);
        GameObject buttonTemplate=null;
        try{General(go);buttonTemplate=UnityEngine.Object.Instantiate(go.GetComponentInChildren<GeneralConfigurationMenu>(true).transform.Find("General/Back").gameObject);buttonTemplate.hideFlags=HideFlags.HideAndDontSave;PrefabUtility.SaveAsPrefabAsset(go,path);}
        finally{PrefabUtility.UnloadPrefabContents(go);}
        foreach(var menu in UnityEngine.Object.FindObjectsByType<GeneralConfigurationMenu>(FindObjectsInactive.Include,FindObjectsSortMode.None))General(menu.transform.root.gameObject);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var routing=EditorSceneManager.OpenScene("Assets/Scenes/LevelRouting.unity",OpenSceneMode.Additive);
        try
        {
            var board=routing.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RouteMapBoard>(true)).Single();
            // The screen-space controls are a separate scene root, not a child of the 3D board.
            var copies=routing.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)).Where(c=>c.name=="Route Controls").ToArray();
            var canvas=board.RandomRouteButton!=null?board.RandomRouteButton.GetComponentInParent<Canvas>():copies.LastOrDefault();
            if(canvas==null)canvas=board.GetComponentInChildren<Canvas>(true);
            foreach(var duplicate in copies.Where(c=>c!=canvas))UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
            if(canvas==null)
            {
                var surface=new GameObject("Route Controls",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(surface,routing);surface.layer=5;
                canvas=surface.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=85;
                var scaler=surface.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            }
            var existing=canvas.transform.Find("Random Route");var button=existing!=null?existing.gameObject:UnityEngine.Object.Instantiate(buttonTemplate,canvas.transform,false);button.name="Random Route";
            button.hideFlags=HideFlags.None;
            if(PrefabUtility.IsPartOfPrefabInstance(button)&&PrefabUtility.GetOutermostPrefabInstanceRoot(button)==button)PrefabUtility.UnpackPrefabInstance(button,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var old=button.GetComponent<UnityEngine.UI.Button>();if(!(old is RouteRandomButton))
            {
                var feedback=button.GetComponent<UiInteractionFeedback>();var feedbackSettings=feedback!=null?new SerializedObject(feedback).FindProperty("settings").objectReferenceValue:null;
                var border=feedback!=null?new SerializedObject(feedback).FindProperty("selectionBorder").objectReferenceValue:null;
                var audio=button.GetComponent<UiAudioFeedback>();if(audio!=null)UnityEngine.Object.DestroyImmediate(audio);
                if(feedback!=null)UnityEngine.Object.DestroyImmediate(feedback);
                UnityEngine.Object.DestroyImmediate(old);old=button.AddComponent<RouteRandomButton>();
                feedback=button.AddComponent<UiInteractionFeedback>();Assign(feedback,"settings",feedbackSettings);Assign(feedback,"selectionBorder",border);
                button.AddComponent<UiAudioFeedback>();
            }
            old.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();old.targetGraphic=button.GetComponent<UnityEngine.UI.Image>();
            var rect=button.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,0);rect.anchoredPosition=new Vector2(38,38);rect.sizeDelta=new Vector2(300,56);
            foreach(var frame in button.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="Focus Frame"||t.name=="Selection Border")){frame.anchorMin=Vector2.zero;frame.anchorMax=Vector2.one;frame.offsetMin=frame.offsetMax=Vector2.zero;}
            var labels=button.GetComponentsInChildren<TMP_Text>(true);var buttonLabel=labels.Last();foreach(var extra in labels.Where(t=>t!=buttonLabel))UnityEngine.Object.DestroyImmediate(extra.gameObject);
            buttonLabel.rectTransform.anchorMin=Vector2.zero;buttonLabel.rectTransform.anchorMax=Vector2.one;buttonLabel.rectTransform.pivot=Vector2.one*.5f;buttonLabel.rectTransform.offsetMin=new Vector2(16,0);buttonLabel.rectTransform.offsetMax=new Vector2(-16,0);
            Text(buttonLabel,"Routing","route.random","Random route");
            Assign(board,"randomRoute",old);MechaLocalizationAuthoring.LocalizeRoot(button);
            EditorSceneManager.MarkSceneDirty(routing);EditorSceneManager.SaveScene(routing);
        }
        finally{EditorSceneManager.CloseScene(routing,true);UnityEngine.Object.DestroyImmediate(buttonTemplate);}
        Localizations();return "General highlight controls and localized controller-accessible Random route button authored";
    }
    static void MoveGun(GameObject visual,Transform source)
    {
        var hand=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Bip001 L Hand");
        var mounts=visual.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Secondary Weapon Aim").ToArray();
        var gun=mounts.FirstOrDefault(t=>t.parent==hand)??mounts.FirstOrDefault();
        if(gun==null){gun=UnityEngine.Object.Instantiate(source.gameObject,hand,false).transform;gun.name="Secondary Weapon Aim";}
        else gun.SetParent(hand,false);
        gun.localPosition=hand.Find("Bip001 L Finger0").localPosition*.5f;gun.localRotation=Quaternion.identity;
        gun.gameObject.SetActive(true);
        // Old added prefab overrides can coexist with the new inherited hand mount.
        foreach(var extra in mounts.Where(t=>t!=gun))extra.gameObject.SetActive(false);
        var binding=visual.GetComponent<CharacterRigBinding>();if(binding!=null){binding.leftArm=hand;binding.gun=gun;}
    }
    public static string WeaponsAndCrates()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/TallgeeseVisual.prefab").GetComponentsInChildren<Transform>(true).First(t=>t.name=="Secondary Weapon Aim");
        foreach(string path in new[]{"Assets/_Project/Prefabs/Characters/TallgeeseVisual.prefab","Assets/_Project/Prefabs/Characters/WingZeroVisual.prefab","Assets/_Project/Prefabs/MechaPlayer.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try{if(path.EndsWith("MechaPlayer.prefab")){foreach(string name in new[]{"Dasher Visual","Zero Visual"})MoveGun(root.transform.Find(name).gameObject,source);}else MoveGun(root,source);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var actor in UnityEngine.Object.FindObjectsByType<CharacterRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(string name in new[]{"Dasher Visual","Zero Visual"})MoveGun(actor.transform.Find(name).gameObject,source);
        var scenario=AssetDatabase.LoadAssetAtPath<ScenarioSettings>("Assets/_Project/Settings/World/Scenario.asset");scenario.propDepth=0;Save(scenario);
        string sceneryPath="Assets/_Project/Prefabs/Assemblies/ArenaScenery.prefab";var scenery=PrefabUtility.LoadPrefabContents(sceneryPath);
        try{foreach(var crate in scenery.GetComponentsInChildren<DestructibleProp>(true)){var p=crate.transform.localPosition;p.z=0;crate.transform.localPosition=p;}PrefabUtility.SaveAsPrefabAsset(scenery,sceneryPath);}
        finally{PrefabUtility.UnloadPrefabContents(scenery);}
        foreach(var crate in UnityEngine.Object.FindObjectsByType<DestructibleProp>(FindObjectsInactive.Include,FindObjectsSortMode.None)){var p=crate.transform.position;p.z=0;crate.transform.position=p;}
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Both SMGs parented to left-hand palm sockets; authored and generated crates use combat depth 0; model meshes unchanged";
    }
}
