using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Manual operator controls and observed telemetry. No Test Runner integration or assertions.
public static class CombatFollowupInspection
{
    public static string ShowGeneral()
    {
        var options=GameOptionsMenu.Instance;options.Open();
        var button=(UnityEngine.UI.Button)new SerializedObject(options).FindProperty("generalButton").objectReferenceValue;button.onClick.Invoke();
        var menu=UnityEngine.Object.FindFirstObjectByType<GeneralConfigurationMenu>(FindObjectsInactive.Include);
        foreach(var canvas in menu.GetComponentsInParent<Canvas>(true))canvas.enabled=true;
        ScreenCapture.CaptureScreenshot("Logs/SwordRevision/GeneralOptions.png");
        return string.Join("\n",menu.GetComponentsInChildren<TMPro.TMP_Dropdown>(true).Select(d=>d.name+" value="+d.value+" text="+d.captionText.text+" up="+d.navigation.selectOnUp?.name+" down="+d.navigation.selectOnDown?.name));
    }
    public static async Task<string> ToggleHighlight()
    {
        var menu=UnityEngine.Object.FindFirstObjectByType<GeneralConfigurationMenu>(FindObjectsInactive.Include);var dropdown=(TMPro.TMP_Dropdown)new SerializedObject(menu).FindProperty("selfHighlight").objectReferenceValue;
        int original=dropdown.value;dropdown.value=1;await Task.Delay(40);
        string result="Self enabled: "+ReadHighlights();dropdown.value=0;await Task.Delay(40);result+="\nSelf disabled: "+ReadHighlights();dropdown.value=original;
        return result;
    }
    static string ReadHighlights()
    {
        var hud=GameObject.Find("Melee Preview Dasher").GetComponentInChildren<ActorWorldHud>(true);var so=new SerializedObject(hud);
        return "name="+((TMPro.TMP_Text)so.FindProperty("pilotName").objectReferenceValue).enabled+" ring="+((UnityEngine.UI.Image)so.FindProperty("ring").objectReferenceValue).enabled;
    }
    public static string StageCrate()
    {
        GameOptionsMenu.Instance.Close();
        var crate=UnityEngine.Object.FindObjectsByType<DestructibleProp>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();crate.gameObject.SetActive(true);crate.transform.position=new Vector3(-50.25f,.5f,0);crate.RestoreForLevel();Physics.SyncTransforms();
        return crate.name+" health="+crate.Health+" bounds="+crate.TargetBounds+" bullet plane Z=0";
    }
    static void Render(Camera camera,string path)
    {
        var previous=camera.targetTexture;var active=RenderTexture.active;
        var rt=RenderTexture.GetTemporary(1400,800,24,RenderTextureFormat.ARGBHalf);var image=new Texture2D(1400,800,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,800),0,0);image.Apply();System.IO.File.WriteAllBytes(path,image.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
    public static async Task<string> FireCrate()
    {
        var crate=UnityEngine.Object.FindObjectsByType<DestructibleProp>(FindObjectsSortMode.None).OrderBy(c=>Mathf.Abs(c.transform.position.x+50.25f)).First();
        var actor=GameObject.Find("Melee Preview Dasher");var settings=(SecondaryWeaponSettings)new SerializedObject(actor.GetComponent<PlayerSecondaryWeapon>()).FindProperty("settings").objectReferenceValue;
        var vfx=AssetDatabase.LoadAssetAtPath<CombatVfxSettings>("Assets/_Project/Settings/Effects/CombatVfx.asset");string effectName=vfx.damageSparkPrefab.name+"(Clone)";
        float before=crate.Health;await Task.Delay(220);int frame=Time.frameCount;
        var oldEffects=new System.Collections.Generic.HashSet<int>(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name==effectName).Select(t=>t.gameObject.GetInstanceID()));
        var bullet=UnityEngine.Object.Instantiate(settings.projectile,crate.TargetBounds.center+Vector3.left*1.2f,Quaternion.identity);
        if(GameManager.Instance.Network.ServerManager.Started)GameManager.Instance.Network.ServerManager.Spawn(bullet.gameObject);
        bullet.GetComponent<SecondaryWeaponProjectile>().Launch(settings,actor.GetComponent<PlayerHealth>(),Vector3.right);
        double limit=EditorApplication.timeSinceStartup+1;GameObject effect=null;
        while(EditorApplication.timeSinceStartup<limit)
        {
            var found=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).FirstOrDefault(t=>t.name==effectName&&!oldEffects.Contains(t.gameObject.GetInstanceID())&&Vector3.Distance(t.position,crate.transform.position)<1f);
            if(found!=null){effect=found.gameObject;break;}await Task.Delay(1);
        }
        string result="frame="+frame+" -> "+Time.frameCount+" crate health="+before+" -> "+crate.Health+" effect="+(effect!=null);
        if(effect!=null)
        {
            int birthFrame=Time.frameCount;while(Time.frameCount<=birthFrame)await Task.Delay(1);
            foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>())ps.Pause(false);var animator=effect.GetComponent<Animator>();animator.speed=0;
            var light=effect.GetComponentInChildren<Light>();float intensity=light.intensity;
            light.enabled=false;Render(Camera.main,"Logs/SwordRevision/SparksLightOff.png");light.enabled=true;Render(Camera.main,"Logs/SwordRevision/SparksLightOn.png");
            var volume=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).First(v=>v.isGlobal);var runtime=volume.profile;
            if(runtime.TryGet<Bloom>(out var bloom)){bool original=bloom.active;bloom.active=false;Render(Camera.main,"Logs/SwordRevision/SparksBloomOff.png");bloom.active=original;}
            result+=" intensity="+intensity+" range="+light.range+" rebound="+effect.transform.forward;
            foreach(var ps in effect.GetComponentsInChildren<ParticleSystem>())ps.Play(false);animator.speed=1;
        }
        return result;
    }
}
