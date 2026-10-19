using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;

// Explicit authoring through Unity APIs. Shared data is never changed in Play Mode.
public static class BuildMeleeCombat
{
    const string Root="Assets/_Project";
    static T Load<T>(string path) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    static void Folder(string path) => MechaPresentationSetup.Folder(path);
    static T Data<T>(string path) where T:ScriptableObject
    {Folder(Path.GetDirectoryName(path).Replace('\\','/'));var value=Load<T>(path);if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}return value;}
    static void Save(UnityEngine.Object value){EditorUtility.SetDirty(value);AssetDatabase.SaveAssetIfDirty(value);}
    static void Text(string category,string key,string value)
    {MechaLocalizationAuthoring.Capture(category,key,value);var collection=LocalizationEditorSettings.GetStringTableCollection(category);var table=collection.StringTables.Single(t=>t.LocaleIdentifier.Code=="en");table.GetEntry(key).Value=value;Save(table);Save(collection.SharedData);}
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new InvalidOperationException("Preserve unsaved scene changes before authoring");
        var combat=Load<PlayerCombatSettings>(Root+"/Settings/Attacks/PlayerCombat.asset");
        var loadout=combat.combos;
        var lights=new[]{PlayerAttack.ActionKind.Slash1,PlayerAttack.ActionKind.Slash2,PlayerAttack.ActionKind.Slash3}.Select(k=>combat.pieces[combat.IndexOf(k)]).ToArray();
        var heavies=new AttackPiece[3];
        for(int i=0;i<3;i++)
        {
            Configure(lights[i].action,false,i);Save(lights[i]);
            heavies[i]=Data<AttackPiece>(Root+"/Settings/Attacks/Heavy"+(i+1)+".asset");
            heavies[i].displayNameLocalized=MechaLocalizationAuthoring.Capture("Combat","heavy.cut"+(i+1),new[]{"Heavy Cleave","Heavy Return","Heavy Finisher"}[i]);
            Configure(heavies[i].action,true,i);Save(heavies[i]);
        }
        var ground=loadout.groundCombo;ground.attacks=lights;ground.inputWindow=1.5f;ground.loop=true;Save(ground);
        loadout.heavyCombo=Sequence("HeavyGround",ComboDomain.Ground,heavies);
        loadout.heavyAirCombo=Sequence("HeavyAir",ComboDomain.Air,heavies);
        loadout.airCombo.inputWindow=1.5f;Save(loadout.airCombo);
        Save(loadout);
        foreach(string path in new[]{Root+"/Settings/Attacks/PlayerCombat.asset",Root+"/Settings/Characters/ZeroCombat.asset"})
        {
            var profile=Load<PlayerCombatSettings>(path);profile.pieces=profile.pieces.Where(p=>p!=null&&!PlayerAttack.IsHeavy(p.action.kind)).Concat(heavies).ToArray();profile.combos=loadout;profile.attackBuffer=.35f;profile.comboDisplayWindow=1.65f;Save(profile);
        }
        foreach(string name in new[]{"Dasher","Zero"}) ConfigureAnimation(name);
        var hp=Load<PlayerHealthSettings>(Root+"/Settings/Player/PlayerHealth.asset");hp.hitStun=.4f;hp.invincibility=.16f;hp.escapeCapacity=3;hp.escapeRefillSeconds=5f;hp.escapeInvincibility=.25f;hp.staggerDrag=24f;Save(hp);
        Text("HUD","escape.charges","{0} BREAK  {1}/{2}");
        Text("Input","melee.sequence","Mix {0}/{1}: 3-hit combos   •   {2}: stagger break (3 charges / 5s refill)");
        Text("Combat","term.Heavy1","Heavy Cleave");Text("Combat","term.Heavy2","Heavy Return");Text("Combat","term.Heavy3","Heavy Finisher");
        Text("Input","help.heavy","Heavy sword attack. Slower, wider and stronger. Mix with light attacks to choose a combo finisher.");
        var swing=Data<SwordSwingSettings>(Root+"/VFX/Melee/Profiles/SwordSwing.asset");swing.optionalMotion=Load<UiFeelSettings>(Root+"/Settings/Interface/UiFeel.asset");Save(swing);
        BuildSword("Blue",swing);BuildSword("Red",swing);
        foreach(string path in new[]{Root+"/Prefabs/Characters/TallgeeseVisual.prefab",Root+"/Prefabs/Characters/WingZeroVisual.prefab",Root+"/Prefabs/MechaPlayer.prefab"})
        {
            var contents=PrefabUtility.LoadPrefabContents(path);
            try
            {
                if(path.EndsWith("MechaPlayer.prefab"))
                {Attach(contents.transform.Find("Dasher Visual").gameObject,"Red");Attach(contents.transform.Find("Zero Visual").gameObject,"Blue");}
                else Attach(contents,path.Contains("WingZero")?"Blue":"Red");
                PrefabUtility.SaveAsPrefabAsset(contents,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(contents);}
        }
        // Existing scene player visuals have explicit overrides from roster authoring.
        foreach(var character in UnityEngine.Object.FindObjectsByType<CharacterRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {Attach(character.transform.Find("Dasher Visual").gameObject,"Red");Attach(character.transform.Find("Zero Visual").gameObject,"Blue");}
        foreach(var hud in UnityEngine.Object.FindObjectsByType<MechaGameplayHud>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var so=new SerializedObject(hud);var motion=so.FindProperty("motion").objectReferenceValue as TMPro.TMP_Text;
            if(motion!=null){motion.rectTransform.sizeDelta=new Vector2(motion.rectTransform.sizeDelta.x,44f);motion.fontSize=11f;EditorUtility.SetDirty(motion);}
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Shared light/heavy/mixed combos; 3 stagger escapes (5s sequential refill); both rig controllers and right-hand Sword7 variants authored";
    }
    public static string UpgradeFeel()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        foreach(string color in new[]{"Red","Blue"})
        {
            string path=Root+"/Prefabs/Weapons/Sword7"+color+".prefab";var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var source=go.GetComponent<Unity.Cinemachine.CinemachineImpulseSource>()??go.AddComponent<Unity.Cinemachine.CinemachineImpulseSource>();
                source.ImpulseDefinition.ImpulseChannel=2;source.ImpulseDefinition.ImpulseShape=Unity.Cinemachine.CinemachineImpulseDefinition.ImpulseShapes.Bump;
                source.ImpulseDefinition.ImpulseType=Unity.Cinemachine.CinemachineImpulseDefinition.ImpulseTypes.Uniform;source.ImpulseDefinition.ImpulseDuration=.12f;
                MechaPresentationSetup.Assign(go.GetComponent<SwordSwingPresentation>(),"impactSource",source);PrefabUtility.SaveAsPrefabAsset(go,path);
            }finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        foreach(var camera in UnityEngine.Object.FindObjectsByType<RoomCameraRig>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var listener=camera.GetComponent<Unity.Cinemachine.CinemachineImpulseListener>()??camera.gameObject.AddComponent<Unity.Cinemachine.CinemachineImpulseListener>();
            listener.ChannelMask=2;listener.Gain=1f;listener.Use2DDistance=true;EditorUtility.SetDirty(listener);
        }
        Text("Input","melee.sequence","Mix {0}/{1}: 3-hit combos   •   {2}: stagger break (3 charges / 5s refill)");
        Text("Input","help.heavy","Heavy sword attack. Slower, wider and stronger. Mix with light attacks to choose a combo finisher.");
        var collection=LocalizationEditorSettings.GetStringTableCollection("Input");var table=collection.StringTables.Single(t=>t.LocaleIdentifier.Code=="en");
        var unused=collection.SharedData.GetEntry("tech.description");if(unused!=null){table.RemoveEntry(unused.Id);collection.SharedData.RemoveKey(unused.Id);Save(table);Save(collection.SharedData);}
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        return "Native Cinemachine impact channel 2; optional motion respected; current heavy help and concise combo tutorial";
    }
    static ComboDefinition Sequence(string name,ComboDomain domain,AttackPiece[] pieces)
    {var data=Data<ComboDefinition>(Root+"/Settings/Combos/"+name+".asset");data.domain=domain;data.attacks=pieces;data.inputWindow=1.5f;data.loop=true;data.displayNameLocalized=MechaLocalizationAuthoring.Capture("Combat",domain==ComboDomain.Ground?"combo.heavy":"combo.heavy.air",domain==ComboDomain.Ground?"Heavy Sword Combo":"Air Heavy Sword Combo");Save(data);return data;}
    static void Configure(PlayerCombatSettings.ActionData a,bool heavy,int i)
    {
        a.kind=(PlayerAttack.ActionKind)((heavy?(int)PlayerAttack.ActionKind.Heavy1:0)+i);
        a.duration=heavy?new[]{.72f,.8f,.96f}[i]:new[]{.42f,.46f,.56f}[i];
        a.impact=heavy?new[]{.3f,.34f,.42f}[i]:new[]{.13f,.15f,.19f}[i];
        a.cancel=a.duration-(heavy?.1f:.08f);a.pulses=1;a.damage=a.finalDamage=heavy?new[]{15f,18f,24f}[i]:new[]{7f,8f,11f}[i];
        a.stun=heavy?.58f:.4f;a.push=heavy?(i==2?5f:2.5f):(i==2?3f:1.1f);a.lift=heavy?4f:0f;
        a.launcher=heavy&&i==2;a.finalPulseLaunch=false;a.requiresGround=false;a.airOnly=false;a.sideSwitch=a.pull=false;
        a.stepSpeed=heavy?3.6f:2.6f;a.stepDuration=heavy?.1f:.08f;a.manaCost=a.cooldown=0f;
        a.hitOrigin=new Vector2(.12f,.85f);a.hitFromHand=false;a.hitLength=heavy?1.85f:1.15f;
        a.hitRadius=heavy?.75f:.5f;a.hitActiveTime=heavy?.17f:.11f;a.hitTravelTime=0f;a.hitFirstAngle=i==1?-12f:8f;a.hitAngleStep=0f;
        a.reach=heavy?1.2f:.85f;a.height=.85f;a.hitHalfSize=new Vector3(heavy?1.3f:.85f,heavy?1f:.7f,.65f);
    }
    static void ConfigureAnimation(string character)
    {
        var profile=Load<CharacterAnimationSettings>(Root+"/Settings/Animation/"+(character=="Dasher"?"PlayerAnimation":"WingZeroAnimation")+".asset");
        var controller=profile.controller as AnimatorController;var machine=controller.layers[0].stateMachine;var entries=profile.clips.ToList();
        var mappings=new Dictionary<string,string>{{"Slash1","Light1"},{"Slash2","Light2"},{"Slash3","Light3"},{"Heavy1","Heavy1"},{"Heavy2","Heavy2"},{"Heavy3","Heavy3"},{"AirSlash","Light2"},{"DashSlash","Light1"},{"RisingSlash","Heavy3"},{"DiveStrike","Heavy2"},{"CriticalStrike","Heavy1"}};
        foreach(var pair in mappings)
        {
            string series=pair.Value.StartsWith("Light")?"A":"B";char number=pair.Value[pair.Value.Length-1];
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/DoubleL/FBX Unity/One Hand Up/Attack_"+series+"/InPlace/1Hand_Up_Attack_"+series+"_"+number+"_InPlace.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
            if(!clip.humanMotion)throw new InvalidOperationException("Use native Humanoid source clips");
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==pair.Key)??machine.AddState(pair.Key);
            state.motion=clip;state.speedParameter="PlaybackSpeed";state.speedParameterActive=true;Save(state);
            entries.RemoveAll(e=>e.state==pair.Key);entries.Add(new CharacterAnimationSettings.StateClip{state=pair.Key,clip=clip});
        }
        profile.clips=entries.ToArray();Save(profile);Save(controller);
    }
    static void BuildSword(string color,SwordSwingSettings settings)
    {
        // Subsequent iteration uses the authored prefab; never overwrite its lights/effect attachments.
        if(Load<GameObject>(Root+"/Prefabs/Weapons/Sword7"+color+".prefab")!=null)return;
        Folder(Root+"/Prefabs/Weapons");Folder(Root+"/VFX/Melee/Materials");
        string basePath="Assets/Blink/Art/Weapons/LowPoly/FreeSwords/Sword7/";
        string materialPath=Root+"/VFX/Melee/Materials/Sword7"+color+".mat";
        var material=Load<Material>(materialPath);
        if(material==null){material=UnityEngine.Object.Instantiate(Load<Material>(Root+"/Materials/MechaBlade.mat"));AssetDatabase.CreateAsset(material,materialPath);}
        var texture=Load<Texture2D>(basePath+"Textures/Sword7_Albedo_"+color+".png");
        foreach(string property in new[]{"_MainTex","_BaseMap","_1st_ShadeMap","_2nd_ShadeMap"})if(material.HasProperty(property))material.SetTexture(property,texture);
        foreach(string property in new[]{"_Color","_BaseColor","_1st_ShadeColor","_2nd_ShadeColor"})if(material.HasProperty(property))material.SetColor(property,Color.white);
        var supplied=Load<Material>(basePath+"Sword7_Material_"+color+".mat");material.SetTexture("_Emissive_Tex",supplied.GetTexture("_EmissionMap"));material.SetColor("_Emissive_Color",supplied.GetColor("_EmissionColor")*1.8f);material.EnableKeyword("_EMISSIVE_SIMPLE");
        Save(material);
        string trailPath=Root+"/VFX/Melee/Materials/BladeTrail"+color+".mat";
        var trailMaterial=Load<Material>(trailPath);
        if(trailMaterial==null){trailMaterial=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(trailMaterial,trailPath);}
        trailMaterial.SetTexture("_BaseMap",Load<Texture2D>(Root+"/VFX/Textures/SpeedWind.png"));
        trailMaterial.SetFloat("_Surface",1f);trailMaterial.SetFloat("_Blend",0f);trailMaterial.SetFloat("_SrcBlend",5f);trailMaterial.SetFloat("_DstBlend",10f);trailMaterial.SetFloat("_ZWrite",0f);trailMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");trailMaterial.renderQueue=3000;
        trailMaterial.SetColor("_BaseColor",color=="Blue"?new Color(.18f,.7f,1.6f,.7f):new Color(1.6f,.2f,.13f,.7f));Save(trailMaterial);
        var sword=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>("Assets/Blink/Art/Weapons/LowPoly/FreeSwords/Prefabs/Sword7_"+color+".prefab"));
        try
        {
            PrefabUtility.UnpackPrefabInstance(sword,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            sword.name="Sword7"+color;
            foreach(var r in sword.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
            foreach(var collider in sword.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
            var tip=new GameObject("Blade Tip");tip.layer=CombatVfxRendering.Layer;tip.transform.SetParent(sword.transform,false);tip.transform.localPosition=new Vector3(0,1.45f,0);
            var trail=tip.AddComponent<TrailRenderer>();trail.sharedMaterial=trailMaterial;trail.time=.1f;trail.widthMultiplier=.22f;trail.minVertexDistance=.035f;trail.emitting=false;trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;trail.receiveShadows=false;
            trail.widthCurve=AnimationCurve.Linear(0f,1f,1f,0f);trail.numCornerVertices=2;trail.numCapVertices=2;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.75f,0),new GradientAlphaKey(0,1)});trail.colorGradient=gradient;
            var presentation=sword.AddComponent<SwordSwingPresentation>();MechaPresentationSetup.Assign(presentation,"settings",settings);MechaPresentationSetup.Assign(presentation,"bladeTrail",trail);
            PrefabUtility.SaveAsPrefabAsset(sword,Root+"/Prefabs/Weapons/Sword7"+color+".prefab");
        }
        finally{UnityEngine.Object.DestroyImmediate(sword);}
    }
    static void Attach(GameObject visual,string color)
    {
        var hand=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Bip001 R Hand");
        var existing=hand.Find("Sword7");if(existing!=null)return;
        var sword=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Root+"/Prefabs/Weapons/Sword7"+color+".prefab"),hand);
        sword.name="Sword7";sword.transform.localPosition=new Vector3(-.06f,0,0);sword.transform.localRotation=Quaternion.Euler(0f,0f,90f);sword.transform.localScale=Vector3.one*(color=="Blue"?.95f:.85f);
        foreach(var t in sword.GetComponentsInChildren<Transform>(true))if(t.GetComponent<TrailRenderer>()==null)t.gameObject.layer=visual.layer;
    }
}
