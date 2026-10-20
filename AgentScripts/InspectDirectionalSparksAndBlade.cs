using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

// Operator-invoked, local Play Mode observation and captures; no assertions/test runner.
public static class InspectDirectionalSparksAndBlade
{
    static string Output="Logs/DirectionalSparks";
    static void Render(string name)
    {
        System.IO.Directory.CreateDirectory(Output);
        var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();System.IO.File.WriteAllBytes(Output+"/"+name+".png",image.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
    public static async Task<object> Swing(string character, string move, int facing)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode");
        var actor=UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(a=>a.name=="Melee Preview "+character).gameObject;
        foreach(string other in new[]{"Dasher","Zero"})
        {var go=UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(a=>a.name=="Melee Preview "+other).gameObject;go.SetActive(other==character);}
        var attack=actor.GetComponent<PlayerAttack>();attack.Interrupt();
        actor.GetComponent<Rigidbody>().position=actor.transform.position=new Vector3(-52,0,0);actor.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
        actor.GetComponent<PlayerMovement>().Face(facing);
        foreach(var target in UnityEngine.Object.FindObjectsByType<ComboTrainingTarget>(FindObjectsSortMode.None))target.gameObject.SetActive(false);
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.Destroy(enemy.gameObject);
        var receiver=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EvaEnemy.prefab"));
        receiver.name="Directional Sparks Receiver";receiver.transform.position=new Vector3(-52+facing*1.1f,0,0);
        receiver.SetActive(true);
        receiver.GetComponent<EnemyController>().PracticePassive=true;
        var camera=Camera.main;camera.transform.SetPositionAndRotation(new Vector3(-52+facing*.5f,1.1f,-12),Quaternion.identity);camera.orthographicSize=3.485f;
        await Task.Delay(200);
        // FishNet's unspawned prefab startup deactivates it once; activate the local
        // inspection receiver after startup, as for the existing preview players.
        receiver.SetActive(true);
        await Awaitable.EndOfFrameAsync();
        var action=attack.Settings.Get((PlayerAttack.ActionKind)Enum.Parse(typeof(PlayerAttack.ActionKind),move));
        int piece=attack.Settings.IndexOf(action.kind);
        typeof(PlayerAttack).GetMethod("TryBeginPiece",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(attack,new object[]{piece,facing});
        int initialFrame=Time.frameCount;float initialTime=Time.time;var observations=new List<object>();
        string prefix=character+"-"+move+"-"+facing;
        foreach(float sample in new[]{Mathf.Max(0,action.impact-.025f),action.impact+.07f,action.impact+action.hitActiveTime+.14f})
        {
            double deadline=EditorApplication.timeSinceStartup+3;
            while(attack.IsBusy && attack.ActionElapsed<sample && EditorApplication.timeSinceStartup<deadline)await Task.Delay(1);
            await Awaitable.EndOfFrameAsync();
            var trails=actor.GetComponentsInChildren<TrailRenderer>().Select(t=>new{t.name,t.emitting,points=t.positionCount,source=t.transform.position.ToString("F3")}).ToArray();
            var effects=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(ps=>ps.name=="Rebounding metal").Select(ps=>new{direction=ps.transform.root.forward.ToString("F2"),count=ps.particleCount}).ToArray();
            observations.Add(new{sample,elapsed=attack.ActionElapsed,frame=Time.frameCount,damage=attack.TotalDamage,receiverHealth=receiver.GetComponent<EnemyController>().Health,trails,effects});
            if (sample >= action.impact) Render(prefix+"-"+sample.ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
        }
        await Task.Delay(900);
        var report=new{character,move,facing,initialFrame,frame=Time.frameCount,initialTime,time=Time.time,observations,
            remainingEffects=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="DamageSparks(Clone)")};
        return report;
    }
    public static async Task<object> Contact(int facing)
    {
        var receiver=GameObject.Find("Directional Sparks Receiver");
        if(receiver==null)
        {
            receiver=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EvaEnemy.prefab"));
            receiver.name="Directional Sparks Receiver";receiver.transform.position=new Vector3(-50.9f,0,0);
            receiver.GetComponent<EnemyController>().PracticePassive=true;
            await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();receiver.SetActive(true);
        }
        var feedback=receiver.GetComponent<CharacterHitFeedback>();
        await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();
        int frame=Time.frameCount;float started=Time.time;feedback.Hit(0,new Vector3(facing,.2f,0));
        while(Time.time-started<.06f){await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();}
        var effect=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).First(ps=>ps.name=="DamageSparks(Clone)");
        foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>())ps.Pause(false);
        var animator=effect.GetComponent<Animator>();animator.speed=0;
        var light=effect.GetComponentInChildren<Light>();float intensity=light.intensity;
        light.enabled=false;Render("Contact-"+facing+"-LightOff");light.enabled=true;Render("Contact-"+facing+"-LightOn");
        var result=new{initialFrame=frame,frame=Time.frameCount,age=Time.time-started,normalized=animator.GetCurrentAnimatorStateInfo(0).normalizedTime,direction=effect.transform.forward.ToString("F3"),intensity,light.range,
            particles=effect.GetComponentsInChildren<ParticleSystem>().Select(ps=>new{ps.name,ps.particleCount}).ToArray()};
        animator.speed=1;foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>())ps.Play(false);
        return result;
    }
}
