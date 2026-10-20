using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

// Operator-driven capture of one real swing and observed PhysX contacts. No test assertions.
public static class InspectBladeSweepRuntime
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly string Output="Logs/BladeCollisionRevision";
    static GameObject Actor(string character)=>UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(a=>a.name=="Melee Preview "+character).gameObject;
    static void Capture(string name)
    {
        System.IO.Directory.CreateDirectory(Output);
        var camera=Camera.main;var old=camera.targetTexture;var active=RenderTexture.active;
        var target=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();System.IO.File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(texture);}
    }
    public static async Task<object> Swing(string character,string move,int facing)
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode");
        foreach(string other in new[]{"Dasher","Zero"})Actor(other).SetActive(other==character);
        var actor=Actor(character);var attack=actor.GetComponent<PlayerAttack>();attack.Interrupt();attack.ResetDamageReadout();
        var body=actor.GetComponent<Rigidbody>();body.position=actor.transform.position=new Vector3(-52,0,0);body.linearVelocity=Vector3.zero;
        actor.GetComponent<PlayerMovement>().Face(facing);
        foreach(var target in UnityEngine.Object.FindObjectsByType<ComboTrainingTarget>(FindObjectsSortMode.None))target.gameObject.SetActive(false);
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include,FindObjectsSortMode.None))UnityEngine.Object.Destroy(enemy.gameObject);
        var receiver=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EvaEnemy.prefab"));
        receiver.name="Blade Contact Receiver";receiver.transform.position=new Vector3(-52+facing*.95f,0,0);receiver.GetComponent<EnemyController>().PracticePassive=true;
        var far=new GameObject("Beyond Blade Receiver");far.layer=9;far.transform.position=new Vector3(-52+facing*2.3f,.85f,0);
        far.AddComponent<BoxCollider>().size=new Vector3(.18f,1.4f,.4f);var farHealth=far.AddComponent<ComboTrainingTarget>();
        var camera=Camera.main;camera.transform.SetPositionAndRotation(new Vector3(-52+facing*.5f,1.1f,-12),Quaternion.identity);camera.orthographicSize=3.485f;
        await Task.Delay(150);receiver.SetActive(true);await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();
        int piece=attack.Settings.IndexOf((PlayerAttack.ActionKind)Enum.Parse(typeof(PlayerAttack.ActionKind),move));
        typeof(PlayerAttack).GetMethod("TryBeginPiece",Private).Invoke(attack,new object[]{piece,facing});
        var sweep=(MeleeBladeSweep)typeof(PlayerAttack).GetField("bladeSweep",Private).GetValue(attack);
        var observations=new List<object>();int firstFrame=Time.frameCount;float began=Time.time;
        bool captured=false;double deadline=EditorApplication.timeSinceStartup+3;
        while(attack.IsBusy && EditorApplication.timeSinceStartup<deadline)
        {
            await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();
            var contacts=((IEnumerable<MeleeBladeSweep.Contact>)typeof(MeleeBladeSweep).GetField("contacts",Private).GetValue(sweep)).ToArray();
            var particles=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(ps=>ps.name=="Rebounding metal").Select(ps=>
            {
                var data=new ParticleSystem.Particle[ps.main.maxParticles];int n=ps.GetParticles(data);Vector3 mean=Vector3.zero;
                for(int i=0;i<n;i++)mean+=data[i].velocity;if(n>0)mean/=n;
                return new{count=n,meanVelocity=mean.ToString("F3"),axis=ps.transform.forward.ToString("F3"),
                    forwardDot=n>0?Vector3.Dot(mean.normalized,ps.transform.forward):0};
            }).ToArray();
            var blade=actor.GetComponent<CombatEmissionOrigin>().Blade;blade.Read(out var a,out var b,actor.transform.position.z);
            observations.Add(new{frame=Time.frameCount,elapsed=attack.ActionElapsed,active=attack.MeleeContactActive,
                bladeBase=a.ToString("F3"),bladeTip=b.ToString("F3"),health=receiver.GetComponent<EnemyController>().Health,farHits=farHealth.Hits,
                contacts=contacts.Select(c=>new{target=c.Collider.name,point=c.Surface.Point.ToString("F3"),incoming=c.Surface.Incoming.ToString("F3")}).ToArray(),particles});
            if(contacts.Length>0 && !captured)
            {
                // Freeze only the newly born cosmetic systems to inspect their real launch velocities.
                foreach(var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(ps=>ps.name=="DamageSparks(Clone)"))
                {foreach(var child in ps.GetComponentsInChildren<ParticleSystem>())child.Pause(false);ps.GetComponent<Animator>().speed=0;}
                Capture(character+"-"+move+"-"+facing+"-contact");captured=true;
                foreach(var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(ps=>ps.name=="DamageSparks(Clone)"))
                {foreach(var child in ps.GetComponentsInChildren<ParticleSystem>())child.Play(false);ps.GetComponent<Animator>().speed=1;}
            }
        }
        Capture(character+"-"+move+"-"+facing+"-recovery");
        var report=new{character,move,facing,firstFrame,frame=Time.frameCount,began,time=Time.time,usesBlade=attack.UsesBladeContact,
            dealt=attack.TotalDamage,farHits=farHealth.Hits,observations};
        UnityEngine.Object.Destroy(far);return report;
    }
    public static async Task<object> HostContact()
    {
        var attack=UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).First(a=>a.IsSpawned&&a.IsOwner);
        foreach(var old in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(e=>e.name=="Network Blade Receiver"||e.name=="Blade Contact Receiver"))
        {if(old.IsSpawned)GameManager.Instance.Network.ServerManager.Despawn(old.NetworkObject);else UnityEngine.Object.Destroy(old.gameObject);}
        foreach(string character in new[]{"Dasher","Zero"})Actor(character).SetActive(false);
        var actor=attack.gameObject;attack.Interrupt();attack.ResetDamageReadout();
        actor.GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled=false;actor.GetComponent<PlayerInputs>().enabled=false;
        actor.GetComponent<Rigidbody>().position=actor.transform.position=new Vector3(-52,0,0);
        actor.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;actor.GetComponent<PlayerMovement>().Face(-1);
        var receiver=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EvaEnemy.prefab"));
        receiver.name="Network Blade Receiver";receiver.transform.position=new Vector3(-52.85f,0,0);
        receiver.GetComponent<EnemyController>().PracticePassive=true;
        GameManager.Instance.Network.ServerManager.Spawn(receiver);
        await Task.Delay(150);await Awaitable.EndOfFrameAsync();
        receiver.GetComponent<Rigidbody>().position=receiver.transform.position=new Vector3(-52.85f,0,0);
        receiver.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
        receiver.GetComponent<FishNet.Component.Transforming.NetworkTransform>()?.Teleport();
        await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();
        var enemy=receiver.GetComponent<EnemyController>();float before=enemy.Health;int firstFrame=Time.frameCount;
        attack.HandleAttack();
        var samples=new List<object>();Vector3 incoming=Vector3.zero;double deadline=EditorApplication.timeSinceStartup+2;
        while(EditorApplication.timeSinceStartup<deadline)
        {
            await Awaitable.NextFrameAsync();await Awaitable.EndOfFrameAsync();
            var sweep=(MeleeBladeSweep)typeof(PlayerAttack).GetField("bladeSweep",Private).GetValue(attack);
            var contacts=((IEnumerable<MeleeBladeSweep.Contact>)typeof(MeleeBladeSweep).GetField("contacts",Private).GetValue(sweep)).ToArray();
            if(contacts.Length>0)incoming=contacts[0].Surface.Incoming;
            if(attack.IsBusy || attack.ServerBusy)
            {
                actor.GetComponent<CombatEmissionOrigin>().Blade.Read(out var a,out var b,actor.transform.position.z);
                samples.Add(new{frame=Time.frameCount,elapsed=attack.ActionElapsed,action=attack.CurrentAction,attack.ServerBusy,enemyHealth=enemy.Health,
                    target=receiver.transform.position.ToString("F2"),bladeBase=a.ToString("F2"),bladeTip=b.ToString("F2"),incoming=incoming.ToString("F3")});
            }
            if(incoming.sqrMagnitude>0)
            {
                var ps=UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).FirstOrDefault(p=>p.name=="Rebounding metal"&&p.particleCount>0);
                if(ps!=null)
                {
                    var p=new ParticleSystem.Particle[ps.main.maxParticles];int n=ps.GetParticles(p);Vector3 mean=Vector3.zero;for(int i=0;i<n;i++)mean+=p[i].velocity;
                    samples.Add(new{frame=Time.frameCount,count=n,velocity=mean.normalized.ToString("F3"),dotIncoming=Vector3.Dot(mean.normalized,incoming)});
                    Capture("LocalHost-contact");break;
                }
            }
        }
        return new{firstFrame,frame=Time.frameCount,attack.IsSpawned,attack.IsServerInitialized,receiverSpawned=enemy.IsSpawned,before,after=enemy.Health,attack.TotalDamage,usesBlade=attack.UsesBladeContact,samples};
    }
}
