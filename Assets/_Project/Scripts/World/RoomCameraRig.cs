using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(CinemachineCamera),typeof(CinemachinePositionComposer))]
public class RoomCameraRig : MonoBehaviour
{
    [SerializeField] private RoomCameraSettings settings;
    private CinemachineCamera virtualCamera;
    private CinemachinePositionComposer composer;
    private PlayerMovement player;
    private Camera output;
    private float nextSearch;
    private Transform trackingPoint;
    private float cameraHeight, landingStartHeight, landingTargetHeight, landingElapsed, stableHeight;
    private float airborneStartHeight, airborneStartFoot, airborneVelocity;
    private Collider stableSupport;
    private bool wasGrounded, followingAirborne;
    private bool needsInitialPose;
    private void Awake() { virtualCamera=GetComponent<CinemachineCamera>(); composer=GetComponent<CinemachinePositionComposer>(); output=Camera.main; Configure(); }
    public void Configure()
    {
        if (settings==null) return;
        virtualCamera=GetComponent<CinemachineCamera>(); composer=GetComponent<CinemachinePositionComposer>();
        virtualCamera.Lens.OrthographicSize=settings.orthographicSize;
        virtualCamera.Lens.ModeOverride=LensSettings.OverrideModes.Orthographic;
        composer.CameraDistance=settings.cameraDistance; composer.TargetOffset=Vector3.up*settings.targetHeight;
        composer.CenterOnActivate=false; composer.Lookahead.Enabled=false;
        composer.Composition.ScreenPosition=settings.screenPosition;
        composer.Composition.DeadZone=new ScreenComposerSettings.DeadZoneSettings {Enabled=true,Size=new Vector2(settings.deadZone.x,0f)};
        composer.Composition.HardLimits=new ScreenComposerSettings.HardLimitSettings {Enabled=true,Size=new Vector2(settings.hardLimits.x,1f)};
        composer.Damping=new Vector3(settings.centerDamping,0f,0f);
    }
    private void Update()
    {
        if (settings==null) return;
        if (player==null && Time.unscaledTime>=nextSearch)
        {
            nextSearch=Time.unscaledTime+settings.searchInterval;
            foreach(var candidate in FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None))
                if(candidate.HasControl) {Focus(candidate);return;}
        }
        if(player==null || output==null) return;
        if(needsInitialPose)
        {
            virtualCamera.ForceCameraPosition(settings.initialPosition,Quaternion.identity);
            needsInitialPose=false;
        }
        UpdateHeight();
        // Supply a virtual follow point at the gently followed/eased height.
        // Native horizontal composition and room confinement remain active during jumps.
        float trackedY=cameraHeight-settings.screenPosition.y*2f*virtualCamera.Lens.OrthographicSize-settings.targetHeight;
        trackingPoint.position=new Vector3(player.transform.position.x,trackedY,player.transform.position.z);
        float x=Mathf.Abs(output.WorldToViewportPoint(player.transform.position).x-0.5f)*2f;
        float pressure=Mathf.InverseLerp(settings.deadZone.x,settings.hardLimits.x,x);
        composer.Damping=new Vector3(Mathf.Lerp(settings.centerDamping,settings.edgeDamping,pressure*pressure),0f,0f);
    }
    private void UpdateHeight()
    {
        if (!player.IsGrounded)
        {
            if (!followingAirborne)
            {
                cameraHeight=airborneStartHeight=output.transform.position.y;
                airborneStartFoot=wasGrounded ? stableHeight : player.transform.position.y;
                airborneVelocity=0f; followingAirborne=true;
            }
            float offset=Mathf.Clamp((player.transform.position.y-airborneStartFoot)*settings.airborneFollowWeight,
                -settings.maximumAirborneOffset,settings.maximumAirborneOffset);
            float desired=Mathf.Clamp(airborneStartHeight+offset,settings.viewBounds.yMin+virtualCamera.Lens.OrthographicSize,
                settings.viewBounds.yMax-virtualCamera.Lens.OrthographicSize);
            cameraHeight=Mathf.SmoothDamp(cameraHeight,desired,ref airborneVelocity,settings.airborneSmoothTime,
                settings.airborneMaximumSpeed,Time.deltaTime);
            wasGrounded=false; stableSupport=null;
            return;
        }
        followingAirborne=false; airborneVelocity=0f;
        // The arena uses flat collider tops. Anchor to the support, avoiding probe/contact jitter.
        float foot=player.Support!=null ? player.Support.bounds.max.y : player.transform.position.y;
        if (!wasGrounded || stableSupport!=player.Support || Mathf.Abs(foot-stableHeight)>settings.landingHeightTolerance)
        {
            stableSupport=player.Support; stableHeight=foot; wasGrounded=true;
            landingStartHeight=output.transform.position.y;
            landingTargetHeight=settings.GroundedHeightAtSupport(foot);
            landingTargetHeight=Mathf.Clamp(landingTargetHeight,settings.viewBounds.yMin+settings.orthographicSize,settings.viewBounds.yMax-settings.orthographicSize);
            landingElapsed=0f;
        }
        landingElapsed+=Time.deltaTime;
        float progress=Mathf.Clamp01(landingElapsed/Mathf.Max(0.01f,settings.landingBlendDuration));
        float blend=settings.landingBlend==null ? progress : settings.landingBlend.Evaluate(progress);
        cameraHeight=progress>=1f ? landingTargetHeight : Mathf.Lerp(landingStartHeight,landingTargetHeight,blend);
    }
    public void Focus(PlayerMovement target)
    {
        player=target; cameraHeight=settings.initialPosition.y; wasGrounded=followingAirborne=false; stableSupport=null;
        GetComponent<RoomCameraZoom>()?.Focus(target);
        if(trackingPoint==null)trackingPoint=new GameObject("Room Camera Tracking Point"){hideFlags=HideFlags.DontSave}.transform;
        trackingPoint.position=target.transform.position;virtualCamera.Follow=trackingPoint;needsInitialPose=true;
    }
    private void OnDestroy() { if(trackingPoint!=null)Destroy(trackingPoint.gameObject); }
}
