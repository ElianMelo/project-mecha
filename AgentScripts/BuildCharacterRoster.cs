using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Events;
using UnityEditor.Events;
using TMPro;
using UnityEngine.InputSystem;

// Explicit, repeatable authoring through Pipeline. Never runs automatically on import.
public static class BuildCharacterRoster
{
    public static string InspectPortraits()
    {
        var player=PrefabUtility.LoadPrefabContents(Player);var result="";
        try{foreach(var name in new[]{"Dasher Visual","Zero Visual"}){var v=player.transform.Find(name);v.gameObject.SetActive(true);var definition=Load<CharacterDefinition>(Root+"/"+(name.StartsWith("Dasher")?"Dasher":"Zero")+".asset");definition.animation.Clip("Idle").SampleAnimation(v.gameObject,.3f);foreach(var r in v.GetComponentsInChildren<Renderer>()){result+=name+" "+r.name+" bounds "+r.bounds+" scale "+r.transform.lossyScale+"\n";if(r is SkinnedMeshRenderer sk){var mesh=new Mesh();sk.BakeMesh(mesh);result+="baked "+mesh.bounds+" sample "+sk.transform.TransformPoint(mesh.bounds.center)+"\n";UnityEngine.Object.DestroyImmediate(mesh);}}}return result;}finally{PrefabUtility.UnloadPrefabContents(player);}
    }
    const string Root = "Assets/_Project/Settings/Characters";
    const string Vfx = "Assets/_Project/VFX/ZeroWater";
    const string Player = "Assets/_Project/Prefabs/MechaPlayer.prefab";
    static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    static void Folder(string path) => MechaPresentationSetup.Folder(path);
    static T Asset<T>(string path) where T : ScriptableObject
    { Folder(Path.GetDirectoryName(path).Replace('\\','/')); var value=Load<T>(path); if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}return value; }
    static void Set(UnityEngine.Component owner,string field,UnityEngine.Object value) => MechaPresentationSetup.Assign(owner,field,value);
    static void Array(UnityEngine.Object owner,string field,UnityEngine.Object[] values)
    {var so=new SerializedObject(owner);var p=so.FindProperty(field);p.arraySize=values.Length;for(int i=0;i<values.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];so.ApplyModifiedPropertiesWithoutUndo();}
    static LocalizedString Text(string key,string english) => MechaLocalizationAuthoring.Capture("Combat",key,english);
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Preserve unsaved scene edits before authoring.");
        BuildDirectionalBindings();
        var sourceCombat=Load<PlayerCombatSettings>("Assets/_Project/Settings/Attacks/PlayerCombat.asset");
        Text("mode.remaining","ACTIVE  /  {0:0.0}s");
        MechaLocalizationAuthoring.Capture("Input","zero.traversal","{0} + direction: directional dash   •   Tap {1}: short hop / hold: full jump");
        var dasher=Asset<CharacterDefinition>(Root+"/Dasher.asset");var zero=Asset<CharacterDefinition>(Root+"/Zero.asset");
        dasher.id="dasher";dasher.displayName=Text("character.dasher","Dasher");
        dasher.description=Text("dasher.overview","DASHER  /  IMPULSE FRAME\n\nWalk, dash-run and chained air dashes.\nRocket and Mushidon impulse techniques.\n\nA  Thermal Burst\nS  Sword Fire\nD  Critical Cross\n\nShared light / heavy melee combos and ranged fire.");
        dasher.movement=Load<PlayerMovementSettings>("Assets/_Project/Settings/Player/PlayerMovement.asset");dasher.combat=sourceCombat;
        dasher.animation=Load<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/PlayerAnimation.asset");
        dasher.visualPrefab=Load<GameObject>("Assets/_Project/Prefabs/Characters/TallgeeseVisual.prefab");
        zero.id="zero";zero.displayName=Text("character.zero","Zero");
        zero.description=Text("zero.overview","ZERO  /  HYDRAULIC FRAME\n\nRun by default. Responsive air control.\nRB / Shift + direction: eight-direction dash.\nTap jump for a short hop; hold for full height.\n\nA  Bubble Burst  /  pressurized water detonation\nS  Rising Fountain  /  surrounding water pillars\nD  Water Mode  /  faster movement and attacks\n\nShared light / heavy melee combos and ranged fire.");
        var motion=Asset<PlayerMovementSettings>(Root+"/ZeroMovement.asset");EditorUtility.CopySerialized(dasher.movement,motion);motion.name="ZeroMovement";
        motion.style=PlayerMovementSettings.MovementStyle.Agile;motion.runByDefault=motion.directionalDash=motion.variableJump=true;
        motion.walkSpeed=motion.runSpeed=7.225f;motion.dashSpeed=dasher.movement.dashSpeed;motion.dashDuration=dasher.movement.dashDuration;
        motion.dashCooldown=.12f;motion.groundAcceleration=125f;motion.airAcceleration=85f;motion.slideDrag=100f;
        motion.jumpSpeed=11.4f;motion.coyoteTime=.12f;motion.jumpBuffer=.15f;motion.jumpCutGravity=68f;motion.fallingGravity=44f;
        zero.movement=motion;zero.animation=Load<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/WingZeroAnimation.asset");zero.visualPrefab=Load<GameObject>("Assets/_Project/Prefabs/Characters/WingZeroVisual.prefab");
        zero.visualScale=Load<WingZeroRigSettings>(Root+"/ZeroRig.asset")?.visualScale??.5f;
        zero.animation.runReferenceSpeed=motion.runSpeed;EditorUtility.SetDirty(zero.animation);
        dasher.visualScale=.65f;
        var combat=Asset<PlayerCombatSettings>(Root+"/ZeroCombat.asset");EditorUtility.CopySerialized(sourceCombat,combat);combat.name="ZeroCombat";
        zero.combat=combat;combat.pieces=(AttackPiece[])sourceCombat.pieces.Clone();
        string[] names={"Bubble Burst","Rising Fountain","Water Mode"};
        string[] keys={"bubble.burst","rising.fountain","water.mode"};
        var definitions=new CharacterSkillDefinition[3];
        for(int i=0;i<3;i++)
        {
            var fire=Asset<FireCharacterSkillDefinition>(Root+"/DasherSkill"+i+".asset");fire.action=sourceCombat.pieces[sourceCombat.IndexOf((PlayerAttack.ActionKind)(i+5))];fire.displayName=fire.action.displayNameLocalized;dasher.skills[i]=fire;EditorUtility.SetDirty(fire);
            var piece=Asset<AttackPiece>(Vfx+"/Profiles/"+names[i].Replace(" ","")+"Action.asset");EditorUtility.CopySerialized(fire.action,piece);piece.name=names[i].Replace(" ","")+"Action";
            piece.displayNameLocalized=Text(keys[i],names[i]);var a=piece.action;
            a.hitLength=a.hitActiveTime=a.hitTravelTime=0f;a.hitFromHand=false;a.sideSwitch=a.pull=a.airOnly=a.requiresGround=false;a.stepSpeed=0f;a.pulseAdvance=0f;
            a.duration=i==0?.75f:i==1?1.25f:.5f;a.impact=i==0?.32f:i==1?.28f:.2f;a.cancel=a.duration;a.pulses=i==1?4:1;a.pulseInterval=.22f;
            a.manaCost=i==0?25f:i==1?40f:60f;a.cooldown=i==0?2f:i==1?5f:14f;
            a.reach=i==0?1.9f:0f;a.height=i==1?1.5f:.95f;a.hitHalfSize=i==0?new Vector3(1.2f,1.2f,.85f):new Vector3(2.8f,1.65f,.9f);
            a.damage=a.finalDamage=i==0?24f:i==1?8f:0f;a.launcher=i==1;a.finalPulseLaunch=false;a.lift=i==1?4f:0f;a.push=i==0?3f:0f;a.stun=.22f;
            int index=combat.IndexOf(a.kind);combat.pieces[index]=piece;EditorUtility.SetDirty(piece);
            CharacterSkillDefinition skill=i==0?(CharacterSkillDefinition)Asset<WaterBubbleSkill>(Vfx+"/Profiles/BubbleBurst.asset"):i==1?Asset<WaterFountainSkill>(Vfx+"/Profiles/RisingFountain.asset"):Asset<WaterModeSkill>(Vfx+"/Profiles/WaterMode.asset");
            skill.action=piece;skill.displayName=piece.displayNameLocalized;definitions[i]=skill;
        }
        zero.skills=definitions;dasher.capabilities=zero.capabilities=CombatCapabilities.All;
        BuildEffects((WaterBubbleSkill)definitions[0],(WaterFountainSkill)definitions[1],(WaterModeSkill)definitions[2]);
        var catalog=Asset<CharacterCatalog>(Root+"/Roster.asset");catalog.characters=new[]{dasher,zero};
        BuildPlayer(catalog);
        Portrait(dasher,0);Portrait(zero,1);
        foreach(var a in new UnityEngine.Object[]{dasher,zero,motion,combat,catalog,definitions[0],definitions[1],definitions[2]})EditorUtility.SetDirty(a);
        BuildMenu(catalog);
        AssetDatabase.SaveAssets();return "Authored Dasher/Zero profiles, dual rigs, portraits, water effects and selection screen.";
    }
    static void BuildDirectionalBindings()
    {
        const string path="Assets/_Project/Inputs/InputSystem_Actions.inputactions";var actions=Load<InputActionAsset>(path);var move=actions.FindAction("Player/Move",true);
        if(move.bindings.Any(b=>b.isPartOfComposite&&b.name.Equals("Up",StringComparison.OrdinalIgnoreCase)))return;
        move.ChangeBinding(0).InsertPartBinding("Up","<Keyboard>/upArrow").WithGroups("Keyboard&Mouse");
        move.ChangeBinding(0).InsertPartBinding("Down","<Keyboard>/downArrow").WithGroups("Keyboard&Mouse");
        File.WriteAllText(path,actions.ToJson());AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
    }
    static void BuildPlayer(CharacterCatalog catalog)
    {
        var root=PrefabUtility.LoadPrefabContents(Player);
        try
        {
            var runtime=MechaPresentationSetup.Ensure<CharacterRuntime>(root);var rigs=new CharacterRigBinding[2];
            var original=root.transform.Find("Player Visual");
            for(int i=0;i<2;i++)
            {
                var character=catalog.At(i);var existing=root.transform.Find(i==0?"Dasher Visual":"Zero Visual");
                GameObject visual;
                if(existing!=null)visual=existing.gameObject;
                else if(i==1&&original!=null) {visual=original.gameObject;visual.name="Zero Visual";}
                else {visual=(GameObject)PrefabUtility.InstantiatePrefab(character.visualPrefab,root.transform);visual.name=i==0?"Dasher Visual":"Zero Visual";}
                var transforms=visual.GetComponentsInChildren<Transform>(true);
                visual.transform.localScale=Vector3.one*character.visualScale;
                var arm=transforms.First(t=>t.name=="Bip001 L Hand");
                var gun=transforms.FirstOrDefault(t=>t.name=="Secondary Weapon Aim");
                if(gun==null)
                {
                    var source=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Secondary Weapon Aim");gun=UnityEngine.Object.Instantiate(source.gameObject,arm).transform;gun.name="Secondary Weapon Aim";
                }
                var rig=MechaPresentationSetup.Ensure<CharacterRigBinding>(visual);rig.animator=visual.GetComponent<Animator>();rig.leftArm=arm;rig.gun=gun;
                gun.SetParent(arm,false);gun.localPosition=arm.Find("Bip001 L Finger0").localPosition*.5f;
                rig.skillHand=transforms.First(t=>t.name=="Bip001 R Hand");rig.animator.runtimeAnimatorController=character.animation.controller;rig.animator.applyRootMotion=false;
                foreach(var t in visual.GetComponentsInChildren<Transform>(true))t.gameObject.layer=root.layer;
                rigs[i]=rig;visual.SetActive(i==0);
            }
            Set(runtime,"catalog",catalog);Array(runtime,"rigs",rigs);
            MechaPresentationSetup.Ensure<CharacterModifiers>(root);MechaPresentationSetup.Ensure<CharacterSkills>(root);
            Set(root.GetComponent<PlayerMovement>(),"settings",catalog.At(0).movement);Set(root.GetComponent<PlayerMovement>(),"visuals",rigs[0].gameObject);
            Set(root.GetComponent<CharacterAnimator>(),"settings",catalog.At(0).animation);
            Set(root.GetComponent<FishNet.Component.Animating.NetworkAnimator>(),"_animator",rigs[0].animator);
            Set(root.GetComponent<CombatEmissionOrigin>(),"skillHand",rigs[0].skillHand);
            Set(root.GetComponent<MechaAutoAim>(),"leftArm",rigs[0].leftArm);
            Set(root.GetComponent<SecondaryWeaponPresentation>(),"leftArm",rigs[0].leftArm);Set(root.GetComponent<SecondaryWeaponPresentation>(),"gun",rigs[0].gun);
            PrefabUtility.SaveAsPrefabAsset(root,Player);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void BuildEffects(WaterBubbleSkill bubble,WaterFountainSkill fountain,WaterModeSkill mode)
    {
        WaterAnimeAuthoring.ApplyAll();
        bubble.bubble=Load<WaterEffect>(Vfx+"/Prefabs/BubblePreparation.prefab");
        bubble.explosion=Load<WaterEffect>(Vfx+"/Prefabs/BubbleDetonation.prefab");
        fountain.preparation=Load<WaterEffect>(Vfx+"/Prefabs/FountainPreparation.prefab");
        fountain.fountain=Load<WaterEffect>(Vfx+"/Prefabs/RisingFountain.prefab");
        mode.aura=Load<WaterEffect>(Vfx+"/Prefabs/WaterModeAura.prefab");
    }
    static void Portrait(CharacterDefinition definition,int index)
    {
        Folder("Assets/_Project/Art/Characters");string path="Assets/_Project/Art/Characters/"+definition.name+"Portrait.png";
        // Refresh these actual-model captures while retaining their imported GUIDs.
        {
            var player=PrefabUtility.LoadPrefabContents(Player);var visual=player.GetComponent<CharacterRuntime>();
            var binding=player.transform.Find(index==0?"Dasher Visual":"Zero Visual").GetComponent<CharacterRigBinding>();
            var go=UnityEngine.Object.Instantiate(binding.gameObject);go.SetActive(true);go.transform.SetParent(null);go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.Euler(0,155,0);
            var preview=new PreviewRenderUtility();
            try
            {
                definition.animation.Clip("Idle").SampleAnimation(go,.3f);preview.AddSingleGO(go);
                // Shared world-space framing avoids import-scale-dependent baked bounds.
                preview.camera.orthographic=true;preview.camera.aspect=640f/720f;preview.camera.orthographicSize=index==0?.95f:1.75f;preview.camera.transform.position=new Vector3(0,index==0?.6f:.8f,-8);preview.camera.transform.rotation=Quaternion.identity;
                preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=30;preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.035f,.055f,.08f,1);
                preview.lights[0].intensity=1.5f;preview.lights[0].transform.rotation=Quaternion.Euler(40,30,0);preview.lights[1].intensity=.7f;
                preview.BeginPreview(new Rect(0,0,640,720),GUIStyle.none);preview.camera.Render();var rt=preview.EndPreview() as RenderTexture;
                var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(640,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,720),0,0);image.Apply();RenderTexture.active=previous;
                File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            }
            finally{preview.Cleanup();UnityEngine.Object.DestroyImmediate(go);PrefabUtility.UnloadPrefabContents(player);}
            AssetDatabase.ImportAsset(path);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();definition.portrait=Load<Sprite>(path);
    }
    public static string RefreshPortraits()
    {
        var dasher=Load<CharacterDefinition>(Root+"/Dasher.asset");var zero=Load<CharacterDefinition>(Root+"/Zero.asset");
        Portrait(dasher,0);Portrait(zero,1);EditorUtility.SetDirty(dasher);EditorUtility.SetDirty(zero);AssetDatabase.SaveAssets();return "Updated actual-model portraits with retained GUIDs.";
    }
    static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max)
    {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
    static UnityEngine.UI.Image Image(RectTransform rect,Color color,bool raycast=false)
    {var i=rect.gameObject.AddComponent<UnityEngine.UI.Image>();i.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");i.type=UnityEngine.UI.Image.Type.Sliced;i.color=color;i.raycastTarget=raycast;return i;}
    static TMP_Text Label(Transform parent,string name,LocalizedString reference,Vector2 min,Vector2 max,float size)
    {
        var t=Rect(parent,name,min,max).gameObject.AddComponent<TextMeshProUGUI>();t.font=Load<TMP_FontAsset>("Assets/_Project/Localization/Fonts/EnglishDefault.asset");t.fontSize=size;t.color=new Color(.88f,.95f,1);t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;
        if(reference!=null){var l=t.gameObject.AddComponent<LocalizeStringEvent>();l.StringReference=reference;UnityEventTools.AddPersistentListener(l.OnUpdateString,(UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>),t,"set_text"));l.OnUpdateString.SetPersistentListenerState(0,UnityEventCallState.EditorAndRuntime);l.RefreshString();}GameText.BindFont(t);return t;
    }
    static UnityEngine.UI.Button Button(Transform parent,string name,LocalizedString text,Vector2 min,Vector2 max,UiFeelSettings style)
    {
        var r=Rect(parent,name,min,max);var im=Image(r,new Color(.07f,.16f,.22f),true);var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();b.targetGraphic=im;
        if(text!=null)Label(r,"Label",text,Vector2.zero,Vector2.one,20);
        MechaUiFeelSetup.Button(b,style);r.gameObject.AddComponent<UiAudioFeedback>();return b;
    }
    static void BuildMenu(CharacterCatalog catalog)
    {
        var manager=UnityEngine.Object.FindFirstObjectByType<GameManager>();
        var old=UnityEngine.Object.FindFirstObjectByType<CharacterSelectionMenu>(FindObjectsInactive.Include);
        if(old!=null){old.GetComponent<Canvas>().sortingOrder=1000;EditorSceneManager.MarkSceneDirty(old.gameObject.scene);EditorSceneManager.SaveScene(old.gameObject.scene);return;}
        var go=new GameObject("Character Selection",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
        var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var menu=go.AddComponent<CharacterSelectionMenu>();var panel=Rect(go.transform,"Selection Panel",Vector2.zero,Vector2.one);Image(panel,new Color(.018f,.03f,.05f,.99f),true);panel.gameObject.AddComponent<UiNavigationScope>();
        var style=Load<UiFeelSettings>("Assets/_Project/Settings/Interface/UiFeel.asset");
        Label(panel,"Title",Text("selection.title","SELECT YOUR MECHA"),new Vector2(.06f,.86f),new Vector2(.94f,.95f),34);
        Label(panel,"Hint",Text("selection.hint","Choose a frame, then confirm deployment.  /  Back returns to the menu."),new Vector2(.06f,.80f),new Vector2(.94f,.86f),16);
        var cards=new UnityEngine.UI.Button[2];var borders=new UnityEngine.UI.Image[2];
        for(int i=0;i<2;i++)
        {
            float x=.055f+i*.235f;var card=Button(panel,catalog.At(i).name+" Card",null,new Vector2(x,.22f),new Vector2(x+.215f,.77f),style);cards[i]=card;
            var photo=Image(Rect(card.transform,"Model Portrait",new Vector2(.025f,.15f),new Vector2(.975f,.98f)),Color.white);photo.sprite=catalog.At(i).portrait;photo.type=UnityEngine.UI.Image.Type.Simple;photo.preserveAspect=true;
            Label(card.transform,"Mecha Name",catalog.At(i).displayName,new Vector2(.05f,.01f),new Vector2(.95f,.14f),26);
            var selected=Image(Rect(card.transform,"Chosen Frame",new Vector2(0,0),new Vector2(1,1)),new Color(1,.63f,.25f));selected.fillCenter=false;borders[i]=selected;
        }
        var selectedName=Label(panel,"Selected Mecha",null,new Vector2(.57f,.68f),new Vector2(.94f,.77f),30);selectedName.alignment=TextAlignmentOptions.Left;
        var details=Label(panel,"Character Details",null,new Vector2(.57f,.23f),new Vector2(.94f,.68f),18);details.alignment=TextAlignmentOptions.TopLeft;
        var confirm=Button(panel,"Confirm",Text("selection.confirm","DEPLOY SELECTED MECHA"),new Vector2(.55f,.075f),new Vector2(.94f,.17f),style);
        var cancel=Button(panel,"Back",Text("selection.back","BACK"),new Vector2(.055f,.075f),new Vector2(.27f,.17f),style);
        Set(menu,"catalog",catalog);Set(menu,"panel",panel.gameObject);Array(menu,"cards",cards);Array(menu,"chosenBorders",borders);Set(menu,"confirm",confirm);Set(menu,"cancel",cancel);Set(menu,"details",details);Set(menu,"selectedName",selectedName);
        MechaLocalizationAuthoring.LocalizeRoot(go);panel.gameObject.SetActive(false);
        var scene=manager.gameObject.scene;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
