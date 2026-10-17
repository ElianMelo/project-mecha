using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MechaCombatUpgradeSetup
{
    [MenuItem("Tools/Mecha/Apply Combat and Combo Configurator")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var combat = MechaPresentationSetup.Profile<PlayerCombatSettings>("PlayerCombat");
        if (combat.pieces == null || combat.pieces.Length == 0)
        {
            combat.pieces = combat.actions.Select(action =>
            {
                var piece = MechaPresentationSetup.Profile<AttackPiece>(action.kind.ToString());
                if (piece == null)
                {
                    piece = MechaPresentationSetup.Create<AttackPiece>(action.kind.ToString());
                    piece.displayName = ObjectNames.NicifyVariableName(action.kind.ToString());
                    piece.action = JsonUtility.FromJson<PlayerCombatSettings.ActionData>(JsonUtility.ToJson(action));
                    EditorUtility.SetDirty(piece);
                }
                return piece;
            }).ToArray();
        }
        combat.damageMultiplier = 2f;
        AttackPiece Piece(PlayerAttack.ActionKind kind) => combat.pieces.First(p => p.action.kind == kind);
        ComboDefinition Combo(string name, ComboDomain domain, params PlayerAttack.ActionKind[] actions)
        {
            var combo = MechaPresentationSetup.Profile<ComboDefinition>(name);
            if (combo != null) return combo;
            combo = MechaPresentationSetup.Create<ComboDefinition>(name); combo.displayName = ObjectNames.NicifyVariableName(name);
            combo.domain = domain; combo.inputWindow = combat.comboWindow; combo.attacks = actions.Select(Piece).ToArray();
            EditorUtility.SetDirty(combo); return combo;
        }
        var ground = Combo("GroundOriginal", ComboDomain.Ground, PlayerAttack.ActionKind.Slash1, PlayerAttack.ActionKind.Slash2, PlayerAttack.ActionKind.Slash3);
        var heavy = Combo("GroundHeavy", ComboDomain.Ground, PlayerAttack.ActionKind.Slash2, PlayerAttack.ActionKind.Slash3);
        var air = Combo("AirOriginal", ComboDomain.Air, PlayerAttack.ActionKind.AirSlash, PlayerAttack.ActionKind.AirSlash);
        var launcher = Combo("AirLauncher", ComboDomain.Air, PlayerAttack.ActionKind.AirSlash, PlayerAttack.ActionKind.RisingSlash);
        if (combat.combos == null)
        {
            var loadout = MechaPresentationSetup.Create<ComboLoadout>("PlayerCombos");
            loadout.groundCombo = ground; loadout.airCombo = air;
            loadout.dashAttack = Piece(PlayerAttack.ActionKind.DashSlash); loadout.risingBranch = Piece(PlayerAttack.ActionKind.RisingSlash);
            combat.combos = loadout; EditorUtility.SetDirty(loadout);
        }
        EditorUtility.SetDirty(combat);
        var config = MechaPresentationSetup.Create<ComboConfiguratorSettings>("ComboConfigurator");
        config.combat = combat; config.loadout = combat.combos;
        if (config.presets == null || config.presets.Length == 0) config.presets = new[] {ground, heavy, air, launcher};
        EditorUtility.SetDirty(config);
        var enemy = MechaPresentationSetup.Profile<EnemySettings>("EnemyBehavior");
        enemy.windup = 0.34f; EditorUtility.SetDirty(enemy);
        var prediction = MechaPresentationSetup.Profile<EnemyPredictionSettings>("EnemyPrediction");
        EditorUtility.SetDirty(prediction);
        var material = EnsureMotionMaterial();
        EditorUtility.SetDirty(MechaPresentationSetup.Profile<PlayerMovementSettings>("PlayerMovement"));
        EditorUtility.SetDirty(MechaPresentationSetup.Profile<OneWayPlatformSettings>("OneWayPlatform"));
        EditorUtility.SetDirty(MechaPresentationSetup.Profile<EnemyLanceSettings>("EnemyLance"));
        foreach (string name in new[] {"MechaPlayer", "EvaEnemy", "LanceEnemy"})
        {
            string path = "Assets/_Project/Prefabs/" + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.GetComponent<CapsuleCollider>().sharedMaterial = material;
                if (name == "MechaPlayer") MechaPresentationSetup.Ensure<PlayerLedgeAssist>(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        MechaComboConfiguratorSetup.Apply(config);
    }

    public static PhysicsMaterial EnsureMotionMaterial()
    {
        const string path = "Assets/_Project/Materials/CharacterMotion.physicMaterial";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (material == null)
        {
            material = new PhysicsMaterial("Character Motion"); AssetDatabase.CreateAsset(material, path);
        }
        material.staticFriction = material.dynamicFriction = 0f;
        material.frictionCombine = PhysicsMaterialCombine.Minimum;
        material.bounciness = 0f; EditorUtility.SetDirty(material); return material;
    }
}
