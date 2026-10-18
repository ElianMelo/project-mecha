using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MechaCharacterSetup
{
    private const string Temp="Assets/_Project/TEMP_NewPlayer";
    private const string Models="Assets/_Project/Models/Player";
    private const string Animations="Assets/_Project/Animations/Player";
    private const string VisualPrefab="Assets/_Project/Prefabs/Characters/EvaVisual.prefab";
    public static void Apply()
    {
        MechaPresentationSetup.Folder(Animations); MechaPresentationSetup.Folder("Assets/_Project/Prefabs/Characters");
        ExtractAnimations();
        OrganizeEnemySources();
        var playerAnimation=BuildAnimationProfile("PlayerAnimation",true);
        var enemyAnimation=BuildAnimationProfile("EnemyAnimation",false);
        EnsureLocomotionLoops(enemyAnimation); EnsureLocomotionLoops(playerAnimation);
        PreserveOldVisual();
        Move(Temp+"/Mecha@Idle_V2.fbx",Models+"/Mecha.fbx");
        Move(Temp+"/Gundam_Memories_-_OZ-00MS2B_Tallgeese_III_-_Ren.png",Models+"/MechaAlbedo.png");
        MechaPresentationSetup.Folder("Assets/_Project/VFX/Textures");
        Move(Temp+"/GMTK_-_Burst_Wind_FX_-_Ren.png","Assets/_Project/VFX/Textures/SpeedWind.png");
        ConfigurePlayer(playerAnimation); ConfigureEnemy(enemyAnimation);
        Physics.IgnoreLayerCollision(8, 9, false); Physics.IgnoreLayerCollision(9, 9, true);
        EditorUtility.SetDirty(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")[0]);
        ArchiveUnusedSources();
    }
    private static void EnsureLocomotionLoops(CharacterAnimationSettings profile)
    {
        foreach (var state in new[] {"Idle", "Run", "Dash"})
        {
            var clip = profile.Clip(state);
            if (clip == null) continue;
            var data = AnimationUtility.GetAnimationClipSettings(clip);
            data.loopTime = true; data.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip, data);
            clip.wrapMode = WrapMode.Loop; EditorUtility.SetDirty(clip);
        }
    }
    private static void OrganizeEnemySources()
    {
        const string enemy="Assets/_Project/Models/Enemies/Eva";
        MechaPresentationSetup.Folder(enemy);MechaPresentationSetup.Folder("Assets/_Project/Animations/Enemies");
        foreach(string file in new[]{"EvaModel.fbx","EvaMaterial.mat","EvaMaterial.png","EvaMaterialLight.png"})Move(Models+"/"+file,enemy+"/"+file);
        foreach(string clip in new[]{"Idle.anim","Run.anim","Attack.anim"})Move("Assets/_Project/Animations/"+clip,"Assets/_Project/Animations/Enemies/"+clip);
        Move("Assets/_Project/Animations/PlayerAnimator.controller","Assets/_Project/Animations/Enemies/EvaLegacy.controller");
    }
    private static void Move(string source,string destination)
    {
        if(!File.Exists(source) || File.Exists(destination)) return;
        string error=AssetDatabase.MoveAsset(source,destination); if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
    }
    private static void ExtractAnimations()
    {
        foreach(string clipName in new[]{"Idle_V2","Running","Dash","Jump","JumpLoop","Attack_A","Attack_B","Attack_C","Death"})
        {
            string destination=Animations+"/"+clipName+".anim";
            if(AssetDatabase.LoadAssetAtPath<AnimationClip>(destination)!=null)continue;
            var source=AssetDatabase.LoadAllAssetsAtPath(Temp+"/Mecha@"+clipName+".fbx").OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__"));
            if(source==null)throw new InvalidOperationException("Missing supplied clip "+clipName);
            var copy=UnityEngine.Object.Instantiate(source); copy.name=clipName;
            // The rigidbody owns travel. Keep exported root translation fixed, retaining skeletal posing.
            foreach(var binding in AnimationUtility.GetCurveBindings(copy))
                if(binding.path=="Bip001" && binding.propertyName.StartsWith("m_LocalPosition."))
                {
                    var curve=AnimationUtility.GetEditorCurve(copy,binding);
                    AnimationUtility.SetEditorCurve(copy,binding,AnimationCurve.Constant(0f,copy.length,curve.Evaluate(0f)));
                }
            var data=AnimationUtility.GetAnimationClipSettings(copy);
            data.loopTime=clipName=="Idle_V2"||clipName=="Running"||clipName=="JumpLoop";
            AnimationUtility.SetAnimationClipSettings(copy,data);
            AssetDatabase.CreateAsset(copy,destination);
        }
    }
    private static CharacterAnimationSettings BuildAnimationProfile(string name,bool player)
    {
        var profile=MechaPresentationSetup.Create<CharacterAnimationSettings>(name);
        if(profile.controller!=null)return profile;
        profile.runReferenceSpeed=player?4.2f:3.6f;
        profile.dashReferenceSpeed=player?10.5f:3.6f;
        string folder=player?Animations:"Assets/_Project/Animations/Enemies";
        MechaPresentationSetup.Folder(folder);
        AnimationClip Clip(string key)=>AssetDatabase.LoadAssetAtPath<AnimationClip>(Animations+"/"+key+".anim");
        AnimationClip Old(string key)=>AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/Enemies/"+key+".anim");
        var states=new System.Collections.Generic.List<CharacterAnimationSettings.StateClip>();
        void Add(string state,AnimationClip clip)=>states.Add(new CharacterAnimationSettings.StateClip{state=state,clip=clip});
        Add("Idle",player?Clip("Idle_V2"):Old("Idle")); Add("Run",player?Clip("Running"):Old("Run"));
        Add("Dash",player?Clip("Dash"):Old("Run"));Add("Jump",player?Clip("Jump"):Old("Idle"));
        Add("Fall",player?Clip("JumpLoop"):Old("Attack")); Add("Hit",player?Clip("Attack_B"):Old("Attack"));
        Add("Death",player?Clip("Death"):Old("Attack"));Add("Attack",player?Clip("Attack_A"):Old("Attack"));
        foreach(PlayerAttack.ActionKind action in Enum.GetValues(typeof(PlayerAttack.ActionKind)))
        {
            string clip=action==PlayerAttack.ActionKind.Slash2||action==PlayerAttack.ActionKind.SwordFire?"Attack_B":
                action==PlayerAttack.ActionKind.Slash3||action==PlayerAttack.ActionKind.RisingSlash||action==PlayerAttack.ActionKind.CriticalX?"Attack_C":"Attack_A";
            Add(action.ToString(),player?Clip(clip):Old("Attack"));
        }
        var controller=AnimatorController.CreateAnimatorControllerAtPath(folder+"/"+name+".controller");
        controller.AddParameter("PlaybackSpeed",AnimatorControllerParameterType.Float);
        var parameters=controller.parameters;parameters[0].defaultFloat=1f;controller.parameters=parameters;
        foreach(var entry in states)
        {
            if(entry.clip==null)throw new InvalidOperationException("Missing animation "+entry.state);
            var state=controller.layers[0].stateMachine.AddState(entry.state);state.motion=entry.clip;
            state.speedParameter="PlaybackSpeed";state.speedParameterActive=true;
        }
        profile.clips=states.ToArray();profile.controller=controller;EditorUtility.SetDirty(profile);return profile;
    }
    private static void PreserveOldVisual()
    {
        if(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefab)!=null)return;
        var root=PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/MechaPlayer.prefab");
        try
        {
            var serialized=new SerializedObject(root.GetComponent<PlayerMovement>());
            var visual=serialized.FindProperty("visuals").objectReferenceValue as GameObject;
            if(visual==null)throw new InvalidOperationException("Original player visual is missing.");
            PrefabUtility.SaveAsPrefabAsset(visual,VisualPrefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static void ConfigurePlayer(CharacterAnimationSettings animation)
    {
        const string path="Assets/_Project/Prefabs/MechaPlayer.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var existing=root.transform.Find("Player Visual");
            if(existing==null)
            {
                foreach(Transform child in root.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
                var model=AssetDatabase.LoadAssetAtPath<GameObject>(Models+"/Mecha.fbx");
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(model,root.transform); visual.name="Player Visual";
                var look=MechaPresentationSetup.Profile<ToonLookSettings>("ToonLook");
                visual.transform.localPosition=look.playerModelOffset;visual.transform.localRotation=Quaternion.identity;visual.transform.localScale=Vector3.one*look.playerModelScale;
            }
            var visualRoot=root.transform.Find("Player Visual").gameObject;
            var visualLook=MechaPresentationSetup.Profile<ToonLookSettings>("ToonLook");
            visualRoot.transform.localPosition=visualLook.playerModelOffset;visualRoot.transform.localScale=Vector3.one*visualLook.playerModelScale;
            MechaPresentationSetup.Assign(root.GetComponent<PlayerMovement>(),"visuals",visualRoot);
            MechaPresentationSetup.Assign(root.GetComponent<PlayerMovement>(),"settings",MechaPresentationSetup.Profile<PlayerMovementSettings>("PlayerMovement"));
            MechaPresentationSetup.Assign(root.GetComponent<PlayerInputs>(),"settings",MechaPresentationSetup.Profile<PlayerInputSettings>("PlayerInput"));
            MechaPresentationSetup.Assign(root.GetComponent<PlayerAttack>(),"settings",MechaPresentationSetup.Profile<PlayerCombatSettings>("PlayerCombat"));
            MechaPresentationSetup.Assign(root.GetComponent<PlayerHealth>(),"settings",MechaPresentationSetup.Profile<PlayerHealthSettings>("PlayerHealth"));
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<PlayerAwareness>(root),"settings",MechaPresentationSetup.Profile<PlayerAwarenessSettings>("PlayerAwareness"));
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<PlayerEnemyCollision>(root),"settings",MechaPresentationSetup.Create<PlayerBodyCollisionSettings>("PlayerBodyCollision"));
            // An explicitly selected replacement visual owns its retargeted profile.
            // Reapplying presentation must not pair that rig with the original clips.
            var currentPresentation=root.GetComponent<CharacterAnimator>();
            if(currentPresentation!=null)
            {
                var currentProfile=new SerializedObject(currentPresentation).FindProperty("settings").objectReferenceValue as CharacterAnimationSettings;
                if(currentProfile!=null&&currentProfile.controller!=null)animation=currentProfile;
            }
            ConfigurePresentation(root,animation);
            var movement=MechaPresentationSetup.Profile<PlayerMovementSettings>("PlayerMovement");
            var capsule=root.GetComponent<CapsuleCollider>();capsule.height=movement.capsuleHeight;capsule.radius=movement.capsuleRadius;capsule.center=movement.capsuleCenter;
            root.transform.rotation=Quaternion.Euler(0f,90f,0f);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static void ConfigureEnemy(CharacterAnimationSettings animation)
    {
        const string path="Assets/_Project/Prefabs/EvaEnemy.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            if(root.transform.Find("Enemy Visual")==null)
            {
                foreach(Transform child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                foreach(var mesh in root.GetComponents<Renderer>()) UnityEngine.Object.DestroyImmediate(mesh);
                var filter=root.GetComponent<MeshFilter>();if(filter!=null)UnityEngine.Object.DestroyImmediate(filter);
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefab),root.transform);
                visual.name="Enemy Visual";visual.transform.localPosition=Vector3.zero;visual.transform.localRotation=Quaternion.identity;
            }
            MechaPresentationSetup.Assign(root.GetComponent<EnemyController>(),"settings",MechaPresentationSetup.Profile<EnemySettings>("EnemyBehavior"));
            MechaPresentationSetup.Assign(root.GetComponent<EnemyPresentation>(),"settings",MechaPresentationSetup.Profile<EnemySettings>("EnemyBehavior"));
            MechaPresentationSetup.Ensure<EnemyBodyCollider>(root);
            var capsule = root.GetComponent<CapsuleCollider>(); capsule.enabled = true; capsule.isTrigger = false;
            var motor = MechaPresentationSetup.Ensure<EnemyMotor>(root);
            MechaPresentationSetup.Assign(motor,"settings",MechaPresentationSetup.Profile<EnemyTraversalSettings>("EnemyTraversal"));
            var crowd = MechaPresentationSetup.Ensure<EnemyCrowdAvoidance>(root);
            MechaPresentationSetup.Assign(crowd,"settings",MechaPresentationSetup.Profile<EnemyCrowdSettings>("EnemyCrowd"));
            MechaPresentationSetup.Ensure<EnemyPlatformNavigator>(root);
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<EnemyPredictionPlanner>(root),"settings",MechaPresentationSetup.Profile<EnemyPredictionSettings>("EnemyPrediction"));
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<EnemyStateMachine>(root),"settings",MechaPresentationSetup.Profile<EnemyStateSettings>("EnemyStates"));
            ConfigurePresentation(root,animation);
            root.transform.rotation=Quaternion.Euler(0f,-90f,0f);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static void ConfigurePresentation(GameObject root,CharacterAnimationSettings animation)
    {
        var view=MechaPresentationSetup.Ensure<CharacterAnimator>(root);MechaPresentationSetup.Assign(view,"settings",animation);
        var animator=root.GetComponentInChildren<Animator>(true);
        if(animator==null) animator=root.transform.GetChild(0).gameObject.AddComponent<Animator>();
        root.SetActive(true);
        animator.runtimeAnimatorController=animation.controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var net= root.GetComponent<FishNet.Component.Animating.NetworkAnimator>();
        if(net!=null)MechaPresentationSetup.Assign(net,"_animator",animator);
        var feedback=MechaPresentationSetup.Ensure<CharacterHitFeedback>(root);
        MechaPresentationSetup.Assign(feedback,"settings",MechaPresentationSetup.Profile<CombatVfxSettings>("CombatVfx"));
        var vfx=root.GetComponent<PlayerCombatVfx>();
        if(vfx!=null)
        {
            MechaPresentationSetup.Assign(vfx,"settings",MechaPresentationSetup.Profile<CombatVfxSettings>("CombatVfx"));
            MechaPresentationSetup.Assign(vfx,"combat",MechaPresentationSetup.Profile<PlayerCombatSettings>("PlayerCombat"));
        }
        foreach(var collider in root.GetComponentsInChildren<Collider>(true)) if(collider.gameObject!=root)collider.enabled=false;
        foreach(var transform in root.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=root.layer;
    }
    private static void ArchiveUnusedSources()
    {
        if(!Directory.Exists(Temp))return;
        Directory.CreateDirectory("SourceArchive");
        string archive="SourceArchive/NewPlayerUnused-2026-10-14.zip";
        if(File.Exists(archive))throw new InvalidOperationException("Archive already exists while temporary inputs remain; inspect before overwriting.");
        ZipFile.CreateFromDirectory(Temp,archive,System.IO.Compression.CompressionLevel.Optimal,false);
        using(var zip=ZipFile.OpenRead(archive))if(zip.Entries.Count==0)throw new IOException("Empty source archive.");
        if(!AssetDatabase.DeleteAsset(Temp))throw new IOException("Could not remove archived temporary sources.");
    }
}
