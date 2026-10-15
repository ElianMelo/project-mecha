using Unity.Cinemachine;
using UnityEngine;

// Evaluate lens changes before the rig computes its tracking point and before the Brain.
[DefaultExecutionOrder(-60)]
[RequireComponent(typeof(CinemachineCamera))]
public class RoomCameraZoom : MonoBehaviour
{
    [SerializeField] private RoomCameraSettings framing;
    [SerializeField] private RoomCameraZoomSettings settings;
    private CinemachineCamera virtualCamera;
    private CinemachineConfiner2D confiner;
    private PlayerMovement player;
    private PlayerAttack attack;
    private float amount, velocity, returnTime;
    public float CurrentAmount => amount;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineCamera>();
        confiner = GetComponent<CinemachineConfiner2D>();
    }

    public void Focus(PlayerMovement target)
    {
        player = target;
        attack = target != null ? target.GetComponent<PlayerAttack>() : null;
    }

    private void Update()
    {
        if (settings == null || framing == null) return;
        bool ownsView = player != null && player.isActiveAndEnabled && player.HasControl;
        bool skill = ownsView && attack != null && attack.IsSkillActive;
        bool dash = ownsView && player.IsDashing;
        float target = 0f;
        float smoothTime = returnTime;
        if (skill)
        {
            target = settings.skillZoomOut;
            smoothTime = target > amount ? settings.skillEaseIn : settings.skillEaseOut;
            returnTime = settings.skillEaseOut;
        }
        else if (dash)
        {
            target = settings.dashZoomOut;
            smoothTime = target > amount ? settings.dashEaseIn : settings.dashEaseOut;
            returnTime = settings.dashEaseOut;
        }
        // Absolute offsets blend across cancels/chains instead of accumulating zoom.
        amount = Mathf.SmoothDamp(amount, target, ref velocity, Mathf.Max(0.01f, smoothTime));
        SetSize(framing.orthographicSize * (1f + amount));
    }

    private void SetSize(float size)
    {
        if (virtualCamera == null || virtualCamera.Lens.OrthographicSize == size) return;
        virtualCamera.Lens.OrthographicSize = size;
        confiner?.InvalidateLensCache();
    }

    private void OnDisable()
    {
        amount = velocity = 0f;
        if (framing != null) SetSize(framing.orthographicSize);
    }
}
