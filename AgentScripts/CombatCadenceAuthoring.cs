using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// Explicit Edit Mode authoring. No import hooks, tests, pose baking or runtime tuning.
public static class CombatCadenceAuthoring
{
    const string Root = "Assets/_Project";
    const string ReactionSource = Root + "/Animations/Humanoid/CombatReactions.fbx";
    static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path);
    static void Save(UnityEngine.Object asset) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
    static void Editing() { if (EditorApplication.isPlaying) throw new InvalidOperationException("Use Edit Mode"); }
    static T Data<T>(string path) where T : ScriptableObject
    {
        var asset = Load<T>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
        return asset;
    }
    static AnimationClip Clip(string path, string name = null) => AssetDatabase.LoadAllAssetsAtPath(path)
        .OfType<AnimationClip>().First(c => name == null ? !c.name.StartsWith("__") : c.name == name);
    static void Field(UnityEngine.Object obj, string name, UnityEngine.Object value)
    { var so = new SerializedObject(obj); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void State(CharacterAnimationSettings profile, string name, AnimationClip clip)
    {
        var controller = (AnimatorController)profile.controller;
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
        state.motion = clip; state.speed = 1f;
        state.speedParameter = "PlaybackSpeed"; state.speedParameterActive = true;
        // No automatic exits: the gameplay reaction state decides recovery/death.
        foreach (var transition in state.transitions) state.RemoveTransition(transition);
        var clips = profile.clips.Where(c => c.state != name).ToList();
        clips.Add(new CharacterAnimationSettings.StateClip { state = name, clip = clip }); profile.clips = clips.ToArray();
        Save(state); Save(controller); Save(profile);
    }

    public static string ReactionClips()
    {
        Editing();
        if (Load<GameObject>(ReactionSource) == null && !AssetDatabase.CopyAsset("Assets/DoubleL/FBX Unity/Hit/InPlace/Hit_B_4_InPlace.fbx", ReactionSource))
            throw new InvalidOperationException("Cannot copy supplied native reaction source");
        var importer = (ModelImporter)AssetImporter.GetAtPath(ReactionSource);
        var source = importer.defaultClipAnimations[0];
        Func<string,float,float,ModelImporterClipAnimation> segment = (name,first,last) => new ModelImporterClipAnimation {
            name=name,takeName=source.takeName,firstFrame=first,lastFrame=last,loopTime=false,
            lockRootRotation=true,lockRootPositionXZ=true,lockRootHeightY=true,
            keepOriginalPositionY=true,keepOriginalPositionXZ=true,keepOriginalOrientation=true
        };
        importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.clipAnimations = new[]{segment("Collapse",0f,14f),segment("Downed",14f,15f),segment("Recover",14f,55f)};
        importer.SaveAndReimport();
        var death = Clip(ReactionSource, "Collapse"); var knockedDown = Clip(ReactionSource, "Downed");
        var hit = Clip("Assets/DoubleL/FBX Unity/Hit/InPlace/Hit_B_4_InPlace.fbx");
        var flinch = Clip("Assets/DoubleL/FBX Unity/Hit/InPlace/Hit_B_5_InPlace.fbx");
        foreach (string name in new[] { "Player", "WingZero", "Enemy" })
        {
            var profile = Load<CharacterAnimationSettings>(Root + "/Settings/Animation/" + name + "Animation.asset");
            State(profile, "Flinch", flinch); State(profile, "HitReaction", hit); State(profile, "Hit", flinch);
            State(profile, "KnockUp", death); State(profile, "AirHit", knockedDown); State(profile, "Knockdown", knockedDown);
            State(profile, "Death", death); State(profile, "StandUp", Clip(ReactionSource,"Recover"));
            if (name == "Enemy")
            {
                const string one = "Assets/DoubleL/FBX Unity/One Hand Up/";
                State(profile, "Idle", Clip(one + "Movement/Idle/Idle/1Hand_Up_Stand_Idle_A_2.fbx"));
                State(profile, "Run", Clip(one + "Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace.fbx"));
                State(profile, "Dash", profile.Clip("Run"));
                const string knight = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/Knight Warrior Mecanim Animation Pack/Animations/";
                State(profile, "Jump", Clip(knight + "Knight@Fall.FBX")); State(profile, "Fall", profile.Clip("Jump"));
                State(profile, "Attack", Clip(one + "Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx"));
                State(profile, "SpecialAttack", Clip(one + "Attack_B/InPlace/1Hand_Up_Attack_B_3_InPlace.fbx"));
                // Unused legacy states also participate in Animator binding; retain no Generic bone clips.
                foreach(var legacy in profile.clips.Where(c=>c.clip!=null&&!c.clip.humanMotion).ToArray())
                    State(profile,legacy.state,profile.Clip("Attack"));
            }
        }
        // Rejected restored source is only our temporary import; the archive is untouched.
        AssetDatabase.DeleteAsset(Root+"/Animations/Humanoid/MechaDeath.fbx");
        return "User-approved placeholders: native recoil/collapse, held downed recoil, forward recovery. No reverse playback, pose baking or corrective curves.";
    }

    public static string Tuning()
    {
        Editing();
        var engagement = Data<EnemyEngagementSettings>(Root + "/Settings/Enemies/EnemyEngagement.asset");
        engagement.attackTokens = 2; engagement.approachLease = 3f; engagement.attackSpacing = .3f;
        Save(engagement);
        var reactions = Data<CombatReactionSettings>(Root + "/Settings/Player/CombatReactions.asset"); Save(reactions);
        var enemy = Load<EnemySettings>(Root + "/Settings/Enemies/EnemyBehavior.asset");
        enemy.engagement = engagement; enemy.windup = .38f; enemy.attackCooldown = 1.15f; enemy.deathDuration = 1.25f;
        enemy.specialWindup = .8f; enemy.specialEveryAttacks = 3; enemy.specialCooldown = 5f;
        enemy.attackRecovery = .42f; enemy.specialRecovery = .8f; Save(enemy);
        foreach (var path in AssetDatabase.FindAssets("t:EnemyLanceSettings").Select(AssetDatabase.GUIDToAssetPath))
        {
            var lance = Load<EnemyLanceSettings>(path); lance.windup = .85f; lance.cooldown = 2.8f;
            lance.leadTime = .12f; lance.maximumLead = .75f; Save(lance);
        }
        var hp = Load<PlayerHealthSettings>(Root + "/Settings/Player/PlayerHealth.asset");
        hp.enemyHitProtection = .35f; Save(hp);
        var durations = new Dictionary<PlayerAttack.ActionKind, float[]> {
            {PlayerAttack.ActionKind.Slash1, new[]{.78f,.24f,.58f}}, {PlayerAttack.ActionKind.Slash2,new[]{.84f,.27f,.64f}},
            {PlayerAttack.ActionKind.Slash3,new[]{.96f,.33f,.76f}}, {PlayerAttack.ActionKind.Heavy1,new[]{1.12f,.47f,.9f}},
            {PlayerAttack.ActionKind.Heavy2,new[]{1.24f,.53f,1f}}, {PlayerAttack.ActionKind.Heavy3,new[]{1.44f,.64f,1.18f}},
            {PlayerAttack.ActionKind.AirSlash,new[]{.78f,.27f,.66f}}, {PlayerAttack.ActionKind.DashSlash,new[]{.76f,.24f,.6f}},
            {PlayerAttack.ActionKind.RisingSlash,new[]{1.02f,.38f,.86f}}, {PlayerAttack.ActionKind.DiveStrike,new[]{.92f,.35f,.78f}},
            {PlayerAttack.ActionKind.CriticalStrike,new[]{1.02f,.4f,.84f}}
        };
        var changed = new HashSet<AttackPiece>();
        foreach (string path in new[] {Root+"/Settings/Attacks/PlayerCombat.asset",Root+"/Settings/Characters/ZeroCombat.asset"})
        {
            var combat = Load<PlayerCombatSettings>(path);
            combat.attackBuffer = .95f; combat.comboDisplayWindow = 2f; Save(combat);
            foreach (var piece in combat.pieces)
            {
                if (piece == null || !changed.Add(piece) || !durations.TryGetValue(piece.action.kind, out var timing)) continue;
                piece.action.duration = timing[0]; piece.action.impact = timing[1]; piece.action.cancel = timing[2]; Save(piece);
            }
            foreach (var combo in new[]{combat.combos.groundCombo,combat.combos.heavyCombo,combat.combos.airCombo,combat.combos.heavyAirCombo})
            { if (combo != null) { combo.inputWindow = 1.8f; Save(combo); } }
        }
        return "Shared tokens 2; normal/special startup .38/.80; lights .78/.84/.96; heavies 1.12/1.24/1.44 seconds";
    }

    static Avatar EnemyAvatar(GameObject go)
    {
        string path = Root + "/Animations/Humanoid/EnemyHumanoid.asset";
        var avatar = Load<Avatar>(path); if (avatar != null) return avatar;
        var setup = typeof(Editor).Assembly.GetType("UnityEditor.AvatarSetupTool");
        // AvatarBuilder must see model units. The visual prefab's 30x scale is applied at runtime.
        var visualScale=go.transform.localScale;
        go.transform.localScale=Vector3.one;
        var pose = go.GetComponentsInChildren<Transform>(true).ToDictionary(t=>t,t=>(t.localPosition,t.localRotation,t.localScale));
        object[] args = {go, new HumanBone[0], new SkeletonBone[0], false};
        try { setup.GetMethod("SetupHumanSkeleton", BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static).Invoke(null,args); }
        finally { foreach(var p in pose){p.Key.localPosition=p.Value.localPosition;p.Key.localRotation=p.Value.localRotation;p.Key.localScale=p.Value.localScale;} }
        var description = new HumanDescription {human=(HumanBone[])args[1],skeleton=(SkeletonBone[])args[2],hasTranslationDoF=(bool)args[3],
            upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f};
        avatar = AvatarBuilder.BuildHumanAvatar(go, description); avatar.name = "EnemyHumanoid";
        go.transform.localScale=visualScale;
        if (!avatar.isValid || !avatar.isHuman) { UnityEngine.Object.DestroyImmediate(avatar); throw new InvalidOperationException("Enemy rig cannot use native Humanoid reactions"); }
        AssetDatabase.CreateAsset(avatar, path); return avatar;
    }
    public static string Prefabs()
    {
        Editing();
        var profile = Load<CombatReactionSettings>(Root + "/Settings/Player/CombatReactions.asset");
        foreach (string path in new[]{Root+"/Prefabs/Characters/EvaVisual.prefab",Root+"/Prefabs/EvaEnemy.prefab",Root+"/Prefabs/LanceEnemy.prefab",Root+"/Prefabs/MechaPlayer.prefab"})
        {
            var go = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!path.EndsWith("MechaPlayer.prefab"))
                {
                    var animator = go.GetComponentInChildren<Animator>(true);
                    animator.avatar = EnemyAvatar(animator.gameObject); animator.applyRootMotion = false;
                    animator.runtimeAnimatorController=Load<CharacterAnimationSettings>(Root+"/Settings/Animation/EnemyAnimation.asset").controller;
                }
                if (!path.EndsWith("EvaVisual.prefab"))
                {
                    var reaction = go.GetComponent<CombatReaction>() ?? go.AddComponent<CombatReaction>(); Field(reaction, "settings", profile);
                }
                PrefabUtility.SaveAsPrefabAsset(go, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(go); }
        }
        // Scene instances retain their existing overrides; only add the new component.
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.isDirty) return "Player/enemy prefabs updated; existing unsaved scene changes preserved without saving";
        bool changed = false;
        foreach (var actor in UnityEngine.Object.FindObjectsByType<CharacterAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (actor.GetComponent<PlayerHealth>() == null && actor.GetComponent<EnemyController>() == null) continue;
            var reaction = actor.GetComponent<CombatReaction>();
            if (reaction == null) { reaction = actor.gameObject.AddComponent<CombatReaction>(); changed = true; }
            Field(reaction, "settings", profile);
            if (actor.GetComponent<EnemyController>() != null)
            {
                var animator = actor.GetComponentInChildren<Animator>(true); animator.avatar = Load<Avatar>(Root+"/Animations/Humanoid/EnemyHumanoid.asset");
                EditorUtility.SetDirty(animator); changed = true;
            }
        }
        if (changed) { UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene); }
        return "Reactions bound on player and both enemy prefabs, native enemy Avatar, existing scene actors updated";
    }

    public static string Sword()
    {
        Editing();
        const string vfx = Root + "/VFX/Melee";
        var profile = Load<SwordSwingSettings>(vfx + "/Profiles/SwordSwing.asset");
        profile.lightWidth=.11f; profile.heavyWidth=.19f; profile.lightTail=.065f; profile.heavyTail=.1f;
        profile.idleLight=.65f;
        profile.lightStrikeLight=2.5f; profile.heavyStrikeLight=3.5f; profile.lightDecay=.13f; Save(profile);
        foreach (string color in new[]{"Red","Blue"})
        {
            var source = Load<Material>("Assets/Blink/Art/Weapons/LowPoly/FreeSwords/Sword7/Sword7_Material_"+color+".mat");
            var blade = Load<Material>(vfx+"/Materials/Sword7"+color+".mat");
            blade.SetColor("_Emissive_Color",source.GetColor("_EmissionColor")*.7f); Save(blade);
            foreach (string layer in new[]{"CutBand","CutRim","BladePeel"})
            {
                var material = Load<Material>(vfx+"/Materials/"+layer+color+".mat");
                if (material == null) continue;
                bool blue=color=="Blue";
                var baseColor = layer=="CutRim" ? (blue?new Color(.7f,1.8f,3f,1f):new Color(3f,.65f,.35f,1f))
                    : (blue?new Color(.04f,.65f,2.4f,1f):new Color(2.4f,.1f,.025f,1f));
                float factor = layer=="CutRim"?.4f:.55f;
                baseColor.r*=factor; baseColor.g*=factor; baseColor.b*=factor;
                if (layer=="CutRim") baseColor.a*=.65f;
                material.SetColor("_BaseColor",baseColor); Save(material);
            }
            string path=Root+"/Prefabs/Weapons/Sword7"+color+".prefab";
            var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var light=go.GetComponentInChildren<Light>(true); if(light!=null){light.range=1.65f;light.intensity=.65f;}
                var trail=go.GetComponentInChildren<TrailRenderer>(true);
                if(trail!=null){trail.widthMultiplier=.11f;trail.time=.065f;}
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally {PrefabUtility.UnloadPrefabContents(go);}
        }
        return "Sword cuts 36–40% smaller; trails half width; local emission/light/rim reduced. Global exposure/bloom unchanged.";
    }
}
