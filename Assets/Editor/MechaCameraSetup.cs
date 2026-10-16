using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

public static class MechaCameraSetup
{
    public static void Apply()
    {
        var settings=MechaPresentationSetup.Profile<RoomCameraSettings>("RoomCamera");
        var output=Camera.main;
        if(output==null) throw new System.InvalidOperationException("Main camera missing.");
        MechaRenderSetup.Apply(output);
        output.orthographic=true; output.orthographicSize=settings.orthographicSize;
        output.transform.SetPositionAndRotation(settings.initialPosition,Quaternion.identity);
        var brain=MechaPresentationSetup.Ensure<CinemachineBrain>(output.gameObject);
        brain.UpdateMethod=CinemachineBrain.UpdateMethods.LateUpdate;
        brain.DefaultBlend=new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut,0f);
        var go=GameObject.Find("Room Camera") ?? new GameObject("Room Camera");
        go.transform.SetPositionAndRotation(settings.initialPosition,Quaternion.identity);
        var virtualCamera=MechaPresentationSetup.Ensure<CinemachineCamera>(go);
        MechaPresentationSetup.Ensure<CinemachinePositionComposer>(go);
        var rig=MechaPresentationSetup.Ensure<RoomCameraRig>(go);
        MechaPresentationSetup.Assign(rig,"settings",settings); rig.Configure();
        var zoom=MechaPresentationSetup.Ensure<RoomCameraZoom>(go);
        MechaPresentationSetup.Assign(zoom,"framing",settings);
        MechaPresentationSetup.Assign(zoom,"settings",MechaPresentationSetup.Create<RoomCameraZoomSettings>("RoomCameraZoom"));
        var boundsGo=GameObject.Find("Camera Room Bounds") ?? new GameObject("Camera Room Bounds");
        var bounds=MechaPresentationSetup.Ensure<BoxCollider2D>(boundsGo);
        boundsGo.transform.position=Vector3.zero; bounds.size=settings.viewBounds.size; bounds.offset=settings.viewBounds.center; bounds.isTrigger=true;
        var confiner=MechaPresentationSetup.Ensure<CinemachineConfiner2D>(go);
        confiner.BoundingShape2D=bounds; confiner.Damping=0f; confiner.SlowingDistance=0f; confiner.InvalidateBoundingShapeCache();
        confiner.OversizeWindow = new CinemachineConfiner2D.OversizeWindowSettings {Enabled = true, MaxWindowSize = settings.orthographicSize * 1.2f, Padding = 0.05f};
        foreach(string side in new[]{"Left","Right"})
        {
            var wall=GameObject.Find(side) ?? new GameObject(side);
            var box=MechaPresentationSetup.Ensure<BoxCollider>(wall);
            wall.transform.SetPositionAndRotation(new Vector3(side=="Left"?settings.physicalWalls.x-settings.wallThickness*0.5f:settings.physicalWalls.y+settings.wallThickness*0.5f,2f,0f),Quaternion.identity);
            wall.transform.localScale=Vector3.one;box.size=new Vector3(settings.wallThickness,settings.wallHeight,settings.wallDepth);box.isTrigger=false;
            foreach (var r in wall.GetComponentsInChildren<Renderer>(true)) r.enabled=false;
            wall.layer=0;
        }
        EditorUtility.SetDirty(output);EditorUtility.SetDirty(virtualCamera);
    }
}
