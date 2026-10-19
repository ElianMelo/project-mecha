using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Operator-driven manual preview controls and telemetry; no test runner or assertions.
public static class MeleeSessionInspection
{
    public static string StartAuthorityPreview()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode");
        var manager=GameManager.Instance.Network;
        if(manager.ServerManager.Started||manager.ClientManager.Started)throw new InvalidOperationException("Preserve existing connection");
        manager.TransportManager.GetTransport<FishNet.Transporting.UTP.UnityTransport>().SetConnectionData("127.0.0.1",7777,"127.0.0.1");
        return "Loopback server="+manager.ServerManager.StartConnection()+" client="+manager.ClientManager.StartConnection();
    }
    public static string AuthorityRead()
    {
        return string.Join("\n",UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).Where(a=>a.IsSpawned).Select(a=>a.name+" owner="+a.IsOwner+" server="+a.IsServerInitialized+" ready="+a.GetComponent<CharacterRuntime>().Ready+" action="+a.CurrentAction+" serverBusy="+a.ServerBusy+" charges="+a.GetComponent<PlayerHealth>().EscapeCharges+" stunned="+a.GetComponent<PlayerHealth>().IsStunned+" dash="+a.GetComponent<PlayerMovement>().IsDashing));
    }
    public static async System.Threading.Tasks.Task<string> AuthorityEscape()
    {
        var attack=UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).First(a=>a.IsSpawned&&a.IsOwner);
        var health=attack.GetComponent<PlayerHealth>();health.TakeHit(1f,new Vector3(-2,0,0));string report=AuthorityRead();
        attack.GetComponent<PlayerMovement>().HandleDash(1);await System.Threading.Tasks.Task.Delay(90);return report+"\nAfter request: "+AuthorityRead();
    }
    public static async System.Threading.Tasks.Task<string> AuthorityHeavy()
    {
        var attack=UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).First(a=>a.IsSpawned&&a.IsOwner);
        attack.HandleTech();string report=AuthorityRead();await System.Threading.Tasks.Task.Delay(100);return report+"\nAfter request: "+AuthorityRead();
    }
    static GameObject Actor(string name) => GameObject.Find("Melee Preview "+name);
    public static async System.Threading.Tasks.Task<string> Stage()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode");
        foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))c.enabled=false;
        foreach(var c in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if(c is RoomCameraRig||c is RoomCameraZoom||c.GetType().Name=="CinemachineBrain"||c is EnemySpawner)c.enabled=false;
        var camera=Camera.main;camera.transform.SetPositionAndRotation(new Vector3(-50,1,-12),Quaternion.identity);camera.orthographic=true;camera.orthographicSize=2.3f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.055f,.07f,.09f);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Melee Preview Floor";floor.layer=6;floor.transform.position=new Vector3(-50,-.15f,0);floor.transform.localScale=new Vector3(18,.3f,2);
        floor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/MechaGround.mat");
        for(int i=0;i<2;i++)
        {
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/MechaPlayer.prefab"));go.name="Melee Preview "+(i==0?"Dasher":"Zero");go.transform.SetPositionAndRotation(new Vector3(-52+i*4,0,0),Quaternion.Euler(0,90,0));
            go.GetComponent<CharacterRuntime>().enabled=false;go.GetComponent<CharacterRuntime>().Apply(i);
            go.GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled=false;go.GetComponent<PlayerInputs>().enabled=false;
            go.GetComponent<PlayerMovement>().Face(1);
        }
        foreach(float distance in new[]{1.4f,2.3f})
        {
            var target=new GameObject("Melee Receiver "+distance.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));target.layer=9;target.transform.position=new Vector3(-52+distance,.85f,0);var collider=target.AddComponent<BoxCollider>();collider.size=new Vector3(.25f,1.4f,.4f);target.AddComponent<ComboTrainingTarget>();
        }
        await System.Threading.Tasks.Task.Delay(100);
        foreach(var actor in UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(a=>a.name.StartsWith("Melee Preview")))actor.gameObject.SetActive(true);
        return Read();
    }
    public static string Read()
    {
        string result="frame="+Time.frameCount+" time="+Time.time+"\n";
        foreach(string name in new[]{"Dasher","Zero"})
        {
            var go=Actor(name);if(go==null)continue;var attack=go.GetComponent<PlayerAttack>();var hp=go.GetComponent<PlayerHealth>();
            var animator=go.GetComponent<CharacterAnimator>().Animator;
            result+=name+" pos="+go.transform.position+" grounded="+go.GetComponent<PlayerMovement>().IsGrounded+" action="+attack.CurrentAction+" phase="+attack.Phase+" armor="+hp.Health+" stagger="+hp.IsStunned+" dash="+go.GetComponent<PlayerMovement>().IsDashing+" charges="+hp.EscapeCharges+" refill="+hp.EscapeRefillRemaining+" damage="+attack.TotalDamage+" anim="+animator.GetCurrentAnimatorStateInfo(0).normalizedTime+"\n";
        }
        foreach(var target in UnityEngine.Object.FindObjectsByType<ComboTrainingTarget>(FindObjectsSortMode.None))result+=target.name+" hits="+target.Hits+" damage="+target.TotalDamage+"\n";
        return result;
    }
    public static string Strike(string character,bool heavy)
    {
        var go=Actor(character);go.GetComponent<CharacterAnimator>().enabled=true;go.GetComponent<PlayerMovement>().enabled=true;go.GetComponent<Rigidbody>().isKinematic=false;
        if(heavy)go.GetComponent<PlayerAttack>().HandleTech();else go.GetComponent<PlayerAttack>().HandleAttack();return Read();
    }
    public static string Pose(string character,string state,float fraction)
    {
        var go=Actor(character);go.GetComponent<PlayerAttack>().Interrupt();go.GetComponent<CharacterAnimator>().enabled=false;go.GetComponent<PlayerMovement>().enabled=false;go.GetComponent<Rigidbody>().isKinematic=true;
        var animator=go.GetComponent<CharacterAnimator>().Animator;animator.SetFloat("PlaybackSpeed",0f);animator.Play(state,0,fraction);animator.Update(0f);return Read();
    }
    public static string Hit(string character)
    {var go=Actor(character);go.GetComponent<PlayerMovement>().enabled=true;go.GetComponent<Rigidbody>().isKinematic=false;go.GetComponent<CharacterAnimator>().enabled=true;go.GetComponent<PlayerHealth>().TakeHit(1f,new Vector3(-2,0,0));return Read();}
    public static string Escape(string character)
    {Actor(character).GetComponent<PlayerMovement>().HandleDash(1);return Read();}
    public static string ResetPositions()
    {
        foreach(string name in new[]{"Dasher","Zero"})
        {var go=Actor(name);go.GetComponent<PlayerAttack>().Interrupt();go.GetComponent<PlayerMovement>().enabled=true;go.GetComponent<Rigidbody>().isKinematic=false;go.GetComponent<Rigidbody>().position=go.transform.position=new Vector3(name=="Dasher"?-52:-48,0,0);go.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;}
        return Read();
    }
    public static async System.Threading.Tasks.Task<string> StaggerEscape(string character)
    {
        string report=Hit(character);report+="\nEscape: "+Escape(character);await System.Threading.Tasks.Task.Delay(100);report+="\nAfter escape: "+Read();return report;
    }
    public static async System.Threading.Tasks.Task<string> MixedInputs(string character)
    {
        ResetPositions();string report=Strike(character,false);
        await System.Threading.Tasks.Task.Delay(340);report+="\nY: "+Strike(character,true);
        await System.Threading.Tasks.Task.Delay(740);report+="\nX: "+Strike(character,false);
        return report;
    }
    public static async System.Threading.Tasks.Task<string> CaptureSwing(string character,bool heavy)
    {
        ResetPositions();await System.Threading.Tasks.Task.Delay(220);int frame=Time.frameCount;float time=Time.time;Strike(character,heavy);
        var attack=Actor(character).GetComponent<PlayerAttack>();
        double deadline=EditorApplication.timeSinceStartup+2;
        while(attack.IsBusy&&attack.ActionElapsed<attack.ActiveData.impact+.025f&&EditorApplication.timeSinceStartup<deadline)await System.Threading.Tasks.Task.Delay(1);
        string pose=Read();
        System.IO.Directory.CreateDirectory("Logs/MeleeCombat");string path=System.IO.Path.GetFullPath("Logs/MeleeCombat/"+character+(heavy?"Heavy":"Light")+".png");
        ScreenCapture.CaptureScreenshot(path);await System.Threading.Tasks.Task.Delay(150);
        return "Advancing frames "+frame+" -> "+Time.frameCount+" time "+time+" -> "+Time.time+" image="+path+"\nAt capture: "+pose+"\n"+Read();
    }
}
