using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

// Operator-invoked scene-depth/light observation on disposable Play Mode objects.
public static class PreviewWorldVfx
{
    private static async Task Capture(string name,int delay)
    {
        await Task.Delay(delay);Directory.CreateDirectory("Logs/WorldVfx");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/WorldVfx/"+name+".png"));await Task.Delay(100);
    }
    public static async Task<string> Observe()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode and stage the isolated player preview first.");
        var player=GameObject.Find("Zero Revision Preview");var animator=player.GetComponent<CharacterAnimator>().Animator;
        animator.Play("Idle",0,.3f);animator.Update(0);animator.speed=0;
        var platform=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Foreground policy preview platform").gameObject;
        platform.SetActive(true);platform.layer=7;platform.transform.position=new Vector3(0,-.12f,0);platform.transform.localScale=new Vector3(4,.25f,1);
        GameObject.Find("Preview ground").layer=7;
        Physics.SyncTransforms();int startFrame=Time.frameCount;float startTime=Time.time;
        var water=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<WaterEffect>("Assets/_Project/VFX/ZeroWater/Prefabs/RisingFountain.prefab"),Vector3.zero,Quaternion.identity);
        water.Play(2f);var waterRoot=water.transform.position;var emitter=water.GetComponentInChildren<ParticleSystem>().transform;
        var text="Water root="+waterRoot+" emitter="+emitter.position+" platform front="+platform.GetComponent<Collider>().bounds.min.z+"\n";
        await Capture("Surface-clearance",450);UnityEngine.Object.Destroy(water.gameObject);
        var profile=AssetDatabase.LoadAssetAtPath<FireSkillSettings>("Assets/_Project/VFX/FireSkills/Profiles/MegaSlashFire.asset");
        var fire=UnityEngine.Object.Instantiate(profile.burst);fire.Play(profile,new Vector3(.35f,.7f,0),Vector3.right,false,null);
        foreach(var controller in fire.GetComponentsInChildren<Animator>(true))
        {
            var clip=controller.runtimeAnimatorController.animationClips.First();clip.SampleAnimation(controller.gameObject,.06f);controller.enabled=false;
        }
        foreach(var ps in fire.GetComponentsInChildren<ParticleSystem>(true))
        {ps.Simulate(.12f,false,true,true);ps.Pause(false);}
        var lights=fire.GetComponentsInChildren<Light>(true);
        text+="Native lights="+string.Join(",",lights.Select(l=>l.name+" intensity="+l.intensity+" range="+l.range+" mask="+l.cullingMask+" position="+l.transform.position))+"\n";
        var collision=fire.GetComponentsInChildren<ParticleSystem>(true).Where(p=>p.collision.enabled).ToArray();
        text+="Collision emitters="+string.Join(",",collision.Select(p=>p.name+" local="+p.transform.localPosition))+"\n";
        foreach(var light in lights)light.enabled=true;
        await Capture("Native-light-on",50);
        foreach(var light in lights)light.enabled=false;
        await Capture("Native-light-off",50);
        return text+"Frames "+startFrame+"->"+Time.frameCount+" time "+startTime+"->"+Time.time;
    }
}
