using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// One category policy for authoring and migration; native asset moves retain GUIDs.
public static class MechaSettingsSetup
{
    public static string Category(Type type, string name)
    {
        if (type == typeof(SecondaryWeaponSettings)) return "Guns";
        if (type == typeof(AttackPiece))
            return name == "MegaSlash" || name == "SwordFire" || name == "CriticalX" ? "Skills" : "Attacks";
        if (type == typeof(SkillPresentationSettings)) return "Skills";
        if (type == typeof(ComboDefinition) || type == typeof(ComboLoadout) || type == typeof(ComboConfiguratorSettings)) return "Combos";
        if (type == typeof(PlayerCombatSettings)) return "Attacks";
        if (type == typeof(CharacterAnimationSettings)) return "Animation";
        if (type == typeof(ParticleEffectSettings) || type == typeof(SwordFireVfxSettings) || type == typeof(CombatVfxSettings)) return "Effects";
        if (type == typeof(PresentationRenderSettings) || type == typeof(ToonLookSettings)) return "Rendering";
        if (type == typeof(ParallaxSettings) || type == typeof(ParallaxSceneSettings)) return "Backgrounds";
        if (type == typeof(RoomCameraSettings) || type == typeof(RoomCameraZoomSettings)) return "Camera";
        if (type == typeof(LobbySettings) || type == typeof(NetworkSimulationSettings)) return "Network";
        if (type == typeof(ChatSettings) || type == typeof(ControllerInterfaceSettings) || type == typeof(GameplayHudLayoutSettings)
            || type == typeof(SquadPresentationSettings) || type == typeof(UiFeelSettings)) return "Interface";
        if (type.Name.StartsWith("Enemy", StringComparison.Ordinal)) return "Enemies";
        if (type.Name.StartsWith("Player", StringComparison.Ordinal)) return "Player";
        return "World";
    }
    public static string PathFor(Type type, string name)
        => MechaPresentationSetup.ConfigFolder + "/" + Category(type, name) + "/" + name + ".asset";

    [MenuItem("Tools/Mecha/Settings/Organize Categories")]
    public static void Organize()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before moving settings.");
        var moves = AssetDatabase.FindAssets("t:ScriptableObject", new[] { MechaPresentationSetup.ConfigFolder })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".asset", StringComparison.Ordinal))
            .Select(p => new { source = p, asset = AssetDatabase.LoadMainAssetAtPath(p) })
            .Select(a => new { a.source, destination = PathFor(a.asset.GetType(), a.asset.name), a.asset }).ToArray();
        // Validate the complete move set before changing any asset locations.
        foreach (var move in moves)
            if (move.source != move.destination && AssetDatabase.LoadMainAssetAtPath(move.destination) != null)
                throw new InvalidOperationException("Destination already exists: " + move.destination);
        var receipts = new List<string>();
        foreach (var move in moves)
        {
            string folder = System.IO.Path.GetDirectoryName(move.destination).Replace('\\', '/');
            MechaPresentationSetup.Folder(folder);
            AssetDatabase.SetLabels(AssetDatabase.LoadMainAssetAtPath(folder), new[] { "Others" });
            if (move.source == move.destination) continue;
            string guid = AssetDatabase.AssetPathToGUID(move.source);
            string error = AssetDatabase.MoveAsset(move.source, move.destination);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            if (AssetDatabase.AssetPathToGUID(move.destination) != guid)
                throw new InvalidOperationException("GUID changed while moving " + move.source);
            receipts.Add(move.source + " -> " + move.destination);
        }
        const string legacy = MechaPresentationSetup.ConfigFolder + "/AttackPieces";
        if (AssetDatabase.IsValidFolder(legacy) && AssetDatabase.FindAssets("", new[] { legacy }).Length == 0)
            AssetDatabase.DeleteAsset(legacy);
        AssetDatabase.SaveAssets();
        Debug.Log("Organized " + moves.Length + " settings; moved " + receipts.Count + " assets without changing GUIDs.");
    }
    [MenuItem("Tools/Mecha/Settings/Open Categories")]
    public static void OpenCategories()
    {
        var folder = AssetDatabase.LoadMainAssetAtPath(MechaPresentationSetup.ConfigFolder);
        Selection.activeObject = folder; EditorGUIUtility.PingObject(folder);
    }
}
