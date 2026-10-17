using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Reference-derived authored data. Apply explicitly; runtime never writes these profiles.
public static class MechaCadenceSetup
{
    [MenuItem("Tools/Mecha/Apply Reference Cadence and Cockpit")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before applying cadence.");
        var movement = MechaPresentationSetup.Profile<PlayerMovementSettings>("PlayerMovement");
        movement.walkSpeed = 1.8f; movement.runSpeed = 4.2f; movement.dashSpeed = 10.5f;
        movement.dashDuration = 12f / 60f; movement.dashCooldown = 4f / 60f;
        movement.dashBuffer = 5f / 60f; movement.groundAcceleration = 100f; movement.airAcceleration = 25f;
        movement.rocketSpeed = 14f; movement.rocketRiseSpeed = 7.8f; movement.rocketDuration = 15f / 60f;
        movement.mushidonSpeed = 18.5f; movement.mushidonDuration = 22f / 60f; movement.mushidonFallSpeed = 12f;
        movement.chordWindow = 5f / 60f; movement.mushidonWindow = 15f / 60f; movement.brakeLock = 4f / 60f;
        movement.impulseEndRatio = 0.52f; movement.slideDrag = 22f; movement.attackDrag = 35f;
        EditorUtility.SetDirty(movement);
        var enemy = MechaPresentationSetup.Profile<EnemySettings>("EnemyBehavior");
        enemy.hitDrag = 6f; enemy.juggleGravityScale = 0.55f; EditorUtility.SetDirty(enemy);
        const string inputPath = "Assets/_Project/Inputs/InputSystem_Actions.inputactions";
        var inputs = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(inputPath);
        var map = inputs.FindActionMap("Player");
        if (map.FindAction("Tech", false) == null)
        {
            var techInput = map.AddAction("Tech", UnityEngine.InputSystem.InputActionType.Button);
            techInput.AddBinding("<Keyboard>/x", groups: "Keyboard&Mouse");
            techInput.AddBinding("<Gamepad>/rightStickPress", groups: "Gamepad");
            System.IO.File.WriteAllText(inputPath, inputs.ToJson()); AssetDatabase.ImportAsset(inputPath);
        }
        var combat = MechaPresentationSetup.Profile<PlayerCombatSettings>("PlayerCombat");
        PlayerCombatSettings.ActionData Action(PlayerAttack.ActionKind kind) => combat.Get(kind);
        void Tune(PlayerAttack.ActionKind kind, float duration, float impact, float cancel)
        {
            var data = Action(kind); data.duration = duration / 60f; data.impact = impact / 60f; data.cancel = cancel / 60f;
            data.airLift = 6.3f; data.stepSpeed = 3f; data.stepDuration = 5f / 60f;
            data.stun = 0.38f; data.push = 2f;
        }
        Tune(PlayerAttack.ActionKind.Slash1, 25, 7, 14);
        Tune(PlayerAttack.ActionKind.Slash2, 28, 9, 16);
        Tune(PlayerAttack.ActionKind.Slash3, 34, 12, 23);
        Action(PlayerAttack.ActionKind.Slash3).launcher = false;
        Action(PlayerAttack.ActionKind.Slash3).push = 3.4f;
        Tune(PlayerAttack.ActionKind.AirSlash, 28, 8, 18);
        Tune(PlayerAttack.ActionKind.DashSlash, 29, 8, 19);
        Tune(PlayerAttack.ActionKind.RisingSlash, 30, 8, 19);
        var rising = Action(PlayerAttack.ActionKind.RisingSlash);
        rising.lift = 11.5f; rising.push = 1.2f; rising.launcher = true; rising.requiresGround = false;
        rising.hitHalfSize = new Vector3(0.8f, 1.05f, 0.7f); rising.height = 1.2f; rising.stun = 0.6f;
        Action(PlayerAttack.ActionKind.AirSlash).airOnly = true;
        foreach (var kind in new[] { PlayerAttack.ActionKind.MegaSlash, PlayerAttack.ActionKind.SwordFire, PlayerAttack.ActionKind.CriticalX })
        {
            var data = Action(kind);
            data.duration = kind == PlayerAttack.ActionKind.CriticalX ? 0.72f : 0.62f;
            data.impact = 0.2f; data.cancel = data.duration; data.pulseInterval = 0.08f;
            data.stepSpeed = 0f; data.stepDuration = 0f; data.airLift = 5.2f; data.stun = 0.5f;
        }
        var dive = EnsurePiece(combat, PlayerAttack.ActionKind.DiveStrike, "Dive Strike");
        dive.action = new PlayerCombatSettings.ActionData { kind = PlayerAttack.ActionKind.DiveStrike,
            duration = 2f, impact = 0.08f, cancel = 2f, pulses = 24, pulseInterval = 0.08f,
            damage = 15f, finalDamage = 15f, airOnly = true, diveSpeed = 18f,
            hitHalfSize = new Vector3(0.65f, 0.8f, 0.7f), height = 0.35f, reach = 0.25f,
            push = 3f, lift = 0f, stun = 0.45f, stepSpeed = 3f, stepDuration = 0.08f };
        var critical = EnsurePiece(combat, PlayerAttack.ActionKind.CriticalStrike, "Critical Cross-through");
        critical.action = new PlayerCombatSettings.ActionData { kind = PlayerAttack.ActionKind.CriticalStrike,
            duration = 0.45f, impact = 0.09f, cancel = 0.31f, damage = 18f, finalDamage = 18f,
            sideSwitch = true, requiresGround = true, stepSpeed = 10.5f, stepDuration = 0.21f,
            reach = 0.25f, hitHalfSize = new Vector3(0.85f, 0.85f, 0.7f),
            push = 2.5f, stun = 0.4f, airLift = 5f };
        var tech = EnsurePiece(combat, PlayerAttack.ActionKind.MagneticPull, "Magnetic Recall");
        tech.action = new PlayerCombatSettings.ActionData { kind = PlayerAttack.ActionKind.MagneticPull,
            duration = 0.55f, impact = 0.18f, cancel = 0.37f, damage = 5f, finalDamage = 5f,
            pull = true, pullSpeed = 6f, stepSpeed = 0f, reach = 1.7f,
            hitHalfSize = new Vector3(2.3f, 1.25f, 0.7f), height = 1.1f,
            stun = 0.48f, airLift = 5f };
        combat.combos.diveAttack = dive; combat.combos.criticalBranch = critical; combat.combos.techBranch = tech;
        combat.attackBuffer = 0.28f; combat.skillBuffer = 0.3f;
        foreach (var piece in combat.pieces) EditorUtility.SetDirty(piece);
        EditorUtility.SetDirty(combat.combos); EditorUtility.SetDirty(combat);
        var effects = MechaPresentationSetup.Profile<CombatVfxSettings>("CombatVfx");
        effects.skillCharge = MechaPresentationSetup.Create<ParticleEffectSettings>("SkillCharge");
        EditorUtility.CopySerialized(effects.hitSparks, effects.skillCharge);
        effects.skillCharge.name = "SkillCharge"; effects.skillCharge.count = 22;
        effects.skillCharge.lifetime = new Vector2(0.4f, 0.65f); effects.skillCharge.speed = new Vector2(0.4f, 2f);
        effects.skillCharge.size = new Vector2(0.025f, 0.065f); effects.skillCharge.color = new Color(0.4f, 2f, 2.5f, 1f);
        effects.skillCharge.emissionRadius = 0.6f; effects.skillCharge.spread = 0.3f;
        effects.magneticRecall = MechaPresentationSetup.Create<ParticleEffectSettings>("MagneticRecall");
        EditorUtility.CopySerialized(effects.dashStreaks, effects.magneticRecall);
        effects.magneticRecall.name = "MagneticRecall"; effects.magneticRecall.count = 30;
        effects.magneticRecall.lifetime = new Vector2(0.16f, 0.32f); effects.magneticRecall.speed = new Vector2(4f, 8f);
        effects.magneticRecall.color = new Color(0.25f, 1.7f, 2f, 1f); effects.magneticRecall.emissionRadius = 0.7f;
        effects.magneticRecall.spread = 0.2f;
        EditorUtility.SetDirty(effects.skillCharge); EditorUtility.SetDirty(effects.magneticRecall); EditorUtility.SetDirty(effects);
        var zoom = MechaPresentationSetup.Profile<RoomCameraZoomSettings>("RoomCameraZoom");
        zoom.skillZoomOut = -0.075f; zoom.skillEaseIn = 0.08f; zoom.skillEaseOut = 0.32f; EditorUtility.SetDirty(zoom);
        var layout = MechaPresentationSetup.Create<GameplayHudLayoutSettings>("GameplayHudLayout");
        layout.pilotHudScale = 0.5f;
        layout.cockpitAnchor = new Vector2(0f, 1f); layout.cockpitOffset = new Vector2(28f, -48f);
        layout.skillBarAnchor = new Vector2(0f, 1f); layout.skillBarOffset = new Vector2(28f, -126f);
        layout.skillSize = new Vector2(144f, 74f); layout.skillSpacing = 6f; layout.skillFontSize = 16f;
        layout.tutorialText = "ARROWS Move   Z Combo   X Magnetic recall   A / S / D Skills\n>> + UP Rocket   then DOWN Mushidon   DOWN + Z Air dive\n1 Local host   2 Local join   3 Lobbies   E / UP Portal";
        EditorUtility.SetDirty(layout);
        var hud = UnityEngine.Object.FindFirstObjectByType<MechaGameplayHud>();
        if (hud != null) { MechaHudSetup.Apply(hud); EditorSceneManager.MarkSceneDirty(hud.gameObject.scene); EditorSceneManager.SaveScene(hud.gameObject.scene); }
        AssetDatabase.SaveAssets();
        Debug.Log("Reference cadence and cockpit applied through native Unity APIs.");
    }
    private static AttackPiece EnsurePiece(PlayerCombatSettings combat, PlayerAttack.ActionKind kind, string label)
    {
        var piece = MechaPresentationSetup.Create<AttackPiece>(kind.ToString());
        piece.displayName = label;
        if (!combat.pieces.Contains(piece)) combat.pieces = combat.pieces.Concat(new[] { piece }).ToArray();
        return piece;
    }
}
