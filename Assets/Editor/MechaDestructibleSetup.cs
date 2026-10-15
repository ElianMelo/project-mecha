using UnityEditor;
using UnityEngine;

public static class MechaDestructibleSetup
{
    public static void Apply(ScenarioSettings scenario, Transform root)
    {
        var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layer = tags.FindProperty("layers").GetArrayElementAtIndex(10);
        if (string.IsNullOrEmpty(layer.stringValue)) { layer.stringValue = "Destructible"; tags.ApplyModifiedPropertiesWithoutUndo(); }
        if (layer.stringValue != "Destructible") throw new System.InvalidOperationException("Destructible layer 10 is already occupied.");
        Physics.IgnoreLayerCollision(8, 10, true); Physics.IgnoreLayerCollision(9, 10, true); Physics.IgnoreLayerCollision(10, 10, true);
        var settings = MechaPresentationSetup.Create<DestructibleSettings>("DestructibleBoxes");
        var vfx = MechaPresentationSetup.Profile<CombatVfxSettings>("CombatVfx");
        if (settings.debris == null) { settings.debris = vfx.hitDebris; settings.smoke = vfx.hitSmoke; settings.hitFeedback = vfx; EditorUtility.SetDirty(settings); }
        var combat = MechaPresentationSetup.Profile<PlayerCombatSettings>("PlayerCombat");
        combat.enemyLayers |= 1 << 10; EditorUtility.SetDirty(combat);
        if (scenario.boxFeet == null || scenario.boxFeet.Length == 0)
        {
            scenario.boxFeet = new[] {new Vector3(-25f,-3.5f,0f),new Vector3(-17f,-3.5f,0f),new Vector3(-5f,-3.5f,0f),new Vector3(7f,-3.5f,0f),new Vector3(21f,-3.5f,0f),new Vector3(31f,-3.5f,0f),new Vector3(-30f,-1.69f,0f),new Vector3(-14f,-1.69f,0f),new Vector3(2f,-1.69f,0f),new Vector3(10f,0.16f,0f),new Vector3(27f,0.16f,0f)};
            EditorUtility.SetDirty(scenario);
        }
        var bodyMaterial = MechaScenarioSetup.Material("SupplyCrate", scenario.crateColor);
        var trimMaterial = MechaScenarioSetup.Material("CrateTrim", scenario.crateTrimColor);
        var lightMaterial = MechaScenarioSetup.Material("CrateLamp", scenario.crateLampColor);
        var props = MechaScenarioSetup.Child(root, "Destructible Supply Crates");
        for (int i = 0; i < scenario.boxFeet.Length; i++)
        {
            var go = MechaScenarioSetup.Child(props.transform, "Supply Crate " + (i + 1).ToString("00"));
            go.transform.position = scenario.boxFeet[i] + new Vector3(0f, scenario.boxSize.y * 0.5f, scenario.propDepth);
            go.layer = 10;
            MechaPresentationSetup.Ensure<FishNet.Object.NetworkObject>(go);
            MechaPresentationSetup.Ensure<BoxCollider>(go).size = scenario.boxSize;
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<DestructibleProp>(go), "settings", settings);
            MechaPresentationSetup.Assign(MechaPresentationSetup.Ensure<CharacterHitFeedback>(go), "settings", settings.hitFeedback);
            var body = MechaScenarioSetup.Primitive(go.transform, "Body", bodyMaterial);
            body.transform.localPosition = Vector3.zero; body.transform.localScale = scenario.boxSize;
            for (int side = -1; side <= 1; side += 2)
            {
                var trim = MechaScenarioSetup.Primitive(go.transform, "Reinforcement " + side, trimMaterial);
                trim.transform.localPosition = Vector3.Scale(scenario.trimOffset, new Vector3(side,1f,1f)); trim.transform.localScale = scenario.trimSize;
            }
            var lamp = MechaScenarioSetup.Primitive(go.transform, "Status Light", lightMaterial);
            lamp.transform.localPosition = scenario.lampOffset; lamp.transform.localScale = scenario.lampSize;
        }
        EditorUtility.SetDirty(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset")[0]);
        // New scene NetworkObjects are created in one editor call, bypassing FishNet's throttled automatic ID pass.
        var sceneIds = typeof(FishNet.Object.NetworkObject).GetMethod("CreateSceneId", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        var serialize = typeof(FishNet.Object.NetworkObject).GetMethod("ReserializeEditorSetValues", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (sceneIds == null || serialize == null) throw new System.InvalidOperationException("FishNet editor scene serialization API is unavailable.");
        sceneIds.Invoke(null, new object[] {root.gameObject.scene, false, 0});
        foreach (var prop in props.GetComponentsInChildren<FishNet.Object.NetworkObject>(true))
        {
            serialize.Invoke(prop, new object[] {true, true});
            EditorUtility.SetDirty(prop);
        }
    }
}
