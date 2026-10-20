using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor.Animations;
using System.Collections.Generic;

// Operator-invoked native Humanoid authoring. No runtime retargeting or generated motion curves.
public static class HumanoidRigAuthoring
{
    static readonly Type Setup=typeof(Editor).Assembly.GetType("UnityEditor.AvatarSetupTool");
    static readonly BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
    static Avatar NativeAvatar(GameObject go,string name)
    {
        object[] args={go,new HumanBone[0],new SkeletonBone[0],false};
        Setup.GetMethod("SetupHumanSkeleton",Flags).Invoke(null,args);
        var description=new HumanDescription{human=(HumanBone[])args[1],skeleton=(SkeletonBone[])args[2],hasTranslationDoF=(bool)args[3],upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f};
        var avatar=AvatarBuilder.BuildHumanAvatar(go,description);avatar.name=name;
        if(!avatar.isValid||!avatar.isHuman)throw new InvalidOperationException(name+" is not a valid native Humanoid");
        return avatar;
    }
    static AnimationClip Clip(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
    public static string InspectReactions()
    {
        string result="";
        foreach(string name in new[]{"4","5"})
        {
            string path="Assets/DoubleL/FBX Unity/Hit/InPlace/Hit_B_"+name+"_InPlace.fbx";var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));var a=go.GetComponent<Animator>();if(a==null)a=go.AddComponent<Animator>();a.avatar=((ModelImporter)AssetImporter.GetAtPath(path)).sourceAvatar;var graph=PlayableGraph.Create("Reaction inspection");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            try{var clip=Clip(path);var p=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"Pose",a);output.SetSourcePlayable(p);graph.Play();result+="Hit"+name+" human="+clip.humanMotion+" length="+clip.length+" ";foreach(float f in new[]{0f,.5f,.95f}){p.SetTime(clip.length*f);graph.Evaluate(0);result+=f+":"+a.bodyPosition+" ";}result+="\n";}
            finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(go);}
        }
        return result;
    }
    public static string NativeLocomotion()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        var replacements=new Dictionary<string,AnimationClip>();
        replacements["Idle"]=Clip("Assets/DoubleL/FBX Unity/One Hand Up/Movement/Idle/Idle/1Hand_Up_Stand_Idle_A_2.fbx");
        replacements["Run"]=Clip("Assets/DoubleL/FBX Unity/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace.fbx");replacements["Dash"]=replacements["Run"];
        const string knight="Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/";
        replacements["Jump"]=Clip(knight+"Knight@Jump.FBX");replacements["Fall"]=Clip(knight+"Knight@Fall.FBX");
        replacements["Hit"]=Clip("Assets/DoubleL/FBX Unity/Hit/InPlace/Hit_B_4_InPlace.fbx");
        replacements["Attack"]=Clip("Assets/DoubleL/FBX Unity/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx");replacements["MegaSlash"]=replacements["Attack"];
        replacements["SwordFire"]=replacements["Idle"];replacements["CriticalX"]=Clip("Assets/DoubleL/FBX Unity/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_3_InPlace.fbx");
        foreach(string name in new[]{"Player","WingZero"})
        {
            var profile=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/"+name+"Animation.asset");var controller=(AnimatorController)profile.controller;
            foreach(var state in controller.layers.SelectMany(l=>l.stateMachine.states).Select(s=>s.state))if(replacements.TryGetValue(state.name,out var clip)){if(!clip.humanMotion)throw new InvalidOperationException(state.name+" is not Humanoid");state.motion=clip;}
            profile.clips=profile.clips.Select(p=>new CharacterAnimationSettings.StateClip{state=p.state,clip=replacements.TryGetValue(p.state,out var clip)?clip:p.clip}).ToArray();
            RestoreLocomotionProfile(profile);
            Save(profile);Save(controller);
        }
        return "Original supplied Idle/Run/Dash retained; other movement/reaction/skill states use direct native Humanoid clips";
    }
    static void RestoreLocomotionProfile(CharacterAnimationSettings profile)
    {
        var original=new Dictionary<string,AnimationClip>();
        foreach(var state in new[]{"Idle","Run","Dash"})
        {
            string path="Assets/_Project/Animations/Humanoid/OriginalLocomotion/"+state+".fbx";
            original[state]=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__"));
            if(original[state]==null || !original[state].humanMotion)throw new InvalidOperationException("Missing native original locomotion: "+path);
        }
        profile.clips=profile.clips.Select(p=>new CharacterAnimationSettings.StateClip{state=p.state,clip=original.TryGetValue(p.state,out var clip)?clip:p.clip}).ToArray();
        foreach(var state in ((AnimatorController)profile.controller).layers.SelectMany(l=>l.stateMachine.states).Select(s=>s.state))
            if(original.TryGetValue(state.name,out var clip))state.motion=clip;
    }
    public static string RestoreLocomotion()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        PrepareOriginalLocomotion();
        foreach(string name in new[]{"Player","WingZero"})
        {
            var profile=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/"+name+"Animation.asset");
            RestoreLocomotionProfile(profile);
            Save(profile);Save(profile.controller);
        }
        return "Restored only Idle, Run and Dash in both profiles/controllers from the original source motion using Unity's native Humanoid importer.";
    }
    public static string PrepareOriginalLocomotion()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Import in Edit Mode");
        const string folder="Assets/_Project/Animations/Humanoid/OriginalLocomotion";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/_Project/Animations/Humanoid","OriginalLocomotion");
        string idle=folder+"/Idle.fbx";
        if(!System.IO.File.Exists(idle) && !AssetDatabase.CopyAsset("Assets/_Project/Models/Player/Mecha.fbx",idle))throw new InvalidOperationException("Cannot copy supplied idle source");
        using(var zip=System.IO.Compression.ZipFile.OpenRead("SourceArchive/NewPlayerUnused-2026-10-14.zip"))
        foreach(var pair in new[]{("Run","Mecha@Running.fbx"),("Dash","Mecha@Dash.fbx")})
        {
            string path=folder+"/"+pair.Item1+".fbx";
            if(System.IO.File.Exists(path))continue;
            var entry=zip.GetEntry(pair.Item2);if(entry==null)throw new InvalidOperationException("Missing supplied source "+pair.Item2);
            using(var input=entry.Open())using(var output=System.IO.File.Create(path))input.CopyTo(output);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        }
        string report="";
        foreach(string state in new[]{"Idle","Run","Dash"})
        {
            string path=folder+"/"+state+".fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;
            var clips=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
            foreach(var clip in clips){clip.loopTime=true;clip.loopPose=true;clip.lockRootRotation=true;clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.heightFromFeet=true;}
            importer.clipAnimations=clips;importer.SaveAndReimport();
            var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            report+=state+" nativeAvatar="+(avatar!=null&&avatar.isHuman&&avatar.isValid)+"\n";
        }
        return report;
    }
    static void Save(UnityEngine.Object asset){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
    static void ApplyRig(GameObject go,Avatar avatar,GameObject reference)
    {
        var map=reference.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Bip001")).ToDictionary(t=>t.name);
        foreach(var bone in go.GetComponentsInChildren<Transform>(true).Where(t=>map.ContainsKey(t.name)))
        {var src=map[bone.name];bone.localPosition=src.localPosition;bone.localRotation=src.localRotation;bone.localScale=src.localScale;}
        go.GetComponent<Animator>().avatar=avatar;
        // Preserve the authored rigid skin, including source weapon geometry. Never filter mesh triangles.
        if(go.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh.name=="WingZeroSwordEquipped")
            go.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Models/Player/WingZeroEW/WingZeroRigged.asset");
        var referenceSword=reference.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Sword7");
        var sword=go.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Sword7");sword.localPosition=referenceSword.localPosition;sword.localRotation=referenceSword.localRotation;
    }
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Author in Edit Mode");
        const string folder="Assets/_Project/Animations/Humanoid";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/_Project/Animations","Humanoid");
        var avatars=new Dictionary<string,Avatar>();var references=new Dictionary<string,GameObject>();
        string sourcePath="Assets/DoubleL/FBX Unity/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx";
        var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath));var sa=source.GetComponent<Animator>();if(sa==null)sa=source.AddComponent<Animator>();sa.avatar=((ModelImporter)AssetImporter.GetAtPath(sourcePath)).sourceAvatar;sa.runtimeAnimatorController=null;
        var neutral=new HumanPose{bodyPosition=Vector3.zero,bodyRotation=Quaternion.identity,muscles=new float[HumanTrait.MuscleCount]};
        using(var handler=new HumanPoseHandler(sa.avatar,source.transform))handler.SetHumanPose(ref neutral);
        var socket=source.GetComponentsInChildren<Transform>().Single(t=>t.name=="weapon_r");
        string report="";
        try
        {
            foreach(string name in new[]{"Dasher","Zero"})
            {
                string path="Assets/_Project/Prefabs/Characters/"+(name=="Dasher"?"Tallgeese":"WingZero")+"Visual.prefab";
                var go=PrefabUtility.LoadPrefabContents(path);references[name]=go;go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one;
                var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=null;
                var avatar=NativeAvatar(go,name+"Humanoid");string output=folder+"/"+name+"Humanoid.asset";
                var old=AssetDatabase.LoadAssetAtPath<Avatar>(output);if(old!=null){EditorUtility.CopySerialized(avatar,old);UnityEngine.Object.DestroyImmediate(avatar);avatar=old;Save(avatar);}else AssetDatabase.CreateAsset(avatar,output);
                animator.avatar=avatar;avatars[name]=avatar;
                var pose=go.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Bip001")).ToDictionary(t=>t,t=>(t.localPosition,t.localRotation,t.localScale));
                using(var handler=new HumanPoseHandler(avatar,go.transform))handler.SetHumanPose(ref neutral);
                var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var sword=hand.Find("Sword7");sword.localRotation=Quaternion.Inverse(hand.rotation)*socket.rotation;sword.localPosition=hand.Find("Bip001 R Finger0").localPosition*.5f;
                foreach(var pair in pose){pair.Key.localPosition=pair.Value.localPosition;pair.Key.localRotation=pair.Value.localRotation;pair.Key.localScale=pair.Value.localScale;}
                if(name=="Zero")go.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Models/Player/WingZeroEW/WingZeroRigged.asset");
                var profile=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/"+(name=="Dasher"?"Player":"WingZero")+"Animation.asset");animator.runtimeAnimatorController=profile.controller;
                var replacements=new Dictionary<string,AnimationClip>();
                for(int i=1;i<=3;i++)foreach(string series in new[]{"A","B"})replacements[(series=="A"?"Slash":"Heavy")+i]=Clip("Assets/DoubleL/FBX Unity/One Hand Up/Attack_"+series+"/InPlace/1Hand_Up_Attack_"+series+"_"+i+"_InPlace.fbx");
                replacements["AirSlash"]=replacements["Slash2"];replacements["DashSlash"]=replacements["Slash1"];replacements["RisingSlash"]=replacements["Heavy3"];replacements["DiveStrike"]=replacements["Heavy2"];replacements["CriticalStrike"]=replacements["Heavy1"];
                // The supplied Humanoid clips are referenced directly. No per-frame pose baking, wrist fixes or time warps.
                var controller=(AnimatorController)profile.controller;
                foreach(var state in controller.layers.SelectMany(l=>l.stateMachine.states).Select(s=>s.state))if(replacements.TryGetValue(state.name,out var clip))state.motion=clip;
                profile.clips=profile.clips.Select(p=>new CharacterAnimationSettings.StateClip{state=p.state,clip=replacements.TryGetValue(p.state,out var clip)?clip:p.clip}).ToArray();Save(controller);Save(profile);
                PrefabUtility.SaveAsPrefabAsset(go,path);report+=name+" valid Humanoid with direct source melee clips\n";
            }
            var player=PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/MechaPlayer.prefab");
            try{foreach(string name in new[]{"Dasher","Zero"})ApplyRig(player.transform.Find(name+" Visual").gameObject,avatars[name],references[name]);PrefabUtility.SaveAsPrefabAsset(player,"Assets/_Project/Prefabs/MechaPlayer.prefab");}
            finally{PrefabUtility.UnloadPrefabContents(player);}
            foreach(var actor in UnityEngine.Object.FindObjectsByType<CharacterRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(string name in new[]{"Dasher","Zero"})ApplyRig(actor.transform.Find(name+" Visual").gameObject,avatars[name],references[name]);
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }
        finally{UnityEngine.Object.DestroyImmediate(source);foreach(var go in references.Values)PrefabUtility.UnloadPrefabContents(go);}
        return report;
    }
    public static string Preview(float fraction)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Use a disposable Play Mode preview");
        string result="";
        foreach(string name in new[]{"Dasher","Zero"})
        {
            var root=GameObject.Find("Melee Preview "+name);var view=root.GetComponent<CharacterAnimator>();view.enabled=false;
            root.GetComponent<PlayerMovement>().enabled=false;root.GetComponent<Rigidbody>().isKinematic=true;
            var a=view.Animator;var visual=a.gameObject;visual.transform.localScale=Vector3.one;
            a.runtimeAnimatorController=null;
            var avatar=NativeAvatar(visual,name+"PreviewAvatar");a.avatar=avatar;a.applyRootMotion=false;a.Rebind();
            var clip=AssetDatabase.LoadAllAssetsAtPath("Assets/DoubleL/FBX Unity/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
            var graph=PlayableGraph.Create("Native Humanoid manual pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(true);var output=AnimationPlayableOutput.Create(graph,"Humanoid",a);output.SetSourcePlayable(p);graph.Play();p.SetTime(clip.length*fraction);graph.Evaluate(0);graph.Destroy();
            visual.transform.localScale=Vector3.one*(name=="Dasher"?.65f:.5f);
            result+=name+" nativeHuman="+a.isHuman+" humanScale="+a.humanScale+" body="+a.bodyPosition+" rightHand="+a.GetBoneTransform(HumanBodyBones.RightHand).position+"\n";
        }
        ScreenCapture.CaptureScreenshot("Logs/SwordRevision/NativeHumanoid.png");return result;
    }
    public static string Diagnose()
    {
        string report="";
        foreach(string name in new[]{"Tallgeese","WingZero"})
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+name+"Visual.prefab"));go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one;
            Avatar avatar=null;
            try
            {
                foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))script.enabled=false;
                object[] args={go,new HumanBone[0],new SkeletonBone[0],false};
                Setup.GetMethod("SetupHumanSkeleton",Flags).Invoke(null,args);
                var human=(HumanBone[])args[1];var skeleton=(SkeletonBone[])args[2];
                report+=name+" mapped="+human.Length+" hasTranslation="+args[3]+"\n"+string.Join(";",human.Select(b=>b.humanName+"="+b.boneName))+"\n";
                var description=new HumanDescription{human=human,skeleton=skeleton,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f};
                avatar=AvatarBuilder.BuildHumanAvatar(go,description);report+="valid="+avatar.isValid+" human="+avatar.isHuman+"\n";
            }
            finally{UnityEngine.Object.DestroyImmediate(go);if(avatar!=null)UnityEngine.Object.DestroyImmediate(avatar);}
        }
        return report;
    }
}
