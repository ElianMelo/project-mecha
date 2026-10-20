using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

// Native receiver configuration and operator-invoked same-frame light comparisons.
public static class PlatformLightingRevision
{
    const string Receiver="Assets/_Project/VFX/SceneSupport/Materials/IndustrialThermalReceiver.mat";
    const string Output="Logs/PlatformLighting";
    public static object Configure()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Configure in Edit Mode");
        var scenario=AssetDatabase.LoadAssetAtPath<ScenarioSettings>("Assets/_Project/Settings/World/Scenario.asset");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Receiver);
        Undo.RecordObject(scenario,"Enable native platform light reception");
        scenario.platformMaterial=material;EditorUtility.SetDirty(scenario);AssetDatabase.SaveAssetIfDirty(scenario);
        return new{material=AssetDatabase.GetAssetPath(scenario.platformMaterial),shader=material.shader.name,texture=AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")),emission=material.GetColor("_EmissionColor").ToString()};
    }
    static Color32[] Capture(Camera camera,string name)
    {
        Directory.CreateDirectory(Output);var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());return texture.GetPixels32();}
        finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
    static object Difference(Camera camera,Color32[] a,Color32[] b)
    {
        var lower=camera.WorldToViewportPoint(new Vector3(-1.2f,-4.25f,-.02f));var upper=camera.WorldToViewportPoint(new Vector3(1.2f,-3.6f,-.02f));
        int x0=Mathf.Clamp((int)(lower.x*1280),0,1279),x1=Mathf.Clamp((int)(upper.x*1280),0,1279),y0=Mathf.Clamp((int)(lower.y*720),0,719),y1=Mathf.Clamp((int)(upper.y*720),0,719);
        int changed=0,max=0,total=0;long sum=0;
        for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){int i=y*1280+x;int d=Mathf.Max(Math.Abs(a[i].r-b[i].r),Mathf.Max(Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)));if(d>1)changed++;max=Mathf.Max(max,d);sum+=d;total++;}
        return new{crop=new{x0,x1,y0,y1},pixels=total,changed,maxChannelDelta=max,meanChannelDelta=total>0?(float)sum/total:0};
    }
    public static async Task<object> Preview()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode");
        if(UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).Any())throw new InvalidOperationException("Preserve live player session");
        foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
        foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(c is RoomCameraRig||c is RoomCameraZoom||c is EnemySpawner||c.GetType().Name=="CinemachineBrain")c.enabled=false;
        var settings=AssetDatabase.LoadAssetAtPath<ProceduralLevelSettings>("Assets/_Project/Settings/World/ProceduralLevels.asset");
        var layout=ProceduralLevelLayout.Generate(settings,settings.firstRoomSeed,1,16f/9f);
        var builder=UnityEngine.Object.FindFirstObjectByType<ProceduralRoomBuilder>();if(builder==null)builder=new GameObject("Manual Platform Lighting Preview").AddComponent<ProceduralRoomBuilder>();
        builder.Build(layout,settings);
        var camera=Camera.main;camera.transform.SetPositionAndRotation(new Vector3(0,-2.5f,-12),Quaternion.identity);camera.orthographicSize=3.485f;
        foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(c is RoomCameraRig||c is RoomCameraZoom||c.GetType().Name=="CinemachineBrain")c.enabled=false;
        var faces=GameObject.Find("Generated Room").GetComponentsInChildren<SpriteRenderer>();
        var effects=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/VFX/DamageSparks/Prefabs/DamageSparks.prefab"),new Vector3(0,-3.45f,-.40f),Quaternion.LookRotation(Vector3.right,Vector3.forward));
        foreach(var animator in effects.GetComponentsInChildren<Animator>())animator.enabled=false;
        foreach(var ps in effects.GetComponentsInChildren<ParticleSystem>()){ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);var main=ps.main;main.stopAction=ParticleSystemStopAction.None;}
        var light=effects.GetComponentInChildren<Light>();light.intensity=4.2f;
        await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();int firstFrame=Time.frameCount;float began=Time.time;
        var stock=new GameObject("Stock sprite material observation").AddComponent<SpriteRenderer>();var unlit=stock.sharedMaterial;UnityEngine.Object.Destroy(stock.gameObject);
        var saved=faces.Select(s=>s.sharedMaterial).ToArray();
        foreach(var face in faces)face.sharedMaterial=unlit;
        light.enabled=false;var beforeOff=Capture(camera,"Generated-before-light-off");light.enabled=true;var beforeOn=Capture(camera,"Generated-before-light-on");
        for(int i=0;i<faces.Length;i++)faces[i].sharedMaterial=saved[i];
        light.enabled=false;var afterOff=Capture(camera,"Generated-after-light-off");light.enabled=true;var afterOn=Capture(camera,"Generated-after-light-on");
        var result=new{firstFrame,frame=Time.frameCount,began,time=Time.time,faces=faces.Length,materials=faces.Select(s=>AssetDatabase.GetAssetPath(s.sharedMaterial)).Distinct().ToArray(),before=Difference(camera,beforeOff,beforeOn),after=Difference(camera,afterOff,afterOn),light=new{light.intensity,light.range,position=light.transform.position.ToString()},renderer=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().scriptableRenderer.GetType().Name};
        UnityEngine.Object.Destroy(effects);await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();return result;
    }
}
