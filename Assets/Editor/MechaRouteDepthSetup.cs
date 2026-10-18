using System;
using System.Linq;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Focused upgrade of the existing routing scene, preserving its composition and localized terminal.
public static class MechaRouteDepthSetup
{
    [MenuItem("Tools/Mecha/Update Routing Depth and Scrolling")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before authoring routing.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty)) throw new InvalidOperationException("Save open scenes first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var settings = MechaPresentationSetup.Profile<LevelRouteSettings>("LevelRouting");
            var scene = SceneManager.GetSceneByPath(settings.routingScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(settings.routingScenePath, OpenSceneMode.Additive);
            var board = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<RouteMapBoard>(true)).Single();
            var table = board.View.transform.root;
            var terminal = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).Single(t => t.name == "Encounter Briefing Terminal");
            Configure(board, table, terminal, settings);
            var content = board.transform.Find("Encounter Nodes and Paths");
            foreach (var child in content.Cast<Transform>().Where(t => t.name.StartsWith("Editor Preview /", StringComparison.Ordinal)).ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            var catalogue = Resources.LoadAll<RouteEncounterDefinition>("RouteEncounters").OrderBy(x => x.encounterId).ToArray();
            foreach (var node in LevelRouteGraph.Generate(settings, catalogue, 104729, true))
            {
                var room = catalogue.Single(x => x.encounterId == node.encounterId);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(room.mapModel, scene);
                model.transform.SetParent(content, false); model.name = "Editor Preview / " + room.encounterId;
                model.transform.localPosition = RouteMapBoard.MapPosition(settings, node);
                foreach (var child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = settings.boardLayer;
            }
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
    public static void Configure(RouteMapBoard board, Transform table, Transform terminal, LevelRouteSettings settings)
    {
        var view = board.View;
        var pose = view.transform.position; var rotation = view.transform.rotation; var lens = LensSettings.FromCamera(view);
        var target = board.transform.Find("Routing Camera Target");
        if (target == null) { target = new GameObject("Routing Camera Target").transform; target.SetParent(board.transform, false); }
        var rigTransform = board.transform.Find("Routing Camera Rig");
        if (rigTransform == null) { rigTransform = new GameObject("Routing Camera Rig").transform; rigTransform.SetParent(board.transform, false); }
        rigTransform.gameObject.SetActive(false); rigTransform.SetPositionAndRotation(pose, rotation);
        var rig = MechaPresentationSetup.Ensure<CinemachineCamera>(rigTransform.gameObject);
        rig.Lens = lens; rig.Follow = target; rig.OutputChannel = OutputChannels.Channel02;
        var follow = MechaPresentationSetup.Ensure<CinemachineFollow>(rigTransform.gameObject);
        follow.FollowOffset = pose - target.position;
        var tracker = follow.TrackerSettings; tracker.BindingMode = BindingMode.WorldSpace; tracker.PositionDamping = Vector3.zero; follow.TrackerSettings = tracker;
        var brain = MechaPresentationSetup.Ensure<CinemachineBrain>(view.gameObject);
        brain.ChannelMask = OutputChannels.Channel02; brain.IgnoreTimeScale = true;
        brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        var depth = MechaPresentationSetup.Ensure<RouteMapCamera>(board.gameObject);
        Assign(depth, "view", view); Assign(depth, "rig", rig); Assign(depth, "follow", follow); Assign(depth, "trackingTarget", target); Assign(depth, "boardSpace", board.transform);
        Assign(board, "depthCamera", depth); Assign(board, "tableSurface", table.Find("Mission Table"));
        var fields = new SerializedObject(board);
        SetArray(fields, "tableRails", table.Cast<Transform>().Where(t => t.name == "Rail").ToArray());
        SetArray(fields, "depthDividers", table.Cast<Transform>().Where(t => t.name == "Depth Divider").ToArray());
        fields.ApplyModifiedPropertiesWithoutUndo();
        board.ConfigureTable(settings, settings.stepCount);
        fields.Update(); SetArray(fields, "depthDividers", table.Cast<Transform>().Where(t => t.name == "Depth Divider").ToArray()); fields.ApplyModifiedPropertiesWithoutUndo();
        // The 3D terminal keeps exactly the same screen placement during the depth pan.
        terminal.SetParent(view.transform, true); rigTransform.gameObject.SetActive(true);
        foreach (var child in new[] { target, rigTransform }) child.gameObject.layer = settings.boardLayer;
        EditorUtility.SetDirty(board); EditorUtility.SetDirty(depth); EditorUtility.SetDirty(brain); EditorUtility.SetDirty(rig); EditorUtility.SetDirty(follow);
    }
    private static void SetArray(SerializedObject target, string name, Transform[] values)
    {
        var array = target.FindProperty(name); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
    private static void Assign(Component target, string name, UnityEngine.Object value) => MechaPresentationSetup.Assign(target, name, value);
}
