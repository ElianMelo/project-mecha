using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// Operator-invoked native pose/physics inspection; no test runner or assertions.
public static class InspectZeroGrounding
{
    public static async Task<string> Stage()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Use Play Mode.");
        var root=GameObject.Find("Zero Revision Preview");
        GameObject.Find("Foreground policy preview platform")?.SetActive(false);
        var floor=GameObject.Find("Preview ground");floor.transform.position=new Vector3(40,-.1f,0);floor.layer=7;
        var body=root.GetComponent<Rigidbody>();body.isKinematic=false;
        body.position=new Vector3(40,.1f,0);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();
        root.GetComponent<PlayerMovement>().enabled=true;root.GetComponent<CharacterAnimator>().enabled=true;
        var camera=Camera.main;camera.transform.position=new Vector3(40,.8f,-12);camera.orthographicSize=1.45f;
        await Task.Delay(1200);
        return await Capture("Idle-flat-final");
    }
    public static string Measure()
    {
        var root=GameObject.Find("Zero Revision Preview");var skin=root.transform.Find("Zero Visual").GetComponentInChildren<SkinnedMeshRenderer>();
        var baked=new Mesh();skin.BakeMesh(baked,true);var vertices=baked.vertices;var weights=skin.sharedMesh.boneWeights;
        string text="root="+root.transform.position+" capsuleBottom="+root.GetComponent<CapsuleCollider>().bounds.min.y+" grounded="+root.GetComponent<PlayerMovement>().IsGrounded+" state="+root.GetComponent<CharacterAnimator>().Animator.GetCurrentAnimatorStateInfo(0).shortNameHash+"\n";
        foreach(string side in new[]{"L","R"})
        {
            float sole=Enumerable.Range(0,vertices.Length).Where(i=>skin.bones[weights[i].boneIndex0].name=="Bip001 "+side+" Foot").Min(i=>skin.transform.TransformPoint(vertices[i]).y);
            text+=side+" sole="+sole+" foot="+skin.bones.First(b=>b.name=="Bip001 "+side+" Foot").position+"\n";
        }
        UnityEngine.Object.Destroy(baked);return text+"frame="+Time.frameCount+" time="+Time.time;
    }
    public static async Task<string> Capture(string name)
    {
        string dir=Path.GetFullPath("Logs/ZeroGrounding");Directory.CreateDirectory(dir);
        ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));await Task.Delay(180);return Measure();
    }
    public static string Clips()
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/WingZeroVisual.prefab"));
        var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();var baked=new Mesh();var weights=skin.sharedMesh.boneWeights;
        var indices=Enumerable.Range(0,weights.Length).Where(i=>skin.bones[weights[i].boneIndex0].name=="Bip001 L Foot"||skin.bones[weights[i].boneIndex0].name=="Bip001 R Foot").ToArray();
        string text="";
        try
        {
            foreach(string name in new[]{"Idle_V2","Running","Jump"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Animations/WingZeroEW/"+name+".anim");
                float min=float.MaxValue,max=float.MinValue,minTime=0,maxTime=0;
                // Inspect native baked poses at authoring sample boundaries and midway.
                for(float t=0;t<clip.length;t+=1f/1920f)
                {
                    clip.SampleAnimation(go,t);skin.BakeMesh(baked,true);var vertices=baked.vertices;
                    float sole=indices.Min(i=>skin.transform.TransformPoint(vertices[i]).y);
                    if(sole<min){min=sole;minTime=t;}if(sole>max){max=sole;maxTime=t;}
                }
                text+=name+" lowest sole range="+min+" at "+minTime+".."+max+" at "+maxTime+" length="+clip.length+"\n";
            }
            return text;
        }
        finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(baked);}
    }
}
