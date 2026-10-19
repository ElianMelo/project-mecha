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
    static Material Material(string name,string texture,bool shell=false)
    {
        string path=Vfx+"/Materials/"+name+".mat";Folder(Vfx+"/Materials");var mat=Load<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Mecha/Zero Water"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetTexture("_BaseMap",Load<Texture2D>(texture));mat.SetColor("_Color",new Color(.025f,.22f,.44f,shell?.65f:.85f));mat.SetColor("_EdgeColor",new Color(.58f,1.05f,1.2f));mat.SetFloat("_Fresnel",shell?1f:0f);EditorUtility.SetDirty(mat);return mat;
    }
    static ParticleSystem Layer(GameObject root,string name,Material mat,Vector3 point,int count,float delay,float life,float size,float speed,bool loop=false,float radius=.1f)
    {
        var go=new GameObject(name);go.transform.SetParent(root.transform,false);go.transform.localPosition=point;go.transform.localRotation=Quaternion.Euler(-90,0,0);
        go.layer=CombatVfxRendering.Layer;
        var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=ps.main;m.duration=1f;m.loop=loop;m.playOnAwake=true;m.startDelay=delay;m.startLifetime=new ParticleSystem.MinMaxCurve(life*.85f,life);m.startSpeed=new ParticleSystem.MinMaxCurve(speed*.8f,speed);m.startSize=new ParticleSystem.MinMaxCurve(size*.8f,size);m.startColor=Color.white;m.maxParticles=240;m.simulationSpace=loop?ParticleSystemSimulationSpace.Local:ParticleSystemSimulationSpace.World;m.scalingMode=ParticleSystemScalingMode.Hierarchy;
        m.startRotation=new ParticleSystem.MinMaxCurve(-3.14f,3.14f);
        var e=ps.emission;e.rateOverTime=loop?count:0;if(!loop)e.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
        var sh=ps.shape;sh.enabled=true;sh.shapeType=ParticleSystemShapeType.Cone;sh.radius=radius;sh.angle=25f;
        var color=ps.colorOverLifetime;color.enabled=true;var g=new Gradient();g.SetKeys(new[]{new GradientColorKey(new Color(.2f,.6f,.8f),0),new GradientColorKey(Color.white,.35f),new GradientColorKey(new Color(.1f,.35f,.6f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.1f),new GradientAlphaKey(.8f,.55f),new GradientAlphaKey(0,1)});color.color=g;
        var scale=ps.sizeOverLifetime;scale.enabled=true;scale.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.2f,1),new Keyframe(1,1.5f)));
        var noise=ps.noise;noise.enabled=true;noise.strength=.16f;noise.frequency=1.5f;noise.scrollSpeed=.45f;
        var r=ps.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=mat;r.renderMode=ParticleSystemRenderMode.Billboard;r.sortingFudge=.1f;
        return ps;
    }
    static void Sheet(ParticleSystem ps,int x,int y)
    {var s=ps.textureSheetAnimation;s.enabled=true;s.numTilesX=x;s.numTilesY=y;s.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));s.startFrame=new ParticleSystem.MinMaxCurve(0f,.2f);}
    static WaterEffect Effect(string name,Action<GameObject> build)
    {
        Folder(Vfx+"/Prefabs");var go=new GameObject(name);try{go.AddComponent<WaterEffect>();build(go);MechaRenderSetup.ConfigureSurfaceDepth(go);return PrefabUtility.SaveAsPrefabAsset(go,Vfx+"/Prefabs/"+name+".prefab").GetComponent<WaterEffect>();}finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    static void BuildEffects(WaterBubbleSkill bubble,WaterFountainSkill fountain,WaterModeSkill mode)
    {
        var water=Material("WaterVolume","Assets/Hovl Studio/HSFiles/Textures/Water2.png");
        var foam=Material("Foam","Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr water foam.png");
        var droplets=Material("Droplets","Assets/JMO Assets/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr water small splash hollow stretch.png");
        var shell=Material("PressureShell","Assets/Hovl Studio/HSFiles/Textures/Water2.png",true);
        var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);var mesh=sphere.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(sphere);
        bubble.bubble=Effect("BubblePreparation",go=>{
            var p=Layer(go,"Pressurized shell",shell,Vector3.zero,1,0,.32f,2.1f,0);var sh=p.shape;sh.enabled=false;var r=p.GetComponent<ParticleSystemRenderer>();r.renderMode=ParticleSystemRenderMode.Mesh;r.mesh=mesh;
            var m=p.main;m.startRotation=0;m.startSize=2.1f;var scale=p.sizeOverLifetime;scale.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.1f,1,1));
            Sheet(Layer(go,"Contained rolling water",water,Vector3.zero,5,.02f,.28f,.8f,.4f,false,.45f),7,7);
        });
        bubble.explosion=Effect("BubbleDetonation",go=>{
            Sheet(Layer(go,"Dark liquid body",water,Vector3.zero,9,0,.52f,1.4f,1.8f,false,.35f),7,7);
            var p=Layer(go,"Radial pressure spray",droplets,Vector3.zero,28,.03f,.65f,.42f,4.2f,false,.35f);var s=p.shape;s.shapeType=ParticleSystemShapeType.Sphere;s.radius=.3f;Sheet(p,2,2);var m=p.main;m.gravityModifier=.6f;
            Layer(go,"Foam aftermath",foam,Vector3.zero,14,.12f,.85f,.5f,1.3f,false,.6f);
        });
        fountain.preparation=Effect("FountainPreparation",go=>{for(int i=0;i<4;i++)Layer(go,"Nozzle pressure "+i,foam,new Vector3((i-1.5f)*1.45f,.12f,0),3,0,.3f,.65f,.25f);});
        fountain.fountain=Effect("RisingFountain",go=>{
            for(int i=0;i<4;i++)
            {
                float x=(i-1.5f)*1.45f;float delay=.03f*(i%2);
                var jet=Layer(go,"Rising flow "+i,water,new Vector3(x,.05f,(i%2==0?.2f:-.2f)),10,delay,.62f,1.05f,5f,false,.22f);Sheet(jet,7,7);
                var e=jet.emission;e.SetBursts(new[]{new ParticleSystem.Burst(0,10,4,.22f)});
                var spray=Layer(go,"Crown spray "+i,droplets,new Vector3(x,2.7f,0),8,.42f,.65f,.28f,2f,false,.3f);Sheet(spray,2,2);var m=spray.main;m.gravityModifier=.8f;
                Layer(go,"Turbulent base "+i,foam,new Vector3(x,.12f,0),9,.1f,.9f,.6f,.55f,false,.4f);
            }
        });
        mode.aura=Effect("WaterModeAura",go=>{
            var shellLayer=Layer(go,"Water envelope",shell,new Vector3(0,.85f,0),2,0,.9f,1.8f,0,true,0);
            var shellShape=shellLayer.shape;shellShape.enabled=false;var shellRenderer=shellLayer.GetComponent<ParticleSystemRenderer>();shellRenderer.renderMode=ParticleSystemRenderMode.Mesh;shellRenderer.mesh=mesh;
            var shellMain=shellLayer.main;shellMain.startRotation=0;shellMain.startColor=new Color(1,1,1,.6f);var shellSize=shellLayer.sizeOverLifetime;shellSize.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.85f),new Keyframe(.4f,1),new Keyframe(1,1.08f)));
            var body=Layer(go,"Circulating coolant",water,new Vector3(0,.6f,.25f),9,0,.7f,.7f,.9f,true,.7f);Sheet(body,7,7);var sh=body.shape;sh.shapeType=ParticleSystemShapeType.Donut;sh.radius=.65f;sh.donutRadius=.1f;
            var vel=body.velocityOverLifetime;vel.enabled=true;vel.orbitalY=2.5f;vel.radial=.1f;
            Sheet(Layer(go,"Left recirculation",water,new Vector3(-.65f,.15f,-.1f),8,0,.65f,.6f,1.7f,true,.12f),7,7);
            Sheet(Layer(go,"Right recirculation",water,new Vector3(.65f,.15f,-.1f),8,0,.65f,.6f,1.7f,true,.12f),7,7);
            Layer(go,"Vent foam",foam,new Vector3(0,.2f,0),8,0,.65f,.26f,1.6f,true,.45f);
        });
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
