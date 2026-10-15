using UnityEngine;

[CreateAssetMenu(menuName = "Mecha/Player/Movement")]
public class PlayerMovementSettings : ScriptableObject
{
    public float walkSpeed = 4.5f, runSpeed = 7f, jumpSpeed = 10f;
    public float dashSpeed = 11.5f, dashDuration = 0.18f, dashCooldown = 0.12f, dashBuffer = 0.14f;
    public float dashNetworkGrace = 0.08f;
    public float risingGravity = 24f, fallingGravity = 40f, apexGravity = 14f, terminalVelocity = 20f;
    public float coyoteTime = 0.1f, jumpBuffer = 0.12f, apexSpeed = 1.5f;
    public float groundAcceleration = 90f, airAcceleration = 45f, airAttackAcceleration = 20f;
    public float rocketMomentum = 0.28f, dropSpeed = 4f, fastFallSpeed = 6f;
    public float groundProbe = 0.15f, groundProbeInset = 0.08f;
    public LayerMask groundLayers = (1 << 6) | (1 << 7);
    public float capsuleHeight = 1.75f, capsuleRadius = 0.27f;
    public Vector3 capsuleCenter = new Vector3(0f,0.875f,0f);
}
