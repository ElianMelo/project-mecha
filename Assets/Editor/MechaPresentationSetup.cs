using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit setup only; configuration assets are preserved when the setup is reapplied.
public static class MechaPresentationSetup
{
    public const string ConfigFolder = "Assets/_Project/Settings";
    public static T Profile<T>(string name) where T : ScriptableObject => AssetDatabase.LoadAssetAtPath<T>(ConfigFolder+"/"+name+".asset");
    public static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent);
        AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));
    }
    public static T Create<T>(string name) where T : ScriptableObject
    {
        Folder(ConfigFolder);
        var asset=Profile<T>(name);
        if(asset!=null) return asset;
        asset=ScriptableObject.CreateInstance<T>(); asset.name=name;
        AssetDatabase.CreateAsset(asset,ConfigFolder+"/"+name+".asset"); return asset;
    }
    public static T Ensure<T>(GameObject go) where T : Component
    {
        var component=go.GetComponent<T>();
        return component!=null?component:go.AddComponent<T>();
    }
    public static void Assign(Component component,string field,UnityEngine.Object value)
    {
        var so=new SerializedObject(component); var property=so.FindProperty(field);
        if(property==null) throw new InvalidOperationException(component.GetType().Name+" has no "+field);
        property.objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void EnsureProfiles()
    {
        Create<PlayerMovementSettings>("PlayerMovement"); Create<PlayerInputSettings>("PlayerInput");
        Create<PlayerHealthSettings>("PlayerHealth"); Create<EnemySettings>("EnemyBehavior");
        Create<PlayerBodyCollisionSettings>("PlayerBodyCollision");
        Create<EnemySpawnSettings>("EnemySpawning");
        Create<EnemyTraversalSettings>("EnemyTraversal"); Create<EnemyCrowdSettings>("EnemyCrowd");
        Create<EnemyStateSettings>("EnemyStates"); Create<PlayerAwarenessSettings>("PlayerAwareness");
        Create<RoomCameraSettings>("RoomCamera"); Create<ToonLookSettings>("ToonLook"); Create<OneWayPlatformSettings>("OneWayPlatform");
        Create<RoomCameraZoomSettings>("RoomCameraZoom");
        Create<PresentationRenderSettings>("Rendering");
        Create<GameplayHudLayoutSettings>("GameplayHudLayout");
        Create<EnemyPredictionSettings>("EnemyPrediction");
        Create<ScenarioSettings>("Scenario"); Create<DestructibleSettings>("DestructibleBoxes");
        Create<CombatVfxSettings>("CombatVfx");
        var combat=Create<PlayerCombatSettings>("PlayerCombat");
        if(combat.actions==null || combat.actions.Length==0)
        {
            combat.actions=Enum.GetValues(typeof(PlayerAttack.ActionKind)).Cast<PlayerAttack.ActionKind>().Select(k=>new PlayerCombatSettings.ActionData{kind=k}).ToArray();
            SetAction(combat,PlayerAttack.ActionKind.Slash1,0.48f,0.18f,0.32f,9f);
            SetAction(combat,PlayerAttack.ActionKind.Slash2,0.52f,0.2f,0.35f,11f);
            SetAction(combat,PlayerAttack.ActionKind.Slash3,0.65f,0.26f,0.48f,15f,true);
            SetAction(combat,PlayerAttack.ActionKind.AirSlash,0.46f,0.16f,0.31f,10f);
            SetAction(combat,PlayerAttack.ActionKind.DashSlash,0.48f,0.16f,0.32f,12f);
            SetAction(combat,PlayerAttack.ActionKind.RisingSlash,0.55f,0.2f,0.38f,13f,true);
            SetAction(combat,PlayerAttack.ActionKind.MegaSlash,0.8f,0.12f,0.62f,8f);
            SetAction(combat,PlayerAttack.ActionKind.SwordFire,0.85f,0.24f,0.62f,14f);
            SetAction(combat,PlayerAttack.ActionKind.CriticalX,1.05f,0.25f,0.62f,28f,true);
            var a=combat.Get(PlayerAttack.ActionKind.MegaSlash); a.pulses=4;a.finalDamage=18f;a.finalPulseLaunch=true;a.lift=7f;a.manaCost=25f;a.cooldown=3f;
            var s=combat.Get(PlayerAttack.ActionKind.SwordFire);s.pulses=3;s.pulseInterval=0.12f;s.reach=1.7f;s.pulseAdvance=0.7f;s.manaCost=40f;s.cooldown=5f;
            var d=combat.Get(PlayerAttack.ActionKind.CriticalX);d.pulses=2;d.pulseInterval=0.2f;d.hitHalfSize=new Vector3(1.55f,1.5f,0.75f);d.manaCost=60f;d.cooldown=7f;
            EditorUtility.SetDirty(combat);
        }
    }
    private static void SetAction(PlayerCombatSettings config,PlayerAttack.ActionKind kind,float duration,float impact,float cancel,float damage,bool launcher=false)
    {
        var a=config.Get(kind);a.duration=duration;a.impact=impact;a.cancel=cancel;a.damage=a.finalDamage=damage;a.launcher=launcher;
        if(launcher){a.push=4f;a.lift=7f;a.stun=0.55f;}
    }
    [MenuItem("Tools/Mecha/Apply Character and Presentation")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before changing presentation assets.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/MainScene.unity") throw new InvalidOperationException("Open MainScene first.");
        EnsureProfiles(); MechaCharacterSetup.Apply(); MechaParticleSetup.Apply(); MechaToonSetup.Apply(); MechaArenaSetup.Apply(); MechaCameraSetup.Apply();
        MechaScenarioSetup.Apply();
        var hud = UnityEngine.Object.FindFirstObjectByType<MechaGameplayHud>();
        if (hud != null) MechaHudSetup.Apply(hud);
        var spawning=Profile<EnemySpawnSettings>("EnemySpawning");
        if(spawning.prefab==null)spawning.prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EvaEnemy.prefab").GetComponent<FishNet.Object.NetworkObject>();
        EditorUtility.SetDirty(spawning);
        foreach(var spawner in UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))Assign(spawner,"settings",spawning);
        foreach(var p in UnityEngine.Object.FindObjectsByType<OneWayPlatform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            Assign(p,"settings",Profile<OneWayPlatformSettings>("OneWayPlatform"));
            var r=p.GetComponent<Renderer>(); if(r!=null)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Mecha presentation applied: configured character, reactive enemies, Particle Systems, toon shading and Cinemachine room camera.");
    }
}
