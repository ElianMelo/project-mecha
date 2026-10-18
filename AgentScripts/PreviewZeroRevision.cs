using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

// Operator-invoked visual preview. No networking, tests, damage or asset mutations.
public static class PreviewZeroRevision
{
    public static async Task<string> Stage()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode for the visual preview.");
        var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/MechaPlayer.prefab"));root.name="Zero Revision Preview";root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,90,0));
        foreach(var c in root.GetComponents<MonoBehaviour>())if(!(c is CharacterRuntime)&&!(c is SecondaryWeaponPresentation)&&!(c is MechaAutoAim))c.enabled=false;
        root.GetComponent<Rigidbody>().isKinematic=true;root.GetComponent<CharacterRuntime>().enabled=false;root.GetComponent<CharacterRuntime>().Apply(1);
        var camera=Camera.main;
        foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if(c.GetType().Name=="CinemachineBrain"||c is RoomCameraRig||c is RoomCameraZoom)c.enabled=false;
        camera.transform.SetPositionAndRotation(new Vector3(0,.9f,-12),Quaternion.identity);camera.orthographic=true;camera.orthographicSize=2.2f;
        var barrier=GameObject.CreatePrimitive(PrimitiveType.Cube);barrier.name="Foreground policy preview platform";barrier.transform.position=new Vector3(0,.65f,-1);barrier.transform.localScale=new Vector3(4f,.3f,1f);
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.25f,.28f,.3f));barrier.GetComponent<Renderer>().material=material;
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Preview ground";floor.transform.position=new Vector3(0,-.12f,0);floor.transform.localScale=new Vector3(8,.2f,2);
        floor.GetComponent<Renderer>().material=material;
        await Task.Delay(150);var animator=root.GetComponent<CharacterAnimator>().Animator;animator.SetFloat("PlaybackSpeed",1f);animator.Play("Run",0,0);
        return "Native Run animator active; Zero rig scale="+root.GetComponent<CharacterRuntime>().Definition.visualScale+"; frame="+Time.frameCount+" time="+Time.time;
    }
    public static string Pose(string state)
    {var root=GameObject.Find("Zero Revision Preview");root.GetComponent<CharacterAnimator>().Animator.Play(state,0,0);return "Playing "+state+" at frame "+Time.frameCount;}
    public static async Task<string> Fountain()
    {
        UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(t=>t.name=="Foreground policy preview platform").gameObject.SetActive(true);
        var prefab=AssetDatabase.LoadAssetAtPath<WaterEffect>("Assets/_Project/VFX/ZeroWater/Prefabs/RisingFountain.prefab");var effect=UnityEngine.Object.Instantiate(prefab,new Vector3(0,0,1.5f),Quaternion.identity);effect.Play(1.6f);
        return "Fountain behind platform/player; "+await Capture("Logs/ZeroRevision/Foreground.png",450);
    }
    public static async Task<string> Motion()
    {
        GameObject.Find("Foreground policy preview platform").SetActive(false);var text=Pose("Run");
        for(int i=0;i<4;i++)text+="\n"+await Capture("Logs/ZeroRevision/Run"+i+".png",100);
        return text;
    }
    public static async Task<string> Capture(string path,int delay)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));int frame=Time.frameCount;float start=Time.time;await Task.Delay(delay);
        ScreenCapture.CaptureScreenshot(Path.GetFullPath(path));await Task.Delay(150);
        return "frames="+frame+"->"+Time.frameCount+" time="+start+"->"+Time.time+" output="+path;
    }
}
