using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Operator-invoked authoring and manual preview; no runtime retargeting dependency.
public static class WingZeroPlaceholder
{
    const string Folder = "Assets/_Project/Models/Player/WingZeroEW";
    const string Source = "Assets/_Project/Models/Player/Mecha.fbx";
    const string Visual = "Assets/_Project/Prefabs/Characters/WingZeroVisual.prefab";
    const string Animations = "Assets/_Project/Animations/WingZeroEW";
    const string Profile = "Assets/_Project/Settings/Animation/WingZeroAnimation.asset";
    const string Player = "Assets/_Project/Prefabs/MechaPlayer.prefab";
    const float Unit = .01f;
    static readonly Vector3 Origin = new Vector3(0, 16.3f, -12);
    static WingZeroRigSettings RigSettings()
    {
        const string path="Assets/_Project/Settings/Characters/ZeroRig.asset";
        var settings=AssetDatabase.LoadAssetAtPath<WingZeroRigSettings>(path);
        if(settings==null){FolderAt("Assets/_Project/Settings/Characters");settings=ScriptableObject.CreateInstance<WingZeroRigSettings>();AssetDatabase.CreateAsset(settings,path);}
        return settings;
    }
    static void FolderAt(string path) { if(AssetDatabase.IsValidFolder(path)) return; FolderAt(Path.GetDirectoryName(path).Replace('\\','/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path)); }
    static T Save<T>(T value,string path) where T:UnityEngine.Object
    {
        var old=AssetDatabase.LoadAssetAtPath<T>(path);
        if(old!=null){EditorUtility.CopySerialized(value,old);UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);return old;}
        AssetDatabase.CreateAsset(value,path);return value;
    }
    static Dictionary<string,Transform> Bones(GameObject go) => go.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Bip001")).ToDictionary(t=>t.name);
    static GameObject SourceAtBind()
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source));
        go.hideFlags=HideFlags.HideAndDontSave;
        var r=go.GetComponentInChildren<SkinnedMeshRenderer>();
        var bind=r.sharedMesh.bindposes;
        var matrices=r.bones.Select((b,i)=>new {b,m=r.transform.localToWorldMatrix*bind[i].inverse}).OrderBy(x=>AnimationUtility.CalculateTransformPath(x.b,go.transform).Count(c=>c=='/')).ToArray();
        foreach(var x in matrices)x.b.SetPositionAndRotation(x.m.GetColumn(3),x.m.rotation);
        return go;
    }
    static Vector3 Joint(string name)
    {
        string n=name.Replace("Bip001 ","");
        float side=n.StartsWith("L ")?-1:1;
        n=n.Replace("L ","").Replace("R ","");
        Vector3 p;
        switch(n)
        {
            case "Bip001": case "Pelvis":p=new Vector3(0,130,12);break;
            case "Spine":p=new Vector3(0,139,12);break;
            case "Spine1":p=new Vector3(0,148,12);break;
            case "Spine2":p=new Vector3(0,157,12);break;
            case "Spine3":p=new Vector3(0,164,12);break;
            case "Neck":p=new Vector3(0,173,12);break;
            case "Head":p=new Vector3(0,178,12);break;
            case "Clavicle":p=new Vector3(side*5,170,12);break;
            case "UpperArm":p=new Vector3(side*31,170,12);break;
            case "Forearm":p=new Vector3(side*48,132,14);break;
            case "Hand":p=new Vector3(side*63,109,38);break;
            case "Finger0":p=new Vector3(side*70,99,52);break;
            case "Thigh":p=new Vector3(side*15,126,10);break;
            case "Calf":p=new Vector3(side*23,80,0);break;
            case "Foot":p=new Vector3(side*29,29,-53);break;
            case "Toe0":p=new Vector3(side*33,-4,-29);break;
            case "Toe01":p=new Vector3(side*33,-5,-26);break;
            case "Toe02":p=new Vector3(side*33,-6,-23);break;
            case "Xtra01":p=new Vector3(0,164,-12);break;
            default:throw new InvalidOperationException(name);
        }
        return (p+Origin)*Unit;
    }
    static string Child(string name)
    {
        if(name=="Bip001")return "Bip001 Spine";
        if(name=="Bip001 Pelvis")return "Bip001 Spine";
        if(name=="Bip001 Spine")return "Bip001 Spine1";
        if(name=="Bip001 Spine1")return "Bip001 Spine2";
        if(name=="Bip001 Spine2")return "Bip001 Spine3";
        if(name=="Bip001 Spine3")return "Bip001 Neck";
        if(name=="Bip001 Neck")return "Bip001 Head";
        foreach(string side in new[]{"L","R"})
        {
            string p="Bip001 "+side+" ";
            foreach(var pair in new[]{("Clavicle","UpperArm"),("UpperArm","Forearm"),("Forearm","Hand"),("Hand","Finger0"),("Thigh","Calf"),("Calf","Foot"),("Foot","Toe0"),("Toe0","Toe01"),("Toe01","Toe02")})
                if(name==p+pair.Item1)return p+pair.Item2;
        }
        return null;
    }
    static string Part(Vector3 p,Vector3 min,Vector3 max)
    {
        string side=p.x<0?"L ":"R ";
        // Wings and their articulated struts stay rigid on the back, never on an arm.
        if(IsWing(p,min,max))return "Bip001 Spine3";
        if(p.y>174 && Mathf.Abs(p.x)<25 && p.z>-15)return "Bip001 Head";
        if(p.y>170 && Mathf.Abs(p.x)>25 && p.z>-15)return "Bip001 "+side+"Clavicle";
        if(Mathf.Abs(p.x)>65 && p.z>35 && p.y<95)return "Bip001 "+side+"Hand"; // Twin buster rifles, rigid at the hands.
        if(Mathf.Abs(p.x)>55 && p.z>25 && p.y<115)return "Bip001 "+side+"Hand";
        if(Mathf.Abs(p.x)>38 && p.y>110 && p.y<150 && p.z>-10)return "Bip001 "+side+"Forearm";
        if(Mathf.Abs(p.x)>25 && p.y>=145 && p.z>-15)return "Bip001 "+side+"UpperArm";
        if(p.y<40 && Mathf.Abs(p.x)<50)return "Bip001 "+side+"Foot";
        if(p.y<85 && Mathf.Abs(p.x)<45)return "Bip001 "+side+"Calf";
        if(p.y<120 && Mathf.Abs(p.x)<35 && p.z<27)return "Bip001 "+side+"Thigh";
        if(p.y<139)return "Bip001 Pelvis";
        return "Bip001 Spine3";
    }
    static bool IsWing(Vector3 p,Vector3 min,Vector3 max)
        => p.z < -65 || (Mathf.Abs(p.x)>120 && p.z<35) || (max.y>205 && max.x-min.x>100);
    public static object BuildRig()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before authoring.");
        FolderAt(Animations);
        var source=SourceAtBind();var sourceBones=Bones(source);
        var go=UnityEngine.Object.Instantiate(source);go.hideFlags=HideFlags.None;go.name="Wing Zero Visual";
        try
        {
            var bones=Bones(go);
            var orientations=new Dictionary<string,Quaternion>();
            foreach(var pair in sourceBones)
            {
                string child=Child(pair.Key);
                orientations[pair.Key]=child==null?pair.Value.rotation:Quaternion.FromToRotation(sourceBones[child].position-pair.Value.position,Joint(child)-Joint(pair.Key))*pair.Value.rotation;
            }
            foreach(var pair in bones.OrderBy(x=>AnimationUtility.CalculateTransformPath(x.Value,go.transform).Count(c=>c=='/')))
                pair.Value.SetPositionAndRotation(Joint(pair.Key),orientations[pair.Key]);
            foreach(var r in go.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(r.gameObject);
            var obj=AssetDatabase.FindAssets("t:Model",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).Single();
            var filter=AssetDatabase.LoadAssetAtPath<GameObject>(obj).GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Mesh01");
            var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name="WingZeroEW_Rigged";
            var raw=mesh.vertices;var vertices=raw.Select(p=>(p+Origin)*Unit).ToArray();
            var rigSettings=RigSettings();
            var components=ReadComponents(raw,mesh.triangles);
            var ordered=bones.Values.ToArray();var weights=new BoneWeight[raw.Length];var assignments=new List<string>();
            foreach(var group in components)
            {
                var pts=group.Select(i=>raw[i]).ToArray();var c=pts.Aggregate(Vector3.zero,(a,b)=>a+b)/pts.Length;
                string bone=Part(c,pts.Aggregate(Vector3.Min),pts.Aggregate(Vector3.Max));int index=Array.FindIndex(ordered,t=>t.name==bone);
                if(IsWing(c,pts.Aggregate(Vector3.Min),pts.Aggregate(Vector3.Max)))
                {
                    var attachment=(new Vector3(c.x<0?-15:15,164,-12)+Origin)*Unit;
                    foreach(int i in group)vertices[i]=attachment+(vertices[i]-attachment)*rigSettings.wingScale;
                }
                foreach(int i in group)weights[i]=new BoneWeight{boneIndex0=index,weight0=1};
                assignments.Add($"{group[0]},{group.Length},{bone},{c.x:F3},{c.y:F3},{c.z:F3}");
            }
            mesh.vertices=vertices;mesh.boneWeights=weights;mesh.bindposes=ordered.Select(t=>t.worldToLocalMatrix).ToArray();mesh.RecalculateBounds();
            mesh=Save(mesh,Folder+"/WingZeroRigged.asset");
            var material=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/MechaArmor.mat"));material.name="WingZeroArmor";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Gundam Memories - XXXG-00W0 Wing Gundam Zero (EW) - Ren.png");
            foreach(string key in new[]{"_MainTex","_BaseMap","_1st_ShadeMap","_2nd_ShadeMap"})if(material.HasProperty(key))material.SetTexture(key,texture);
            // The derived mesh uses metres rather than the source FBX's 100x renderer transform.
            if(material.HasProperty("_Outline_Width"))material.SetFloat("_Outline_Width",.0012f);
            material=Save(material,Folder+"/WingZeroArmor.mat");
            var body=new GameObject("Wing Zero Armor");body.transform.SetParent(go.transform,false);
            var skin=body.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.bones=ordered;skin.rootBone=bones["Bip001"];skin.sharedMaterial=material;skin.updateWhenOffscreen=true;
            skin.localBounds=new Bounds(new Vector3(0,1,-.2f),new Vector3(6,5,6));
            var animator=go.GetComponent<Animator>();if(animator==null)animator=go.AddComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.SaveAsPrefabAsset(go,Visual);
            File.WriteAllLines("Logs/MechaRetarget/rig-parts.csv",new[]{"FirstVertex,Vertices,Bone,X,Y,Z"}.Concat(assignments));
            AssetDatabase.SetLabels(mesh,new[]{"Placeholder"});AssetDatabase.SetLabels(material,new[]{"Placeholder"});AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(Visual),new[]{"Prefabs"});
            return new {vertices=raw.Length,rigidPieces=components.Count,bones=ordered.Length,prefab=Visual};
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(source);}
    }
    static List<int[]> ReadComponents(Vector3[] vertices,int[] triangles)
    {
        int[] parent=Enumerable.Range(0,vertices.Length).ToArray();
        Func<int,int> root=null;root=i=>parent[i]==i?i:(parent[i]=root(parent[i]));
        Action<int,int> join=(a,b)=>parent[root(a)]=root(b);
        var weld=new Dictionary<Vector3Int,int>();
        for(int i=0;i<vertices.Length;i++)
        {var p=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*1000),Mathf.RoundToInt(p.y*1000),Mathf.RoundToInt(p.z*1000));if(weld.TryGetValue(key,out int other))join(i,other);else weld[key]=i;}
        for(int i=0;i<triangles.Length;i+=3){join(triangles[i],triangles[i+1]);join(triangles[i],triangles[i+2]);}
        return Enumerable.Range(0,vertices.Length).GroupBy(i=>root(i)).Select(g=>g.ToArray()).OrderByDescending(g=>g.Length).ToList();
    }
    public static object Retarget()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Retarget in Edit Mode.");
        var source=SourceAtBind();var target=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Visual));
        try
        {
            var sb=Bones(source);var tb=Bones(target);
            var localPositions=tb.ToDictionary(x=>x.Key,x=>x.Value.localPosition);
            var sourceBind=sb.ToDictionary(x=>x.Key,x=>x.Value.position);
            var targetBind=tb.ToDictionary(x=>x.Key,x=>x.Value.position);
            var rotationOffsets=tb.ToDictionary(x=>x.Key,x=>Quaternion.Inverse(sb[x.Key].rotation)*x.Value.rotation);
            var sourceSkin=source.GetComponentInChildren<SkinnedMeshRenderer>();var targetSkin=target.GetComponentInChildren<SkinnedMeshRenderer>();
            var sourceFootIndices=new Dictionary<string,int[]>();var targetFootPoints=new Dictionary<string,Vector3[]>();
            foreach(string side in new[]{"L","R"})
            {
                sourceFootIndices[side]=sourceSkin.sharedMesh.boneWeights.Select((w,i)=>new{w,i}).Where(x=>sourceSkin.bones[x.w.boneIndex0].name.StartsWith("Bip001 "+side+" Foot")||sourceSkin.bones[x.w.boneIndex0].name.StartsWith("Bip001 "+side+" Toe")).Select(x=>x.i).ToArray();
                var verts=targetSkin.sharedMesh.vertices;var weights=targetSkin.sharedMesh.boneWeights;
                targetFootPoints[side]=Enumerable.Range(0,verts.Length).Where(i=>targetSkin.bones[weights[i].boneIndex0].name=="Bip001 "+side+" Foot").Select(i=>tb["Bip001 "+side+" Foot"].InverseTransformPoint(targetSkin.transform.TransformPoint(verts[i]))).ToArray();
            }
            var sampledMesh=new Mesh();
            var profile=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>("Assets/_Project/Settings/Animation/PlayerAnimation.asset");
            // BakeMesh(true) yields renderer-local vertices. The old default bake
            // already included the 100x import scale; TransformPoint applied it again.
            profile.Clip("Idle").SampleAnimation(source,0f);sourceSkin.BakeMesh(sampledMesh,true);
            var referenceRoot=sb["Bip001"].position;
            var referenceVertices=sampledMesh.vertices;
            float referenceFloor=sourceFootIndices.Values.SelectMany(i=>i).Min(i=>sourceSkin.transform.TransformPoint(referenceVertices[i]).y);
            float LegLength(Dictionary<string,Vector3> pose,string side)=>Vector3.Distance(pose["Bip001 "+side+" Thigh"],pose["Bip001 "+side+" Calf"])+Vector3.Distance(pose["Bip001 "+side+" Calf"],pose["Bip001 "+side+" Foot"]);
            float legScale=(LegLength(targetBind,"L")+LegLength(targetBind,"R"))/(LegLength(sourceBind,"L")+LegLength(sourceBind,"R"));
            var clips=profile.controller.animationClips.Concat(profile.clips.Select(x=>x.clip)).Where(c=>c!=null).Distinct().ToArray();
            var newClips=new Dictionary<AnimationClip,AnimationClip>();
            var order=tb.Keys.OrderBy(k=>AnimationUtility.CalculateTransformPath(tb[k],target.transform).Count(c=>c=='/')).ToArray();
            foreach(var clip in clips)
            {
                var curves=order.ToDictionary(n=>n,n=>Enumerable.Range(0,7).Select(_=>new AnimationCurve()).ToArray());
                int frames=Mathf.CeilToInt(clip.length*60);
                for(int f=0;f<=frames;f++)
                {
                    float time=Mathf.Min(clip.length,f/60f);clip.SampleAnimation(source,time);
                    foreach(string n in order)
                    {
                        tb[n].localPosition=localPositions[n];
                        // Preserve anatomical bind offsets rather than assigning the
                        // source's absolute joint orientations to different geometry.
                        tb[n].rotation=sb[n].rotation*rotationOffsets[n];
                        if(n=="Bip001")tb[n].position=targetBind[n]+(sb[n].position-referenceRoot)*legScale;
                    }
                    sourceSkin.BakeMesh(sampledMesh,true);var sourceVertices=sampledMesh.vertices;
                    var footGoals=new Dictionary<string,Vector3>();var footRotations=new Dictionary<string,Quaternion>();
                    float lowestFloor=float.MaxValue;
                    foreach(string side in new[]{"L","R"})
                    {
                        string prefix="Bip001 "+side+" ";var foot=tb[prefix+"Foot"];
                        // The OBJ bind is an airborne pose with both feet behind the
                        // hips. Carrying that offset into a planted source stance
                        // drives one foot outside the leg's reachable radius.
                        var goal=tb["Bip001"].position+(sb[prefix+"Foot"].position-sb["Bip001"].position)*legScale;
                        float floor=(sourceFootIndices[side].Min(i=>sourceSkin.transform.TransformPoint(sourceVertices[i]).y)-referenceFloor)*legScale;
                        float underside=targetFootPoints[side].Min(p=>foot.TransformVector(p).y);
                        goal.y=floor-underside;
                        footGoals[side]=goal;footRotations[side]=foot.rotation;lowestFloor=Mathf.Min(lowestFloor,floor);
                    }
                    // Generic source clips use physics-owned root travel. Keep the new
                    // visual soles above that root's foot plane, including airborne poses.
                    float raise=Mathf.Max(0f,-lowestFloor);tb["Bip001"].position+=Vector3.up*raise;
                    foreach(string side in new[]{"L","R"})footGoals[side]+=Vector3.up*raise;
                    // Different proportions need pelvis compensation before both legs
                    // are solved. Clamping an unreachable IK goal alone leaves floating feet.
                    float lower=0f;
                    foreach(string side in new[]{"L","R"})
                    {
                        string prefix="Bip001 "+side+" ";var hip=tb[prefix+"Thigh"];var knee=tb[prefix+"Calf"];var foot=tb[prefix+"Foot"];
                        float reach=Vector3.Distance(hip.position,knee.position)+Vector3.Distance(knee.position,foot.position)-.001f;
                        var delta=footGoals[side]-hip.position;float horizontal=delta.x*delta.x+delta.z*delta.z;
                        float vertical=Mathf.Sqrt(Mathf.Max(0f,reach*reach-horizontal));
                        lower=Mathf.Max(lower,hip.position.y-footGoals[side].y-vertical);
                    }
                    tb["Bip001"].position-=Vector3.up*lower;
                    foreach(string side in new[]{"L","R"})
                    {
                        string prefix="Bip001 "+side+" ";
                        var hint=tb[prefix+"Thigh"].position+(sb[prefix+"Calf"].position-sb[prefix+"Thigh"].position)*legScale;
                        SolveLeg(tb[prefix+"Thigh"],tb[prefix+"Calf"],tb[prefix+"Foot"],footGoals[side],hint,footRotations[side]);
                    }
                    foreach(string n in order)
                    {
                        var q=tb[n].localRotation;var p=tb[n].localPosition;
                        float[] values={q.x,q.y,q.z,q.w,p.x,p.y,p.z};for(int j=0;j<7;j++)curves[n][j].AddKey(time,values[j]);
                    }
                }
                var output=new AnimationClip{name=clip.name,frameRate=60,wrapMode=clip.wrapMode};
                foreach(string n in order)
                {
                    string path=AnimationUtility.CalculateTransformPath(tb[n],target.transform);
                    string[] properties={"m_LocalRotation.x","m_LocalRotation.y","m_LocalRotation.z","m_LocalRotation.w","m_LocalPosition.x","m_LocalPosition.y","m_LocalPosition.z"};
                    for(int j=0;j<7;j++)
                    {
                        curves[n][j]=Reduce(curves[n][j],j<4?.0001f:.00001f);
                        for(int k=0;k<curves[n][j].length;k++){AnimationUtility.SetKeyLeftTangentMode(curves[n][j],k,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curves[n][j],k,AnimationUtility.TangentMode.Linear);}
                        AnimationUtility.SetEditorCurve(output,EditorCurveBinding.FloatCurve(path,typeof(Transform),properties[j]),curves[n][j]);
                    }
                }
                output.EnsureQuaternionContinuity();AnimationUtility.SetAnimationClipSettings(output,AnimationUtility.GetAnimationClipSettings(clip));
                AnimationUtility.SetAnimationEvents(output,AnimationUtility.GetAnimationEvents(clip));newClips[clip]=Save(output,Animations+"/"+clip.name+".anim");
            }
            UnityEngine.Object.DestroyImmediate(sampledMesh);
            // Keep the Animator graph, state names and playback parameter contract intact.
            string controllerPath=Animations+"/WingZeroAnimation.controller";
            if(AssetDatabase.LoadMainAssetAtPath(controllerPath)==null)AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(profile.controller),controllerPath);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            foreach(var layer in controller.layers)ReplaceClips(layer.stateMachine,newClips);
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssetIfDirty(controller);
            var newProfile=UnityEngine.Object.Instantiate(profile);newProfile.name="WingZeroAnimation";newProfile.controller=controller;
            var character=AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/_Project/Settings/Characters/Zero.asset");
            if(character!=null&&character.movement!=null)newProfile.runReferenceSpeed=character.movement.runSpeed;
            for(int i=0;i<newProfile.clips.Length;i++){var entry=newProfile.clips[i];if(entry.clip!=null&&newClips.TryGetValue(entry.clip,out var replacement))entry.clip=replacement;newProfile.clips[i]=entry;}
            Save(newProfile,Profile);
            var contents=PrefabUtility.LoadPrefabContents(Visual);try{contents.GetComponent<Animator>().runtimeAnimatorController=controller;PrefabUtility.SaveAsPrefabAsset(contents,Visual);}finally{PrefabUtility.UnloadPrefabContents(contents);}
            return new {clips=newClips.Count,controller=controllerPath,profile=Profile};
        }
        finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(target);}
    }
    public static string InspectLegMapping()
    {
        var source=SourceAtBind();var target=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Visual));var text="";
        try
        {
            foreach(var pair in new[]{(source,"Source"),(target,"Target")})
            {
                var bones=Bones(pair.Item1);
                foreach(string pose in new[]{"Bind","Idle_V2"})
                {
                    if(pose!="Bind")AssetDatabase.LoadAssetAtPath<AnimationClip>((pair.Item2=="Source"?"Assets/_Project/Animations/Player":Animations)+"/Idle_V2.anim").SampleAnimation(pair.Item1,.3f);
                    text+=pair.Item2+" "+pose+"\n";
                    foreach(string n in new[]{"Bip001","Bip001 L Thigh","Bip001 L Calf","Bip001 L Foot","Bip001 L Toe0","Bip001 R Thigh","Bip001 R Calf","Bip001 R Foot"})text+=n+" "+bones[n].position+" rot="+bones[n].rotation.eulerAngles+"\n";
                }
            }
            return text;
        }
        finally{UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(target);}
    }
    public static string InspectLegGeometry()
    {
        var path=AssetDatabase.FindAssets("t:Model",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).Single();
        var mesh=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Mesh01").sharedMesh;var raw=mesh.vertices;var text="";
        foreach(var group in ReadComponents(raw,mesh.triangles))
        {
            var p=group.Select(i=>raw[i]).ToArray();var c=p.Aggregate(Vector3.zero,(a,b)=>a+b)/p.Length;var min=p.Aggregate(Vector3.Min);var max=p.Aggregate(Vector3.Max);string bone=Part(c,min,max);
            if(c.x>0&&c.y<125&&Mathf.Abs(c.x)<50)text+=bone+" center="+c+" min="+min+" max="+max+" vertices="+p.Length+"\n";
        }
        return text;
    }
    static void ReplaceClips(AnimatorStateMachine machine,Dictionary<AnimationClip,AnimationClip> clips)
    {
        foreach(var state in machine.states)if(state.state.motion is AnimationClip clip&&clips.TryGetValue(clip,out var replacement)){state.state.motion=replacement;EditorUtility.SetDirty(state.state);}
        foreach(var child in machine.stateMachines)ReplaceClips(child.stateMachine,clips);
    }
    static AnimationCurve Reduce(AnimationCurve curve,float tolerance)
    {
        var keys=curve.keys;var keep=new SortedSet<int>{0,keys.Length-1};
        Action<int,int> split=null;split=(a,b)=>
        {
            float error=0;int index=-1;
            for(int k=a+1;k<b;k++){float expected=Mathf.Lerp(keys[a].value,keys[b].value,Mathf.InverseLerp(keys[a].time,keys[b].time,keys[k].time));float delta=Mathf.Abs(keys[k].value-expected);if(delta>error){error=delta;index=k;}}
            if(error>tolerance&&index>=0){keep.Add(index);split(a,index);split(index,b);}
        };
        split(0,keys.Length-1);return new AnimationCurve(keep.Select(k=>keys[k]).ToArray());
    }
    static void SolveLeg(Transform hip,Transform knee,Transform foot,Vector3 goal,Vector3 hint,Quaternion footRotation)
    {
        float a=Vector3.Distance(hip.position,knee.position),b=Vector3.Distance(knee.position,foot.position);
        var direction=goal-hip.position;float distance=Mathf.Clamp(direction.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);direction.Normalize();
        var bend=Vector3.ProjectOnPlane(hint-hip.position,direction).normalized;
        if(bend.sqrMagnitude<.1f)bend=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;
        float along=(a*a-b*b+distance*distance)/(2*distance);float across=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        var pivot=hip.position+direction*along+bend*across;
        hip.rotation=Quaternion.FromToRotation(knee.position-hip.position,pivot-hip.position)*hip.rotation;
        knee.rotation=Quaternion.FromToRotation(foot.position-knee.position,goal-knee.position)*knee.rotation;
        foot.rotation=footRotation;
    }
    public static object Integrate()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before integration.");
        var root=PrefabUtility.LoadPrefabContents(Player);
        try
        {
            var old=root.transform.Find("Player Visual");
            if(old.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.name=="WingZeroEW_Rigged")return "Already integrated";
            const string backup="Assets/_Project/Prefabs/Characters/TallgeeseVisual.prefab";
            if(AssetDatabase.LoadMainAssetAtPath(backup)==null)PrefabUtility.SaveAsPrefabAsset(old.gameObject,backup);
            var replacement=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Visual),root.transform);
            replacement.name="Player Visual";replacement.transform.SetLocalPositionAndRotation(old.localPosition,old.localRotation);replacement.transform.localScale=old.localScale;
            replacement.transform.SetSiblingIndex(old.GetSiblingIndex());
            var map=new Dictionary<UnityEngine.Object,UnityEngine.Object>{{old,replacement.transform},{old.gameObject,replacement}};
            foreach(var bone in Bones(old.gameObject))
            {var t=replacement.transform.Find(AnimationUtility.CalculateTransformPath(bone.Value,old));if(t==null)throw new InvalidOperationException("Missing joint: "+bone.Key);map[bone.Value]=t;map[bone.Value.gameObject]=t.gameObject;}
            map[old.GetComponent<Animator>()]=replacement.GetComponent<Animator>();
            // Preserve the authored secondary gun assembly and move its existing object
            // references with it; only the visual skeleton is replaced.
            var gun=old.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Secondary Weapon Aim");
            var newArm=(Transform)map[gun.parent];gun.SetParent(newArm,false);
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                if(component==null||component.transform==old||component.transform.IsChildOf(old))continue;
                var so=new SerializedObject(component);var it=so.GetIterator();bool changed=false;
                while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue!=null&&map.TryGetValue(it.objectReferenceValue,out var value)){it.objectReferenceValue=value;changed=true;}
                if(changed)so.ApplyModifiedPropertiesWithoutUndo();
            }
            var settings=new SerializedObject(root.GetComponent<CharacterAnimator>());settings.FindProperty("settings").objectReferenceValue=AssetDatabase.LoadAssetAtPath<CharacterAnimationSettings>(Profile);settings.ApplyModifiedPropertiesWithoutUndo();
            foreach(var t in replacement.GetComponentsInChildren<Transform>(true))t.gameObject.layer=root.layer;
            UnityEngine.Object.DestroyImmediate(old.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root,Player);
            AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(backup),new[]{"Prefabs"});
            return new {player=Player,original=backup,visual=Visual,profile=Profile};
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static object Preview(string clipName,float time,string output)
    {
        bool original=clipName.StartsWith("Source:");if(original)clipName=clipName.Substring(7);
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(original?Source:Visual));
        var preview=new PreviewRenderUtility();
        try
        {
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>((original?"Assets/_Project/Animations/Player":Animations)+"/"+clipName+".anim");if(clip!=null)clip.SampleAnimation(go,time);
            if(original){var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/MechaArmor.mat");foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();}
            go.transform.rotation=Quaternion.Euler(0,155,0);preview.AddSingleGO(go);
            preview.camera.orthographic=true;preview.camera.orthographicSize=2.5f;preview.camera.transform.position=new Vector3(0,1.1f,-7);preview.camera.transform.rotation=Quaternion.identity;
            preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=20;preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=new Color(.07f,.09f,.13f);
            preview.lights[0].intensity=1.5f;preview.lights[0].transform.rotation=Quaternion.Euler(40,30,0);preview.lights[1].intensity=.7f;
            preview.BeginPreview(new Rect(0,0,900,900),GUIStyle.none);preview.camera.Render();var rt=preview.EndPreview() as RenderTexture;
            var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(900,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,900,900),0,0);image.Apply();RenderTexture.active=previous;
            File.WriteAllBytes(output,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);return output;
        }
        finally{preview.Cleanup();UnityEngine.Object.DestroyImmediate(go);}
    }
}
