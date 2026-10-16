using System;
using System.Reflection;
using FishNet.Component.Animating;
using FishNet.Component.Transforming;
using FishNet.Managing;
using FishNet.Managing.Timing;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Object;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Applies only synchronization settings; does not rebuild gameplay, art, UI or encounters.
public static class MechaNetworkSynchronizationSetup
{
    [MenuItem("Tools/Mecha/Apply Network Synchronization")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var profile = MechaPresentationSetup.Create<NetworkSimulationSettings>("NetworkSimulation");
        var network = UnityEngine.Object.FindFirstObjectByType<NetworkManager>();
        if (network == null) throw new InvalidOperationException("Open MainScene before applying network settings.");
        var timing = MechaPresentationSetup.Ensure<TimeManager>(network.gameObject);
        var timingData = new SerializedObject(timing);
        timingData.FindProperty("_tickRate").intValue = profile.tickRate;
        // Gameplay uses FixedUpdate. Keep Unity physics in charge of simulation.
        timingData.FindProperty("_physicsMode").enumValueIndex = (int)PhysicsMode.Unity;
        timingData.ApplyModifiedPropertiesWithoutUndo();
        var managerData = new SerializedObject(network);
        managerData.FindProperty("_runInBackground").boolValue = true;
        managerData.ApplyModifiedPropertiesWithoutUndo();
        ConfigureFrameRate(MechaPresentationSetup.Ensure<ClientManager>(network.gameObject), profile.maximumFrameRate);
        ConfigureFrameRate(MechaPresentationSetup.Ensure<ServerManager>(network.gameObject), profile.maximumFrameRate);
        PlayerSettings.runInBackground = true;

        ConfigurePrefab("MechaPlayer", true, profile);
        ConfigurePrefab("EvaEnemy", false, profile);
        ConfigurePrefab("LanceEnemy", false, profile);
        ConfigurePrefab("EnemyLance", false, profile, true);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssetIfDirty(profile);
        EditorSceneManager.MarkSceneDirty(network.gameObject.scene);
        EditorSceneManager.SaveScene(network.gameObject.scene);
    }

    private static void ConfigureFrameRate(UnityEngine.Object manager, ushort frameRate)
    {
        var data = new SerializedObject(manager);
        data.FindProperty("_changeFrameRate").boolValue = true;
        data.FindProperty("_frameRate").intValue = frameRate;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurePrefab(string name, bool owner, NetworkSimulationSettings profile, bool projectile = false)
    {
        string path = $"Assets/_Project/Prefabs/{name}.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ConfigureTransform(root.GetComponent<NetworkTransform>(), profile, owner, projectile);
            var input = root.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (input != null) input.enabled = false;
            if (!projectile)
            {
                var animator = MechaPresentationSetup.Ensure<NetworkAnimator>(root);
                var data = new SerializedObject(animator);
                data.FindProperty("_animator").objectReferenceValue = root.GetComponentInChildren<Animator>(true);
                data.FindProperty("_clientAuthoritative").boolValue = owner;
                data.FindProperty("_sendToOwner").boolValue = !owner;
                data.FindProperty("_interpolation").intValue = profile.interpolationTicks;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            // Rebuild FishNet's behaviour indices after adding NetworkAnimator.
            var serialize = typeof(NetworkObject).GetMethod("ReserializeEditorSetValues", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var obj in root.GetComponentsInChildren<NetworkObject>(true))
                serialize.Invoke(obj, new object[] { true, true });
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static void ConfigureTransform(NetworkTransform transform, NetworkSimulationSettings profile, bool owner, bool projectile = false)
    {
        if (transform == null) throw new InvalidOperationException("NetworkTransform is required.");
        var data = new SerializedObject(transform);
        // Projectiles intentionally use kinematic MovePosition on the server.
        data.FindProperty("_componentConfiguration").enumValueIndex = (int)(projectile
            ? NetworkTransform.ComponentConfigurationType.Disabled : NetworkTransform.ComponentConfigurationType.Rigidbody);
        data.FindProperty("_clientAuthoritative").boolValue = owner;
        data.FindProperty("_sendToOwner").boolValue = true;
        data.FindProperty("_interpolation").intValue = profile.interpolationTicks;
        data.FindProperty("_interval").intValue = profile.sendInterval;
        data.FindProperty("_positionSensitivity").floatValue = profile.positionSensitivity;
        data.FindProperty("_synchronizeScale").boolValue = false;
        data.FindProperty("_rotationSnapping.Y").boolValue = !projectile;
        data.FindProperty("_enableTeleport").boolValue = profile.automaticDistanceTeleport;
        // This is a Pro feature. Leave it disabled instead of implementing a replacement.
        data.FindProperty("_extrapolation").intValue = 0;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
