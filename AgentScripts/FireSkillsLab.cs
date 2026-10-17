using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

// Manual Play Mode staging/capture. Uses real combat and existing training receivers;
// no assertions, automated tests, builds, network sessions or persistent scene edits.
public static class FireSkillsLab
{
    public static object Stage()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).Any())
            throw new InvalidOperationException("Stage expects no live player; do not replace a user's session.");
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) canvas.enabled = false;
        var player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/MechaPlayer.prefab"));
        player.GetComponent<FishNet.Object.NetworkObject>().SetIsNetworked(false);
        player.name = "Manual Fire Skill Preview";
        player.transform.position = new Vector3(-5f, -3.4f, 0f);
        player.GetComponent<PlayerMovement>().enabled = false;
        player.GetComponent<PlayerInputs>().enabled = false;
        player.GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled = false;
        var body = player.GetComponent<Rigidbody>(); body.isKinematic = true;
        player.GetComponent<PlayerMovement>().Face(1);
        var camera = Camera.main;
        var brain = camera.GetComponent<Unity.Cinemachine.CinemachineBrain>(); if (brain != null) brain.enabled = false;
        camera.transform.SetPositionAndRotation(new Vector3(-3.4f,-2.25f,-10f),Quaternion.identity);
        Dummy("Near receiver",new Vector3(-3.3f,-3.4f,0f));
        Dummy("Far receiver",new Vector3(-1.7f,-3.4f,0f));
        Dummy("Behind receiver",new Vector3(-6.8f,-3.4f,0f));
        Dummy("Above receiver",new Vector3(-2.7f,-0.6f,0f));
        Physics.SyncTransforms();
        return JsonConvert.SerializeObject(new { frame=Time.frameCount,time=Time.time,camera.orthographicSize,
            actor=player.transform.position.ToString(), hand=player.GetComponent<CombatEmissionOrigin>().SkillHand.position.ToString() });
    }

    public static async Task<object> Preview(int skill, int facing = 1, bool obstruction = false, bool moving = false)
    {
        var player=GameObject.Find("Manual Fire Skill Preview");
        var attack=player.GetComponent<PlayerAttack>();attack.Interrupt();attack.ResetPracticeResources();attack.ResetDamageReadout();
        player.GetComponent<PlayerMovement>().Face(facing);
        foreach(var target in UnityEngine.Object.FindObjectsByType<ComboTrainingTarget>(FindObjectsSortMode.None))target.ResetTarget();
        var near=GameObject.Find("Near receiver");near.transform.position=new Vector3(-3.3f,moving?-0.5f:-3.4f,0f);
        GameObject barrier=null;
        if(obstruction){barrier=new GameObject("Manual occluding wall",typeof(BoxCollider));barrier.layer=6;barrier.transform.position=new Vector3(-4.0f,-2.55f,0f);barrier.GetComponent<BoxCollider>().size=new Vector3(0.12f,1.7f,0.8f);}
        Physics.SyncTransforms();
        foreach(var effect in UnityEngine.Object.FindObjectsByType<FireSkillBurst>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(effect.gameObject);
        foreach(var effect in UnityEngine.Object.FindObjectsByType<FireSkillHitVfx>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(effect.gameObject);
        await Task.Delay(160);
        int firstFrame=Time.frameCount;float started=Time.time;
        attack.HandleSkill(skill);
        string label="ASD"[skill]+(facing<0?"-left":"-right")+(obstruction?"-wall":"")+(moving?"-entry":"");
        string directory="Logs/FireSkills/Feedback07";Directory.CreateDirectory(directory);
        var captures=new System.Collections.Generic.List<object>();
        foreach(float at in new[]{0.12f,0.24f,0.42f,0.70f,1.05f,1.40f,1.80f,2.40f,3.60f})
        {
            float deadline=Time.realtimeSinceStartup+12f;
            while(Time.time-started<at && Time.realtimeSinceStartup<deadline)await Task.Delay(8);
            if(moving && at>=0.32f){near.transform.position=new Vector3(-3.3f,-3.4f,0f);Physics.SyncTransforms();}
            string path=directory+"/"+label+"-"+at.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".png";
            Capture(path);
            var roots=UnityEngine.Object.FindObjectsByType<FireSkillBurst>(FindObjectsSortMode.None);
            if(skill==0 && at==0.42f)
            {
                var lights=roots.SelectMany(r=>r.GetComponentsInChildren<Light>()).ToArray();
                var enabled=lights.Select(l=>l.enabled).ToArray();
                try {foreach(var light in lights)light.enabled=false;Capture(directory+"/A-light-disabled.png");}
                finally {for(int i=0;i<lights.Length;i++)lights[i].enabled=enabled[i];}
                var volume=UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None).Single(v=>v.name=="Global Volume");
                if(volume.profile.TryGet<UnityEngine.Rendering.Universal.Bloom>(out var bloom))
                {
                    bool active=bloom.active;
                    try{bloom.active=false;Capture(directory+"/A-bloom-disabled.png");}
                    finally{bloom.active=active;}
                }
            }
            captures.Add(new{elapsed=Time.time-started,frame=Time.frameCount,path,bursts=roots.Length,
                particles=roots.Sum(r=>r.GetComponentsInChildren<ParticleSystem>().Sum(p=>p.particleCount)),
                lights=roots.SelectMany(r=>r.GetComponentsInChildren<Light>()).Select(l=>new{l.enabled,l.intensity,l.range}).ToArray(),
                preparing=UnityEngine.Object.FindObjectsByType<FireSourceWarmup>(FindObjectsSortMode.None).Length,
                warmupPositions=UnityEngine.Object.FindObjectsByType<FireSourceWarmup>(FindObjectsSortMode.None).Select(p=>p.transform.position.ToString()).ToArray(),
                fireParticles=roots.Sum(r=>r.GetComponentsInChildren<ParticleSystem>().Where(p=>p.name.Contains("Flame")).Sum(p=>p.particleCount)),
                smokeParticles=roots.Sum(r=>r.GetComponentsInChildren<ParticleSystem>().Where(p=>p.name.Contains("Rear")).Sum(p=>p.particleCount)),
                wakeBounds=roots.SelectMany(r=>r.GetComponentsInChildren<ParticleSystem>()).Where(p=>p.name.StartsWith("Rear Wake")&&p.particleCount>0).Select(p=>new{p.name,center=p.GetComponent<ParticleSystemRenderer>().bounds.center.ToString()}).ToArray(),
                hand=player.GetComponent<CombatEmissionOrigin>().SkillHand.position.ToString(),
                receivers=UnityEngine.Object.FindObjectsByType<ComboTrainingTarget>(FindObjectsSortMode.None).Select(t=>new{t.transform.parent.name,t.Hits,t.TotalDamage}).ToArray()});
        }
        string report=JsonConvert.SerializeObject(new{label,firstFrame,lastFrame=Time.frameCount,captures},Formatting.Indented);
        File.WriteAllText(directory+"/"+label+".json",report);
        if(barrier!=null)UnityEngine.Object.Destroy(barrier);
        return report;
    }

    private static void Dummy(string name,Vector3 position)
    {
        var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/LanceEnemy.prefab"));
        go.GetComponent<FishNet.Object.NetworkObject>().SetIsNetworked(false);
        go.name=name;go.transform.position=position;
        foreach(var script in go.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
        foreach(var collider in go.GetComponentsInChildren<Collider>())collider.enabled=false;
        go.GetComponent<Rigidbody>().isKinematic=true;
        var receiver=new GameObject("Manual receiver",typeof(CapsuleCollider),typeof(ComboTrainingTarget));
        receiver.layer=9;receiver.transform.SetParent(go.transform,false);
        var capsule=receiver.GetComponent<CapsuleCollider>();capsule.height=1.5f;capsule.radius=0.24f;capsule.center=Vector3.up*0.75f;
    }

    private static void Capture(string path)
    {
        var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
        var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally {camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(texture);}
    }
}
