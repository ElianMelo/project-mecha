using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

// Operator-driven native sword-light inspection on the real generated block room.
public static class PlatformBlocksPreview
{
    const string Output="Logs/PlatformBlocks";
    static void Warm(Material material)
    {
        for(int pass=0;pass<material.passCount;pass++)ShaderUtil.CompilePass(material,pass,true);
    }
    static Color32[] Capture(Camera camera,string name)
    {
        Directory.CreateDirectory(Output);var old=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());return texture.GetPixels32();}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static object Difference(Camera camera,Color32[] a,Color32[] b)
    {
        var lower=camera.WorldToViewportPoint(new Vector3(-1.5f,-4.25f,.1f));var upper=camera.WorldToViewportPoint(new Vector3(1.5f,-3.60f,.1f));
        int x0=Mathf.Clamp((int)(lower.x*1280),0,1279),x1=Mathf.Clamp((int)(upper.x*1280),0,1279),y0=Mathf.Clamp((int)(lower.y*720),0,719),y1=Mathf.Clamp((int)(upper.y*720),0,719),changed=0,max=0;long sum=0;int total=0;
        for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){int i=y*1280+x;int d=Mathf.Max(Math.Abs(a[i].r-b[i].r),Mathf.Max(Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)));if(d>1)changed++;max=Mathf.Max(max,d);sum+=d;total++;}
        return new{pixels=total,changed,maxChannelDelta=max,meanChannelDelta=total>0?(float)sum/total:0};
    }
    public static async Task<object> Observe(string character,int index)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode");
        if(UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).Any())throw new InvalidOperationException("Preserve existing play session");
        foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
        foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(c is RoomCameraRig||c is RoomCameraZoom||c is EnemySpawner||c.GetType().Name=="CinemachineBrain")c.enabled=false;
        var settings=AssetDatabase.LoadAssetAtPath<ProceduralLevelSettings>("Assets/_Project/Settings/World/ProceduralLevels.asset");
        var builder=UnityEngine.Object.FindFirstObjectByType<ProceduralRoomBuilder>();if(builder==null)builder=new GameObject("Block Room Preview").AddComponent<ProceduralRoomBuilder>();
        builder.Build(ProceduralLevelLayout.Generate(settings,settings.firstRoomSeed,1,16f/9f),settings);
        foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(c is RoomCameraRig||c is RoomCameraZoom||c.GetType().Name=="CinemachineBrain")c.enabled=false;
        var camera=Camera.main;camera.transform.SetPositionAndRotation(new Vector3(.3f,-2.8f,-12),Quaternion.identity);camera.orthographicSize=3.485f;
        var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/MechaPlayer.prefab"));actor.name="Platform Sword Preview "+character;
        actor.GetComponent<CharacterRuntime>().enabled=false;actor.GetComponent<CharacterRuntime>().Apply(index);
        actor.GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled=false;actor.GetComponent<PlayerInputs>().enabled=false;
        await Task.Delay(180);actor.SetActive(true);var body=actor.GetComponent<Rigidbody>();body.position=actor.transform.position=new Vector3(0,-3.5f,0);body.linearVelocity=Vector3.zero;actor.GetComponent<PlayerMovement>().Face(1);
        await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();
        int firstFrame=Time.frameCount;float began=Time.time;
        while(Time.time-began<.12f){await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();}
        foreach(var material in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct())Warm(material);
        await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();
        var lights=actor.GetComponentsInChildren<Light>().Where(l=>l.gameObject.activeInHierarchy).ToArray();var enabled=lights.Select(l=>l.enabled).ToArray();
        foreach(var light in lights)light.enabled=false;var idleOff=Capture(camera,character+"-idle-light-off");for(int i=0;i<lights.Length;i++)lights[i].enabled=enabled[i];var idleOn=Capture(camera,character+"-idle-light-on");
        var idle=lights.Select(l=>new{l.name,l.intensity,l.range,position=l.transform.position.ToString("F3")}).ToArray();
        var fronts=GameObject.Find("Generated Room").GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Textured Front").ToArray();var originals=fronts.Select(r=>r.sharedMaterial).ToArray();var owned=originals.Distinct().ToDictionary(m=>m,m=>new Material(m));
        foreach(var m in owned.Values){m.DisableKeyword("_NORMALMAP");Warm(m);}for(int i=0;i<fronts.Length;i++)fronts[i].sharedMaterial=owned[originals[i]];
        var flat=Capture(camera,character+"-normal-map-off");for(int i=0;i<fronts.Length;i++)fronts[i].sharedMaterial=originals[i];foreach(var m in owned.Values)UnityEngine.Object.Destroy(m);
        // Let first-use shader work settle before observing the short strike-light envelope.
        for(int frame=0;frame<20;frame++)await Awaitable.NextFrameAsync();
        var attack=actor.GetComponent<PlayerAttack>();attack.HandleAttack();double deadline=EditorApplication.timeSinceStartup+2;
        while(attack.IsBusy&&lights.All(l=>l.intensity<=.7f)&&EditorApplication.timeSinceStartup<deadline){await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();}
        foreach(var light in lights)light.enabled=false;var strikeOff=Capture(camera,character+"-strike-light-off");for(int i=0;i<lights.Length;i++)lights[i].enabled=enabled[i];var strikeOn=Capture(camera,character+"-strike-light-on");
        var report=new{character,firstFrame,frame=Time.frameCount,began,time=Time.time,move=attack.CurrentAction,elapsed=attack.ActionElapsed,idle,peak=lights.Select(l=>new{l.name,l.intensity,l.range,position=l.transform.position.ToString("F3")}).ToArray(),idleResponse=Difference(camera,idleOff,idleOn),normalContribution=Difference(camera,flat,idleOn),strikeResponse=Difference(camera,strikeOff,strikeOn),meshFaces=fronts.Length,enabledSprites=GameObject.Find("Generated Room").GetComponentsInChildren<SpriteRenderer>().Count(s=>s.enabled),visualColliders=fronts.Sum(r=>r.transform.parent.GetComponentsInChildren<Collider>().Length)};
        UnityEngine.Object.Destroy(actor);await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();return report;
    }
}
