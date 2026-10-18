using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Operator-invoked manual staging and phase captures. No test runner or assertions.
public static class PreviewCharacterSkills
{
    static PlayerAttack Player => UnityEngine.Object.FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None).First(p=>!p.IsSpawned||p.IsOwner);
    public static string Stage()
    {
        var p=Player;
        foreach(var e in EnemyController.ActiveEnemies){e.PracticePassive=true;e.CancelAttack();}
        p.GetComponent<PlayerHealth>().EnterLevel(new Vector3(-5f,-3.4f,0));p.GetComponent<PlayerMovement>().Face(1);
        Target("Water Front",new Vector3(-3.1f,-2.5f,0));Target("Water Rear",new Vector3(-7f,-2.5f,0));
        return "Staged local owner and two native damage receivers, front/rear, for manual skill preview.";
    }
    static void Target(string name,Vector3 position)
    {
        var old=GameObject.Find(name);if(old!=null)UnityEngine.Object.Destroy(old);
        var go=new GameObject(name,typeof(BoxCollider),typeof(ComboTrainingTarget));go.layer=9;go.transform.position=position;go.GetComponent<BoxCollider>().size=new Vector3(.45f,1.4f,.5f);
    }
    static void Capture(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));var camera=Camera.main;var previous=camera.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGB32);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
        finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
    }
    public static async Task<string> Skill(int slot)
    {
        var p=Player;var started=Time.time;var frame=Time.frameCount;var initialMana=p.Mana;
        foreach(var t in UnityEngine.Object.FindObjectsByType<ComboTrainingTarget>(FindObjectsSortMode.None))t.ResetTarget();
        p.HandleSkill(slot);string folder="Logs/Characters/"+p.GetComponent<CharacterRuntime>().Definition.id+"/Skill"+slot;await Task.Delay(150);Capture(folder+"/Preparation.png");
        await Task.Delay(300);Capture(folder+"/Main.png");await Task.Delay(500);Capture(folder+"/Decay.png");
        var text="slot="+slot+"; frame="+frame+"->"+Time.frameCount+"; time="+started+"->"+Time.time+"; mana="+initialMana+"->"+p.Mana+"; ";
        text+=string.Join("; ",UnityEngine.Object.FindObjectsByType<ComboTrainingTarget>(FindObjectsSortMode.None).Select(t=>t.name+" hits="+t.Hits+" damage="+t.TotalDamage));
        var m=p.GetComponent<CharacterModifiers>();text+="; speed="+m.MoveSpeed+" attack="+m.AttackSpeed+" remaining="+m.Remaining;
        File.WriteAllText(folder+"/Observation.txt",text);return text;
    }
    public static string Snapshot()
    {
        var p=Player;return p.GetComponent<CharacterRuntime>().Definition.id+" frame="+Time.frameCount+" time="+Time.time+" position="+p.transform.position+" velocity="+p.GetComponent<Rigidbody>().linearVelocity+" state="+p.GetComponent<PlayerMovement>().MovementState+" mana="+p.Mana;
    }
    public static async Task<string> Gesture(string direction, bool dash)
    {
        var p=Player;foreach(var t in UnityEngine.Object.FindObjectsByType<ComboTrainingTarget>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(t.gameObject);
        p.GetComponent<PlayerHealth>().EnterLevel(new Vector3(-5,-3.4f,0));await Task.Delay(120);
        var keys=direction=="up-right"?new[]{Key.UpArrow,Key.RightArrow}:direction=="up"?new[]{Key.UpArrow}:new[]{Key.RightArrow};
        var originalSettings=InputSystem.settings;var temporary=UnityEngine.Object.Instantiate(originalSettings);temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=temporary;
        var start=p.transform.position;var frame=Time.frameCount;InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys));InputSystem.Update();
        try
        {
            await Task.Delay(80);if(dash){InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys.Concat(new[]{Key.LeftShift}).ToArray()));InputSystem.Update();}
            await Task.Delay(180);var velocity=p.GetComponent<Rigidbody>().linearVelocity;var state=p.GetComponent<PlayerMovement>().MovementState;
            var text="direction="+direction+" dash="+dash+" frame="+frame+"->"+Time.frameCount+" velocity="+velocity+" state="+state+" delta="+(p.transform.position-start);
            Directory.CreateDirectory("Logs/Characters");File.AppendAllText("Logs/Characters/Movement.txt",text+"\n");return text;
        }
        finally{InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState());InputSystem.Update();InputSystem.settings=originalSettings;UnityEngine.Object.Destroy(temporary);}
    }
    public static async Task<string> Navigation()
    {
        CharacterSelectionMenu.Instance.Open(null);await Task.Delay(150);
        var es=UnityEngine.EventSystems.EventSystem.current;
        es.SetSelectedGameObject(CharacterSelectionMenu.Instance.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Dasher Card").gameObject);
        var first=es.currentSelectedGameObject;
        UnityEngine.EventSystems.ExecuteEvents.Execute(first,new UnityEngine.EventSystems.AxisEventData(es){moveDir=UnityEngine.EventSystems.MoveDirection.Right,moveVector=Vector2.right},UnityEngine.EventSystems.ExecuteEvents.moveHandler);
        var right=es.currentSelectedGameObject;
        UnityEngine.EventSystems.ExecuteEvents.Execute(right,new UnityEngine.EventSystems.BaseEventData(es),UnityEngine.EventSystems.ExecuteEvents.submitHandler);
        var animator=right.GetComponent<Animator>();bool motion=animator.enabled;animator.enabled=false;await Task.Delay(120);
        var feedback=new SerializedObject(right.GetComponent<UiInteractionFeedback>());var border=(UnityEngine.UI.Image)feedback.FindProperty("selectionBorder").objectReferenceValue;
        string text="first="+first.name+" right="+right.name+" persistentFocusWithoutMotion="+border.enabled+"; ";
        foreach(var button in CharacterSelectionMenu.Instance.GetComponentsInChildren<UnityEngine.UI.Button>()){var n=button.navigation;text+=button.name+"[L="+n.selectOnLeft?.name+",R="+n.selectOnRight?.name+",U="+n.selectOnUp?.name+",D="+n.selectOnDown?.name+"] ";}
        animator.enabled=motion;CharacterSelectionMenu.Instance.Cancel();GameOptionsMenu.Instance.Close();return text;
    }
}
